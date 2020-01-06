using System.Collections;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace PKSoft
{
    internal class NgenInstaller : Installer
    {
        private readonly string NGEN_PATH = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "ngen.exe");
        private readonly string DisplayName;
        private readonly string FilePath;

        internal NgenInstaller(string displayName, string filePath)
        {
            DisplayName = displayName;
            FilePath = filePath;
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

            try
            {
                using (Process p = Utils.StartProcess(NGEN_PATH, "install \"" + DisplayName + "\" /ExeConfig:\"" + FilePath + "\"", true, true))
                {
                    p.WaitForExit();
                }
            }
            catch { }
        }

        public override void Uninstall(IDictionary stateSaver)
        {
            base.Uninstall(stateSaver);

            try
            {
                using (Process p = Utils.StartProcess(NGEN_PATH, "uninstall \"" + DisplayName + "\"", true, true))
                {
                    p.WaitForExit();
                }
            }
            catch { }
        }
    }
}