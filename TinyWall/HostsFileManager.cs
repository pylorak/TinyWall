using System;
using System.IO;
using pylorak.Utilities;

namespace pylorak.TinyWall
{
    internal class HostsFileManager : Disposable
    {
        // Active system hosts file
        private static string HOSTS_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
        // Local copy of active hosts file
        private static string HOSTS_BACKUP = Path.Combine(Utils.AppDataPath, "hosts.bck");
        // User's original hosts file
        private static string HOSTS_ORIGINAL = Path.Combine(Utils.AppDataPath, "hosts.orig");

        public readonly FileLocker FileLocker = new FileLocker();

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                FileLocker.Dispose();
            }

            base.Dispose(disposing);
        }


        private bool _EnableProtection;
        public bool EnableProtection
        {
            get => _EnableProtection;
            set
            {
                _EnableProtection = value;
                if (File.Exists(HOSTS_PATH))
                {
                    if (_EnableProtection)
                        FileLocker.Lock(HOSTS_PATH, FileAccess.Read, FileShare.Read);
                    else
                        FileLocker.Unlock(HOSTS_PATH);
                }

                if (File.Exists(HOSTS_BACKUP))
                    FileLocker.Lock(HOSTS_BACKUP, FileAccess.Read, FileShare.Read);

                if (File.Exists(HOSTS_ORIGINAL))
                    FileLocker.Lock(HOSTS_ORIGINAL, FileAccess.Read, FileShare.Read);
            }
        }

        private void CreateOriginalBackup()
        {
            FileLocker.Unlock(HOSTS_ORIGINAL);
            File.Copy(HOSTS_PATH, HOSTS_ORIGINAL, true);
            FileLocker.Lock(HOSTS_ORIGINAL, FileAccess.Read, FileShare.Read);
        }

        public void UpdateHostsFile(string path)
        {
            // We keep a copy of the hosts file for ourself, so that
            // we can re-install it any time without a net connection.
            FileLocker.Unlock(HOSTS_BACKUP);
            using (var afu = new AtomicFileUpdater(HOSTS_BACKUP))
            {
                File.Copy(path, afu.TemporaryFilePath, true);
                afu.Commit();
            }
            FileLocker.Lock(HOSTS_BACKUP, FileAccess.Read, FileShare.Read);
        }

        public string GetHostsHash()
        {
            if (File.Exists(HOSTS_BACKUP))
                return Hasher.HashFile(HOSTS_BACKUP);
            else
                return string.Empty;
        }

        public bool EnableHostsFile()
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

        public bool DisableHostsFile()
        {
            try
            {
                InstallHostsFile(HOSTS_ORIGINAL);

                // Delete backup of original so that it can be
                // recreated next time we install a custom hosts.
                if (File.Exists(HOSTS_ORIGINAL))
                {
                    FileLocker.Unlock(HOSTS_ORIGINAL);
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

        private void InstallHostsFile(string sourcePath)
        {
            try
            {
                if (File.Exists(sourcePath))
                {
                    FileLocker.Unlock(HOSTS_PATH);
                    File.Copy(sourcePath, HOSTS_PATH, true);
                }
            }
            finally
            {
                if (_EnableProtection)
                    FileLocker.Lock(HOSTS_PATH, FileAccess.Read, FileShare.Read);
                else
                    FileLocker.Unlock(HOSTS_PATH);
            }
        }

    }
}
