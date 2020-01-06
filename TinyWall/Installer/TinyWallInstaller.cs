using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using System.Reflection;
using System.IO;

namespace PKSoft
{
    [RunInstaller(true)]
    public class TinyWallInstaller : Installer
    {
        private List<string> names = new List<string>();
        private List<string> files = new List<string>();

        public TinyWallInstaller()
        {
            string installDir = Path.GetDirectoryName(TinyWall.Interface.Internal.Utils.ExecutablePath);
            GetFiles(installDir, "*.exe");

            for (int i = 0; i < files.Count; ++i)
            {
                this.Installers.Add(new NgenInstaller(names[i], files[i]));
            }
            this.Installers.Add(new ServiceInstaller());
        }

        private void GetFiles(string dir, string filter)
        {
            string[] filePaths = Directory.GetFiles(dir, filter, SearchOption.TopDirectoryOnly);
            foreach (string filePath in filePaths)
            {
                try
                {
                    Assembly a = Assembly.ReflectionOnlyLoadFrom(filePath);
                    files.Add(filePath);
                    names.Add(a.FullName);  // same as the "display name"
                }
                catch { }
            }
        }

    }
}