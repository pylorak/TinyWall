using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PKSoft
{
    internal class FileLocker : DisposableObject
    {
        private Dictionary<string, FileStream> LockedFiles;

        internal FileLocker()
        {
            LockedFiles = new Dictionary<string, FileStream>();
        }

        internal bool LockFile(string fn, FileAccess localAccess, FileShare shareMode)
        {
            try
            {
                LockedFiles.Add(fn, new FileStream(fn, FileMode.OpenOrCreate, localAccess, shareMode));
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal FileStream GetStream(string fn)
        {
            return LockedFiles[fn];
        }

        internal bool UnlockFile(string fn)
        {
            try
            {
                LockedFiles[fn].Close();
                LockedFiles.Remove(fn);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal void UnlockAll()
        {
            foreach (string fn in LockedFiles.Keys)
                UnlockFile(fn);
        }

        protected override void DisposeManaged()
        {
            UnlockAll();
            base.DisposeManaged();
        }

        ~FileLocker()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
    }
}
