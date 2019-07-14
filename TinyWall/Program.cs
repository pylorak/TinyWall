using System;
using System.ServiceProcess;
using System.Threading;
using System.Diagnostics;
using System.Net;

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
        
        private static int StartService(TinyWallService tw)
        {
#if DEBUG
            if (!Utils.RunningAsAdmin())
            {
                Console.WriteLine("Error: Not started as an admin process.");
                return -1;
            }
#endif

            bool mutexok;
            using (Mutex SingleInstanceMutex = new Mutex(true, @"Global\TinyWallService", out mutexok))
            {
                if (!mutexok)
                {
                    return -1;
                }

                tw.ServiceName = TinyWallService.SERVICE_NAME;
                if (!EventLog.SourceExists("TinyWallService"))
                    EventLog.CreateEventSource("TinyWallService", null);
                tw.EventLog.Source = "TinyWallService";

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
            System.Windows.Forms.Application.Run(new TinyWallController(opts));
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
            /*
            DatabaseClasses.Application app = TinyWall.Interface.Internal.SerializationHelper.LoadFromXMLFile<DatabaseClasses.Application>(@"C:\Users\Dev\ownCloud\TinyWall\TinyWall3\TinyWall\Database\Special\Special File and printer sharing.xml2");
            //DatabaseClasses.Application app = new DatabaseClasses.Application();
            TinyWall.Interface.RuleListPolicy rp = new TinyWall.Interface.RuleListPolicy();
            rp.Rules = new System.Collections.Generic.List<TinyWall.Interface.RuleDef>();
            rp.Rules.Add(new TinyWall.Interface.RuleDef(Guid.NewGuid(), "Name", null, TinyWall.Interface.RuleAction.Allow, TinyWall.Interface.RuleDirection.Out, TinyWall.Interface.Protocol.UDP));
            rp.Rules.Add(new TinyWall.Interface.RuleDef());
            app.Components = new System.Collections.Generic.List<DatabaseClasses.SubjectIdentity>();
            app.Components.Add(new DatabaseClasses.SubjectIdentity(TinyWall.Interface.GlobalSubject.Instance));
            app.Components[0].Policy = rp;
            TinyWall.Interface.Internal.SerializationHelper.SaveToXMLFile(app, @"C:\Users\Dev\ownCloud\TinyWall\TinyWall3\TinyWall\Database\Special\Special File and printer sharing.xml3");
            */

#if DEBUG
            AppDomain.CurrentDomain.AssemblyLoad += new AssemblyLoadEventHandler(CurrentDomain_AssemblyLoad);
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
#endif

            // WerAddExcludedApplication will fail every time we are not running as admin, 
            // so wrap it around a try-catch.
            try
            {
                // Prevent Windows Error Reporting running for us
                NativeMethods.WerAddExcludedApplication(TinyWall.Interface.Internal.Utils.ExecutablePath, true);
            }
            catch { }

            // Setup TLS 1.2 & 1.3 support
            const SecurityProtocolType _tls12 = (SecurityProtocolType)3072;
            const SecurityProtocolType _tls13 = (SecurityProtocolType)12288;
            ServicePointManager.SecurityProtocol = _tls12 | _tls13 | SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;

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
                opts.ProgramMode = StartUpMode.Service;

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
                    using (TinyWallService srv = new TinyWallService())
                    {
                        StartService(srv);
                        int ret = StartController(opts);
                        srv.Stop();
                        return ret;
                    }
                case StartUpMode.Service:
                    using (TinyWallService srv = new TinyWallService())
                    {
                        StartService(srv);
#if DEBUG
                        Console.WriteLine("Press ENTER to end this process...");
                        Console.ReadLine();

                        try
                        {
                            PKSoft.WindowsFirewall.Policy Firewall = new PKSoft.WindowsFirewall.Policy();
                            Firewall.ResetFirewall();
                        }
                        catch { }
#endif

                    }
                    return 0;
                default:
                    return -1;
            } // switch
        } // Main

#if DEBUG
        static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assembly = args.Name;
            return null;
        }

        static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            string assembly = args.LoadedAssembly.FullName;
        }
#endif

    } // class
} //namespace
