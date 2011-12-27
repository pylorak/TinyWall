using System;
using System.Diagnostics;
using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;

namespace PKSoft
{
    static class Program
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            // WerAddExcludedApplication will fail every time we are not running as admin, 
            // so wrap it around a try-catch.
            try
            {
                // Prevent Windows Error Reporting running for us
                NativeMethods.WerAddExcludedApplication(Utils.ExecutablePath, true);
            }
            catch { }

            // Parse comman-line options
            CmdLineArgs opts = new CmdLineArgs();
            opts.desktop = Utils.ArrayContains(args, "/desktop");
            opts.detectnow = Utils.ArrayContains(args, "/detectnow");
            opts.updatenow = Utils.ArrayContains(args, "/updatenow");
            opts.service = Utils.ArrayContains(args, "/service");
            opts.install = Utils.ArrayContains(args, "/install");
            opts.uninstall = Utils.ArrayContains(args, "/uninstall");
            opts.develtool = Utils.ArrayContains(args, "/develtool");

            #region /develtool
            if (opts.develtool)
            {
                // Start builtin tool
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new DevelToolForm());
                return 0;
            }
            #endregion

            #region /install
            if (opts.install)
            {
                // Install service
                try
                {
                    ManagedInstallerClass.InstallHelper(new string[] { "/i", Utils.ExecutablePath });
                }
                catch { }

                // Install service
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
                            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
                        }
                    }
                }
                catch 
                {
                    string msg = "The TinyWall Service could not be started. Please ensure that the Windows Firewall service and TinyWall Service are not in the disabled state and that both can be started. "+
                    "If you cannot start Windows Firewall, another software may be preventing its use. In that case make sure that no other firewall products are installed and that your system is clean from viruses.";
                    MessageBox.Show(msg, "TinyWall startup error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return -1;
                }

                return 0;
            }
            #endregion

            #region /uninstall
            if (opts.uninstall)
            {
                // Uninstall registry key
                Utils.RunAtStartup("TinyWall Controller", null);

                // Uninstall service
                try
                {
                    ManagedInstallerClass.InstallHelper(new string[] { "/u", Utils.ExecutablePath });
                }
                catch { }

                return 0;
            }
            #endregion

            if ((opts.service == true) && (opts.desktop == true))
            {
                Console.WriteLine("Invalid combination of command line options.");
                return -1;
            }

            if ((opts.service == false) && (opts.desktop == false))
            {
                opts.desktop = true;
            }

            if (opts.desktop)
            {
                #region /desktop
#if !DEBUG
                #region See if the service is installed and running, and try to correct if not

                bool isRunning;
                try
                {
                    using (ServiceController sc = new ServiceController(TinyWallService.SERVICE_NAME))
                    {
                        isRunning = (sc.Status == ServiceControllerStatus.Running) || (sc.Status == ServiceControllerStatus.StartPending);
                    }
                }
                catch
                {
                    isRunning = false;
                }

                if (!isRunning)
                {
                    try
                    {
                        Process p = Utils.StartProcess(Utils.ExecutablePath, "/install", true);
                        p.WaitForExit();
                    }
                    catch { }
                }

                #endregion
#endif

                // Start controller application
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm(opts));
                return 0;
                #endregion
            }
            else
            {
                #region /service
                // Start service

                bool mutexok;
                using (Mutex SingleInstanceMutex = new Mutex(true, @"Global\TinyWallService", out mutexok))
                {
                    if (!mutexok)
                    {
                        return -1;
                    }

                    TinyWallService tw = new TinyWallService();

#if DEBUG
                tw.Start(null);
                for (; ; )
                    Thread.Sleep(10000000);
#endif

                    ServiceBase.Run(tw);
                    return 0;
                }
                #endregion
            } // if
        } // Main
    } // class
} //namespace
