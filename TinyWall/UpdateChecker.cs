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

        private static DirectorySecurity ToDirectorySecurity(List<FileSystemAccessRule> acl, SecurityIdentifier owner)
        {
            var security = new DirectorySecurity();
            security.SetAccessRuleProtection(true, true);
            security.SetOwner(owner);
            foreach (var rule in acl)
                security.AddAccessRule(rule);
            return security;
        }

        private static FileSecurity ToFileSecurity(List<FileSystemAccessRule> acl, SecurityIdentifier owner)
        {
            var security = new FileSecurity();
            security.SetAccessRuleProtection(true, true);
            security.SetOwner(owner);
            foreach (var rule in acl)
                security.AddAccessRule(rule);
            return security;
        }

        private static void ReplaceFilesystemAccessRules(FileSystemInfo fsi, List<FileSystemAccessRule> newAcl, SecurityIdentifier newOwner)
        {
            static FileSystemSecurity GetAccessControl(FileSystemInfo fsi)
            {
                if (fsi is DirectoryInfo di)
                    return di.GetAccessControl();
                else if (fsi is FileInfo fi)
                    return fi.GetAccessControl();
                else
                    throw new ArgumentException("Unknown FileSystemInfo subclass.", nameof(fsi));
            }

            static void SetAccessControl(FileSystemInfo fsi, FileSystemSecurity fss)
            {
                if (fsi is DirectoryInfo di)
                    di.SetAccessControl(fss as DirectorySecurity);
                else if (fsi is FileInfo fi)
                    fi.SetAccessControl(fss as FileSecurity);
                else
                    throw new ArgumentException("Unknown FileSystemInfo subclass.", nameof(fsi));
            }

            // If this is a directory we disable inheritance first
            if (fsi is DirectoryInfo di)
            {
                var acl2 = di.GetAccessControl();
                acl2.SetAccessRuleProtection(true, true);
                di.SetAccessControl(acl2);
            }

            // Remove old rules
            var acl = GetAccessControl(fsi);
            var ruleCollection = acl.GetAccessRules(true, true, typeof(NTAccount));
            foreach (var rule in ruleCollection)
            {
                if (rule is FileSystemAccessRule fsaRule)
                    acl.RemoveAccessRuleAll(fsaRule);
            }

            // Set new rules
            acl.SetOwner(newOwner);
            foreach (var rule in newAcl)
                acl.AddAccessRule(rule);

            // Apply
            SetAccessControl(fsi, acl);
        }

        // Creates and opens a file atomically with access rights only given to admins.
        // The immediate parent directory of the file will also be modified to be only accessible for admins.
        private static FileStream CreateSecureFileStream(string filePath, FileMode mode, FileSystemRights rights, FileShare share)
        {
            // Define ACL that we want to assign to directory and file
            var acl = new List<FileSystemAccessRule>();
            var systemIdentity = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            var adminIdentity = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            acl.Add(new FileSystemAccessRule(systemIdentity, FileSystemRights.FullControl, AccessControlType.Allow));
            acl.Add(new FileSystemAccessRule(adminIdentity, FileSystemRights.FullControl, AccessControlType.Allow));

            // Create (if necessary) parent directory, and adjust its ACLs
            var parentDirPath = Path.GetDirectoryName(filePath);
            var parentDir = new DirectoryInfo(parentDirPath);
            parentDir.Create(ToDirectorySecurity(acl, adminIdentity));

            // DirectoryInfo.Create(security) does nothing if the directory already exists.
            // So to make sure the directory gets the required ACLs, we set permissions again
            // just in case Create() did nothing.
            ReplaceFilesystemAccessRules(parentDir, acl, adminIdentity);

            // Create and open file with defined permissions
            return new FileStream(filePath, mode, rights, share, 4096, FileOptions.None, ToFileSecurity(acl, adminIdentity));
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

                        var tmpFilePath = Path.Combine(Utils.SecureTempPath, Utils.RandomString(12) + ".msi");
                        using (var tmpFileStream = CreateSecureFileStream(tmpFilePath, FileMode.CreateNew, FileSystemRights.Write, FileShare.None))
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
