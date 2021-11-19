using System.Collections.Generic;
using System.IO;

namespace pylorak.Utilities
{
    public sealed class FileLocker : Disposable
    {
        private readonly Dictionary<string, FileStream> LockedFiles = new();

        public bool Lock(string filePath, FileAccess localAccess, FileShare shareMode)
        {
            if (IsLocked(filePath))
                return false;

            try
            {
                LockedFiles.Add(filePath, new FileStream(filePath, FileMode.OpenOrCreate, localAccess, shareMode));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public FileStream GetStream(string filePath)
        {
            return LockedFiles[filePath];
        }

        public bool IsLocked(string filePath)
        {
            return LockedFiles.ContainsKey(filePath);
        }

        public bool Unlock(string filePath)
        {
            if (!IsLocked(filePath))
                return false;

            try
            {
                LockedFiles[filePath].Close();
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
            foreach (var stream in LockedFiles.Values)
            {
                try { stream.Close(); } catch { }
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
