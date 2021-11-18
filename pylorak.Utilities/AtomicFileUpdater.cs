using System;
using System.IO;
using TinyWall.Interface.Internal;

namespace TinyWall.Interface
{
    public class AtomicFileUpdater : Disposable
    {
        public AtomicFileUpdater(string targetFile)
        {
            // File.Replace needs the target and temporary files to be on the same volume.
            // To ensure this, we create our temporary file in the same folder as our target.
            TargetFilePath = targetFile;
            TemporaryFilePath = RandomFileInSameDir(targetFile);
        }

        private static string RandomFileInSameDir(string file)
        {
            string targetDir = Path.GetDirectoryName(file);
            return Path.Combine(targetDir, Path.GetRandomFileName());
        }

        public string TemporaryFilePath { get; }
        public string TargetFilePath { get; }

        public void Commit()
        {
            string backup = RandomFileInSameDir(TargetFilePath);
            try
            {
                if (File.Exists(TargetFilePath))
                    File.Replace(TemporaryFilePath, TargetFilePath, backup, true);
                else
                    File.Move(TemporaryFilePath, TargetFilePath);
            }
            finally
            {
                try
                {
                    File.Delete(backup);
                }
                catch { }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                try
                {
                    File.Delete(TemporaryFilePath);
                }
                catch { }
            }

            base.Dispose(disposing);
        }
    }
}
