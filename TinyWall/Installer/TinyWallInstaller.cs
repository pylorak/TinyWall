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
            this.Installers.Add(new ServiceInstaller());
        }
    }
}
