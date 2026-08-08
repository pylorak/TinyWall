using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace pylorak.TinyWall
{
    public enum UserAccess
    {
        None,
        ReadOnly
    };

    public static class FilesystemProtection
    {
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
        private static List<FileSystemAccessRule>? _AdminRwUserRAcl;

        private static List<FileSystemAccessRule> GetAcl(UserAccess userAccess)
        {
            switch (userAccess)
            {
                case UserAccess.None:
                    if (_AdminOnlyAcl is null)
                    {
                        var acl = new List<FileSystemAccessRule>();
                        var systemIdentity = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
                        acl.Add(new FileSystemAccessRule(systemIdentity, FileSystemRights.FullControl, AccessControlType.Allow));
                        acl.Add(new FileSystemAccessRule(AdminIdentity, FileSystemRights.FullControl, AccessControlType.Allow));
                        _AdminOnlyAcl = acl;
                    }
                    return _AdminOnlyAcl;
                case UserAccess.ReadOnly:
                    if (_AdminRwUserRAcl is null)
                    {
                        var acl = new List<FileSystemAccessRule>();
                        var systemIdentity = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
                        var userIdentity = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                        acl.Add(new FileSystemAccessRule(systemIdentity, FileSystemRights.FullControl, AccessControlType.Allow));
                        acl.Add(new FileSystemAccessRule(AdminIdentity, FileSystemRights.FullControl, AccessControlType.Allow));
                        acl.Add(new FileSystemAccessRule(userIdentity, FileSystemRights.Read, AccessControlType.Allow));
                        _AdminRwUserRAcl = acl;
                    }
                    return _AdminRwUserRAcl;
            }

            throw new InvalidOperationException();
        }

        public static FileStream CreateProtectedFile(string filePath, FileShare share, UserAccess userAccess, FileSystemRights rights = FileSystemRights.FullControl)
        {
            return new FileStream(filePath, FileMode.CreateNew, rights, share, 4096, FileOptions.None, ToFileSecurity(GetAcl(userAccess), AdminIdentity));
        }

        public static void EnsureFolder(string folderPath, UserAccess userAccess)
        {
            // Create (if necessary) folder with correct ACLs
            var dir = new DirectoryInfo(folderPath);
            dir.Create(ToDirectorySecurity(GetAcl(userAccess), AdminIdentity));

            // DirectoryInfo.Create(security) does nothing if the directory already exists.
            // So to make sure the directory gets the required ACLs, we set permissions again
            // just in case Create() did nothing.
            ReplaceFilesystemAccessRules(dir, GetAcl(userAccess), AdminIdentity);
        }

        public static void EnsureFile(string filePath, UserAccess userAccess)
        {
            try
            {
                using var _ = CreateProtectedFile(filePath, FileShare.None, userAccess, FileSystemRights.CreateFiles);
            }
            catch(IOException)
            {
                ReplaceFilesystemAccessRules(new FileInfo(filePath), GetAcl(userAccess), AdminIdentity);
            }
        }
    }
}
