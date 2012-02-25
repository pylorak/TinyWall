using System.Collections;
using System.Collections.Generic;
using System.Gac;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PKSoft
{
    public class GacInstaller : Installer
    {
        private readonly string GACUTIL_PATH = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "gacutil.exe");
        private readonly string TargetFile;

        internal GacInstaller(string file)
        {
            TargetFile = file;
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

            try
            {
                AssemblyCache.InstallAssembly(TargetFile, null, AssemblyCommitFlags.Default);
            }
            catch { }
        }

        public override void Uninstall(IDictionary stateSaver)
        {
            base.Uninstall(stateSaver);

            try
            {
                RemoveAssembly(new AssemblyName(TargetFile));
            }
            catch { }
        }

        private bool RemoveAssembly(AssemblyName asmName)
        {
            byte[] targetPublicKeyToken = asmName.GetPublicKeyToken();
            AssemblyCacheEnum AssembCache = new AssemblyCacheEnum(null);

            List<string> foundAssemblies = new List<string>();

            for(;;)
            {
                string AssembNameLoc = AssembCache.GetNextAssembly();
                if (AssembNameLoc == null)
                    break;

                AssemblyName asmCache = new AssemblyName(AssembNameLoc);

                if (asmCache.Name.Equals(asmName.Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (asmCache.GetPublicKeyToken().SequenceEqual(targetPublicKeyToken))
                    {
                        foundAssemblies.Add(AssembNameLoc);
                    }
                }
            }

            foreach (string name in foundAssemblies)
            {
                AssemblyCacheUninstallDisposition UninstDisp;
                AssemblyCache.UninstallAssembly(name, null, out UninstDisp);
            }
            return (foundAssemblies.Count > 0);
        }
    }
}