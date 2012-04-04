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
        public TinyWallInstaller()
        {
            List<string> files = new List<string>();
            string installDir = Path.GetDirectoryName(Utils.ExecutablePath);
            files.AddRange(GetFiles(installDir, "*.exe"));
            files.AddRange(GetFiles(installDir, "*.dll"));

            foreach (string target in files)
            {
                this.Installers.Add(new NgenInstaller(target));
            }
            this.Installers.Add(new ServiceInstaller());
        }

        private List<string> GetFiles(string dir, string filter)
        {
            List<string> files = new List<string>();
            string[] filePaths = Directory.GetFiles(dir, filter, SearchOption.AllDirectories);
            foreach (string filePath in filePaths)
            {
                try
                {
                    Assembly a = Assembly.ReflectionOnlyLoadFrom(filePath);
                    string name = a.FullName;

                    files.Add(filePath);
                }
                catch { }
            }

            return files;
        }

    }
}