using System;
using System.IO;
using TinyWall.Interface.Internal;
using TinyWall.Interface;

namespace PKSoft
{
    internal static class HostsFileManager
    {
        // Active system hosts file
        private static string HOSTS_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
        // Local copy of active hosts file
        private static string HOSTS_BACKUP = Path.Combine(Utils.AppDataPath, "hosts.bck");
        // User's original hosts file
        private static string HOSTS_ORIGINAL = Path.Combine(Utils.AppDataPath, "hosts.orig");
        // Should we lock the system's hosts file
        private static bool _ProtectHostsFile = false;

        internal static void EnableProtection(bool protectHosts)
        {
            _ProtectHostsFile = protectHosts;
            if (File.Exists(HOSTS_PATH))
            {
                if (_ProtectHostsFile)
                    FileLocker.LockFile(HOSTS_PATH, FileAccess.Read, FileShare.Read);
                else
                    FileLocker.UnlockFile(HOSTS_PATH);
            }

            if (File.Exists(HOSTS_BACKUP))
                FileLocker.LockFile(HOSTS_BACKUP, FileAccess.Read, FileShare.Read);

            if (File.Exists(HOSTS_ORIGINAL))
                FileLocker.LockFile(HOSTS_ORIGINAL, FileAccess.Read, FileShare.Read);
        }

        private static void CreateOriginalBackup()
        {
            FileLocker.UnlockFile(HOSTS_ORIGINAL);
            File.Copy(HOSTS_PATH, HOSTS_ORIGINAL, true);
            FileLocker.LockFile(HOSTS_ORIGINAL, FileAccess.Read, FileShare.Read);
        }

        internal static void UpdateHostsFile(string path)
        {
            // We keep a copy of the hosts file for ourself, so that
            // we can re-install it any time without a net connection.
            FileLocker.UnlockFile(HOSTS_BACKUP);
            File.Copy(path, HOSTS_BACKUP, true);
            FileLocker.LockFile(HOSTS_BACKUP, FileAccess.Read, FileShare.Read);
        }

        internal static string GetHostsHash()
        {
            if (File.Exists(HOSTS_BACKUP))
                return Hasher.HashFile(HOSTS_BACKUP);
            else
                return string.Empty;
        }

        internal static bool EnableHostsFile()
        {
            // If we have no backup of the user's original hosts file,
            // we make a copy of it.
            if (!File.Exists(HOSTS_ORIGINAL))
                CreateOriginalBackup();

            try
            {
                InstallHostsFile(HOSTS_BACKUP);
                FlushDNSCache();
                return false;
            }
            catch
            {
                return false;
            }
        }

        internal static bool DisableHostsFile()
        {
            try
            {
                InstallHostsFile(HOSTS_ORIGINAL);

                // Delete backup of original so that it can be
                // recreated next time we install a custom hosts.
                if (File.Exists(HOSTS_ORIGINAL))
                {
                    FileLocker.UnlockFile(HOSTS_ORIGINAL);
                    File.Delete(HOSTS_ORIGINAL);
                }

                FlushDNSCache();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void FlushDNSCache()
        {
            try
            {
                // Flush DNS cache
                Utils.FlushDnsCache();
            }
            catch
            {
                // We just want to block exceptions.
            }
        }

        private static void InstallHostsFile(string sourcePath)
        {
            try
            {
                if (File.Exists(sourcePath))
                {
                    FileLocker.UnlockFile(HOSTS_PATH);
                    File.Copy(sourcePath, HOSTS_PATH, true);
                }
            }
            finally
            {
                if (_ProtectHostsFile)
                    FileLocker.LockFile(HOSTS_PATH, FileAccess.Read, FileShare.Read);
                else
                    FileLocker.UnlockFile(HOSTS_PATH);
            }
        }

    }
}
