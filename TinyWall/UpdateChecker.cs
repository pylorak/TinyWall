using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Samples.TaskDialog;
using pylorak.Windows;
using pylorak.Utilities;

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
            var oldVersion = new Version(Application.ProductVersion);
            var newVersion = new Version(UpdateModule?.ComponentVersion ?? Application.ProductVersion);

            bool win10v1903 = VersionInfo.Win10OrNewer && (Environment.OSVersion.Version.Build >= 18362);
            bool WindowsNew_AnyTwUpdate = win10v1903 && (newVersion > oldVersion);
            bool WindowsOld_TwMinorFixOnly = (newVersion > oldVersion) && (newVersion.Major == oldVersion.Major) && (newVersion.Minor == oldVersion.Minor);

            if (WindowsNew_AnyTwUpdate || WindowsOld_TwMinorFixOnly)
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

            var tmpFile = Path.GetTempFileName() + ".msi";
            var UpdateURL = new Uri(mainModule.UpdateURL);
            using var downloader = new WebClient();
            downloader.DownloadFileCompleted += new AsyncCompletedEventHandler(Updater_DownloadFinished);
            downloader.DownloadProgressChanged += new DownloadProgressChangedEventHandler(Updater_DownloadProgressChanged);
            downloader.DownloadFileAsync(UpdateURL, tmpFile, tmpFile);

            switch (TDialog.Show())
            {
                case (int)DialogResult.Cancel:
                    downloader.CancelAsync();
                    break;
                case (int)DialogResult.OK:
                    {
                        // Checking against expected hash in update descriptor is useless for security.
                        // If an attacker can control the executable download, then he can also control
                        // the descriptor download, hence the hash in the descriptor is not trustworthy.
                        // We increase security instead by verifying the authenticode signature.
                        var signatureCheck = WinTrust.VerifyFileAuthenticode(tmpFile);
                        if (signatureCheck == WinTrust.VerifyResult.SIGNATURE_VALID)
                            InstallUpdate(tmpFile);
                        else
                            Utils.ShowMessageBox(Resources.Messages.UpdateInstallError, Resources.Messages.TinyWall, TaskDialogCommonButtons.Ok, TaskDialogIcon.Error);
                        break;
                    }
                case (int)DialogResult.Abort:
                    Utils.ShowMessageBox(ErrorMsg, Resources.Messages.TinyWall, TaskDialogCommonButtons.Ok, TaskDialogIcon.Error);
                    break;
            }
        }

        private static void InstallUpdate(string localFilePath)
        {
            Utils.StartProcess(localFilePath, string.Empty, false, false);
        }

        private void Updater_DownloadFinished(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled || (e.Error != null))
            {
                ErrorMsg = Resources.Messages.DownloadInterrupted;
                return;
            }

            State = UpdaterState.UpdateDownloadReady;
        }

        private void Updater_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadProgress = e.ProgressPercentage;
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
