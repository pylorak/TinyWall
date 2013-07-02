using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Net;
using System.ServiceProcess;
using System.Threading;
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
        private DateTime LastFwLogReadTime = DateTime.Now;
        private FirewallLogWatcher LogWatcher;
        private ServiceSettings ServiceLocker = null;

        // Context needed for learning mode
        ApplicationCollection LearningKnownApplication;
        List<FirewallException> LearningNewExceptions;
        
        private List<RuleDef> ActiveRules;
        private List<RuleDef> AppExRules;
        private List<RuleDef> SpecialRules;

        private bool UninstallRequested = false;

        private ServiceState VisibleState = null;

        private void AssembleActiveRules()
        {
            ActiveRules.Clear();
            ActiveRules.AddRange(AppExRules);
            ActiveRules.AddRange(SpecialRules);

            string ModeId = FirewallException.GenerateID();

            // Do we want to let local traffic through?
            if (ActiveConfig.Service.AllowLocalSubnet)
            {
                RuleDef def = new RuleDef(ModeId, "Allow local subnet", PacketAction.Allow, RuleDirection.InOut, Protocol.Any);
                def.RemoteAddresses = "LocalSubnet";
                ActiveRules.Add(def);
            }

            // Do we want to block known malware ports?
            if (ActiveConfig.Service.Blocklists.EnableBlocklists
                && ActiveConfig.Service.Blocklists.EnablePortBlocklist)
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
            switch (VisibleState.Mode)
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
                case FirewallMode.Learning:
                    {
                        // Remove all rules
                        ActiveRules.Clear();

                        // Add rule to explicitly allow everything
                        RuleDef def = new RuleDef(ModeId, "Allow everything", PacketAction.Allow, RuleDirection.InOut, Protocol.Any);
                        ActiveRules.Add(def);

                        // Start up firewall logging
                        Q.Enqueue(new ReqResp(new Message(TWControllerMessages.READ_FW_LOG)));
                        break;
                    }
                case FirewallMode.Normal:
                    {
                        // Nothing to do here
                        break;
                    }
            }

            ActiveRules.TrimExcess();
        }

        private void MergeActiveRulesIntoWinFirewall(WindowsFirewall.Rules FwRules)
        {
            int lenId = FirewallException.GenerateID().Length;
            List<Rule> rules = new List<Rule>();

            // We cache rule names locally to lighten on string creation.
            // Each time Rule.Name is accessed, interop creates new strings.
            // By preloading all rules names (which we will use often)
            // we spare CPU and memory resources.
            List<string> rule_names = new List<string>(FwRules.Count);
            for (int i = 0; i < FwRules.Count; ++i)
                rule_names.Add(FwRules[i].Name);

            // Add new rules
            for (int i = ActiveRules.Count - 1; i >= 0; --i)          // for each TW firewall rule
            {
                RuleDef rule_i = ActiveRules[i];
                string id_i = rule_i.ExceptionId.Substring(0, lenId);

                bool found = false;
                for (int j = rule_names.Count - 1; j >= 0; --j)    // for each Win firewall rule
                {
                    string name_j = rule_names[j];

                    // Skip if this is not a TinyWall rule
                    if (!name_j.StartsWith("[TW", StringComparison.Ordinal))
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
            List<Rule> failedRules = new List<Rule>();
            FwRules.Add(rules, ref failedRules);

            // If we couldn't add a rule, log rule details to log for later inspection
            if (failedRules.Count > 0)
            {
                string log = string.Empty;
                for (int i = 0; i< failedRules.Count;++i)
                {
                    Rule r = failedRules[i];
                    log += r.Name + "; " + r.Action + "; " + r.ApplicationName + "; " + r.Direction + "; " + r.Protocol + "; " + r.LocalAddresses + "; " + r.LocalPorts + "; " + r.RemoteAddresses + "; " + r.RemotePorts + "; " + r.ServiceName + Environment.NewLine;
                }
                Utils.Log(log);
            }

            // Remove dead rules
            for (int i = rule_names.Count - 1; i >= 0; --i)    // for each Win firewall rule
            {
                string name_i = rule_names[i];

                // Skip if this is not a TinyWall rule
                if (!name_i.StartsWith("[TW", StringComparison.Ordinal))
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

            ApplicationCollection allApps = GlobalInstances.ProfileMan.KnownApplications;
            for (int i = 0; i < ActiveConfig.Service.SpecialExceptions.Count; ++i)
            {
                try
                {   //This try-catch will prevent errors if an exception profile string is invalid
                    Application app = allApps.GetApplicationByName(ActiveConfig.Service.SpecialExceptions[i]);
                    app.ResolveFilePaths();
                    foreach (AppExceptionAssoc template in app.FileTemplates)
                    {
                        foreach (string execPath in template.ExecutableRealizations)
                        {
                            FirewallException ex = template.CreateException(execPath);
                            ex.AppID = FirewallException.GenerateID();
                            GetRulesForException(ex, SpecialRules);
                        }
                    }
                }
                catch { }
            }

            SpecialRules.TrimExcess();
        }

        private void RebuildApplicationRuleDefs()
        {
            // We will collect all our rules into this list
            AppExRules.Clear();

            for (int i = 0; i < ActiveConfig.Service.AppExceptions.Count; ++i)
            {
                try
                {   //This try-catch will prevent errors if an exception profile string is invalid
                    FirewallException ex = ActiveConfig.Service.AppExceptions[i];
                    GetRulesForException(ex, AppExRules);
                }
                catch (Exception e)
                {
                    Utils.LogCrash(e);
#if DEBUG
                    throw;
#endif
                }
            }

            AppExRules.TrimExcess();
        }

        private void GetRulesForException(FirewallException ex, List<RuleDef> ruleset)
        {
            if (string.IsNullOrEmpty(ex.AppID))
            {
// Do not let the service crash if a rule cannot be constructed 
#if DEBUG
                throw new InvalidOperationException("Firewall exception specification must have an ID.");
#else
                ex.RegenerateID();
                ActiveConfig.Service.SequenceNumber = Utils.GetRandomNumber();
#endif
            }

            if (ex.AlwaysBlockTraffic)
            {
                RuleDef def = new RuleDef(ex.AppID, "Block", PacketAction.Block, RuleDirection.InOut, Protocol.Any);
                def.Application = ex.ExecutablePath;
                def.ServiceName = ex.ServiceName;
                ruleset.Add(def);
                return;
            }
            if (ex.UnrestricedTraffic)
            {
                RuleDef def = new RuleDef(ex.AppID, "Full access", PacketAction.Allow, RuleDirection.InOut, Protocol.Any);
                def.Application = ex.ExecutablePath;
                def.ServiceName = ex.ServiceName;
                if (ex.LocalNetworkOnly)
                    def.RemoteAddresses = "LocalSubnet";
                ruleset.Add(def);
                return;
            }

            if (ex.Profiles != null)
            {
                for (int i = 0; i < ex.Profiles.Count; ++i)    // for each profile
                {
                    // Get the rules for this profile
                    Profile p = GlobalInstances.ProfileMan.GetProfile(ex.Profiles[i]);
                    if (p == null)
                        continue;

                    for (int j = 0; j < p.Rules.Length; ++j)    // for each rule in profile
                    {
                        RuleDef def = p.Rules[j];
                        def.ExceptionId = ex.AppID;
                        def.Application = ex.ExecutablePath;
                        def.ServiceName = ex.ServiceName;
                        if (ex.LocalNetworkOnly)
                            def.RemoteAddresses = "LocalSubnet";
                        ruleset.Add(def);
                    }
                }
            }

            // Add extra ports
            if (!string.IsNullOrEmpty(ex.OpenPortListenLocalTCP))
            {
                RuleDef def = new RuleDef(ex.AppID, "TCP Listen Ports", PacketAction.Allow, RuleDirection.In,  Protocol.TCP);
                def.Application = ex.ExecutablePath;
                def.ServiceName = ex.ServiceName;
                if (!ex.OpenPortListenLocalTCP.Equals("*"))
                    def.LocalPorts = ex.OpenPortListenLocalTCP;
                if (ex.LocalNetworkOnly)
                    def.RemoteAddresses = "LocalSubnet";
                ruleset.Add(def);
            }
            if (!string.IsNullOrEmpty(ex.OpenPortListenLocalUDP))
            {
                RuleDef def = new RuleDef(ex.AppID, "UDP Listen Ports", PacketAction.Allow, RuleDirection.In, Protocol.UDP);
                def.Application = ex.ExecutablePath;
                def.ServiceName = ex.ServiceName;
                if (!ex.OpenPortListenLocalUDP.Equals("*"))
                    def.LocalPorts = ex.OpenPortListenLocalUDP;
                if (ex.LocalNetworkOnly)
                    def.RemoteAddresses = "LocalSubnet";
                ruleset.Add(def);
            }
            if (!string.IsNullOrEmpty(ex.OpenPortOutboundRemoteTCP))
            {
                RuleDef def = new RuleDef(ex.AppID, "TCP Outbound Ports", PacketAction.Allow, RuleDirection.Out, Protocol.TCP);
                def.Application = ex.ExecutablePath;
                def.ServiceName = ex.ServiceName;
                if (!ex.OpenPortOutboundRemoteTCP.Equals("*"))
                    def.RemotePorts = ex.OpenPortOutboundRemoteTCP;
                if (ex.LocalNetworkOnly)
                    def.RemoteAddresses = "LocalSubnet";
                ruleset.Add(def);
            }
            if (!string.IsNullOrEmpty(ex.OpenPortOutboundRemoteUDP))
            {
                RuleDef def = new RuleDef(ex.AppID, "UDP Outbound Ports", PacketAction.Allow, RuleDirection.Out, Protocol.UDP);
                def.Application = ex.ExecutablePath;
                def.ServiceName = ex.ServiceName;
                if (!ex.OpenPortOutboundRemoteUDP.Equals("*"))
                    def.RemotePorts = ex.OpenPortOutboundRemoteUDP;
                if (ex.LocalNetworkOnly)
                    def.RemoteAddresses = "LocalSubnet";
                ruleset.Add(def);
            }
        }

        // This method completely reinitializes the firewall.
        private void InitFirewall()
        {
            Policy Firewall = new Policy();
            WindowsFirewall.Rules FwRules = null;

            using (ThreadBarrier barrier = new ThreadBarrier(2))
            {
                ThreadPool.QueueUserWorkItem((WaitCallback)delegate(object state)
                {
                    try
                    {
                        LoadDatabase();
                    }
                    finally
                    {
                        barrier.Wait();
                    }
                });

                Firewall.ResetFirewall();
                Firewall.Enabled = true;
                Firewall.DefaultInboundAction = PacketAction.Block;
                Firewall.DefaultOutboundAction = PacketAction.Block;
                Firewall.BlockAllInboundTraffic = false;
                Firewall.NotificationsDisabled = true;

                FwRules = Firewall.GetRules(false);
                for (int i = 0; i < FwRules.Count; ++i)
                    FwRules[i].Enabled = false;

                barrier.Wait();
                // --- THREAD BARRIER ---
            }

            ActiveConfig.Service = ServiceSettings21.Load();
            ActiveConfig.Service.SequenceNumber = Utils.GetRandomNumber();
            VisibleState.Mode = ActiveConfig.Service.StartupMode;

            ReapplySettings(FwRules);
        }


        // This method reapplies all firewall settings.
        private void ReapplySettings(WindowsFirewall.Rules FwRules)
        {
            RebuildApplicationRuleDefs();
            RebuildSpecialRuleDefs();
            AssembleActiveRules();
            MergeActiveRulesIntoWinFirewall(FwRules);

            HostsFileManager.EnableProtection(ActiveConfig.Service.LockHostsFile);
            if (ActiveConfig.Service.Blocklists.EnableBlocklists
                && ActiveConfig.Service.Blocklists.EnableHostsBlocklist)
                HostsFileManager.EnableHostsFile();
            else
                HostsFileManager.DisableHostsFile();

            if (MinuteTimer != null)
            {
                using (WaitHandle wh = new AutoResetEvent(false))
                {
                    MinuteTimer.Dispose(wh);
                    wh.WaitOne();
                }
                MinuteTimer = null;
            }

            MinuteTimer = new Timer(new TimerCallback(TimerCallback), null, 60000, 60000);
        }

        private void LoadDatabase()
        {
            try
            {
                GlobalInstances.ProfileMan = ProfileManager.Load(ProfileManager.DBPath);
            }
            catch
            {
                GlobalInstances.ProfileMan = new ProfileManager();
            }
        }

        private void UpdaterMethod(object state)
        {
            try
            {
                VisibleState.Update = UpdateChecker.GetDescriptor();
            }
            catch
            {
                // This is an automatic update check in the background.
                // If we fail (for whatever reason, no internet, server down etc.),
                // we fail silently.
            }
            finally
            {
                ActiveConfig.Service.LastUpdateCheck = DateTime.Now;
                ActiveConfig.Service.SequenceNumber = Utils.GetRandomNumber();
                ActiveConfig.Service.Save();
            }

            if (VisibleState.Update == null)
                return;

            UpdateModule module = UpdateChecker.GetDatabaseFileModule(VisibleState.Update);
            if (!module.DownloadHash.Equals(Utils.HexEncode(Hasher.HashFile(ProfileManager.DBPath)), StringComparison.OrdinalIgnoreCase))
            {
                GetCompressedUpdate(module, DatabaseUpdateInstall);
            }

            module = UpdateChecker.GetHostsFileModule(VisibleState.Update);
            if (!module.DownloadHash.Equals(HostsFileManager.GetHostsHash(), StringComparison.OrdinalIgnoreCase))
            {
                GetCompressedUpdate(module, HostsUpdateInstall);
            }
        }

        private void GetCompressedUpdate(UpdateModule module, WaitCallback installMethod)
        {
            string tmpCompressedPath = Path.GetTempFileName();
            string tmpFile = Path.GetTempFileName();
            try
            {
                using (WebClient downloader = new WebClient())
                {
                    downloader.DownloadFile(module.UpdateURL, tmpCompressedPath);
                }
                Utils.DecompressDeflate(tmpCompressedPath, tmpFile);

                if (Utils.HexEncode(Hasher.HashFile(tmpFile)).Equals(module.DownloadHash))
                    installMethod(tmpFile);
            }
            catch { }
            finally
            {
                if (File.Exists(tmpCompressedPath))
                    File.Delete(tmpCompressedPath);
                if (File.Exists(tmpFile))
                    File.Delete(tmpFile);
            }
        }

        private void HostsUpdateInstall(object file)
        {
            string tmpHostsPath = (string)file;
            HostsFileManager.UpdateHostsFile(tmpHostsPath);

            if (ActiveConfig.Service.Blocklists.EnableBlocklists
                && ActiveConfig.Service.Blocklists.EnableHostsBlocklist)
            {
                HostsFileManager.EnableHostsFile();
            }
        }
        private void DatabaseUpdateInstall(object file)
        {
            string tmpFilePath = (string)file;

            FileLocker.UnlockFile(ProfileManager.DBPath);
            File.Copy(tmpFilePath, ProfileManager.DBPath, true);
            FileLocker.LockFile(ProfileManager.DBPath, FileAccess.Read, FileShare.Read);
            NotifyController(TWServiceMessages.DATABASE_UPDATED);
            Q.Enqueue(new ReqResp(new Message(TWControllerMessages.REINIT)));
        }

        private void NotifyController(TWServiceMessages msg)
        {
            VisibleState.ClientNotifs.Add(msg);
            ActiveConfig.Service.SequenceNumber = Utils.GetRandomNumber();
        }

        internal void TimerCallback(Object state)
        {
            // This timer is called every minute.

            // Check if a timed exception has expired
            if (!Q.HasRequest(TWControllerMessages.MINUTE_TIMER))
                Q.Enqueue(new ReqResp(new Message(TWControllerMessages.MINUTE_TIMER)));

            // Check for inactivity and lock if necessary
            if (DateTime.Now - LastControllerCommandTime > TimeSpan.FromMinutes(10))
            {
                Q.Enqueue(new ReqResp(new Message(TWControllerMessages.LOCK)));
            }

            // Check for updates once every week
            if (ActiveConfig.Service.AutoUpdateCheck)
            {
                if (DateTime.Now - ActiveConfig.Service.LastUpdateCheck >= TimeSpan.FromDays(2))
                {
                    ThreadPool.QueueUserWorkItem(UpdaterMethod);
                }
            }
        }

        private List<FirewallLogEntry> GetFwLog()
        {
            if (LogWatcher == null)
            {
                LogWatcher = new FirewallLogWatcher();
                LogWatcher.NewLogEntry += new EventHandler(LogWatcher_NewLogEntry);
            }

            LastFwLogReadTime = DateTime.Now;
            return LogWatcher.QueryNewEntries();
        }

        private void LogWatcher_NewLogEntry(object sender, EventArgs e)
        {
            if (VisibleState.Mode != FirewallMode.Learning)
                return;

            lock (LogWatcher)
            {
                List<FirewallLogEntry> list = GetFwLog();
                foreach (FirewallLogEntry entry in list)
                {
                    if (  // IPv4
                        ((entry.DestinationIP.Equals("127.0.0.1", StringComparison.Ordinal)
                        && entry.SourceIP.Equals("127.0.0.1", StringComparison.Ordinal)))
                       || // IPv6
                        ((entry.DestinationIP.Equals("::1", StringComparison.Ordinal)
                        && entry.SourceIP.Equals("::1", StringComparison.Ordinal)))
                       )
                    {
                        // Ignore communication within local machine
                        continue;
                    }

                    if (entry.AppPath == null)
                        continue;

                    if (LearningNewExceptions == null)
                        LearningNewExceptions = new List<FirewallException>();

                    bool alreadyExists = false;
                    for (int j = 0; j < LearningNewExceptions.Count; ++j)
                    {
                        if (LearningNewExceptions[j].ExecutablePath.Equals(entry.AppPath, StringComparison.OrdinalIgnoreCase))
                        {
                            alreadyExists = true;
                            break;
                        }
                    }
                    if (alreadyExists)
                        continue;

                    if (LearningKnownApplication == null)
                        LearningKnownApplication = Utils.DeepClone(GlobalInstances.ProfileMan.KnownApplications);

                    FirewallException ex = new FirewallException(entry.AppPath, null);
                    if (((entry.Direction == RuleDirection.In) && (entry.Event == EventLogEvent.ALLOWED_CONNECTION))
                        || entry.Event == EventLogEvent.ALLOWED_LISTEN)
                    {
                        ex.OpenPortListenLocalTCP = "*";
                        ex.OpenPortListenLocalUDP = "*";
                    }
                    else
                    {
                        ex.OpenPortOutboundRemoteTCP = "*";
                        ex.OpenPortOutboundRemoteUDP = "*";
                    }
                    List<FirewallException> exceptions = FirewallException.CheckForAppDependencies(ex, false, false, false, LearningKnownApplication);
                    LearningNewExceptions.AddRange(exceptions);
                }
            }
        }

        private bool CommitLearnedRules()
        {
            bool needsSave = false;

            if (LogWatcher == null)
                return needsSave;

            if (LearningNewExceptions == null)
                return needsSave;

            if (LearningNewExceptions.Count > 0)
            {
                lock (LogWatcher)
                {
                    foreach (FirewallException ex in LearningNewExceptions)
                    {
                        needsSave = true;
                        ActiveConfig.Service.AppExceptions.Add(ex);
                    }

                    LearningKnownApplication = null;
                    LearningNewExceptions = null;
                }

                ActiveConfig.Service.SequenceNumber = Utils.GetRandomNumber();
            }

            return needsSave;
        }

        private Message ProcessCmd(Message req)
        {
            switch (req.Command)
            {
                case TWControllerMessages.READ_FW_LOG:
                    {
                        return new Message(TWControllerMessages.RESPONSE_OK, GetFwLog());
                    }
                case TWControllerMessages.PING:
                    {
                        return new Message(TWControllerMessages.RESPONSE_OK);
                    }
                case TWControllerMessages.MODE_SWITCH:
                    {
                        VisibleState.Mode = (FirewallMode)req.Arguments[0];

                        Policy Firewall = new Policy();
                        if (CommitLearnedRules())
                            ActiveConfig.Service.Save();

                        RebuildApplicationRuleDefs();
                        AssembleActiveRules();
                        MergeActiveRulesIntoWinFirewall(Firewall.GetRules(false));

                        if (
                               (VisibleState.Mode != FirewallMode.Disabled)
                            && (VisibleState.Mode != FirewallMode.Learning)
                           )
                        {
                            ActiveConfig.Service.StartupMode = VisibleState.Mode;
                            ActiveConfig.Service.Save();
                        }

                        if (Firewall.LocalPolicyModifyState == LocalPolicyState.GP_OVERRRIDE)
                            return new Message(TWControllerMessages.RESPONSE_WARNING);
                        else
                            return new Message(TWControllerMessages.RESPONSE_OK);
                    }
                case TWControllerMessages.PUT_SETTINGS:
                    {
                        ServiceSettings21 newConf = (ServiceSettings21)req.Arguments[0];
                        if (newConf.SequenceNumber == ActiveConfig.Service.SequenceNumber)
                        {
                            ActiveConfig.Service = newConf;
                            ActiveConfig.Service.SequenceNumber = Utils.GetRandomNumber();
                            ActiveConfig.Service.Save();
                            Policy Firewall = new Policy();
                            ReapplySettings(Firewall.GetRules(false));
                            return new Message(TWControllerMessages.RESPONSE_OK, ActiveConfig.Service.SequenceNumber);
                        }
                        else
                        {
                            return new Message(TWControllerMessages.RESPONSE_ERROR,
                                ActiveConfig.Service,
                                VisibleState
                                );
                        }
                    }
                case TWControllerMessages.GET_SETTINGS:
                    {
                        // Get changeset of client
                        int changeset = (int)req.Arguments[0];

                        // If our changeset is different, send new settings to client
                        if (changeset != ActiveConfig.Service.SequenceNumber)
                        {
                            VisibleState.HasPassword = ServiceLocker.HasPassword;
                            VisibleState.Locked = ServiceLocker.Locked;

                            Message ret = new Message(TWControllerMessages.RESPONSE_OK,
                                ActiveConfig.Service.SequenceNumber,
                                ActiveConfig.Service,
                                Utils.DeepClone(VisibleState)
                                );

                            VisibleState.ClientNotifs.Clear();
                            return ret;
                        }
                        else
                        {
                            // Our changeset is the same, so do not send settings again
                            return new Message(TWControllerMessages.RESPONSE_OK, ActiveConfig.Service.SequenceNumber);
                        }
                    }
                case TWControllerMessages.REINIT:
                    {
                        if (CommitLearnedRules())
                            ActiveConfig.Service.Save();
                        InitFirewall();
                        return new Message(TWControllerMessages.RESPONSE_OK);
                    }
                case TWControllerMessages.UNLOCK:
                    {
                        if (ServiceLocker.Unlock((string)req.Arguments[0]))
                            return new Message(TWControllerMessages.RESPONSE_OK);
                        else
                            return new Message(TWControllerMessages.RESPONSE_ERROR);
                    }
                case TWControllerMessages.LOCK:
                    {
                        ServiceLocker.Locked = true;
                        return new Message(TWControllerMessages.RESPONSE_OK);
                    }
                case TWControllerMessages.GET_PROCESS_PATH:
                    {
                        int pid = (int)req.Arguments[0];
                        string path = Utils.GetPathOfProcess(pid);
                        if (string.IsNullOrEmpty(path))
                            return new Message(TWControllerMessages.RESPONSE_ERROR);
                        else
                            return new Message(TWControllerMessages.RESPONSE_OK, path);
                    }
                case TWControllerMessages.SET_PASSPHRASE:
                    {
                        FileLocker.UnlockFile(ServiceSettings.PasswordFilePath);
                        try
                        {
                            ServiceLocker.SetPass((string)req.Arguments[0]);
                            return new Message(TWControllerMessages.RESPONSE_OK);
                        }
                        catch
                        {
                            return new Message(TWControllerMessages.RESPONSE_ERROR);
                        }
                        finally
                        {
                            FileLocker.LockFile(ServiceSettings.PasswordFilePath, FileAccess.Read, FileShare.Read);
                        }
                    }
                case TWControllerMessages.STOP_DISABLE:
                    {
                        UninstallRequested = true;
                        Shutdown();
                        Environment.Exit(0);
                        return new Message(TWControllerMessages.RESPONSE_OK);
                    }
                case TWControllerMessages.MINUTE_TIMER:
                    {
                        if (VisibleState.Mode != FirewallMode.Learning)
                        {
                            // Disable firewall logging if its log has not been read recently
                            if (DateTime.Now - LastFwLogReadTime > TimeSpan.FromMinutes(2))
                            {
                                if (LogWatcher != null)
                                {
                                    LogWatcher.Dispose();
                                    LogWatcher = null;
                                }
                            }
                        }

                        bool needsSave = false;

                        // Check all exceptions if any one has expired
                        Policy Firewall = new Policy();
                        WindowsFirewall.Rules FwRules = Firewall.GetRules(false);
                        List<FirewallException> exs = ActiveConfig.Service.AppExceptions;
                        for (int i = exs.Count-1; i >= 0; --i)
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
                                exs.RemoveAt(i);
                                needsSave = true;
                            }
                        }

                        if (needsSave)
                        {
                            ActiveConfig.Service.AppExceptions = exs;
                            ActiveConfig.Service.SequenceNumber = Utils.GetRandomNumber();
                            ActiveConfig.Service.Save();
                        }

                        return new Message(TWControllerMessages.RESPONSE_OK);
                    }
                default:
                    {
                        return new Message(TWControllerMessages.RESPONSE_ERROR);
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
            if (((int)req.Command > 2047) && ServiceLocker.Locked)
            {
                // Notify that we need to be unlocked first
                return new Message(TWControllerMessages.RESPONSE_LOCKED, 1);
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

        // This is a list of apps that are allowed to change firewall rules
        private readonly string[] WhitelistedApps = new string[]
                {
#if DEBUG
                    Path.Combine(Path.GetDirectoryName(Utils.ExecutablePath), "TinyWall.vshost.exe"),
#endif
                    Utils.ExecutablePath,
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "dllhost.exe")
                };
        private void WFEventWatcher_EventRecordWritten(object sender, EventRecordWrittenEventArgs e)
        {
            // Do nothing if the firewall is in disabled mode
            if (VisibleState.Mode == FirewallMode.Disabled)
                return;

            int propidx = -1;
            TWControllerMessages cmd = TWControllerMessages.REINIT;
            switch (e.EventRecord.Id)
            {
                case 2003:     // firewall setting changed
                    {
                        propidx = 7;
                        cmd = TWControllerMessages.REINIT;
                        break;
                    }
                case 2004:     // rule added
                    {
                        propidx = 22;
                        cmd = TWControllerMessages.REINIT;
                        break;
                    }
                case 2005:     // rule changed
                    {
                        propidx = 22;
                        cmd = TWControllerMessages.REINIT;
                        break;
                    }
                case 2006:     // rule deleted
                    {
                        propidx = 3;
                        cmd = TWControllerMessages.REINIT;
                        break;
                    }
                case 2010:     // network interface changed profile
                    {
                        cmd = TWControllerMessages.REINIT;
                        break;
                    }
                case 2032:     // firewall has been reset
                    {
                        propidx = 1;
                        cmd = TWControllerMessages.REINIT;
                        break;
                    }
                default:
                    break;
            }

            if (propidx != -1)
            {
                if (!Q.HasRequest(cmd))
                {
                    // If the rules were changed by an allowed app, do nothing
                    string EVpath = (string)e.EventRecord.Properties[propidx].Value;
                    for (int i = 0; i < WhitelistedApps.Length; ++i)
                    {
                        if (string.Compare(WhitelistedApps[i], EVpath, StringComparison.OrdinalIgnoreCase) == 0)
                            return;
                    }

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
#if !DEBUG
            // Register an unhandled exception handler
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
#endif
            
            // Continue initialization on a new thread to prevent stalling the SCM
            ThreadPool.QueueUserWorkItem((WaitCallback)delegate(object dummy)
            {
                EventLog.WriteEntry("TinyWall service starting up.");
                VisibleState = new ServiceState();

                FileLocker.LockFile(ProfileManager.DBPath, FileAccess.Read, FileShare.Read);
                FileLocker.LockFile(ServiceSettings.PasswordFilePath, FileAccess.Read, FileShare.Read);

                // Lock configuration if we have a password
                ServiceLocker = new ServiceSettings();
                if (ServiceLocker.HasPassword)
                    ServiceLocker.Locked = true;

                // Issue load command
                Q = new RequestQueue();
                Q.Enqueue(new ReqResp(new Message(TWControllerMessages.REINIT)));

                // Start thread that is going to control Windows Firewall
                FirewallWorkerThread = new Thread(new ThreadStart(FirewallWorkerMethod));
                FirewallWorkerThread.IsBackground = true;
                FirewallWorkerThread.Start();

                // Fire up pipe
                GlobalInstances.CommunicationMan = new PipeCom(new PipeDataReceived(PipeServerDataReceived));

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
            });
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
            FirewallWorkerThread.Abort();

            // Check all exceptions if any one has expired
            {
                List<FirewallException> exs = ActiveConfig.Service.AppExceptions;
                for (int i = exs.Count-1; i >= 0; --i)
                {
                    // Did this one expire?
                    if (exs[i].Timer == AppExceptionTimer.Until_Reboot)
                    {
                        // Remove exception
                        exs.RemoveAt(i);
                    }
                }
                ActiveConfig.Service.AppExceptions = exs;
            }

            CommitLearnedRules();
            ActiveConfig.Service.Save();

            if (LogWatcher != null)
            {
                LogWatcher.Dispose();
                LogWatcher = null;
            }

            try
            {
                if (!UninstallRequested)
                {
                    TinyWallDoctor.EnsureHealth();
                }
                else
                {
                    // Disable automatic re-start of service
                    using (ScmWrapper.ServiceControlManager scm = new ScmWrapper.ServiceControlManager())
                    {
                        scm.SetStartupMode(TinyWallService.SERVICE_NAME, ServiceStartMode.Automatic);
                        scm.SetRestartOnFailure(TinyWallService.SERVICE_NAME, false);
                    }
                }
            }
            catch { }

            Environment.Exit(0);
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
