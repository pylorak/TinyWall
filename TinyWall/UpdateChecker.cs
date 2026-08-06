using Microsoft.Samples.TaskDialog;
using pylorak.Utilities;
using pylorak.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace pylorak.TinyWall
{

    internal class Updater
    {
        private enum UpdaterState
        {
            GettingDescriptor,
            DescriptorReady,
            DownloadingUpdate,
            UpdateDownloadReady
        }

        private UpdaterState State;
        private string ErrorMsg = string.Empty;
        private volatile int DownloadProgress;

        internal static void StartUpdate()
        {
            if (!Utils.RunningAsAdmin())
                throw new InsufficientPrivilegesException("Administrative privileges required.");

            var updater = new Updater();
            var descriptor = new UpdateDescriptor();
            updater.State = UpdaterState.GettingDescriptor;

            var TDialog = new TaskDialog
            {
                CustomMainIcon = Resources.Icons.firewall,
                WindowTitle = Resources.Messages.TinyWall,
                MainInstruction = Resources.Messages.TinyWallUpdater,
                Content = Resources.Messages.PleaseWaitWhileTinyWallChecksForUpdates,
                AllowDialogCancellation = false,
                CommonButtons = TaskDialogCommonButtons.Cancel,
                ShowMarqueeProgressBar = true,
                Callback = updater.DownloadTickCallback,
                CallbackData = updater,
                CallbackTimer = true
            };

            var UpdateThread = new Thread(() =>
            {
                try
                {
                    descriptor = UpdateChecker.GetDescriptor();
                    updater.State = UpdaterState.DescriptorReady;
                }
                catch
                {
                    updater.ErrorMsg = Resources.Messages.ErrorCheckingForUpdates;
                }
            });
            UpdateThread.Start();

            switch (TDialog.Show())
            {
                case (int)DialogResult.Cancel:
                    UpdateThread.Interrupt();
                    if (!UpdateThread.Join(500))
                        UpdateThread.Abort();
                    break;
                case (int)DialogResult.OK:
                    updater.CheckAppVersion(descriptor);
                    break;
                case (int)DialogResult.Abort:
                    Utils.ShowMessageBox(updater.ErrorMsg, Resources.Messages.TinyWall, TaskDialogCommonButtons.Ok, TaskDialogIcon.Error);
                    break;
            }
        }

        private void CheckAppVersion(UpdateDescriptor descriptor)
        {
            var UpdateModule = descriptor.GetModule(UpdateDescriptor.MODULE_NAME_MAINBIN);
            if (UpdateModule is not null)
            {
                var oldVersion = new Version(Application.ProductVersion);
                var newVersion = new Version(UpdateModule.ComponentVersion ?? Application.ProductVersion);

                bool WindowsNew_AnyTwUpdate = VersionInfo.Win10v1903_OrNewer && (newVersion > oldVersion);
                bool WindowsOld_TwMinorFixOnly = (newVersion > oldVersion) && (newVersion.Major == oldVersion.Major) && (newVersion.Minor == oldVersion.Minor);

                if (!WindowsNew_AnyTwUpdate && !WindowsOld_TwMinorFixOnly)
                    UpdateModule = null;
            }

            if (UpdateModule is not null)
            {
                string prompt = string.Format(CultureInfo.CurrentCulture, Resources.Messages.UpdateAvailable, UpdateModule.ComponentVersion);
                if (Utils.ShowMessageBox(prompt, Resources.Messages.TinyWallUpdater, TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No, TaskDialogIcon.Warning) == DialogResult.Yes)
                    DownloadUpdate(UpdateModule);
            }
            else
            {
                string prompt = Resources.Messages.NoUpdateAvailable;
                Utils.ShowMessageBox(prompt, Resources.Messages.TinyWallUpdater, TaskDialogCommonButtons.Ok, TaskDialogIcon.Information);
            }
        }

        private void DownloadUpdate(UpdateModule mainModule)
        {
            ErrorMsg = string.Empty;
            var TDialog = new TaskDialog
            {
                CustomMainIcon = Resources.Icons.firewall,
                WindowTitle = Resources.Messages.TinyWall,
                MainInstruction = Resources.Messages.TinyWallUpdater,
                Content = Resources.Messages.DownloadingUpdate,
                AllowDialogCancellation = false,
                CommonButtons = TaskDialogCommonButtons.Cancel,
                ShowProgressBar = true,
                Callback = DownloadTickCallback,
                CallbackData = this,
                CallbackTimer = true,
                EnableHyperlinks = true
            };

            State = UpdaterState.DownloadingUpdate;
            byte[]? downloadData = null;

            var UpdateURL = new Uri(mainModule.UpdateURL);
            using var downloader = new WebClient();
            downloader.DownloadDataCompleted += (sender, e) =>
            {
                if (e.Cancelled || (e.Error != null))
                {
                    ErrorMsg = Resources.Messages.DownloadInterrupted;
                    return;
                }

                downloadData = e.Result;
                State = UpdaterState.UpdateDownloadReady;
            };
            downloader.DownloadProgressChanged += (sender, e) =>
            {
                DownloadProgress = e.ProgressPercentage;
            };
            downloader.DownloadDataAsync(UpdateURL);

            switch (TDialog.Show())
            {
                case (int)DialogResult.Cancel:
                    downloader.CancelAsync();
                    break;
                case (int)DialogResult.OK:
                    {
                        if ((downloadData is null) || (downloadData.Length == 0))
                        {
                            Utils.ShowMessageBox(Resources.Messages.UpdateInstallError, Resources.Messages.TinyWall, TaskDialogCommonButtons.Ok, TaskDialogIcon.Error);
                            return;
                        }

                        var tmpFilePath = Path.Combine(SecureTemp.FolderPath, Utils.RandomString(12) + ".msi");
                        SecureTemp.EnsureExistence(Path.GetDirectoryName(tmpFilePath));
                        using (var tmpFileStream = SecureTemp.CreateSecureFileStream(tmpFilePath, FileMode.CreateNew, FileSystemRights.Write, FileShare.None))
                        {
                            tmpFileStream.Write(downloadData, 0, downloadData.Length);
                        }

                        // The handle to the MSI file is now closed, but there is no TOCTOU-vulnerability between
                        // writing/checking the file and its execution, because it is only accessible to admins.

                        // Checking against expected hash in update descriptor is useless for security.
                        // If an attacker can control the executable download, then he can also control
                        // the descriptor download, hence the hash in the descriptor is not trustworthy.
                        // We increase security instead by performing an authenticode check.
                        var signatureCheck = WinTrust.VerifyFileAuthenticode(tmpFilePath);
                        if (signatureCheck == WinTrust.VerifyResult.SIGNATURE_VALID)
                            Utils.StartProcessAndForget(tmpFilePath, string.Empty, false, false);
                        else
                            Utils.ShowMessageBox(Resources.Messages.UpdateInstallError, Resources.Messages.TinyWall, TaskDialogCommonButtons.Ok, TaskDialogIcon.Error);
                        break;
                    }
                case (int)DialogResult.Abort:
                    Utils.ShowMessageBox(ErrorMsg, Resources.Messages.TinyWall, TaskDialogCommonButtons.Ok, TaskDialogIcon.Error);
                    break;
            }
        }

        private bool DownloadTickCallback(ActiveTaskDialog taskDialog, TaskDialogNotificationArgs args, object? callbackData)
        {
            switch (args.Notification)
            {
                case TaskDialogNotification.Created:
                    if (State == UpdaterState.GettingDescriptor)
                        taskDialog.SetProgressBarMarquee(true, 25);
                    break;
                case TaskDialogNotification.Timer:
                    if (!string.IsNullOrEmpty(ErrorMsg))
                        taskDialog.ClickButton((int)DialogResult.Abort);
                    switch (State)
                    {
                        case UpdaterState.DescriptorReady:
                        case UpdaterState.UpdateDownloadReady:
                            taskDialog.ClickButton((int)DialogResult.OK);
                            break;
                        case UpdaterState.DownloadingUpdate:
                            taskDialog.SetProgressBarPosition(DownloadProgress);
                            break;
                    }
                    break;
            }
            return false;
        }
    }

    internal static class UpdateChecker
    {
        private const string UPDATER_VERSION = "7";
        private const string URL_UPDATE_DESCRIPTOR = $"https://tinywall.pados.hu/updates/UpdVer{UPDATER_VERSION}/update.json";

        internal static UpdateDescriptor GetDescriptor()
        {
            // Download descriptor
            using var downloader = new WebClient();
            downloader.Headers.Add("TW-Version", Application.ProductVersion);
            var descriptorBytes = downloader.DownloadData(URL_UPDATE_DESCRIPTOR);

            // Deserialize descriptor
            var descriptor = SerializationHelper.Deserialize(descriptorBytes, new UpdateDescriptor());
            if (descriptor.MagicWord != "TinyWall Update Descriptor")
                throw new ApplicationException("Bad update descriptor file.");

            return descriptor;
        }
    }
}
