using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Samples;
using pylorak.Windows;

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
            var updater = new Updater();
            var descriptor = new UpdateDescriptor();
            updater.State = UpdaterState.GettingDescriptor;

            var TDialog = new TaskDialog();
            TDialog.CustomMainIcon = Resources.Icons.firewall;
            TDialog.WindowTitle = Resources.Messages.TinyWall;
            TDialog.MainInstruction = Resources.Messages.TinyWallUpdater;
            TDialog.Content = Resources.Messages.PleaseWaitWhileTinyWallChecksForUpdates;
            TDialog.AllowDialogCancellation = false;
            TDialog.CommonButtons = TaskDialogCommonButtons.Cancel;
            TDialog.ShowMarqueeProgressBar = true;
            TDialog.Callback = updater.DownloadTickCallback;
            TDialog.CallbackData = updater;
            TDialog.CallbackTimer = true;

            var UpdateThread = new Thread( () =>
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
                    updater.CheckVersion(descriptor);
                    break;
                case (int)DialogResult.Abort:
                    Utils.ShowMessageBox(updater.ErrorMsg, Resources.Messages.TinyWall, TaskDialogCommonButtons.Ok, TaskDialogIcon.Error);
                    break;
            }
        }

        private void CheckVersion(UpdateDescriptor descriptor)
        {
            var UpdateModule = UpdateChecker.GetMainAppModule(descriptor)!;
            var oldVersion = new Version(System.Windows.Forms.Application.ProductVersion);
            var newVersion = new Version(UpdateModule.ComponentVersion);

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
            var TDialog = new TaskDialog();
            TDialog.CustomMainIcon = Resources.Icons.firewall;
            TDialog.WindowTitle = Resources.Messages.TinyWall;
            TDialog.MainInstruction = Resources.Messages.TinyWallUpdater;
            TDialog.Content = Resources.Messages.DownloadingUpdate;
            TDialog.AllowDialogCancellation = false;
            TDialog.CommonButtons = TaskDialogCommonButtons.Cancel;
            TDialog.ShowProgressBar = true;
            TDialog.Callback = DownloadTickCallback;
            TDialog.CallbackData = this;
            TDialog.CallbackTimer = true;
            TDialog.EnableHyperlinks = true;

            State = UpdaterState.DownloadingUpdate;

            var tmpFile = Path.GetTempFileName() + ".msi";
            var UpdateURL = new Uri(mainModule.UpdateURL);
            using var HTTPClient = new WebClient();
            HTTPClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Updater_DownloadFinished);
            HTTPClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(Updater_DownloadProgressChanged);
            HTTPClient.DownloadFileAsync(UpdateURL, tmpFile, tmpFile);

            switch (TDialog.Show())
            {
                case (int)DialogResult.Cancel:
                    HTTPClient.CancelAsync();
                    break;
                case (int)DialogResult.OK:
                    InstallUpdate(tmpFile);
                    break;
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
        private const int UPDATER_VERSION = 5;
        private const string URL_UPDATE_DESCRIPTOR = @"https://tinywall.pados.hu/updates/UpdVer{0}/update.xml";

        internal static UpdateDescriptor GetDescriptor()
        {
            var url = string.Format(CultureInfo.InvariantCulture, URL_UPDATE_DESCRIPTOR, UPDATER_VERSION);
            var tmpFile = Path.GetTempFileName();

            try
            {
                using (var HTTPClient = new WebClient())
                {
                    HTTPClient.Headers.Add("TW-Version", Application.ProductVersion);
                    HTTPClient.DownloadFile(url, tmpFile);
                }

                var descriptor = SerializationHelper.LoadFromXMLFile<UpdateDescriptor>(tmpFile);
                if (descriptor.MagicWord != "TinyWall Update Descriptor")
                    throw new ApplicationException("Bad update descriptor file.");

                return descriptor;
            }
            finally
            {
                File.Delete(tmpFile);
            }
        }

        internal static UpdateModule? GetUpdateModule(UpdateDescriptor descriptor, string moduleName)
        {
            for (int i = 0; i < descriptor.Modules.Length; ++i)
            {
                if (descriptor.Modules[i].Component.Equals(moduleName, StringComparison.InvariantCultureIgnoreCase))
                    return descriptor.Modules[i];
            }

            return null;
        }

        internal static UpdateModule? GetMainAppModule(UpdateDescriptor descriptor)
        {
            return GetUpdateModule(descriptor, "TinyWall");
        }
        internal static UpdateModule? GetHostsFileModule(UpdateDescriptor descriptor)
        {
            return GetUpdateModule(descriptor, "HostsFile");
        }
        internal static UpdateModule? GetDatabaseFileModule(UpdateDescriptor descriptor)
        {
            return GetUpdateModule(descriptor, "Database");
        }
    }
}
