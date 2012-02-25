using System.Collections;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace PKSoft
{
    public class NgenInstaller : Installer
    {
        private readonly string NGEN_PATH = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "ngen.exe");
        private readonly InstallTargetFile TargetFile;

        internal NgenInstaller(InstallTargetFile file)
        {
            TargetFile = file;
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

            using (Process p = Utils.StartProcess(NGEN_PATH, "install \"" + TargetFile.Path + "\"", true, true))
            {
                p.WaitForExit();
            }
        }

        public override void Uninstall(IDictionary stateSaver)
        {
            base.Uninstall(stateSaver);

            using (Process p = Utils.StartProcess(NGEN_PATH, "uninstall \"" + TargetFile.Path + "\"", true, true))
            {
                p.WaitForExit();
            }
        }
    }
}