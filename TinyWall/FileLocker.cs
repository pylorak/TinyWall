using System.Collections.Generic;
using System.IO;

namespace PKSoft
{
    internal static class FileLocker
    {
        private static Dictionary<string, FileStream> LockedFiles = new Dictionary<string, FileStream>();

        internal static bool LockFile(string filePath, FileAccess localAccess, FileShare shareMode)
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

        internal static FileStream GetStream(string filePath)
        {
            return LockedFiles[filePath];
        }

        internal static bool IsLocked(string filePath)
        {
            return LockedFiles.ContainsKey(filePath);
        }

        internal static bool UnlockFile(string filePath)
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

        internal static void UnlockAll()
        {
            foreach (string filePath in LockedFiles.Keys)
                UnlockFile(filePath);
        }
    }
}
