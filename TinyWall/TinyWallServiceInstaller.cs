using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace PKSoft
{
    [RunInstaller(true)]
    public class TinyWallServiceInstaller : Installer
    {
        // Service Account Information
        private ServiceProcessInstaller serviceProcessInstaller = null;
        // Service Information
        ServiceInstaller serviceInstaller = null;

        public TinyWallServiceInstaller()
        {
            try
            {
                serviceProcessInstaller = new ServiceProcessInstaller();
                serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
                serviceProcessInstaller.Username = null;
                serviceProcessInstaller.Password = null;

                serviceInstaller = new ServiceInstaller();
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
            catch
            {
                if (serviceInstaller != null)
                    serviceInstaller.Dispose();
                if (serviceProcessInstaller != null)
                    serviceProcessInstaller.Dispose();
            }
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

            serviceInstaller = null;
            serviceProcessInstaller = null;
            base.Dispose(disposing);
        }

        protected override void OnBeforeInstall(System.Collections.IDictionary savedState)
        {
            Context.Parameters["assemblypath"] += "\" /service";
            base.OnBeforeInstall(savedState);
        }

        protected override void OnBeforeUninstall(System.Collections.IDictionary savedState)
        {
            Context.Parameters["assemblypath"] += "\" /service";
            base.OnBeforeUninstall(savedState);
        }
    }
}
