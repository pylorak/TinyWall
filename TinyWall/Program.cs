using System;
using System.Configuration.Install;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;

namespace PKSoft
{
    static class Program
    {
        private static int StartDevelTool()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            System.Windows.Forms.Application.Run(new DevelToolForm());
            return 0;
        }
        
        private static int StartService()
        {
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
#else
                ServiceBase.Run(tw);
#endif
                return 0;
            }
        }

        private static int StartController(CmdLineArgs opts)
        {
#if !DEBUG
            #region See if the service is installed and running, and try to correct it if not

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
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            System.Windows.Forms.Application.Run(new MainForm(opts));
            return 0;
        }

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
            if (Utils.ArrayContains(args, "/desktop"))
                opts.ProgramMode = StartUpMode.Controller;
            if (Utils.ArrayContains(args, "/service"))
                opts.ProgramMode = StartUpMode.Service;
            if (Utils.ArrayContains(args, "/selfhosted"))
                opts.ProgramMode = StartUpMode.SelfHosted;
            if (Utils.ArrayContains(args, "/develtool"))
                opts.ProgramMode = StartUpMode.DevelTool;
            opts.detectnow = Utils.ArrayContains(args, "/detectnow");
            opts.autowhitelist = Utils.ArrayContains(args, "/autowhitelist");
            opts.updatenow = Utils.ArrayContains(args, "/updatenow");
            opts.install = Utils.ArrayContains(args, "/install");
            opts.uninstall = Utils.ArrayContains(args, "/uninstall");

            if (opts.ProgramMode == StartUpMode.Invalid)
                opts.ProgramMode = StartUpMode.Controller;

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

                // Remove user settings
                string UserDir = ControllerSettings.UserDataPath;
                Directory.Delete(UserDir, true);

                return 0;
            }
            #endregion

            switch (opts.ProgramMode)
            {
                case StartUpMode.Controller:
                    return StartController(opts);
                case StartUpMode.DevelTool:
                    return StartDevelTool();
                case StartUpMode.SelfHosted:
                    StartService();
                    Thread.Sleep(500);
                    return StartController(opts);
                case StartUpMode.Service:
                    return StartService();
                default:
                    return -1;
            } // switch

        } // Main
    } // class
} //namespace
