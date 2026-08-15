using System.Collections.Concurrent;
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
            private readonly bool RemainUnlockedOnDispose;

            public TemporaryUnlock(FileLocker parent, string filePath, FileAccess access, FileShare share, bool remainUnlockedOnDispose = false)
            {
                Parent = parent;
                FilePath = filePath;
                Access = access;
                Share = share;
                RemainUnlockedOnDispose = remainUnlockedOnDispose;
            }

            protected override void Dispose(bool disposing)
            {
                if (IsDisposed)
                    return;

                if (disposing)
                {
                    if (!RemainUnlockedOnDispose)
                        Parent.Lock(FilePath, Access, Share);
                }

                base.Dispose(disposing);
            }
        }

        private readonly ConcurrentDictionary<string, FileLock> LockedFiles = new();

        public bool Lock(string filePath, FileAccess localAccess, FileShare shareMode)
        {
            if (IsLocked(filePath))
                return false;

            try
            {
                var flock = new FileLock(filePath, localAccess, shareMode);
                if (!LockedFiles.TryAdd(filePath, flock))
                {
                    flock.Stream.Close();
                    return false;
                }
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
            if (LockedFiles.TryGetValue(filePath, out var lockDetails))
            {
                Unlock(filePath);
                return new TemporaryUnlock(this, filePath, lockDetails.Access, lockDetails.Share);
            }
            else
            {
                // We return a "dummy" object that won't relock the file when disposed.
                // This way users can call UnlockTemporarily() without having to worry if the file is locked or not.
                return new TemporaryUnlock(this, filePath, FileAccess.Read, FileShare.Read, true);
            }
        }

        public bool Unlock(string filePath)
        {
            try
            {
                if (LockedFiles.TryRemove(filePath, out var flock))
                    flock.Stream.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void UnlockAll()
        {
            var keys = new List<string>(LockedFiles.Keys);
            foreach (var k in keys)
                Unlock(k);
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
