using System;
using System.Collections;
using System.ServiceProcess;
using pylorak.Windows.Services;

namespace pylorak.TinyWall.Installer
{
    internal class TinyWallServiceInstaller : System.Configuration.Install.Installer
    {
        // Service Account Information
        private readonly ServiceProcessInstaller serviceProcessInstaller = new();
        // Service Information
        private readonly ServiceInstaller serviceInstaller = new();

        internal TinyWallServiceInstaller()
        {
            try
            {
                serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
                serviceProcessInstaller.Username = null;
                serviceProcessInstaller.Password = null;

                serviceInstaller.DisplayName = TinyWallService.SERVICE_DISPLAY_NAME;
                serviceInstaller.StartType = ServiceStartMode.Automatic;
                // This must be identical to the WindowsService.ServiceBase name
                // set in the constructor of WindowsService.cs
                serviceInstaller.ServiceName = TinyWallService.SERVICE_NAME;
                // Depends on other services
                serviceInstaller.ServicesDependedOn = TinyWallService.ServiceDependencies;

                this.Installers.Add(serviceProcessInstaller);
                this.Installers.Add(serviceInstaller);
            }
            catch(Exception e)
            {
                Utils.LogException(e, Utils.LOG_ID_INSTALLER);
            }
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

            try
            {
                using var scm = new ServiceControlManager();
                scm.SetLoadOrderGroup(TinyWallService.SERVICE_NAME, @"NetworkProvider");
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release managed resources

                serviceInstaller.Dispose();
                serviceProcessInstaller.Dispose();
            }

            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.

            base.Dispose(disposing);
        }
    }
}
