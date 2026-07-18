using pylorak.Utilities;
using pylorak.Windows;
using System;
using System.IO;
using System.Net;
using System.Threading;

namespace pylorak.TinyWall
{
    static class Program
    {
        internal static bool RestartOnQuit { get; set; }
        internal static System.Globalization.CultureInfo? DefaultOsCulture { get; set; }

        private static int RunStartupCommand(CmdLineArgs cliArgs)
        {
            switch (cliArgs.Command)
            {
                case StartupCommand.Install:
                    return TinyWallDoctor.EnsureServiceInstalledAndRunning(Utils.LOG_ID_INSTALLER, true) ? 0 : -1;
                case StartupCommand.Uninstall:
                    return TinyWallDoctor.Uninstall();
                case StartupCommand.Controller:
                    return StartController(cliArgs);
                case StartupCommand.DevelTool:
                    return StartDevelTool();
                case StartupCommand.SelfHosted:
                    using (var srv = new TinyWallService())
                    {
                        StartService(srv);
                        int ret = StartController(cliArgs);
                        srv.Stop();
                        srv.StoppedEvent.WaitOne();
                        return ret;
                    }
                case StartupCommand.Service:
                    using (var srv = new TinyWallService())
                    {
#if !DEBUG
                        pylorak.Windows.PathMapper.Instance.AutoUpdate = false;
#endif
                        StartService(srv);
#if DEBUG
                        Console.WriteLine("Kill process to terminate...");
                        srv.StoppedEvent.WaitOne();
#endif
                        return 0;
                    }
                case StartupCommand.ProfileCreator:
                    if (Utils.IsNullOrEmpty(cliArgs.ExecutablePath))
                    {
                        Console.Error.WriteLine("Error: /executable-path is required.");
                        return 1;
                    }
                    if (Utils.IsNullOrEmpty(cliArgs.OutputFile))
                    {
                        Console.Error.WriteLine("Error: /output-file is required.");
                        return 1;
                    }
                    File.WriteAllText(cliArgs.OutputFile, DevelToolCli.CreateProfile(cliArgs.ExecutablePath));
                    return 0;
                case StartupCommand.DatabaseCreator:
                    if (Utils.IsNullOrEmpty(cliArgs.SourceFolder) || Utils.IsNullOrEmpty(cliArgs.OutputFolder))
                    {
                        Console.Error.WriteLine("Error: /source-folder and /output-folder are required.");
                        return 1;
                    }
                    DevelToolCli.CreateDatabase(cliArgs.SourceFolder, cliArgs.OutputFolder);
                    return 0;
                case StartupCommand.UpdateCreator:
                    if (Utils.IsNullOrEmpty(cliArgs.BaseUrl) || Utils.IsNullOrEmpty(cliArgs.ProjectDir) || Utils.IsNullOrEmpty(cliArgs.OutputFolder))
                    {
                        Console.Error.WriteLine("Error: /base-url, /project-dir and /output-folder are required.");
                        return 1;
                    }
                    DevelToolCli.CreateUpdate(cliArgs.BaseUrl, cliArgs.ProjectDir, cliArgs.OutputFolder);
                    return 0;
                case StartupCommand.ResXOptimizer:
                    if (Utils.IsNullOrEmpty(cliArgs.ResourceDir) || Utils.IsNullOrEmpty(cliArgs.OutputFolder))
                    {
                        Console.Error.WriteLine("Error: /resource-dir and /output-folder are required.");
                        return 1;
                    }
                    DevelToolCli.OptimizeResX(DevelToolCli.CollectResxLocalizations(cliArgs.ResourceDir), cliArgs.OutputFolder);
                    return 0;
                case StartupCommand.BatchSigner:
                    if (Utils.IsNullOrEmpty(cliArgs.CertificateName) && Utils.IsNullOrEmpty(cliArgs.PfxPath))
                    {
                        Console.Error.WriteLine("Error: /certificate-name or /pfx-path is required.");
                        return 1;
                    }
                    if (!Utils.IsNullOrEmpty(cliArgs.PfxPath) && Utils.IsNullOrEmpty(cliArgs.PfxPassword))
                    {
                        Console.Error.WriteLine("Error: /pfx-password is required if /pfx-path is provided.");
                        return 1;
                    }
                    if (Utils.IsNullOrEmpty(cliArgs.SignDir))
                    {
                        Console.Error.WriteLine("Error: /sign-dir is required.");
                        return 1;
                    }
                    string signtoolPath = cliArgs.SigntoolPath ?? @"C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\signtool.exe";
                    string timestampUrl = cliArgs.TimestampUrl ?? "http://time.certum.pl/";
                    bool signSuccess = DevelToolCli.BatchSign(
                        cliArgs.CertificateName ?? string.Empty,
                        cliArgs.SignDir,
                        signtoolPath,
                        timestampUrl,
                        pfxPath: cliArgs.PfxPath,
                        pfxPassword: cliArgs.PfxPassword);
                    if (!signSuccess)
                    {
                        Console.Error.WriteLine("Some files couldn't be signed.");
                        return 1;
                    }
                    return 0;
                default:
                    throw new InvalidOperationException();
            }

            throw new InvalidOperationException();
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

            using var SingleInstanceMutex = new Mutex(true, @"Global\TinyWallService", out bool mutexok);
            if (!mutexok)
            {
                return -1;
            }

#if DEBUG
            tw.Start(Array.Empty<string>());
            tw.StartedEvent.WaitOne();
#else
            pylorak.Windows.Services.ServiceBase.Run(tw);
#endif
            return 0;
        }

        private static int StartController(CmdLineArgs opts)
        {
            // Start controller application
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            do
            {
                RestartOnQuit = false;
                System.Windows.Forms.Application.Run(new TinyWallController(opts));
            } while (RestartOnQuit);
            return 0;
        }

        private static int StartDevelTool()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            System.Windows.Forms.Application.Run(new DevelToolForm());
            return 0;
        }

        private static void ParseArgs(CmdLineArgs ret, string[] args)
        {
            // Index of argument we are currently procesing
            var i = 0;

            // Determine the main command first. This is done separately from the rest of the args
            // because their validation could depend on the selected command.
            if (args.Length != 0)
            {
                // Leading forward slash is optional for command arg.
                string command = args[0].ToLowerInvariant();
                if (command.StartsWith("/")) command = command.Substring(1);
                ret.Command = command switch
                {
                    "service" => StartupCommand.Service,
                    "controller" => StartupCommand.Controller,
                    "selfhosted" => StartupCommand.SelfHosted,
                    "develtool" => StartupCommand.DevelTool,
                    "install" => StartupCommand.Install,
                    "uninstall" => StartupCommand.Uninstall,
                    "profile-creator" => StartupCommand.ProfileCreator,
                    "database-creator" => StartupCommand.DatabaseCreator,
                    "update-creator" => StartupCommand.UpdateCreator,
                    "resx-optimizer" => StartupCommand.ResXOptimizer,
                    "batch-signer" => StartupCommand.BatchSigner,
                    _ => StartupCommand.Invalid
                };
            }

            if (ret.Command == StartupCommand.Invalid)
            {
                // No command was defined, so we select a fallback default
                ret.Command = Environment.UserInteractive ? StartupCommand.Controller : StartupCommand.Service;
            }
            else
            {
                // There was a command, so advance to the next arg
                ++i;
            }

            // Parse the rest of the arguments
            for (; i < args.Length; ++i)
            {
                string arg = args[i].ToLowerInvariant();

                // Check if next token exists and is not another flag
                bool hasNextValue = i + 1 < args.Length;

                switch (arg)
                {
                    case "/autowhitelist":
                        ret.autowhitelist = true;
                        break;
                    case "/updatenow":  // TODO: is this used? Do we still need this option?
                        ret.updatenow = true;
                        break;
                    case "/startup":  // TODO: is this used? Do we still need this option?
                        ret.startup = true;
                        break;

                    case "/executable-path" when hasNextValue:
                        ret.ExecutablePath = args[++i];
                        break;
                    case "/output-file" when hasNextValue:
                        ret.OutputFile = args[++i];
                        break;

                    case "/source-folder" when hasNextValue:
                        ret.SourceFolder = args[++i];
                        break;
                    case "/output-folder" when hasNextValue:
                        ret.OutputFolder = args[++i];
                        break;

                    case "/base-url" when hasNextValue:
                        ret.BaseUrl = args[++i];
                        break;
                    case "/project-dir" when hasNextValue:
                        ret.ProjectDir = args[++i];
                        break;

                    case "/resource-dir" when hasNextValue:
                        ret.ResourceDir = args[++i];
                        break;

                    case "/certificate-name" when hasNextValue:
                        ret.CertificateName = args[++i];
                        break;
                    case "/pfx-path" when hasNextValue:
                        ret.PfxPath = args[++i];
                        break;
                    case "/pfx-password" when hasNextValue:
                        ret.PfxPassword = args[++i];
                        break;
                    case "/sign-dir" when hasNextValue:
                        ret.SignDir = args[++i];
                        break;
                    case "/signtool-path" when hasNextValue:
                        ret.SigntoolPath = args[++i];
                        break;
                    case "/timestamp-url" when hasNextValue:
                        ret.TimestampUrl = args[++i];
                        break;

                    default:
                        throw new ArgumentException($"Invalid commandline switch \"{arg}\" or missing argument.");
                }
            }
        }

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            using var parentConsole = new AttachedParentConsole();

            HierarchicalStopwatch.Enable = File.Exists(Path.Combine(Utils.AppDataPath, "enable-timings"));
            HierarchicalStopwatch.LogFileBase = Path.Combine(Utils.AppDataPath, @"logs\timings");

            DefaultOsCulture ??= Thread.CurrentThread.CurrentUICulture;

            // WerAddExcludedApplication will fail every time we are not running as admin,
            // so wrap it around a try-catch.
            try
            {
                // Prevent Windows Error Reporting running for us
                Utils.SafeNativeMethods.WerAddExcludedApplication(Utils.ExecutablePath, true);
            }
            catch { }

            // Setup TLS 1.2 & 1.3 support, if supported
            if (ServicePointManager.SecurityProtocol != 0)
            {
                try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
                try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls13; } catch { }
            }

            // Parse comman-line options
            var opts = new CmdLineArgs();
            try { ParseArgs(opts, args); }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return -1;
            }

            // After this point the command mode is always valid,
            // guaranteed by the call to ParseArgs() above.
            if (opts.Command == StartupCommand.Invalid)
            {
                // Logic error. We should never get here.
                Console.Error.WriteLine("Invalid command argument.");
                return -1;
            }

#if !DEBUG
            // Register an unhandled exception handler - lol

            void UnhandledException_Gui(object sender, UnhandledExceptionEventArgs e)
            {
                Utils.LogException((Exception)e.ExceptionObject, Utils.LOG_ID_GUI);
            }
            void UnhandledException_Cli(object sender, UnhandledExceptionEventArgs e)
            {
                Utils.LogException((Exception)e.ExceptionObject, Utils.LOG_ID_CLI);
            }
            void UnhandledException_Service(object sender, UnhandledExceptionEventArgs e)
            {
                Utils.LogException((Exception)e.ExceptionObject, Utils.LOG_ID_SERVICE);
            }
            void UnhandledException_Installer(object sender, UnhandledExceptionEventArgs e)
            {
                Utils.LogException((Exception)e.ExceptionObject, Utils.LOG_ID_INSTALLER);
            }

            switch (opts.Command)
            {
                case StartupCommand.Install:
                case StartupCommand.Uninstall:
                    AppDomain.CurrentDomain.UnhandledException += UnhandledException_Installer;
                    break;
                case StartupCommand.Controller:
                case StartupCommand.DevelTool:
                    AppDomain.CurrentDomain.UnhandledException += UnhandledException_Gui;
                    break;
                case StartupCommand.SelfHosted:
                    AppDomain.CurrentDomain.UnhandledException += UnhandledException_Gui;
                    AppDomain.CurrentDomain.UnhandledException += UnhandledException_Service;
                    break;
                case StartupCommand.Service:
                    AppDomain.CurrentDomain.UnhandledException += UnhandledException_Service;
                    break;
                case StartupCommand.ProfileCreator:
                case StartupCommand.DatabaseCreator:
                case StartupCommand.UpdateCreator:
                case StartupCommand.ResXOptimizer:
                case StartupCommand.BatchSigner:
                    AppDomain.CurrentDomain.UnhandledException += UnhandledException_Cli;
                    break;
                default:
                    throw new InvalidOperationException();
            }
#endif

            return RunStartupCommand(opts);
        } // Main

    } // class
} //namespace
