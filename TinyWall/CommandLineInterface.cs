using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace pylorak.TinyWall
{
    internal static class CommandLineInterface
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        private const int ATTACH_PARENT_PROCESS = -1;

        private static void EnsureConsoleAttached()
        {
            AttachConsole(ATTACH_PARENT_PROCESS);
        }

        private static void WriteOutput(object obj, bool asJson)
        {
            EnsureConsoleAttached();
            if (asJson)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.WriteLine(JsonSerializer.Serialize(obj, options));
            }
            else
            {
                if (obj is string s)
                    Console.WriteLine(s);
                else
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    Console.WriteLine(JsonSerializer.Serialize(obj, options));
                }
            }
        }

        private static void WriteError(string message, bool asJson)
        {
            EnsureConsoleAttached();
            if (asJson)
            {
                var res = new { success = false, error = message };
                Console.WriteLine(JsonSerializer.Serialize(res));
            }
            else
            {
                Console.Error.WriteLine($"[TinyWall CLI Error] {message}");
            }
        }

        public static int Run(string[] args)
        {
            if (args.Length == 0 || args[0].Equals("help", StringComparison.OrdinalIgnoreCase) || args[0].Equals("--help", StringComparison.OrdinalIgnoreCase))
            {
                ShowHelp();
                return 0;
            }

            bool asJson = args.Any(a => a.Equals("--json", StringComparison.OrdinalIgnoreCase));
            string subCommand = args[0].ToLowerInvariant();

            try
            {
                var controller = new Controller("TinyWallController");

                switch (subCommand)
                {
                    case "status":
                    case "mode":
                        return HandleMode(controller, args, asJson);

                    case "config":
                        return HandleConfig(controller, args, asJson);

                    case "special":
                        return HandleSpecial(controller, args, asJson);

                    case "rule":
                    case "rules":
                        return HandleRules(controller, args, asJson);

                    case "export":
                        return HandleExport(controller, args, asJson);

                    case "import":
                        return HandleImport(controller, args, asJson);

                    case "log":
                    case "logs":
                        return HandleLogs(controller, args, asJson);

                    default:
                        WriteError($"Unknown CLI command '{subCommand}'. Run 'TinyWall.exe cli help' for usage.", asJson);
                        return 1;
                }
            }
            catch (Exception ex)
            {
                WriteError(ex.Message, asJson);
                return 1;
            }
        }

        // =========================================================================
        // 1. MODE & STATUS
        // =========================================================================
        private static int HandleMode(Controller controller, string[] args, bool asJson)
        {
            Guid changeset = Guid.Empty;
            var ret = controller.GetServerConfig(out var config, out var state, ref changeset);
            if (ret == MessageType.COM_ERROR || config == null || state == null)
            {
                WriteError("Could not connect to TinyWall Service. Is the TinyWall service running?", asJson);
                return 1;
            }

            var nonFlags = args.Where(a => !a.StartsWith("--")).ToArray();
            if (nonFlags.Length == 1 || (nonFlags.Length >= 2 && nonFlags[1].Equals("get", StringComparison.OrdinalIgnoreCase)))
            {
                var result = new
                {
                    success = true,
                    mode = state.Mode.ToString(),
                    isLocked = state.Locked,
                    hasPassword = state.HasPassword,
                    activeProfile = config.ActiveProfileName
                };
                WriteOutput(result, asJson);
                return 0;
            }

            string targetModeStr = nonFlags[1].Equals("set", StringComparison.OrdinalIgnoreCase) && nonFlags.Length > 2 ? nonFlags[2] : nonFlags[1];

            FirewallMode newMode;
            switch (targetModeStr.ToLowerInvariant())
            {
                case "normal":
                    newMode = FirewallMode.Normal;
                    break;
                case "blockall":
                case "block-all":
                    newMode = FirewallMode.BlockAll;
                    break;
                case "allowoutgoing":
                case "allow-outgoing":
                    newMode = FirewallMode.AllowOutgoing;
                    break;
                case "disabled":
                    newMode = FirewallMode.Disabled;
                    break;
                case "learning":
                case "autolearn":
                    newMode = FirewallMode.Learning;
                    break;
                default:
                    WriteError($"Invalid firewall mode '{targetModeStr}'. Valid modes: normal, blockall, allowoutgoing, disabled, autolearn", asJson);
                    return 1;
            }

            var switchRet = controller.SwitchFirewallMode(newMode);
            if (switchRet == MessageType.RESPONSE_LOCKED)
            {
                WriteError("TinyWall is password locked. Mode switch denied.", asJson);
                return 1;
            }
            else if (switchRet == MessageType.COM_ERROR)
            {
                WriteError("Communication error while setting firewall mode.", asJson);
                return 1;
            }

            WriteOutput(new { success = true, mode = newMode.ToString(), message = $"Firewall mode set to {newMode}." }, asJson);
            return 0;
        }

        // =========================================================================
        // 2. CONFIG (GLOBAL OPTIONS)
        // =========================================================================
        private static int HandleConfig(Controller controller, string[] args, bool asJson)
        {
            Guid changeset = Guid.Empty;
            var ret = controller.GetServerConfig(out var config, out var state, ref changeset);
            if (ret == MessageType.COM_ERROR || config == null || state == null)
            {
                WriteError("Could not connect to TinyWall Service.", asJson);
                return 1;
            }

            var nonFlags = args.Where(a => !a.StartsWith("--")).ToArray();
            bool isSet = nonFlags.Length >= 2 && nonFlags[1].Equals("set", StringComparison.OrdinalIgnoreCase);

            if (!isSet)
            {
                var info = new
                {
                    success = true,
                    allowLocalSubnet = config.ActiveProfile.AllowLocalSubnet,
                    displayOffBlock = config.ActiveProfile.DisplayOffBlock,
                    enableBlocklists = config.Blocklists.EnableBlocklists,
                    enableHostsBlocklist = config.Blocklists.EnableHostsBlocklist,
                    enablePortBlocklist = config.Blocklists.EnablePortBlocklist,
                    lockHostsFile = config.LockHostsFile,
                    startupMode = config.StartupMode.ToString(),
                    autoUpdateCheck = config.AutoUpdateCheck
                };
                WriteOutput(info, asJson);
                return 0;
            }

            var copyConfig = Utils.DeepClone(config);
            bool modified = false;

            string? allowSubnet = GetArgValue(args, "--allow-local-subnet");
            if (bool.TryParse(allowSubnet, out bool bSubnet)) { copyConfig.ActiveProfile.AllowLocalSubnet = bSubnet; modified = true; }

            string? dispOff = GetArgValue(args, "--display-off-block");
            if (bool.TryParse(dispOff, out bool bDispOff)) { copyConfig.ActiveProfile.DisplayOffBlock = bDispOff; modified = true; }

            string? blocklists = GetArgValue(args, "--blocklists");
            if (bool.TryParse(blocklists, out bool bBlocklists)) { copyConfig.Blocklists.EnableBlocklists = bBlocklists; modified = true; }

            string? hostsBlock = GetArgValue(args, "--hosts-blocklist");
            if (bool.TryParse(hostsBlock, out bool bHostsBlock)) { copyConfig.Blocklists.EnableHostsBlocklist = bHostsBlock; modified = true; }

            string? lockHosts = GetArgValue(args, "--lock-hosts");
            if (bool.TryParse(lockHosts, out bool bLockHosts)) { copyConfig.LockHostsFile = bLockHosts; modified = true; }

            if (!modified)
            {
                WriteError("No valid configuration option specified to update.", asJson);
                return 1;
            }

            var putResp = controller.SetServerConfig(copyConfig, changeset);
            if (putResp.Type == MessageType.RESPONSE_LOCKED)
            {
                WriteError("TinyWall is password locked. Configuration modification denied.", asJson);
                return 1;
            }

            WriteOutput(new { success = true, message = "Configuration options updated successfully." }, asJson);
            return 0;
        }

        // =========================================================================
        // 3. SPECIAL EXCEPTIONS (BUILT-IN PROFILES)
        // =========================================================================
        private static int HandleSpecial(Controller controller, string[] args, bool asJson)
        {
            Guid changeset = Guid.Empty;
            var ret = controller.GetServerConfig(out var config, out var state, ref changeset);
            if (ret == MessageType.COM_ERROR || config == null || state == null)
            {
                WriteError("Could not connect to TinyWall Service.", asJson);
                return 1;
            }

            var nonFlags = args.Where(a => !a.StartsWith("--")).ToArray();
            string action = nonFlags.Length >= 2 ? nonFlags[1].ToLowerInvariant() : "list";

            EnsureAppDatabaseLoaded();

            switch (action)
            {
                case "list":
                    {
                        var specialApps = GlobalInstances.AppDatabase.KnownApplications
                            .Where(app => app.HasFlag("TWUI:Special") && !app.HasFlag("TWUI:Hidden"))
                            .Select(app => new
                            {
                                id = app.Name,
                                displayName = !string.IsNullOrEmpty(app.LocalizedName) ? app.LocalizedName : app.Name.Replace('_', ' '),
                                isRecommended = app.HasFlag("TWUI:Recommended") ? true : false,
                                isEnabled = config.ActiveProfile.HasSpecialException(app.Name)
                            }).ToList();

                        WriteOutput(new { success = true, total = specialApps.Count, specials = specialApps }, asJson);
                        return 0;
                    }

                case "enable":
                case "disable":
                    {
                        string? name = GetArgValue(args, "--name");
                        if (string.IsNullOrEmpty(name))
                        {
                            WriteError("Option --name is required for special exception action.", asJson);
                            return 1;
                        }

                        var match = GlobalInstances.AppDatabase.KnownApplications
                            .FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                                                 (!string.IsNullOrEmpty(a.LocalizedName) && a.LocalizedName.Equals(name, StringComparison.OrdinalIgnoreCase)));

                        string targetId = match != null ? match.Name : name;
                        var copyConfig = Utils.DeepClone(config);

                        if (action == "enable")
                        {
                            if (!copyConfig.ActiveProfile.SpecialExceptions.Any(s => s.Equals(targetId, StringComparison.OrdinalIgnoreCase)))
                                copyConfig.ActiveProfile.SpecialExceptions.Add(targetId);
                        }
                        else
                        {
                            copyConfig.ActiveProfile.SpecialExceptions.RemoveAll(s => s.Equals(targetId, StringComparison.OrdinalIgnoreCase));
                        }

                        var putResp = controller.SetServerConfig(copyConfig, changeset);
                        if (putResp.Type == MessageType.RESPONSE_LOCKED)
                        {
                            WriteError("TinyWall is password locked. Special exception modification denied.", asJson);
                            return 1;
                        }

                        WriteOutput(new { success = true, action = action, special = targetId }, asJson);
                        return 0;
                    }

                default:
                    WriteError($"Unknown special action '{action}'. Valid actions: list, enable, disable", asJson);
                    return 1;
            }
        }

        // =========================================================================
        // 4. RULES (EXE, SERVICES, UWP, TIMERS, PORTS)
        // =========================================================================
        private static int HandleRules(Controller controller, string[] args, bool asJson)
        {
            if (args.Length < 2)
            {
                WriteError("Missing rule action. Usage: TinyWall.exe cli rule [list|add|block|add-service|add-uwp|list-uwp|remove]", asJson);
                return 1;
            }

            string action = args[1].ToLowerInvariant();
            Guid changeset = Guid.Empty;
            var ret = controller.GetServerConfig(out var config, out var state, ref changeset);
            if (ret == MessageType.COM_ERROR || config == null || state == null)
            {
                WriteError("Could not connect to TinyWall Service.", asJson);
                return 1;
            }

            switch (action)
            {
                case "list":
                    {
                        var ruleItems = config.ActiveProfile.AppExceptions.Select(e => new
                        {
                            id = e.Id.ToString(),
                            subjectType = e.Subject?.SubjectType.ToString(),
                            name = e.Subject?.ToString(),
                            path = (e.Subject is ExecutableSubject exe) ? exe.ExecutablePath : null,
                            service = (e.Subject is ServiceSubject srv) ? srv.ServiceName : null,
                            packageSid = (e.Subject is AppContainerSubject app) ? app.Sid : null,
                            packageDisplayName = (e.Subject is AppContainerSubject app2) ? app2.DisplayName : null,
                            policy = e.Policy?.PolicyType.ToString(),
                            timer = e.Timer.ToString(),
                            creationDate = e.CreationDate.ToString("o")
                        }).ToList();

                        WriteOutput(new { success = true, total = ruleItems.Count, rules = ruleItems }, asJson);
                        return 0;
                    }

                case "list-uwp":
                    {
                        string? filter = GetArgValue(args, "--filter");
                        var uwpList = new UwpPackageList();
                        var items = uwpList.Where(p => string.IsNullOrEmpty(filter) ||
                                                       (p.DisplayName != null && p.DisplayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                                       (p.Name != null && p.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
                                           .Select(p => new
                                           {
                                               name = p.Name,
                                               displayName = p.DisplayName,
                                               sid = p.Sid,
                                               publisher = p.Publisher
                                           }).ToList();

                        WriteOutput(new { success = true, total = items.Count, packages = items }, asJson);
                        return 0;
                    }

                case "add":
                case "allow":
                    {
                        string? path = GetArgValue(args, "--path");
                        if (string.IsNullOrEmpty(path))
                        {
                            WriteError("Option --path is required to add an executable rule.", asJson);
                            return 1;
                        }

                        if (!File.Exists(path))
                        {
                            WriteError($"File not found: '{path}'", asJson);
                            return 1;
                        }

                        var policy = BuildPolicy(args);
                        var subject = new ExecutableSubject(Path.GetFullPath(path));
                        var exception = new FirewallExceptionV3(subject, policy)
                        {
                            Timer = ParseTimer(GetArgValue(args, "--timer"))
                        };

                        return CommitException(controller, config, changeset, exception, path, asJson);
                    }

                case "add-service":
                    {
                        string? serviceName = GetArgValue(args, "--name");
                        if (string.IsNullOrEmpty(serviceName))
                        {
                            WriteError("Option --name is required for add-service.", asJson);
                            return 1;
                        }

                        string serviceExe = GetServiceExecutablePath(serviceName);
                        if (string.IsNullOrEmpty(serviceExe) || !File.Exists(serviceExe))
                        {
                            WriteError($"Could not resolve binary path for Windows Service '{serviceName}'. Is the service registered?", asJson);
                            return 1;
                        }

                        var policy = BuildPolicy(args);
                        var subject = new ServiceSubject(serviceExe, serviceName);
                        var exception = new FirewallExceptionV3(subject, policy)
                        {
                            Timer = ParseTimer(GetArgValue(args, "--timer"))
                        };

                        return CommitException(controller, config, changeset, exception, $"{serviceName} ({serviceExe})", asJson);
                    }

                case "add-uwp":
                    {
                        string? pkgQuery = GetArgValue(args, "--package") ?? GetArgValue(args, "--name");
                        if (string.IsNullOrEmpty(pkgQuery))
                        {
                            WriteError("Option --package is required for add-uwp.", asJson);
                            return 1;
                        }

                        var uwpList = new UwpPackageList();
                        var pkg = uwpList.FirstOrDefault(p => p.Name.Equals(pkgQuery, StringComparison.OrdinalIgnoreCase) ||
                                                              p.DisplayName.Equals(pkgQuery, StringComparison.OrdinalIgnoreCase) ||
                                                              p.Sid.Equals(pkgQuery, StringComparison.OrdinalIgnoreCase));

                        if (string.IsNullOrEmpty(pkg.Sid))
                        {
                            WriteError($"UWP Package '{pkgQuery}' not found.", asJson);
                            return 1;
                        }

                        var policy = BuildPolicy(args);
                        var subject = new AppContainerSubject(pkg.Sid, pkg.DisplayName, pkg.Publisher, pkg.PublisherId);
                        var exception = new FirewallExceptionV3(subject, policy)
                        {
                            Timer = ParseTimer(GetArgValue(args, "--timer"))
                        };

                        return CommitException(controller, config, changeset, exception, pkg.DisplayName, asJson);
                    }

                case "block":
                    {
                        string? path = GetArgValue(args, "--path");
                        if (string.IsNullOrEmpty(path))
                        {
                            WriteError("Option --path is required to block a program.", asJson);
                            return 1;
                        }

                        if (!File.Exists(path))
                        {
                            WriteError($"File not found: '{path}'", asJson);
                            return 1;
                        }

                        var policy = HardBlockPolicy.Instance;
                        var subject = new ExecutableSubject(Path.GetFullPath(path));
                        var exception = new FirewallExceptionV3(subject, policy);

                        return CommitException(controller, config, changeset, exception, path, asJson);
                    }

                case "remove":
                case "delete":
                    {
                        string? path = GetArgValue(args, "--path");
                        string? idStr = GetArgValue(args, "--id");
                        string? service = GetArgValue(args, "--service");

                        if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(idStr) && string.IsNullOrEmpty(service))
                        {
                            WriteError("Either --path, --service, or --id is required to remove a rule.", asJson);
                            return 1;
                        }

                        var copyConfig = Utils.DeepClone(config);
                        int initialCount = copyConfig.ActiveProfile.AppExceptions.Count;

                        if (!string.IsNullOrEmpty(idStr) && Guid.TryParse(idStr, out Guid idGuid))
                        {
                            copyConfig.ActiveProfile.AppExceptions.RemoveAll(e => e.Id == idGuid);
                        }
                        else if (!string.IsNullOrEmpty(service))
                        {
                            copyConfig.ActiveProfile.AppExceptions.RemoveAll(e =>
                                e.Subject is ServiceSubject srv &&
                                string.Equals(srv.ServiceName, service, StringComparison.OrdinalIgnoreCase)
                            );
                        }
                        else if (!string.IsNullOrEmpty(path))
                        {
                            string normPath = Path.GetFullPath(path);
                            copyConfig.ActiveProfile.AppExceptions.RemoveAll(e =>
                                e.Subject is ExecutableSubject exe &&
                                string.Equals(exe.ExecutablePath, normPath, StringComparison.OrdinalIgnoreCase)
                            );
                        }

                        int removedCount = initialCount - copyConfig.ActiveProfile.AppExceptions.Count;
                        if (removedCount == 0)
                        {
                            WriteError("No matching rule found to remove.", asJson);
                            return 1;
                        }

                        var putResp = controller.SetServerConfig(copyConfig, changeset);
                        if (putResp.Type == MessageType.RESPONSE_LOCKED)
                        {
                            WriteError("TinyWall is password locked. Rule modification denied.", asJson);
                            return 1;
                        }

                        WriteOutput(new { success = true, message = $"Removed {removedCount} rule(s).", removed = removedCount }, asJson);
                        return 0;
                    }

                default:
                    WriteError($"Unknown rule command '{action}'.", asJson);
                    return 1;
            }
        }

        // =========================================================================
        // 5. EXPORT & IMPORT
        // =========================================================================
        private static int HandleExport(Controller controller, string[] args, bool asJson)
        {
            string? outputFile = GetArgValue(args, "--output") ?? GetArgValue(args, "-o");
            if (string.IsNullOrEmpty(outputFile))
            {
                WriteError("Option --output <filepath> is required for export.", asJson);
                return 1;
            }

            Guid changeset = Guid.Empty;
            var ret = controller.GetServerConfig(out var config, out var state, ref changeset);
            if (ret == MessageType.COM_ERROR || config == null)
            {
                WriteError("Could not connect to TinyWall Service to read configuration.", asJson);
                return 1;
            }

            var container = new ConfigContainer(config, ControllerSettings.Load());
            outputFile = Path.GetFullPath(outputFile);
            string? dir = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            SerializationHelper.SerializeToFile(container, outputFile);
            WriteOutput(new { success = true, message = $"Configuration successfully exported to '{outputFile}'.", path = outputFile }, asJson);
            return 0;
        }

        private static int HandleImport(Controller controller, string[] args, bool asJson)
        {
            string? inputFile = GetArgValue(args, "--input") ?? GetArgValue(args, "-i");
            if (string.IsNullOrEmpty(inputFile) || !File.Exists(inputFile))
            {
                WriteError($"Input file not found: '{inputFile}'.", asJson);
                return 1;
            }

            ConfigContainer imported;
            try
            {
                imported = SerializationHelper.DeserializeFromFile(inputFile, new ConfigContainer(), true);
            }
            catch (Exception ex)
            {
                WriteError($"Failed to deserialize configuration file: {ex.Message}", asJson);
                return 1;
            }

            Guid changeset = Guid.Empty;
            controller.GetServerConfig(out _, out _, ref changeset);

            var putResp = controller.SetServerConfig(imported.Service, changeset);
            if (putResp.Type == MessageType.RESPONSE_LOCKED)
            {
                WriteError("TinyWall is password locked. Import denied.", asJson);
                return 1;
            }
            else if (putResp.Type == MessageType.COM_ERROR)
            {
                WriteError("Communication error while applying imported configuration.", asJson);
                return 1;
            }

            if (imported.Controller != null)
                imported.Controller.Save();

            WriteOutput(new
            {
                success = true,
                message = "Configuration successfully imported and applied.",
                rulesCount = imported.Service.ActiveProfile.AppExceptions.Count
            }, asJson);
            return 0;
        }

        // =========================================================================
        // 6. LOGS
        // =========================================================================
        private static int HandleLogs(Controller controller, string[] args, bool asJson)
        {
            int limit = 25;
            string? limitStr = GetArgValue(args, "--limit") ?? GetArgValue(args, "--recent");
            if (!string.IsNullOrEmpty(limitStr) && int.TryParse(limitStr, out int parsedLimit))
            {
                if (parsedLimit < 1) parsedLimit = 1;
                if (parsedLimit > 500) parsedLimit = 500;
                limit = parsedLimit;
            }

            var req = controller.BeginReadFwLog();
            var entries = Controller.EndReadFwLog(req.Response);

            var formatted = entries.Take(limit).Select(e => new
            {
                timestamp = e.Timestamp.ToString("o"),
                appPath = e.AppPath,
                packageId = e.PackageId,
                processId = e.ProcessId,
                protocol = e.Protocol.ToString(),
                direction = e.Direction.ToString(),
                eventStatus = e.Event.ToString(),
                localPort = e.LocalPort,
                remoteAddress = (e.RemoteIp != null && e.RemoteIp.Length > 0) ? new IPAddress(e.RemoteIp).ToString() : null,
                remotePort = e.RemotePort
            }).ToList();

            WriteOutput(new { success = true, count = formatted.Count, logs = formatted }, asJson);
            return 0;
        }

        // =========================================================================
        // HELPERS
        // =========================================================================
        private static ExceptionPolicy BuildPolicy(string[] args)
        {
            string policyType = GetArgValue(args, "--policy") ?? "tcpudp";
            string? remotePorts = GetArgValue(args, "--remote-ports") ?? GetArgValue(args, "--ports");
            bool localOnly = args.Any(a => a.Equals("--local-only", StringComparison.OrdinalIgnoreCase));

            if (policyType.Equals("unrestricted", StringComparison.OrdinalIgnoreCase))
                return new UnrestrictedPolicy { LocalNetworkOnly = localOnly };

            var p = new TcpUdpPolicy(unrestricted: string.IsNullOrEmpty(remotePorts))
            {
                LocalNetworkOnly = localOnly
            };
            if (!string.IsNullOrEmpty(remotePorts))
            {
                p.AllowedRemoteTcpConnectPorts = remotePorts;
                p.AllowedRemoteUdpConnectPorts = remotePorts;
            }
            return p;
        }

        private static AppExceptionTimer ParseTimer(string? timerStr)
        {
            if (string.IsNullOrEmpty(timerStr))
                return AppExceptionTimer.Permanent;

            return timerStr.ToLowerInvariant() switch
            {
                "reboot" or "until_reboot" or "until-reboot" => AppExceptionTimer.Until_Reboot,
                "5m" or "5" or "5min" => AppExceptionTimer.For_5_Minutes,
                "30m" or "30" or "30min" => AppExceptionTimer.For_30_Minutes,
                "1h" or "60" or "60min" => AppExceptionTimer.For_1_Hour,
                "4h" or "240" => AppExceptionTimer.For_4_Hours,
                "9h" or "540" => AppExceptionTimer.For_9_Hours,
                "24h" or "1140" or "1d" => AppExceptionTimer.For_24_Hours,
                _ => AppExceptionTimer.Permanent
            };
        }

        private static int CommitException(Controller controller, ServerConfiguration config, Guid changeset, FirewallExceptionV3 exception, string targetDesc, bool asJson)
        {
            var copyConfig = Utils.DeepClone(config);
            copyConfig.ActiveProfile.AddExceptions(new List<FirewallExceptionV3> { exception });

            var putResp = controller.SetServerConfig(copyConfig, changeset);
            if (putResp.Type == MessageType.RESPONSE_LOCKED)
            {
                WriteError("TinyWall is password locked. Rule modification denied.", asJson);
                return 1;
            }

            WriteOutput(new
            {
                success = true,
                message = $"Rule successfully added for '{targetDesc}'.",
                id = exception.Id.ToString(),
                target = targetDesc,
                policy = exception.Policy?.PolicyType.ToString(),
                timer = exception.Timer.ToString()
            }, asJson);
            return 0;
        }

        private static string GetServiceExecutablePath(string serviceName)
        {
            try
            {
                using RegistryKey KeyHKLM = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                using RegistryKey? Key = KeyHKLM.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + serviceName);
                if (Key == null) return string.Empty;

                string? imagePath = Key.GetValue("ImagePath") as string;
                if (string.IsNullOrEmpty(imagePath)) return string.Empty;

                imagePath = imagePath.Replace("\"", string.Empty);
                while (true)
                {
                    if (File.Exists(imagePath))
                        return imagePath;

                    int i = imagePath.LastIndexOf(' ');
                    if (i == -1) break;
                    imagePath = imagePath.Substring(0, i);
                }
            }
            catch { }
            return string.Empty;
        }

        private static void EnsureAppDatabaseLoaded()
        {
            if (GlobalInstances.AppDatabase == null || GlobalInstances.AppDatabase.KnownApplications == null)
            {
                try
                {
                    GlobalInstances.AppDatabase = DatabaseClasses.AppDatabase.Load();
                }
                catch
                {
                    GlobalInstances.AppDatabase = new DatabaseClasses.AppDatabase();
                }
            }
        }

        private static string? GetArgValue(string[] args, string flagName)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(flagName, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    return args[i + 1];
            }
            return null;
        }

        private static void ShowHelp()
        {
            EnsureConsoleAttached();
            Console.WriteLine(@"TinyWall Command-Line Interface (Automation & Scripting)

USAGE:
    TinyWall.exe cli <command> [subcommand/options] [--json]

COMMANDS:
    status / mode
        TinyWall.exe cli status [--json]
        TinyWall.exe cli mode set <normal|blockall|allowoutgoing|disabled|autolearn> [--json]

    config (Global settings)
        TinyWall.exe cli config get [--json]
        TinyWall.exe cli config set [--allow-local-subnet true|false] [--blocklists true|false]
                                   [--hosts-blocklist true|false] [--display-off-block true|false]
                                   [--lock-hosts true|false] [--json]

    special (Built-in special exceptions)
        TinyWall.exe cli special list [--json]
        TinyWall.exe cli special enable --name <SpecialName> [--json]
        TinyWall.exe cli special disable --name <SpecialName> [--json]

    rules
        TinyWall.exe cli rule list [--json]
        TinyWall.exe cli rule add --path <exe_path> [--policy tcpudp|unrestricted] [--ports <ports>] [--timer <30m|1h|...>] [--json]
        TinyWall.exe cli rule add-service --name <service_name> [--policy tcpudp|unrestricted] [--timer <...>] [--json]
        TinyWall.exe cli rule list-uwp [--filter <text>] [--json]
        TinyWall.exe cli rule add-uwp --package <pkg_name_or_sid> [--policy tcpudp|unrestricted] [--timer <...>] [--json]
        TinyWall.exe cli rule block --path <exe_path> [--json]
        TinyWall.exe cli rule remove [--path <exe_path> | --service <name> | --id <guid>] [--json]

    export / import
        TinyWall.exe cli export --output <file.tws> [--json]
        TinyWall.exe cli import --input <file.tws> [--json]

    logs
        TinyWall.exe cli log [--recent <n>] [--json]
");
        }
    }
}
