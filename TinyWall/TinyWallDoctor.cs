using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

namespace PKSoft
{
    internal static class TinyWallDoctor
    {
        internal static bool IsServiceRunning()
        {
#if !DEBUG
            try
            {
                using (ServiceController sc = new ServiceController(TinyWallService.SERVICE_NAME))
                {
                    return (sc.Status == ServiceControllerStatus.Running) || (sc.Status == ServiceControllerStatus.StartPending);
                }
            }
            catch
            {
                return false;
            }
#else
            return true;
#endif
        }

        internal static bool EnsureServiceInstalledAndRunning()
        {
            if (TinyWallDoctor.IsServiceRunning())
                return true;

            if (Utils.RunningAsAdmin())
            {
                // Run installers
                try
                {
                    ManagedInstallerClass.InstallHelper(new string[] { "/i", Utils.ExecutablePath });
                }
                catch { }

                // Ensure dependencies
                try
                {
                    TinyWallDoctor.EnsureHealth();
                }
                catch { }

                // Start service
                try
                {
                    using (ServiceController sc = new ServiceController(TinyWallService.SERVICE_NAME))
                    {
                        if (sc.Status == ServiceControllerStatus.Stopped)
                        {
                            sc.Start();
                            sc.WaitForStatus(ServiceControllerStatus.Running, System.TimeSpan.FromSeconds(5));
                        }
                    }
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                // We are not running as admin.
                try
                {
                    using (Process p = Utils.StartProcess(Utils.ExecutablePath, "/install", true))
                    {
                        p.WaitForExit();
                        return (p.ExitCode == 0);
                    }
                }
                catch { return false; }
            }

            return true;
        }

        internal static int Uninstall()
        {
            if (System.Windows.Forms.MessageBox.Show(
                PKSoft.Resources.Messages.DidYouInitiateTheUninstall,
                PKSoft.Resources.Messages.TinyWall,
                System.Windows.Forms.MessageBoxButtons.YesNo,
                System.Windows.Forms.MessageBoxIcon.Exclamation) != System.Windows.Forms.DialogResult.Yes)
            {
                return -1;
            }

            // Disable automatic re-start of service
            for (int i = 0; i < 5; ++i)
            {
                // Try to stop service
                try
                {
                    using (ServiceController sc = new ServiceController(TinyWallService.SERVICE_NAME))
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, System.TimeSpan.FromSeconds(10));
                    }
                }
                catch { }

                // Disable automatic recovery
                try
                {
                    using (ScmWrapper.ServiceControlManager scm = new ScmWrapper.ServiceControlManager())
                    {
                        scm.SetStartupMode(TinyWallService.SERVICE_NAME, ServiceStartMode.Automatic);
                        scm.SetRestartOnFailure(TinyWallService.SERVICE_NAME, false);
                    }
                }
                catch { }

                // Terminate TinyWall processes
                {
                    int ownPid = Process.GetCurrentProcess().Id;
                    Process[] procs = Process.GetProcesses();
                    foreach (Process p in procs)
                    {
                        try
                        {
                            if (p.ProcessName.Contains("TinyWall") && (p.Id != ownPid))
                            {
                                if (!p.CloseMainWindow())
                                    p.Kill();
                                else if (!p.WaitForExit(5000))
                                    p.Kill();
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }

            // Give some additional time for process shutdown
            System.Threading.Thread.Sleep(5000);

            // Disable automatic start of controller
            Utils.RunAtStartup("TinyWall Controller", null);

            // Put back the user's original hosts file
            HostsFileManager.DisableHostsFile();

            // Reset Windows Firewall to its default state
            WindowsFirewall.Policy Firewall = new WindowsFirewall.Policy();
            Firewall.ResetFirewall();

            // Uninstall service
            try
            {
                ManagedInstallerClass.InstallHelper(new string[] { "/u", Utils.ExecutablePath });
            }
            catch { }

            // Remove user settings
            string UserDir = ControllerSettings.UserDataPath;
            Directory.Delete(UserDir, true);

            return 0;
        }

        internal static void EnsureHealth()
        {
            // Ensure that TinyWall's dependencies can be started
            EnsureServiceDependencies();

            // Ensure that TinyWall itself can be started
            try
            {
                using (ScmWrapper.ServiceControlManager scm = new ScmWrapper.ServiceControlManager())
                {
                    scm.SetStartupMode(TinyWallService.SERVICE_NAME, ServiceStartMode.Automatic);
                    scm.SetRestartOnFailure(TinyWallService.SERVICE_NAME, true);
                }
            }
            catch
            { }

            // Ensure that controller will be started on next reboot
            Utils.RunAtStartup("TinyWall Controller", Utils.ExecutablePath);
        }

        private static void EnsureServiceDependencies()
        {
            // First, do a recursive scan of all service dependencies
            List<string> depcoll = new List<string>();
            foreach (string srv in TinyWallService.ServiceDependencies)
            {
                using (ServiceController sc = new ServiceController(srv))
                {
                    ScanServiceDependencies(sc, depcoll);
                }
            }
            // depcoll now contains all our dependencies

            // Identify disabled services that we depend on
            for (int i = depcoll.Count - 1; i >= 0; --i)
            {
                string srv = depcoll[i];
                using (ScmWrapper.ServiceControlManager scm = new ScmWrapper.ServiceControlManager())
                {
                    // Remove a service if it is not disabled, so that at the end we'll have a list
                    // of all disabled services that we need to fix.
                    if (scm.GetStartupMode(srv) != (uint)ServiceStartMode.Disabled)
                        depcoll.RemoveAt(i);
                }
            }
            // depcoll now contains all our disabled dependencies

            /*
                        // Ask the user what to do
                        string prompt = string.Empty;
                        foreach (string srv in depcoll)
                        {
                            using (ServiceController sc = new ServiceController(srv))
                            {
                                prompt += "- " + sc.DisplayName + " (" + sc.ServiceName + ")" + Environment.NewLine;
                            }
                        }

                        prompt = "Some of the services that TinyWall needs to function are disabled." + Environment.NewLine +
                            Environment.NewLine +
                            prompt + Environment.NewLine +
                            "Dou you want to enable these services? If you choose no, TinyWall will not function correctly.";
                        if (MessageBox.Show(prompt, "TinyWall requirements", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
             */
            {
                // Iterate through all needed (but disabled) services and enable them
                using (ScmWrapper.ServiceControlManager scm = new ScmWrapper.ServiceControlManager())
                {
                    foreach (string srv in depcoll)
                    {
                        scm.SetStartupMode(srv, ServiceStartMode.Manual);
                    }
                }
            }

        }

        private static void ScanServiceDependencies(ServiceController srv, List<string> allDeps)
        {
            if (allDeps.Contains(srv.ServiceName))
                return;

            allDeps.Add(srv.ServiceName);

            ServiceController[] ServicesDependedOn = srv.ServicesDependedOn;
            foreach (ServiceController depOn in ServicesDependedOn)
            {
                ScanServiceDependencies(depOn, allDeps);
            }
        }
    }
}
