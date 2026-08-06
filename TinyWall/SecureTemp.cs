using pylorak.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace pylorak.TinyWall
{
    public static class SecureTemp
    {

        // Returns a path to a folder for temporary files that is supposed to be only accessible for admins.
        // The folder returned here already exists and has the necessary ACLs.
        public static string FolderPath { get; } = Path.Combine(Utils.AppDataPath, "temp_secure");

        public static void Remove(bool removeTempRoot)
        {
            try
            {
                if (removeTempRoot)
                {
                    Directory.Delete(FolderPath, true);
                }
                else
                {
                    var files = Directory.GetFiles(FolderPath);
                    foreach (var f in files)
                    {
                        try { File.Delete(f); }
                        catch { }
                    }

                    var dirs = Directory.GetDirectories(FolderPath);
                    foreach (var d in dirs)
                    {
                        try { Directory.Delete(d, true); }
                        catch { }
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                // Not an error, ignore
            }
            catch (Exception e)
            {
                // We might be executing in an (un)installer, so never fail, only log.
                Utils.LogException(e, Utils.LOG_ID_INSTALLER);
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

            // Disable inheritance first
            var acl = GetAccessControl(fsi);
            acl.SetAccessRuleProtection(true, true);
            SetAccessControl(fsi, acl);

            // Remove old rules
            acl = GetAccessControl(fsi);
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

        private static SecurityIdentifier AdminIdentity { get; } = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

        private static List<FileSystemAccessRule>? _AdminOnlyAcl;
        private static List<FileSystemAccessRule> AdminOnlyAcl
        {
            get
            {
                if (_AdminOnlyAcl is null)
                {
                    var acl = new List<FileSystemAccessRule>();
                    var systemIdentity = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
                    acl.Add(new FileSystemAccessRule(systemIdentity, FileSystemRights.FullControl, AccessControlType.Allow));
                    acl.Add(new FileSystemAccessRule(AdminIdentity, FileSystemRights.FullControl, AccessControlType.Allow));
                    _AdminOnlyAcl = acl;
                }
                return _AdminOnlyAcl;
            }
        }

        public static void EnsureExistence(string folderPath)
        {
            if (!Utils.RunningAsAdmin())
                throw new InsufficientPrivilegesException("Administrative privileges required.");

            // Create (if necessary) folder with correct ACLs
            var dir = new DirectoryInfo(folderPath);
            dir.Create(ToDirectorySecurity(AdminOnlyAcl, AdminIdentity));

            // DirectoryInfo.Create(security) does nothing if the directory already exists.
            // So to make sure the directory gets the required ACLs, we set permissions again
            // just in case Create() did nothing.
            ReplaceFilesystemAccessRules(dir, AdminOnlyAcl, AdminIdentity);
        }

        // Creates and opens a file atomically with access rights only given to admins.
        public static FileStream CreateSecureFileStream(string filePath, FileMode mode, FileSystemRights rights, FileShare share)
        {
            return new FileStream(filePath, mode, rights, share, 4096, FileOptions.None, ToFileSecurity(AdminOnlyAcl, AdminIdentity));
        }

        public static void ProtectFile(string filePath)
        {
            try
            {
                using var _ = CreateSecureFileStream(filePath, FileMode.CreateNew, FileSystemRights.CreateFiles, FileShare.None);
            }
            catch(IOException)
            {
                ReplaceFilesystemAccessRules(new FileInfo(filePath), AdminOnlyAcl, AdminIdentity);
            }
        }
    }
}
