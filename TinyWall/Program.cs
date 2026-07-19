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
        public enum ExitCode
        {
            Success = 0,
            GenericError,
            AlreadyRunning,
            BadArgs,
            ResxDiffers
        }

        internal static bool RestartOnQuit { get; set; }
        internal static System.Globalization.CultureInfo? DefaultOsCulture { get; set; }

        private static ExitCode RunStartupCommand(CmdLineArgs cliArgs)
        {
            switch (cliArgs.Command)
            {
                case StartupCommand.Install:
                    return TinyWallDoctor.EnsureServiceInstalledAndRunning(Utils.LOG_ID_INSTALLER, true) ? ExitCode.Success : ExitCode.GenericError;
                case StartupCommand.Uninstall:
                    return TinyWallDoctor.Uninstall() ? ExitCode.Success : ExitCode.GenericError;
                case StartupCommand.Controller:
                    return StartController(cliArgs);
                case StartupCommand.DevelTool:
                    return StartDevelTool();
                case StartupCommand.SelfHosted:
                    using (var srv = new TinyWallService())
                    {
                        StartService(srv);
                        var ret = StartController(cliArgs);
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
                        return ExitCode.Success;
                    }
                case StartupCommand.ProfileCreator:
                    {
                        var cmdArgs = cliArgs.ProfileCreator;
                        File.WriteAllText(cmdArgs.OutputFile.Value, DevelToolCli.CreateProfile(cmdArgs.ExecutablePath.Value!));
                        return ExitCode.Success;
                    }
                case StartupCommand.DatabaseCreator:
                    {
                        var cmdArgs = cliArgs.DatabaseCreator;
                        DevelToolCli.CreateDatabase(cmdArgs.SourceFolder.Value!, cmdArgs.OutputFolder.Value!);
                        return ExitCode.Success;
                    }
                case StartupCommand.UpdateCreator:
                    {
                        var cmdArgs = cliArgs.UpdateCreator;
                        DevelToolCli.CreateUpdate(cmdArgs.BaseUrl.Value!, cmdArgs.ProjectDir.Value!, cmdArgs.OutputFolder.Value!);
                        return ExitCode.Success;
                    }
                case StartupCommand.ResXOptimizer:
                    {
                        var cmdArgs = cliArgs.ResXOptimizer;
                        return DevelToolCli.OptimizeResX(DevelToolCli.CollectResxLocalizations(cmdArgs.ResourceDir.Value!), cmdArgs.OutputFolder.Value!, cmdArgs.Compare.Value)
                            ? ExitCode.Success
                            : ExitCode.ResxDiffers;
                    }
                case StartupCommand.BatchSigner:
                    {
                        var cmdArgs = cliArgs.BatchSigner;
                        if (Utils.IsNullOrEmpty(cmdArgs.CertificateName.Value) == Utils.IsNullOrEmpty(cmdArgs.PfxPath.Value))
                        {
                            Console.Error.WriteLine($"Either {cmdArgs.CertificateName.Name} or {cmdArgs.PfxPath.Name} is required.");
                            return ExitCode.BadArgs;
                        }
                        if (Utils.IsNullOrEmpty(cmdArgs.PfxPath.Value) != Utils.IsNullOrEmpty(cmdArgs.PfxPassword.Value))
                        {
                            Console.Error.WriteLine($"If either one of {cmdArgs.PfxPath.Name} or {cmdArgs.PfxPassword.Name} is provided, the other is also required.");
                            return ExitCode.BadArgs;
                        }
                        string signtoolPath = cmdArgs.SigntoolPath.Value ?? @"C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\signtool.exe";
                        string timestampUrl = cmdArgs.TimestampUrl.Value ?? "http://time.certum.pl/";
                        bool signSuccess = DevelToolCli.BatchSign(
                            cmdArgs.CertificateName.Value ?? string.Empty,
                            cmdArgs.SignDir.Value!,
                            signtoolPath,
                            timestampUrl,
                            cmdArgs.PfxPath.Value,
                            cmdArgs.PfxPassword.Value);
                        if (!signSuccess)
                        {
                            Console.Error.WriteLine("Some files couldn't be signed.");
                            return ExitCode.GenericError;
                        }
                        return ExitCode.Success;
                    }
                default:
                    throw new InvalidOperationException();
            }

            throw new InvalidOperationException();
        }

        private static ExitCode StartService(TinyWallService tw)
        {
#if DEBUG
            if (!Utils.RunningAsAdmin())
            {
                Console.WriteLine("Error: Not started as an admin process.");
                return ExitCode.GenericError;
            }
#endif

            using var SingleInstanceMutex = new Mutex(true, @"Global\TinyWallService", out bool mutexok);
            if (!mutexok)
            {
                return ExitCode.AlreadyRunning;
            }

#if DEBUG
            tw.Start(Array.Empty<string>());
            tw.StartedEvent.WaitOne();
#else
            pylorak.Windows.Services.ServiceBase.Run(tw);
#endif
            return ExitCode.Success;
        }

        private static ExitCode StartController(CmdLineArgs opts)
        {
            // Start controller application
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            do
            {
                RestartOnQuit = false;
                System.Windows.Forms.Application.Run(new TinyWallController(opts));
            } while (RestartOnQuit);
            return ExitCode.Success;
        }

        private static ExitCode StartDevelTool()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            System.Windows.Forms.Application.Run(new DevelToolForm());
            return ExitCode.Success;
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
            try { opts.ParseArgs(args); }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return (int)ExitCode.BadArgs;
            }

            // After this point the command mode is always valid,
            // guaranteed by the call to ParseArgs() above.
            if (opts.Command == StartupCommand.Invalid)
            {
                // Logic error. We should never get here.
                Console.Error.WriteLine("Unexpected error.");
                return (int)ExitCode.GenericError;
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

            return (int)RunStartupCommand(opts);
        } // Main

    } // class
} //namespace
