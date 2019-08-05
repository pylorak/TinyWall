using System;
using System.IO;
namespace TinyWall.Interface
{
    public class AtomicFileUpdater : TinyWall.Interface.Internal.Disposable
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
            string tmp = RandomFileInSameDir(TargetFilePath);
            try
            {
                if (File.Exists(TargetFilePath))
                    File.Replace(TemporaryFilePath, TargetFilePath, tmp, true);
                else
                    File.Move(TemporaryFilePath, TargetFilePath);
            }
            finally
            {
                try
                {
                    File.Delete(tmp);
                }
                catch { }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    File.Delete(TemporaryFilePath);
                }
                catch { }
            }
        }
    }
}
