using System;
using System.ServiceProcess;
using System.Threading;

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
            // Start controller application
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            System.Windows.Forms.Application.Run(new MainForm(opts));
            return 0;
        }

        private static int InstallService()
        {
            return TinyWallDoctor.EnsureServiceInstalledAndRunning() ? 0 : -1;
        }

        private static int UninstallService()
        {
            return TinyWallDoctor.Uninstall();
        }

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
#if DEBUG
            AppDomain.CurrentDomain.AssemblyLoad += new AssemblyLoadEventHandler(CurrentDomain_AssemblyLoad);
#endif

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
            if (!Environment.UserInteractive || Utils.ArrayContains(args, "/service"))
                opts.ProgramMode = StartUpMode.Service;
            if (Utils.ArrayContains(args, "/selfhosted"))
                opts.ProgramMode = StartUpMode.SelfHosted;
            if (Utils.ArrayContains(args, "/develtool"))
                opts.ProgramMode = StartUpMode.DevelTool;
            opts.autowhitelist = Utils.ArrayContains(args, "/autowhitelist");
            opts.updatenow = Utils.ArrayContains(args, "/updatenow");
            opts.install = Utils.ArrayContains(args, "/install");
            opts.uninstall = Utils.ArrayContains(args, "/uninstall");

            if (opts.ProgramMode == StartUpMode.Invalid)
                opts.ProgramMode = StartUpMode.Controller;

            if (opts.install)
            {
                return InstallService();
            }

            if (opts.uninstall)
            {
                return UninstallService();
            }

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
                    StartService();
#if DEBUG
                    while (true)
                        Thread.Sleep(500);
#endif
                    return 0;
                default:
                    return -1;
            } // switch

        } // Main

        static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            string assembly = args.LoadedAssembly.FullName;
        }

    } // class
} //namespace
