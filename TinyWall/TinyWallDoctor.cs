using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.ServiceProcess;
using TinyWall.Interface;
using WFPdotNet;
using WFPdotNet.Interop;

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

        internal static bool IsServiceStopped()
        {
#if !DEBUG
            try
            {
                using (ServiceController sc = new ServiceController(TinyWallService.SERVICE_NAME))
                {
                    return (sc.Status == ServiceControllerStatus.Stopped);
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
                    ManagedInstallerClass.InstallHelper(new string[] { "/i", TinyWall.Interface.Internal.Utils.ExecutablePath });
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
                    using (Process p = Utils.StartProcess(TinyWall.Interface.Internal.Utils.ExecutablePath, "/install", true))
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
            using (System.Windows.Forms.Form frm = new System.Windows.Forms.Form())
            {
                // See http://www.codeproject.com/Articles/18612/TopMost-MessageBox
                // for an explanation as for why this is needed.
                frm.Size = new System.Drawing.Size(1, 1);
                frm.ShowInTaskbar = false;
                frm.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
                System.Drawing.Rectangle rect = System.Windows.Forms.SystemInformation.VirtualScreen;
                frm.Location = new System.Drawing.Point(rect.Bottom + 10, rect.Right + 10);
                frm.Show();
                frm.Focus();
                frm.BringToFront(); 
                frm.TopMost = true;

                if (System.Windows.Forms.MessageBox.Show(frm,
                    PKSoft.Resources.Messages.DidYouInitiateTheUninstall,
                    PKSoft.Resources.Messages.TinyWall,
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Exclamation) != System.Windows.Forms.DialogResult.Yes)
                {
                    return -1;
                }
            }

            // Stop service
            try
            {
                if (TinyWallDoctor.IsServiceRunning())
                {
                    using (Controller twController = new Controller("TinyWallController"))
                    {
                        // Unlock server
                        while (twController.IsServerLocked)
                        {
                            using (PasswordForm pf = new PasswordForm())
                            {
                                pf.BringToFront();
                                pf.Activate();
                                if (pf.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                                {
                                    twController.TryUnlockServer(pf.PassHash);
                                }
                                else
                                    return -1;
                            }
                        }

                        // Stop server
                        twController.RequestServerStop();
                        DateTime startTs = DateTime.Now;
                        while (!IsServiceStopped() && ((DateTime.Now - startTs) < TimeSpan.FromSeconds(5)))
                            System.Threading.Thread.Sleep(200);
                        if (!IsServiceStopped())
                            return -1;
                    }
                }
            }
            catch
            {
                return -1;
            }

            // Terminate remaining TinyWall processes (e.g. controller)
            {
                int ownPid = Process.GetCurrentProcess().Id;
                Process[] procs = Process.GetProcesses();
                foreach (Process p in procs)
                {
                    try
                    {
                        if (p.ProcessName.Contains("TinyWall") && (p.Id != ownPid))
                        {
                            ProcessManager.TerminateProcess(p, 2000);
                        }
                    }
                    catch { }
                }
            }

            // Remove persistent WFP objects
            using (var WfpEngine = new Engine("TinyWall Uninstall Session", "", FWPM_SESSION_FLAGS.None, 5000))
            using (Transaction trx = WfpEngine.BeginTransaction())
            {
                TinyWallServer.DeleteWfpObjects(WfpEngine, true);
                trx.Commit();
            }

            try
            {
                // Disable automatic start of controller
                Utils.RunAtStartup("TinyWall Controller", null);
            }
            catch { }

            try
            {
                // Put back the user's original hosts file
                HostsFileManager.DisableHostsFile();
            }
            catch { }

            try
            {
                ManagedInstallerClass.InstallHelper(new string[] { "/u", TinyWall.Interface.Internal.Utils.ExecutablePath });
            }
            catch { }

            try
            {
                // Remove user settings
                string UserDir = ControllerSettings.UserDataPath;
                Directory.Delete(UserDir, true);
            }
            catch { }

            return 0;
        }

        internal static void EnsureHealth()
        {
            // Ensure that TinyWall's dependencies can be started
            try
            {
                EnsureServiceDependencies();
            }
            catch { }

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

            // Ensure that controller will be started for users
            try
            {
                Utils.RunAtStartup("TinyWall Controller", TinyWall.Interface.Internal.Utils.ExecutablePath);
            }
            catch { }
        }

        private static void EnsureServiceDependencies()
        {
            // First, do a recursive scan of all service dependencies
            HashSet<string> deps = new HashSet<string>();
            foreach (string srv in TinyWallService.ServiceDependencies)
            {
                using (ServiceController sc = new ServiceController(srv))
                {
                    ScanServiceDependencies(sc, deps);
                }
            }

            // Enable services we need
            using (ScmWrapper.ServiceControlManager scm = new ScmWrapper.ServiceControlManager())
            {
                foreach (string srv in deps)
                {
                    if (scm.GetStartupMode(srv) == (uint)ServiceStartMode.Disabled)
                        scm.SetStartupMode(srv, ServiceStartMode.Manual);
                }
            }
        }

        private static void ScanServiceDependencies(ServiceController srv, HashSet<string> allDeps)
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
