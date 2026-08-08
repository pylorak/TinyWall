using System.Collections.Generic;
using System.IO;

namespace pylorak.Utilities
{
    public sealed class FileLocker : Disposable
    {
        public readonly struct FileLock
        {
            public readonly FileAccess Access;
            public readonly FileShare Share;
            public readonly FileStream Stream;

            public FileLock(string filePath, FileAccess localAccess, FileShare shareMode)
            {
                Access = localAccess;
                Share = shareMode;
                Stream = new FileStream(filePath, FileMode.OpenOrCreate, localAccess, shareMode);
            }
        };

        public class TemporaryUnlock : Disposable
        {
            private readonly FileLocker Parent;
            private readonly string FilePath;
            private readonly FileAccess Access;
            private readonly FileShare Share;

            public TemporaryUnlock(FileLocker parent, string filePath, FileAccess access, FileShare share)
            {
                Parent = parent;
                FilePath = filePath;
                Access = access;
                Share = share;
            }

            protected override void Dispose(bool disposing)
            {
                if (IsDisposed)
                    return;

                if (disposing)
                {
                    Parent.Lock(FilePath, Access, Share);
                }

                base.Dispose(disposing);
            }
        }

        private readonly Dictionary<string, FileLock> LockedFiles = new();

        public bool Lock(string filePath, FileAccess localAccess, FileShare shareMode)
        {
            if (IsLocked(filePath))
                return false;

            try
            {
                LockedFiles.Add(filePath, new FileLock(filePath, localAccess, shareMode));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public FileStream GetStream(string filePath)
        {
            return LockedFiles[filePath].Stream;
        }

        public bool IsLocked(string filePath)
        {
            return LockedFiles.ContainsKey(filePath);
        }

        public TemporaryUnlock UnlockTemporarily(string filePath)
        {
            var lockDetails = LockedFiles[filePath];
            Unlock(filePath);
            return new TemporaryUnlock(this, filePath, lockDetails.Access, lockDetails.Share);
        }

        public bool Unlock(string filePath)
        {
            if (!IsLocked(filePath))
                return false;

            try
            {
                LockedFiles[filePath].Stream.Close();
                LockedFiles.Remove(filePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void UnlockAll()
        {
            foreach (var flock in LockedFiles.Values)
            {
                try { flock.Stream.Close(); } catch { }
            }

            LockedFiles.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
                UnlockAll();

            base.Dispose(disposing);
        }
    }
}
