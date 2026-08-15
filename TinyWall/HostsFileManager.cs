using pylorak.Utilities;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace pylorak.TinyWall
{
    internal class HostsFileManager : Disposable
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class SafeNativeMethods
        {
            [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
            internal static extern uint DnsFlushResolverCache();
        }

        // Active system hosts file
        private readonly static string HOSTS_ACTIVE = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
        // TinyWall's customized hosts file
        private readonly static string HOSTS_CUSTOM = Path.Combine(AppPaths.AppDataPath, "hosts.custom");
        // Backup copy of the system's original hosts file
        private readonly static string HOSTS_RESTORE = Path.Combine(AppPaths.AppDataPath, "hosts.restore");

        private readonly FileLocker FileLocker = new();

        internal HostsFileManager()
        {
            try
            {
                // Migration from versions 3.5.1 and older
                // TODO: Remove in a future version

                // hosts.restore used to be called hosts.orig.
                // After an upgrade hosts.restore doesn't exist hence TinyWall would back it up again,
                // and if TinyWall's custom hosts file is already installed it would get saved as
                // our hosts.restore, losing the user's original file. So to avoid this,
                // migrate over an older hosts.orig
                var hostsOrig = Path.Combine(AppPaths.AppDataPath, "hosts.orig");
                if (File.Exists(hostsOrig))
                {
                    if (File.Exists(HOSTS_RESTORE))
                        File.Delete(hostsOrig);
                    else
                        File.Move(hostsOrig, HOSTS_RESTORE);
                }

                // Cleanup after upgrading from older version. Not strictly necessary.
                var hostsBck = Path.Combine(AppPaths.AppDataPath, "hosts.bck");
                File.Delete(hostsBck);
            }
            catch (Exception e)
            {
                Utils.LogException(e, Utils.LOG_ID_SERVICE);
            }

            FileLocker.Lock(HOSTS_CUSTOM, FileAccess.Read, FileShare.Read);
            if (File.Exists(HOSTS_RESTORE))
                FileLocker.Lock(HOSTS_RESTORE, FileAccess.Read, FileShare.Read);
        }

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

        public bool LockSystemHostsFile
        {
            get => FileLocker.IsLocked(HOSTS_ACTIVE);
            set
            {
                if (value && File.Exists(HOSTS_ACTIVE))
                    FileLocker.Lock(HOSTS_ACTIVE, FileAccess.Read, FileShare.Read);
                else
                    FileLocker.Unlock(HOSTS_ACTIVE);
            }
        }

        public void UpdateCustomHostsSelfCopy(Stream newHostsStream)
        {
            // We keep a copy of the hosts file for ourself, so that
            // we can re-install it any time without a net connection.
            using var unlock = FileLocker.UnlockTemporarily(HOSTS_CUSTOM);
            using var afu = new AtomicFileUpdater(HOSTS_CUSTOM);
            using (var tempFileStream = new FileStream(afu.TemporaryFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                newHostsStream.CopyTo(tempFileStream);
            }
            afu.Commit();

            if (IsCustomHostsAlreadyInstalled())
                InstallHostsFile(HOSTS_CUSTOM);
        }

        public static string GetCustomHostsHash()
        {
            if (File.Exists(HOSTS_CUSTOM))
                return Hasher.HashFile(HOSTS_CUSTOM);
            else
                return string.Empty;
        }

        private static bool IsCustomHostsPresent()
        {
            var file = new FileInfo(HOSTS_CUSTOM);
            return file.Exists && (file.Length != 0);
        }

        private static bool IsCustomHostsAlreadyInstalled()
        {
            return File.Exists(HOSTS_RESTORE);
        }

        public bool EnableCustomHostsFile()
        {
            try
            {
                if (!IsCustomHostsPresent())
                    return false;

                if (IsCustomHostsAlreadyInstalled())
                    return true;

                File.Copy(HOSTS_ACTIVE, HOSTS_RESTORE, true);
                FileLocker.Lock(HOSTS_RESTORE, FileAccess.Read, FileShare.Read);

                InstallHostsFile(HOSTS_CUSTOM);
                return true;
            }
            catch
            {
                // We cannot leave HOSTS_RESTORE on disk as presence
                // is used as a persistent flag whether a custom hosts is installed or not.
                try
                {
                    FileLocker.Unlock(HOSTS_RESTORE);
                    File.Delete(HOSTS_RESTORE); // does not throw if the file already doesn't exist
                }
                catch { }

                return false;
            }
        }

        public bool DisableCustomHostsFile()
        {
            try
            {
                if (!IsCustomHostsAlreadyInstalled())
                    return true;

                InstallHostsFile(HOSTS_RESTORE);

                // We cannot leave HOSTS_RESTORE on disk as presence
                // is used as a persistent flag whether a custom hosts is installed or not.
                FileLocker.Unlock(HOSTS_RESTORE);
                File.Delete(HOSTS_RESTORE);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void FlushDNSCache()
        {
            try { _ = SafeNativeMethods.DnsFlushResolverCache(); }
            catch {
                // Exceptions ignored on purpose
            }
        }

        private void InstallHostsFile(string sourcePath)
        {
            using var unlock = FileLocker.UnlockTemporarily(HOSTS_ACTIVE);
            using (var afu = new AtomicFileUpdater(HOSTS_ACTIVE))
            {
                File.Copy(sourcePath, afu.TemporaryFilePath);
                afu.Commit();
            }

            // Important to only flush DNS cache after disposing afu above
            FlushDNSCache();
        }
    }
}
