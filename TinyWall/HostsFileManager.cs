using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PKSoft
{
    internal static class HostsFileManager
    {
        // Active system hosts file
        private static string HOSTS_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
        // Local copy of active hosts file
        private static string HOSTS_BACKUP = Path.Combine(SettingsManager.AppDataPath, "hosts.bck");
        // User's original hosts file
        private static string HOSTS_ORIGINAL = Path.Combine(SettingsManager.AppDataPath, "hosts.orig");
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

        internal static void UpdateHostsFile(string path, bool enable)
        {
            // We keep a copy of the hosts file for ourself, so that
            // we can re-install it any time without a net connection.
            FileLocker.UnlockFile(HOSTS_BACKUP);
            File.Copy(path, HOSTS_BACKUP, true);
            FileLocker.LockFile(HOSTS_BACKUP, FileAccess.Read, FileShare.Read);

            if (enable)
                EnableHostsFile();
        }

        internal static string HostsMD5()
        {
            byte[] hash;

            using (FileStream fs = new FileStream(HOSTS_PATH, FileMode.Open, FileAccess.Read))
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                hash = md5.ComputeHash(fs);
                fs.Close();
            }

            StringBuilder sb = new StringBuilder();
            foreach (byte hex in hash)
                sb.Append(hex.ToString("x2"));

            return sb.ToString().ToUpperInvariant();
        }

        internal static void EnableHostsFile()
        {
            // If we have no backup of the user's original hosts file,
            // we make a copy of it.
            if (!File.Exists(HOSTS_ORIGINAL))
                CreateOriginalBackup();

            InstallHostsFile(HOSTS_BACKUP);

            FlushDNSCache();
        }

        internal static void DisableHostsFile()
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
        }

        private static void FlushDNSCache()
        {
            try
            {
                // Flush DNS cache
                Utils.StartProcess("ipconfig.exe", "/flushdns", true);
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
