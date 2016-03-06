using System;
using System.IO;
namespace TinyWall.Interface
{
    public class AtomicFileUpdater :TinyWall.Interface.Internal.Disposable
    {
        public AtomicFileUpdater(string targetFile)
        {
            // File.Replace needs the target and temporary files to be on the same volume.
            // To ensure this, we create our temporary file in the same folder as our target.
            TargetFilePath = targetFile;
            string targetDir = Path.GetDirectoryName(targetFile);
            this.TemporaryFilePath = Path.Combine(targetDir, Path.GetRandomFileName());
        }

        public string TemporaryFilePath { get; }
        public string TargetFilePath { get; }

        public void Commit()
        {
            if (File.Exists(TargetFilePath))
                File.Replace(TemporaryFilePath, TargetFilePath, null);
            else
                File.Move(TemporaryFilePath, TargetFilePath);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (File.Exists(TemporaryFilePath))
                        File.Delete(TemporaryFilePath);
                }
                catch { }
            }
        }
    }
}
