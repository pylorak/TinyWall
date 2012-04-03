using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Samples;

namespace PKSoft
{
    [Serializable]
    public class UpdateModule
    {
        public string Component;
        public string ComponentVersion;
        public string DownloadHash;
        public string UpdateURL;
    }

    [Serializable]
    public class UpdateDescriptor
    {
        public string MagicWord = "TinyWall Update Descriptor";
        public UpdateModule[] Modules;
    }

    internal class Updater
    {
        private enum UpdaterState
        {
            GettingDescriptor,
            DescriptorReady,
            DownloadingUpdate,
            UpdateDownloadReady
        }

        private Control ParentControl;
        private TaskDialog TDialog;
        private UpdaterState State;
        private UpdateDescriptor Descriptor;
        private string ErrorMsg;
        private volatile int DownloadProgress;

        internal static void StartUpdate(Control parent)
        {
            Updater updater = new Updater();
            updater.ParentControl = parent;
            updater.State = UpdaterState.GettingDescriptor;

            updater.TDialog = new TaskDialog();
            updater.TDialog.CustomMainIcon = PKSoft.Resources.Icons.firewall;
            updater.TDialog.WindowTitle = PKSoft.Resources.Messages.TinyWall;
            updater.TDialog.MainInstruction = PKSoft.Resources.Messages.TinyWallUpdater;
            updater.TDialog.Content = PKSoft.Resources.Messages.PleaseWaitWhileTinyWallChecksForUpdates;
            updater.TDialog.AllowDialogCancellation = false;
            updater.TDialog.CommonButtons = TaskDialogCommonButtons.Cancel;
            updater.TDialog.ShowMarqueeProgressBar = true;
            updater.TDialog.Callback = updater.DownloadTickCallback;
            updater.TDialog.CallbackData = updater;
            updater.TDialog.CallbackTimer = true;

            Thread UpdateThread = new Thread((ThreadStart)delegate()
                {
                    try
                    {
                        updater.Descriptor = UpdateChecker.GetDescriptor();
                        updater.State = UpdaterState.DescriptorReady;
                    }
                    catch
                    {
                        updater.ErrorMsg = PKSoft.Resources.Messages.ErrorCheckingForUpdates;
                    }
                });
            UpdateThread.Start();

            switch (updater.TDialog.Show(parent))
            {
                case (int)DialogResult.Cancel:
                    UpdateThread.Interrupt();
                    if (!UpdateThread.Join(500))
                        UpdateThread.Abort();
                    break;
                case (int)DialogResult.OK:
                    updater.CheckVersion();
                    break;
                case (int)DialogResult.Abort:
                    Utils.ShowMessageBox(parent, updater.ErrorMsg, PKSoft.Resources.Messages.TinyWall, TaskDialogCommonButtons.Ok, TaskDialogIcon.Error);
                    break;
            }
        }

        private void CheckVersion()
        {
            UpdateModule UpdateModule = UpdateChecker.GetMainAppModule(this.Descriptor);
            Version oldVersion = new Version(System.Windows.Forms.Application.ProductVersion);
            Version newVersion = new Version(UpdateModule.ComponentVersion);
            if (newVersion > oldVersion)
            {
                string prompt = string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.UpdateAvailable, UpdateModule.ComponentVersion);
                if (Utils.ShowMessageBox(ParentControl, prompt, PKSoft.Resources.Messages.TinyWallUpdater, TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No, TaskDialogIcon.Warning) == DialogResult.Yes)
                    DownloadUpdate(UpdateModule);
            }
            else
            {
                string prompt = PKSoft.Resources.Messages.NoUpdateAvailable;
                Utils.ShowMessageBox(ParentControl, prompt, PKSoft.Resources.Messages.TinyWallUpdater, TaskDialogCommonButtons.Ok, TaskDialogIcon.Information);
            }
        }

        private void DownloadUpdate(UpdateModule mainModule)
        {
            ErrorMsg = null;
            Updater updater = this;
            updater.TDialog = new TaskDialog();
            updater.TDialog.CustomMainIcon = PKSoft.Resources.Icons.firewall;
            updater.TDialog.WindowTitle = PKSoft.Resources.Messages.TinyWall;
            updater.TDialog.MainInstruction = PKSoft.Resources.Messages.TinyWallUpdater;
            updater.TDialog.Content = PKSoft.Resources.Messages.DownloadingUpdate;
            updater.TDialog.AllowDialogCancellation = false;
            updater.TDialog.CommonButtons = TaskDialogCommonButtons.Cancel;
            updater.TDialog.ShowProgressBar = true;
            updater.TDialog.Callback = updater.DownloadTickCallback;
            updater.TDialog.CallbackData = updater;
            updater.TDialog.CallbackTimer = true;
            updater.TDialog.EnableHyperlinks = true;

            State = UpdaterState.DownloadingUpdate;

            string tmpFile = Path.GetTempFileName() + ".msi";
            Uri UpdateURL = new Uri(mainModule.UpdateURL);
            WebClient HTTPClient = new WebClient();
            HTTPClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Updater_DownloadFinished);
            HTTPClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(Updater_DownloadProgressChanged);
            HTTPClient.DownloadFileAsync(UpdateURL, tmpFile, tmpFile);

            switch (updater.TDialog.Show(updater.ParentControl))
            {
                case (int)DialogResult.Cancel:
                    HTTPClient.CancelAsync();
                    break;
                case (int)DialogResult.OK:
                    InstallUpdate(tmpFile);
                    break;
                case (int)DialogResult.Abort:
                    Utils.ShowMessageBox(updater.ParentControl, updater.ErrorMsg, PKSoft.Resources.Messages.TinyWall, TaskDialogCommonButtons.Ok, TaskDialogIcon.Error);
                    break;
            }
        }

        private static void InstallUpdate(string localFilePath)
        {
            Utils.StartProcess(localFilePath, null, false, false);
        }

        private void Updater_DownloadFinished(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled || (e.Error != null))
            {
                ErrorMsg = PKSoft.Resources.Messages.DownloadInterrupted;
                return;
            }

            State = UpdaterState.UpdateDownloadReady;
        }

        private void Updater_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadProgress = e.ProgressPercentage;
        }

        private bool DownloadTickCallback(ActiveTaskDialog taskDialog, TaskDialogNotificationArgs args, object callbackData)
        {
            Updater updater = callbackData as Updater;

            switch (args.Notification)
            {
                case TaskDialogNotification.Created:
                    if (updater.State == UpdaterState.GettingDescriptor)
                        taskDialog.SetProgressBarMarquee(true, 25);
                    break;
                case TaskDialogNotification.Timer:
                    if (!string.IsNullOrEmpty(updater.ErrorMsg))
                        taskDialog.ClickButton((int)DialogResult.Abort);
                    switch (updater.State)
                    {
                        case UpdaterState.DescriptorReady:
                        case UpdaterState.UpdateDownloadReady:
                            taskDialog.ClickButton((int)DialogResult.OK);
                            break;
                        case UpdaterState.DownloadingUpdate:
                        taskDialog.SetProgressBarPosition(updater.DownloadProgress);
                            break;
                    }
                    break;
            }
            return false;
        }
    }

    internal static class UpdateChecker
    {
        private const int UPDATER_VERSION = 2;
        //TODO: correct update url
        private const string URL_UPDATE_DESCRIPTOR = @"http://tinywall.pados.hu/updates/UpdVer{0}_/update.xml";

        internal static UpdateDescriptor GetDescriptor()
        {
            string url = string.Format(CultureInfo.InvariantCulture, URL_UPDATE_DESCRIPTOR, UPDATER_VERSION);
            string tmpFile = Path.GetTempFileName();

            try
            {
                using (WebClient HTTPClient = new WebClient())
                {
                    HTTPClient.DownloadFile(url, tmpFile);
                }

                UpdateDescriptor descriptor = SerializationHelper.LoadFromXMLFile<UpdateDescriptor>(tmpFile);
                if (descriptor.MagicWord != "TinyWall Update Descriptor")
                    throw new ApplicationException("Bad update descriptor file.");

                return descriptor;
            }
            finally
            {
                if (File.Exists(tmpFile))
                    File.Delete(tmpFile);
            }
        }

        internal static UpdateModule GetMainAppModule(UpdateDescriptor descriptor)
        {
            for (int i = 0; i < descriptor.Modules.Length; ++i)
            {
                if (descriptor.Modules[i].Component.Equals("TinyWall"))
                    return descriptor.Modules[i];
            }

            return null;
        }
        internal static UpdateModule GetHostsFileModule(UpdateDescriptor descriptor)
        {
            for (int i = 0; i < descriptor.Modules.Length; ++i)
            {
                if (descriptor.Modules[i].Component.Equals("HostsFile"))
                    return descriptor.Modules[i];
            }

            return null;
        }
        internal static UpdateModule GetDatabaseFileModule(UpdateDescriptor descriptor)
        {
            for (int i = 0; i < descriptor.Modules.Length; ++i)
            {
                if (descriptor.Modules[i].Component.Equals("Database"))
                    return descriptor.Modules[i];
            }

            return null;
        }
    }
}
