using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO.Pipes;
using System.IO;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using Microsoft.Win32;
using PKSoft.WindowsFirewall;

namespace PKSoft
{
    internal class TinyWallService : ServiceBase
    {
        internal readonly static string[] ServiceDependencies = new string[]
        {
            "mpssvc",
            "eventlog",
            "Winmgmt"
        };

        internal const string SERVICE_NAME = "TinyWall";
        internal const string SERVICE_DISPLAY_NAME = "TinyWall Service";

        private RequestQueue Q;

        private Thread FirewallWorkerThread;
        private Timer MinuteTimer;
        private DateTime LastControllerCommandTime = DateTime.Now;
        
        private WindowsFirewall.Policy Firewall;
        private WindowsFirewall.Rules FwRules;

        private List<RuleDef> ActiveRules;
        private List<RuleDef> AppExRules;
        private List<RuleDef> SpecialRules;

        private ProfileType Profile;
        private string ProfileDisplayName;
        private FirewallMode Mode = FirewallMode.Normal;
        private bool UninstallRequested = false;

        private void AssembleActiveRules()
        {
            ActiveRules.Clear();
            ActiveRules.AddRange(AppExRules);
            ActiveRules.AddRange(SpecialRules);

            string ModeId = AppExceptionSettings.GenerateID();

            // Do we want to let local traffic through?
            if (SettingsManager.CurrentZone.AllowLocalSubnet)
            {
                RuleDef def = new RuleDef(ModeId, "Allow local subnet", PacketAction.Allow, RuleDirection.InOut, Protocol.Any);
                def.RemoteAddresses = "LocalSubnet";
                ActiveRules.Add(def);
            }

            // Do we want to block known malware ports?
            if (SettingsManager.CurrentZone.BlockMalwarePorts)
            {
                Profile profileMalwarePortBlock = GlobalInstances.ProfileMan.GetProfile("Malware port block");
                if (profileMalwarePortBlock != null)
                {
                    foreach (RuleDef rule in profileMalwarePortBlock.Rules)
                        rule.ExceptionId = ModeId;
                    ActiveRules.AddRange(profileMalwarePortBlock.Rules);
                }
            }

            // This switch should be executed last, as it might modify existing elements in ActiveRules
            switch (this.Mode)
            {
                case FirewallMode.AllowOutgoing:
                    {
                        // Add rule to explicitly allow outgoing connections
                        RuleDef def = new RuleDef(ModeId, "Allow outbound", PacketAction.Allow, RuleDirection.Out, Protocol.Any);
                        ActiveRules.Add(def);
                        break;
                    }
                case FirewallMode.BlockAll:
                    {
                        // Remove all rules
                        ActiveRules.Clear();

                        // Add a rule to deny all traffic. Denial rules have priority, so this will disable all traffic.
                        RuleDef def = new RuleDef(ModeId, "Block all traffic", PacketAction.Block, RuleDirection.InOut, Protocol.Any);
                        ActiveRules.Add(def);
                        break;
                    }
                case FirewallMode.Disabled:
                    {
                        // Remove all rules
                        ActiveRules.Clear();

                        // Add rule to explicitly allow everything
                        RuleDef def = new RuleDef(ModeId, "Allow everything", PacketAction.Allow, RuleDirection.InOut, Protocol.Any);
                        ActiveRules.Add(def);
                        break;
                    }
                case FirewallMode.Normal:
                    {
                        // Nothing to do here
                        break;
                    }
            }
        }

        private void MergeActiveRulesIntoWinFirewall()
        {
            int lenId = AppExceptionSettings.GenerateID().Length;
            List<Rule> rules = new List<Rule>();

            // Add new rules
            for (int i = ActiveRules.Count - 1; i >= 0; --i)          // for each TW firewall rule
            {
                RuleDef rule_i = ActiveRules[i];
                string id_i = rule_i.ExceptionId.Substring(0, lenId);

                bool found = false;
                for (int j = FwRules.Count - 1; j >= 0; --j)    // for each Win firewall rule
                {
                    Rule rule_j = FwRules[j];
                    string name_j = rule_j.Name;

                    // Skip if this is not a TinyWall rule
                    if (!name_j.StartsWith("[TW"))
                        continue;

                    string id_j = name_j.Substring(0, lenId);

                    if (string.Compare(id_i, id_j, StringComparison.Ordinal) == 0)
                    {
                        found = true;
                        break;
                    }
                }

                // Rule is not yet active, add it to the Windows firewall
                if (!found)
                    rule_i.ConstructRule(rules);
            }

            // We add new rules before removing invalid ones, 
            // so that we don't break existing connections.
            FwRules.Add(rules);

            // Remove dead rules
            for (int i = FwRules.Count - 1; i >= 0; --i)    // for each Win firewall rule
            {
                Rule rule_i = FwRules[i];
                string name_i = rule_i.Name;

                // Skip if this is not a TinyWall rule
                if (!name_i.StartsWith("[TW"))
                    continue;

                string id_i = name_i.Substring(0, lenId);

                bool found = false;
                for (int j = ActiveRules.Count - 1; j >= 0; --j)          // for each TW firewall rule
                {
                    RuleDef rule_j = ActiveRules[j];
                    string id_j = rule_j.ExceptionId.Substring(0, lenId);

                    if (string.Compare(id_i, id_j, StringComparison.Ordinal) == 0)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    FwRules.RemoveAt(i);
            }
        }

        private void RebuildSpecialRuleDefs()
        {
            // We will collect all our rules into this list
            SpecialRules.Clear();

            for (int i = 0; i < SettingsManager.CurrentZone.SpecialExceptions.Length; ++i)
            {
                try
                {   //This try-catch will prevent errors if an exception profile string is invalid
                    ProfileAssoc app = GlobalInstances.ProfileMan.GetApplication(SettingsManager.CurrentZone.SpecialExceptions[i]);
                    AppExceptionSettings ex = app.ToExceptionSetting();
                    ex.AppID = AppExceptionSettings.GenerateID();
                    GetRulesForException(ex, SpecialRules);
                }
                catch { }
            }
        }

        private void RebuildApplicationRuleDefs()
        {
            // We will collect all our rules into this list
            AppExRules.Clear();

            for (int i = 0; i < SettingsManager.CurrentZone.AppExceptions.Length; ++i)
            {
                try
                {   //This try-catch will prevent errors if an exception profile string is invalid
                    AppExceptionSettings ex = SettingsManager.CurrentZone.AppExceptions[i];
                    GetRulesForException(ex, AppExRules);
                }
                catch { }
            }
        }

        private void GetRulesForException(AppExceptionSettings ex, List<RuleDef> ruleset)
        {
            if (string.IsNullOrEmpty(ex.AppID))
            {
// Do not let the service crash if a rule cannot be constructed 
#if DEBUG
                throw new InvalidOperationException("Firewall exception specification must have an ID.");
#else
                ex.RegenerateID();
                ++SettingsManager.Changeset;
#endif
            }

            for (int i = 0; i < ex.Profiles.Length; ++i)    // for each profile
            {
                // Get the rules for this profile
                Profile p = GlobalInstances.ProfileMan.GetProfile(ex.Profiles[i]);
                if (p == null)
                    continue;

                for (int j = 0; j < p.Rules.Length; ++j)    // for each rule in profile
                {
                    try
                    {
                        RuleDef def = p.Rules[j];
                        def.ExceptionId = ex.AppID;
                        def.Application = ex.ExecutablePath;
                        def.ServiceName = ex.ServiceName;
                        ruleset.Add(def);
                    }
                    catch
                    {
// Do not let the service crash if a rule cannot be constructed 
#if DEBUG
                        throw;
#endif
                    }
                }
            }

            try
            {
                // Add extra ports
                if (!string.IsNullOrEmpty(ex.OpenPortListenLocalTCP))
                {
                    RuleDef def = new RuleDef(ex.AppID, "Extra Tcp Listen Ports", PacketAction.Allow, RuleDirection.In,  Protocol.TCP);
                    def.LocalPorts = ex.OpenPortListenLocalTCP;
                    def.Application = ex.ExecutablePath;
                    def.ServiceName = ex.ServiceName;
                    ruleset.Add(def);
                }
                if (!string.IsNullOrEmpty(ex.OpenPortListenLocalUDP))
                {
                    RuleDef def = new RuleDef(ex.AppID, "Extra Udp Listen Ports", PacketAction.Allow, RuleDirection.In, Protocol.UDP);
                    def.LocalPorts = ex.OpenPortListenLocalUDP;
                    def.Application = ex.ExecutablePath;
                    def.ServiceName = ex.ServiceName;
                    ruleset.Add(def);
                }
                if (!string.IsNullOrEmpty(ex.OpenPortOutboundRemoteTCP))
                {
                    RuleDef def = new RuleDef(ex.AppID, "Extra Tcp Outbound Ports", PacketAction.Allow, RuleDirection.Out, Protocol.TCP);
                    def.RemotePorts = ex.OpenPortOutboundRemoteTCP;
                    def.Application = ex.ExecutablePath;
                    def.ServiceName = ex.ServiceName;
                    ruleset.Add(def);
                }
                if (!string.IsNullOrEmpty(ex.OpenPortOutboundRemoteUDP))
                {
                    RuleDef def = new RuleDef(ex.AppID, "Extra Udp Outbound Ports", PacketAction.Allow, RuleDirection.Out, Protocol.UDP);
                    def.RemotePorts = ex.OpenPortOutboundRemoteUDP;
                    def.Application = ex.ExecutablePath;
                    def.ServiceName = ex.ServiceName;
                    ruleset.Add(def);
                }
            }
            catch (Exception)
            {
                this.EventLog.WriteEntry("Error applying custom port rules for " + ex.ExecutableName + ".", EventLogEntryType.Error);
            }
        }

        // This method completely reinitializes the firewall.
        private void InitFirewall()
        {
            Firewall = new Policy();
            Firewall.ResetFirewall();
            Firewall.Enabled = true;
            Firewall.DefaultInboundAction = PacketAction.Block;
            Firewall.DefaultOutboundAction = PacketAction.Block;
            Firewall.BlockAllInboundTraffic = false;
            Firewall.NotificationsDisabled = true;
            FwRules = Firewall.GetRules(false);

            LoadProfile();
            if (!SettingsManager.CurrentZone.EnableDefaultWindowsRules)
                FwRules.Clear();

            ReapplySettings();
        }

        private void LoadProfile()
        {
            Profile = Firewall.CurrentProfileTypes;
            if ((int)(Profile & ProfileType.Private) != 0)
                ProfileDisplayName = "Private";
            else if ((int)(Profile & ProfileType.Domain) != 0)
                ProfileDisplayName = "Domain";
            else if ((int)(Profile & ProfileType.Public) != 0)
                ProfileDisplayName = "Public";
            else
                throw new InvalidOperationException("Unexpected network profile value.");

            try
            {
                GlobalInstances.ProfileMan = ProfileManager.Load(ProfileManager.DBPath);
            }
            catch
            {
                GlobalInstances.ProfileMan = new ProfileManager();
            }

            SettingsManager.GlobalConfig = MachineSettings.Load();
            SettingsManager.CurrentZone = ZoneSettings.Load(ProfileDisplayName);
        }

        // This method reapplies all firewall settings.
        private void ReapplySettings()
        {
            LoadProfile();

            RebuildApplicationRuleDefs();
            RebuildSpecialRuleDefs();
            AssembleActiveRules();
            MergeActiveRulesIntoWinFirewall();

            HostsFileManager.EnableProtection(SettingsManager.GlobalConfig.LockHostsFile);
            if (SettingsManager.GlobalConfig.HostsBlocklist)
                HostsFileManager.EnableHostsFile();

            if (MinuteTimer != null)
            {
                using (WaitHandle wh = new AutoResetEvent(false))
                {
                    MinuteTimer.Dispose(wh);
                    wh.WaitOne();
                }
                MinuteTimer = null;
            }

            MinuteTimer = new Timer(new TimerCallback(TimerCallback), null, 0, 60000);
        }

        public void TimerCallback(Object state)
        {
            // This timer is called every minute.

            // Check if a timed exception has expired
            if (!Q.HasRequest(TinyWallCommands.CHECK_SCHEDULED_RULES))
                Q.Enqueue(new ReqResp(new Message(TinyWallCommands.CHECK_SCHEDULED_RULES)));

            // Check for inactivity and lock if necessary
            if (DateTime.Now - LastControllerCommandTime > TimeSpan.FromMinutes(10))
            {
                Q.Enqueue(new ReqResp(new Message(TinyWallCommands.LOCK)));
            }
        }

        private Message ProcessCmd(Message req)
        {
            switch (req.Command)
            {
                case TinyWallCommands.PING:
                    {
                        return new Message(TinyWallCommands.RESPONSE_OK);
                    }
                case TinyWallCommands.MODE_SWITCH:
                    {
                        this.Mode = (FirewallMode)req.Arguments[0];
                        AssembleActiveRules();
                        MergeActiveRulesIntoWinFirewall();

                        if (Firewall.LocalPolicyModifyState == LocalPolicyState.GP_OVERRRIDE)
                            return new Message(TinyWallCommands.RESPONSE_WARNING);
                        else
                            return new Message(TinyWallCommands.RESPONSE_OK);
                    }
                case TinyWallCommands.PUT_SETTINGS:
                    {
                        SettingsManager.GlobalConfig = (MachineSettings)req.Arguments[0];
                        SettingsManager.GlobalConfig.Save();

                        // This roundabout way is to prevent overwriting the wrong zone if the controller is sending us
                        // data from a zone that is not the current one.
                        ZoneSettings oldZone = SettingsManager.CurrentZone;
                        ZoneSettings newZone = (ZoneSettings)req.Arguments[1];
                        newZone.Save();
                        SettingsManager.CurrentZone = newZone;

                        if (newZone.EnableDefaultWindowsRules != oldZone.EnableDefaultWindowsRules)
                            InitFirewall();
                        else
                            ReapplySettings();
                        return new Message(TinyWallCommands.RESPONSE_OK);
                    }
                case TinyWallCommands.GET_SETTINGS:
                    {
                        // Get changeset of client
                        int changeset = (int)req.Arguments[0];

                        // If our changeset is different, send new settings to client
                        if (changeset != SettingsManager.Changeset)
                        {
                            return new Message(TinyWallCommands.RESPONSE_OK,
                                SettingsManager.Changeset,
                                SettingsManager.GlobalConfig,
                                SettingsManager.CurrentZone
                                );
                        }
                        else
                        {
                            // Our changeset is the same, so do not send settings again
                            return new Message(TinyWallCommands.RESPONSE_OK, SettingsManager.Changeset);
                        }
                    }
                case TinyWallCommands.REINIT:
                    {
                        InitFirewall();
                        return new Message(TinyWallCommands.RESPONSE_OK);
                    }
                case TinyWallCommands.RELOAD:
                    {
                        MergeActiveRulesIntoWinFirewall();
                        return new Message(TinyWallCommands.RESPONSE_OK);
                    }
                case TinyWallCommands.GET_PROFILE:
                    {
                        return new Message(TinyWallCommands.RESPONSE_OK, SettingsManager.CurrentZone.ZoneName);
                    }
                case TinyWallCommands.UNLOCK:
                    {
                        if (SettingsManager.ServiceConfig.Unlock((string)req.Arguments[0]))
                            return new Message(TinyWallCommands.RESPONSE_OK);
                        else
                            return new Message(TinyWallCommands.RESPONSE_ERROR);
                    }
                case TinyWallCommands.LOCK:
                    {
                        SettingsManager.ServiceConfig.Locked = true;
                        return new Message(TinyWallCommands.RESPONSE_OK);
                    }
                case TinyWallCommands.GET_LOCK_STATE:
                    {
                        return new Message(TinyWallCommands.RESPONSE_OK, SettingsManager.ServiceConfig.HasPassword ? 1 : 0, SettingsManager.ServiceConfig.Locked ? 1 : 0);
                    }
                case TinyWallCommands.SET_PASSPHRASE:
                    {
                        FileLocker.UnlockFile(ServiceSettings.PasswordFilePath);
                        try
                        {
                            SettingsManager.ServiceConfig.SetPass((string)req.Arguments[0]);
                            return new Message(TinyWallCommands.RESPONSE_OK);
                        }
                        catch
                        {
                            return new Message(TinyWallCommands.RESPONSE_ERROR);
                        }
                        finally
                        {
                            FileLocker.LockFile(ServiceSettings.PasswordFilePath, FileAccess.Read, FileShare.Read);
                        }
                    }
                case TinyWallCommands.GET_MODE:
                    {
                        return new Message(TinyWallCommands.RESPONSE_OK, this.Mode);
                    }
                case TinyWallCommands.STOP_DISABLE:
                    {
                        UninstallRequested = true;

                        // Disable automatic re-start of service
                        try
                        {
                            using (ScmWrapper.ServiceControlManager scm = new ScmWrapper.ServiceControlManager())
                            {
                                scm.SetStartupMode(TinyWallService.SERVICE_NAME, ServiceStartMode.Automatic);
                                scm.SetRestartOnFailure(TinyWallService.SERVICE_NAME, false);
                            }
                        }
                        catch { }

                        // Disable automatic start of controller
                        Utils.RunAtStartup("TinyWall Controller", null);

                        // Put back the user's original hosts file
                        HostsFileManager.DisableHostsFile();

                        // Reset Windows Firewall to its default state
                        Firewall.ResetFirewall();

                        // Stop service execution
                        Environment.Exit(0);

                        return new Message(TinyWallCommands.RESPONSE_OK);
                    }
                case TinyWallCommands.CHECK_SCHEDULED_RULES:
                    {
                        bool needsSave = false;

                        // Check all exceptions if any one has expired
                        AppExceptionSettings[] exs = SettingsManager.CurrentZone.AppExceptions;
                        for (int i = 0; i < exs.Length; ++i)
                        {
                            // Timer values above zero are the number of minutes to stay active
                            if ((int)exs[i].Timer <= 0)
                                continue;

                            // Did this one expire?
                            if (exs[i].CreationDate.AddMinutes((double)exs[i].Timer) <= DateTime.Now)
                            {
                                // Remove rule
                                string appid = exs[i].AppID;

                                // Search for the exception identifier in the rule name.
                                // Remove rules with a match.
                                for (int j = FwRules.Count-1; j >= 0; --j)
                                {
                                    if (FwRules[j].Name.Contains(appid))
                                        FwRules.RemoveAt(j);
                                }

                                // Remove exception
                                exs = Utils.ArrayRemoveItem(exs, exs[i]);
                                needsSave = true;
                            }
                        }

                        if (needsSave)
                        {
                            SettingsManager.CurrentZone.AppExceptions = exs;
                            SettingsManager.CurrentZone.Save();
                        }

                        return new Message(TinyWallCommands.RESPONSE_OK);
                    }
                default:
                    {
                        return new Message(TinyWallCommands.RESPONSE_ERROR);
                    }
            }
        }

        // Entry point for thread that actually issues commands to Windows Firewall.
        // Only one thread (this one) is allowed to issue them.
        private void FirewallWorkerMethod()
        {
            AppExRules = new List<RuleDef>();
            SpecialRules = new List<RuleDef>();
            ActiveRules = new List<RuleDef>();

            EventLogWatcher WFEventWatcher = null;
            try
            {
                try
                {
                    WFEventWatcher = new EventLogWatcher("Microsoft-Windows-Windows Firewall With Advanced Security/Firewall");
                    WFEventWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(WFEventWatcher_EventRecordWritten);
                    WFEventWatcher.Enabled = true;
                }
                catch
                {
                    WFEventWatcher = null;
                    EventLog.WriteEntry("Unable to listen for firewall events. Windows Firewall monitoring will be turned off.");
                }

                while (true)
                {
                    ReqResp req = Q.Dequeue();
                    req.Response = ProcessCmd(req.Request);
                    req.SignalResponse();
                }
            }
            finally
            {
                if (WFEventWatcher != null)
                    WFEventWatcher.Dispose();
            }
        }

        // Entry point for thread that listens to commands from the controller application.
        private Message PipeServerDataReceived(Message req)
        {
            if (((int)req.Command > 2047) && SettingsManager.ServiceConfig.Locked)
            {
                // Notify that we need to be unlocked first
                return new Message(TinyWallCommands.RESPONSE_LOCKED, 1);
            }
            else
            {
                LastControllerCommandTime = DateTime.Now;

                // Process and wait for response
                ReqResp qItem = new ReqResp(req);
                Q.Enqueue(qItem);
                Message resp = qItem.GetResponse();

                // Send response back to pipe
                return resp;
            }
        }

        private void WFEventWatcher_EventRecordWritten(object sender, EventRecordWrittenEventArgs e)
        {
            int propidx = -1;
            TinyWallCommands cmd = TinyWallCommands.REINIT;
            switch (e.EventRecord.Id)
            {
                case 2003:     // firewall setting changed
                    {
                        propidx = 7;
                        cmd = TinyWallCommands.REINIT;
                        break;
                    }
                case 2004:     // rule added
                    {
                        propidx = 22;
                        cmd = TinyWallCommands.RELOAD;
                        break;
                    }
                case 2005:     // rule changed
                    {
                        propidx = 22;
                        cmd = TinyWallCommands.REINIT;
                        break;
                    }
                case 2006:     // rule deleted
                    {
                        propidx = 3;
                        cmd = TinyWallCommands.RELOAD;
                        break;
                    }
                case 2010:     // network interface changed profile
                    {   // Event format is different in this case so we handle this separately
                        ++SettingsManager.Changeset;
                        if (!Q.HasRequest(TinyWallCommands.RELOAD))
                        {
                            EventLog.WriteEntry("Reloading firewall configuration because a network interface changed profile.");
                            Q.Enqueue(new ReqResp(new Message(TinyWallCommands.RELOAD)));
                        }
                        break;
                    }
                case 2032:     // firewall has been reset
                    {
                        propidx = 1;
                        cmd = TinyWallCommands.REINIT;
                        break;
                    }
                default:
                    break;
            }

            if (propidx != -1)
            {
                // Do nothing if the firewall is in disabled mode
                if (this.Mode == FirewallMode.Disabled)
                    return;

                // This is a list of apps that are allowed to change firewall rules
                string[] WhitelistedApps = new string[]
                {
                    Utils.ExecutablePath,
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "dllhost.exe")
                };

                // If the rules were changed by an allowed app, do nothing
                string EVpath = (string)e.EventRecord.Properties[propidx].Value;
                for (int i = 0; i < WhitelistedApps.Length; ++i)
                {
                    if (string.Compare(WhitelistedApps[i], EVpath, StringComparison.OrdinalIgnoreCase) == 0)
                        return;
                }

                if (!Q.HasRequest(cmd))
                {
                    EventLog.WriteEntry("Reloading firewall configuration because " + EVpath + " has modified it.");
                    Q.Enqueue(new ReqResp(new Message(cmd)));
                }
            }
        }

        internal TinyWallService()
        {
            this.CanShutdown = true;
#if DEBUG
            this.CanStop = true;
#else
            this.CanStop = false;
#endif
        }


        // Entry point for Windows service.
        protected override void OnStart(string[] args)
        {
            this.ServiceName = SERVICE_NAME;
            if (!EventLog.SourceExists("TinyWallService"))
                EventLog.CreateEventSource("TinyWallService", null);
            this.EventLog.Source = "TinyWallService";

            // Register an unhandled exception handler
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            try
            {
                EventLog.WriteEntry("TinyWall service starting up.");

                FileLocker.LockFile(ProfileManager.DBPath, FileAccess.Read, FileShare.Read);
                FileLocker.LockFile(ServiceSettings.PasswordFilePath, FileAccess.Read, FileShare.Read);

                // Lock configuration if we have a password
                SettingsManager.Changeset = 0;
                SettingsManager.ServiceConfig = new ServiceSettings();
                if (SettingsManager.ServiceConfig.HasPassword)
                    SettingsManager.ServiceConfig.Locked = true;

                // Set normal mode on stratup
                this.Mode = FirewallMode.Normal;

                // Issue load command
                Q = new RequestQueue();
                Q.Enqueue(new ReqResp(new Message(TinyWallCommands.REINIT)));

                // Start thread that is going to control Windows Firewall
                FirewallWorkerThread = new Thread(new ThreadStart(FirewallWorkerMethod));
                FirewallWorkerThread.IsBackground = true;
                FirewallWorkerThread.Start();

                // Fire up pipe
                GlobalInstances.CommunicationMan = new PipeCom("TinyWallController", new PipeDataReceived(PipeServerDataReceived));

#if !DEBUG
                // Messing with the SCM in this method would hang us, so start it parallel
                ThreadPool.QueueUserWorkItem((WaitCallback)delegate(object state)
                {
                    try
                    {
                        TinyWallDoctor.EnsureHealth();
                    }
                    catch { }
                });
#endif
            }
            catch (Exception e)
            {
                CurrentDomain_UnhandledException(null, new UnhandledExceptionEventArgs(e, false));
                throw;
            }
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Utils.LogCrash(e.ExceptionObject as Exception);
        }

        // Executed when service is stopped manually.
        protected override void OnStop()
        {
            Shutdown();
        }

        private void Shutdown()
        {
            bool needsSave = false;

            // Check all exceptions if any one has expired
            {
                AppExceptionSettings[] exs = SettingsManager.CurrentZone.AppExceptions;
                for (int i = 0; i < exs.Length; ++i)
                {
                    // "Permanent" exceptions do not expire, skip them
                    if (exs[i].Timer == AppExceptionTimer.Permanent)
                        continue;

                    // Did this one expire?
                    if (exs[i].Timer == AppExceptionTimer.Until_Reboot)
                    {
                        // Remove exception
                        exs = Utils.ArrayRemoveItem(exs, exs[i]);
                        needsSave = true;
                    }
                }

                if (needsSave)
                {
                    SettingsManager.CurrentZone.AppExceptions = exs;
                    SettingsManager.CurrentZone.Save();
                }
            }

            FirewallWorkerThread.Abort();
            SettingsManager.GlobalConfig.Save();
            SettingsManager.CurrentZone.Save();
            FileLocker.UnlockAll();

            if (!UninstallRequested)
            {
                try
                {
                    TinyWallDoctor.EnsureHealth();
                }
                catch { }
            }
        }

        // Executed on computer shutdown.
        protected override void OnShutdown()
        {
            Shutdown();
        }

#if DEBUG
        internal void Start(string[] args)
        {
            this.OnStart(args);
        }
#endif
    }
}
