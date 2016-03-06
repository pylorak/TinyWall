using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Net;
using System.ServiceProcess;
using System.Threading;
using PKSoft.WindowsFirewall;
using TinyWall.Interface;
using TinyWall.Interface.Internal;

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

        private BoundedMessageQueue Q;

        private Thread FirewallWorkerThread;
        private Timer MinuteTimer;
        private DateTime LastControllerCommandTime = DateTime.Now;
        private DateTime LastFwLogReadTime = DateTime.Now;
        private FirewallLogWatcher LogWatcher;
        private ServiceSettings ServiceLocker = null;

        // Context needed for learning mode
        List<FirewallExceptionV3> LearningNewExceptions;
        
        private List<RuleDef> ActiveRules;
        private List<RuleDef> AppExRules;
        private List<RuleDef> SpecialRules;

        private bool UninstallRequested = false;
        private bool RunService = false;

        private ServerState VisibleState = null;

        private void AssembleActiveRules()
        {
            ActiveRules.Clear();
            ActiveRules.AddRange(AppExRules);
            ActiveRules.AddRange(SpecialRules);

            Guid ModeId = Guid.NewGuid();

            // Do we want to let local traffic through?
            if (ActiveConfig.Service.ActiveProfile.AllowLocalSubnet)
            {
                RuleDef def = new RuleDef(ModeId, "Allow local subnet", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.InOut, Protocol.Any);
                def.RemoteAddresses = "LocalSubnet";
                ActiveRules.Add(def);
                
                def = new RuleDef(ModeId, "Allow local subnet (broadcast)", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.Out, Protocol.TcpUdp);
                def.RemoteAddresses = "255.255.255.255";
                ActiveRules.Add(def);
            }

            // Do we want to block known malware ports?
            if (ActiveConfig.Service.Blocklists.EnableBlocklists
                && ActiveConfig.Service.Blocklists.EnablePortBlocklist)
            {
                // TODO 
                /* Obsolete.Profile profileMalwarePortBlock = GlobalInstances.ProfileMan.GetProfile("Malware port block");
                if (profileMalwarePortBlock != null)
                {
                    foreach (RuleDef rule in profileMalwarePortBlock.Rules)
                        rule.ExceptionId = ModeId;
                    ActiveRules.AddRange(profileMalwarePortBlock.Rules);
                }
                */
            }

            // This switch should be executed last, as it might modify existing elements in ActiveRules
            switch (VisibleState.Mode)
            {
                case TinyWall.Interface.FirewallMode.AllowOutgoing:
                    {
                        // Add rule to explicitly allow outgoing connections
                        RuleDef def = new RuleDef(ModeId, "Allow outbound", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.Out, Protocol.Any);
                        ActiveRules.Add(def);
                        break;
                    }
                case TinyWall.Interface.FirewallMode.BlockAll:
                    {
                        // Remove all rules
                        ActiveRules.Clear();

                        // Add a rule to deny all traffic. Denial rules have priority, so this will disable all traffic.
                        RuleDef def = new RuleDef(ModeId, "Block all traffic", GlobalSubject.Instance, RuleAction.Block, RuleDirection.InOut, Protocol.Any);
                        ActiveRules.Add(def);
                        break;
                    }
                case TinyWall.Interface.FirewallMode.Disabled:
                    {
                        // Remove all rules
                        ActiveRules.Clear();

                        // Add rule to explicitly allow everything
                        RuleDef def = new RuleDef(ModeId, "Allow everything", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.InOut, Protocol.Any);
                        ActiveRules.Add(def);
                        break;
                    }
                case TinyWall.Interface.FirewallMode.Learning:
                    {
                        // Remove all rules
                        ActiveRules.Clear();

                        // Add rule to explicitly allow everything
                        RuleDef def = new RuleDef(ModeId, "Allow everything", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.InOut, Protocol.Any);
                        ActiveRules.Add(def);

                        // Start up firewall logging
                        Q.Enqueue(new TwMessage(MessageType.READ_FW_LOG), null);
                        break;
                    }
                case TinyWall.Interface.FirewallMode.Normal:
                    {
                        // Nothing to do here
                        break;
                    }
            }

            ActiveRules.TrimExcess();
        }

        private void MergeActiveRulesIntoWinFirewall(WindowsFirewall.Rules FwRules)
        {
            int lenId = Guid.NewGuid().ToString().Length;
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
                string id_i = rule_i.ExceptionId.ToString();

                bool found = false;
                for (int j = rule_names.Count - 1; j >= 0; --j)    // for each Win firewall rule
                {
                    string name_j = rule_names[j];

                    // Skip if this is not a TinyWall rule
                    //if (!name_j.StartsWith("[TW", StringComparison.Ordinal))
                    //    continue;

                    if (name_j.StartsWith(id_i, StringComparison.Ordinal))
                    {
                        found = true;
                        break;
                    }
                }

                // Rule is not yet active, add it to the Windows firewall
                if (!found)
                    Rule.ConstructRule(rules, rule_i);
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
                //if (!name_i.StartsWith("[TW", StringComparison.Ordinal))
                //    continue;

                bool found = false;
                for (int j = ActiveRules.Count - 1; j >= 0; --j)          // for each TW firewall rule
                {
                    RuleDef rule_j = ActiveRules[j];
                    string id_j = rule_j.ExceptionId.ToString();

                    if (name_i.StartsWith(id_j, StringComparison.Ordinal))
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

            /* TODO
            Obsolete.ApplicationCollection allApps = GlobalInstances.ProfileMan.KnownApplications;
            for (int i = 0; i < ActiveConfig.Service.ActiveProfile.SpecialExceptions.Count; ++i)
            {
                try
                {   //This try-catch will prevent errors if an exception profile string is invalid
                    Obsolete.Application app = allApps.GetApplicationByName(ActiveConfig.Service.ActiveProfile.SpecialExceptions[i]);
                    app.ResolveFilePaths();
                    foreach (Obsolete.AppExceptionAssoc template in app.FileTemplates)
                    {
                        foreach (string execPath in template.ExecutableRealizations)
                        {
                            FirewallExceptionV3 ex = template.CreateException(execPath).ToNewFormat();
                            ex.RegenerateId();
                            GetRulesForException(ex, SpecialRules);
                        }
                    }
                }
                catch { }
            }
*/
            SpecialRules.TrimExcess();
        }

        private void RebuildApplicationRuleDefs()
        {
            // We will collect all our rules into this list
            AppExRules.Clear();

            for (int i = 0; i < ActiveConfig.Service.ActiveProfile.AppExceptions.Count; ++i)
            {
                try
                {   //This try-catch will prevent errors if an exception profile string is invalid
                    FirewallExceptionV3 ex = ActiveConfig.Service.ActiveProfile.AppExceptions[i];
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

        private void GetRulesForException(FirewallExceptionV3 ex, List<RuleDef> ruleset)
        {
            if (ex.Id == Guid.Empty)
            {
// Do not let the service crash if a rule cannot be constructed 
#if DEBUG
                throw new InvalidOperationException("Firewall exception specification must have an ID.");
#else
                ex.RegenerateId();
                GlobalInstances.ConfigChangeset = Guid.NewGuid();
#endif
            }

            switch (ex.Policy.PolicyType)
            {
                case PolicyType.HardBlock:
                    {
                        RuleDef def = new RuleDef(ex.Id, "Block", ex.Subject, RuleAction.Block, RuleDirection.InOut, Protocol.Any);
                        ruleset.Add(def);
                        break;
                    }
                case PolicyType.Unrestricted:
                    {
                        RuleDef def = new RuleDef(ex.Id, "Full access", ex.Subject, RuleAction.Allow, RuleDirection.InOut, Protocol.Any);
                        if ((ex.Policy as UnrestrictedPolicy).LocalNetworkOnly)
                            def.RemoteAddresses = "LocalSubnet";
                        ruleset.Add(def);
                        break;
                    }
                case PolicyType.TcpUdpOnly:
                    {
                        TcpUdpPolicy pol = ex.Policy as TcpUdpPolicy;
                        if (!string.IsNullOrEmpty(pol.AllowedLocalTcpListenerPorts))
                        {
                            RuleDef def = new RuleDef(ex.Id, "TCP Listen Ports", ex.Subject, RuleAction.Allow, RuleDirection.In, Protocol.TCP);
                            if (!pol.AllowedLocalTcpListenerPorts.Equals("*"))
                                def.LocalPorts = pol.AllowedLocalTcpListenerPorts;
                            if (pol.LocalNetworkOnly)
                                def.RemoteAddresses = "LocalSubnet";
                            ruleset.Add(def);
                        }
                        if (!string.IsNullOrEmpty(pol.AllowedLocalUdpListenerPorts))
                        {
                            RuleDef def = new RuleDef(ex.Id, "UDP Listen Ports", ex.Subject, RuleAction.Allow, RuleDirection.In, Protocol.UDP);
                            if (!pol.AllowedLocalUdpListenerPorts.Equals("*"))
                                def.LocalPorts = pol.AllowedLocalUdpListenerPorts;
                            if (pol.LocalNetworkOnly)
                                def.RemoteAddresses = "LocalSubnet";
                            ruleset.Add(def);
                        }
                        if (!string.IsNullOrEmpty(pol.AllowedRemoteTcpConnectPorts))
                        {
                            RuleDef def = new RuleDef(ex.Id, "TCP Outbound Ports", ex.Subject, RuleAction.Allow, RuleDirection.Out, Protocol.TCP);
                            if (!pol.AllowedRemoteTcpConnectPorts.Equals("*"))
                                def.RemotePorts = pol.AllowedRemoteTcpConnectPorts;
                            if (pol.LocalNetworkOnly)
                                def.RemoteAddresses = "LocalSubnet";
                            ruleset.Add(def);
                        }
                        if (!string.IsNullOrEmpty(pol.AllowedRemoteUdpConnectPorts))
                        {
                            RuleDef def = new RuleDef(ex.Id, "UDP Outbound Ports", ex.Subject, RuleAction.Allow, RuleDirection.Out, Protocol.UDP);
                            if (!pol.AllowedRemoteUdpConnectPorts.Equals("*"))
                                def.RemotePorts = pol.AllowedRemoteUdpConnectPorts;
                            if (pol.LocalNetworkOnly)
                                def.RemoteAddresses = "LocalSubnet";
                            ruleset.Add(def);
                        }
                        break;
                    }
                case PolicyType.RuleList:
                    {
                        RuleListPolicy pol = ex.Policy as RuleListPolicy;
                        ruleset.AddRange(pol.Rules);
                        break;
                    }
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
                Firewall.DefaultInboundAction = RuleAction.Block;
                Firewall.DefaultOutboundAction = RuleAction.Block;
                Firewall.BlockAllInboundTraffic = false;
                Firewall.NotificationsDisabled = true;

                FwRules = Firewall.GetRules(false);
                for (int i = 0; i < FwRules.Count; ++i)
                    FwRules[i].Enabled = false;

                barrier.Wait();
                // --- THREAD BARRIER ---
            }

            ActiveConfig.Service = ConfigManager.LoadServerConfig();
            GlobalInstances.ConfigChangeset = Guid.NewGuid();
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

            // TODO: re-enable for release
            //MinuteTimer = new Timer(new TimerCallback(TimerCallback), null, 60000, 60000);
        }

        private void LoadDatabase()
        {
            try
            {
                GlobalInstances.AppDatabase = DatabaseClasses.AppDatabase.Load(Obsolete.ProfileManager.DBPath);
            }
            catch
            {
                GlobalInstances.AppDatabase = new DatabaseClasses.AppDatabase();
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
                ActiveConfig.Service.LastUpdateCheck = DateTime.Now;    // TODO do not invalidate client config just because LastUpdateCheck
                GlobalInstances.ConfigChangeset = Guid.NewGuid();
                ActiveConfig.Service.Save();
            }

            if (VisibleState.Update == null)
                return;

            UpdateModule module = UpdateChecker.GetDatabaseFileModule(VisibleState.Update);
            if (!module.DownloadHash.Equals(Hasher.HashFile(Obsolete.ProfileManager.DBPath), StringComparison.OrdinalIgnoreCase))
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

                if (Hasher.HashFile(tmpFile).Equals(module.DownloadHash, StringComparison.OrdinalIgnoreCase))
                    installMethod(tmpFile);
            }
            catch { }
            finally
            {
                try
                {
                    File.Delete(tmpCompressedPath);
                }
                catch { }

                try
                {
                    File.Delete(tmpFile);
                }
                catch { }
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

            FileLocker.UnlockFile(Obsolete.ProfileManager.DBPath);
            File.Copy(tmpFilePath, Obsolete.ProfileManager.DBPath, true);
            FileLocker.LockFile(Obsolete.ProfileManager.DBPath, FileAccess.Read, FileShare.Read);
            NotifyController(MessageType.DATABASE_UPDATED);
            Q.Enqueue(new TwMessage(MessageType.REINIT), null);
        }

        private void NotifyController(MessageType msg)
        {
            VisibleState.ClientNotifs.Add(msg);
            GlobalInstances.ConfigChangeset = Guid.NewGuid();        }

        internal void TimerCallback(Object state)
        {
            // This timer is called every minute.

            // Check if a timed exception has expired
            if (!Q.HasMessageType(MessageType.MINUTE_TIMER))
                Q.Enqueue(new TwMessage(MessageType.MINUTE_TIMER), null);

            // Check for inactivity and lock if necessary
            if (DateTime.Now - LastControllerCommandTime > TimeSpan.FromMinutes(10))
            {
                Q.Enqueue(new TwMessage(MessageType.LOCK), null);
            }

            // Check for updates once every 2 days
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
            if (VisibleState.Mode != TinyWall.Interface.FirewallMode.Learning)
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
                        LearningNewExceptions = new List<FirewallExceptionV3>();

                    ExecutableSubject newSubject = new ExecutableSubject(entry.AppPath);

                    bool alreadyExists = false;
                    for (int j = 0; j < LearningNewExceptions.Count; ++j)
                    {
                        if (LearningNewExceptions[j].Subject.Equals(newSubject))
                        {
                            alreadyExists = true;
                            break;
                        }
                    }
                    if (alreadyExists)
                        continue;

                    List<FirewallExceptionV3> knownExceptions = GlobalInstances.AppDatabase.GetExceptionsForApp(newSubject, false);
                    if (0 == knownExceptions.Count)
                    {
                        // Unknown file, add with unrestricted policy
                        FirewallExceptionV3 fwex = new FirewallExceptionV3(newSubject, null);
                        TcpUdpPolicy policy = new TcpUdpPolicy();
                        if (((entry.Direction == RuleDirection.In) && (entry.Event == EventLogEvent.ALLOWED_CONNECTION))
                            || entry.Event == EventLogEvent.ALLOWED_LISTEN)
                        {
                            policy.AllowedLocalTcpListenerPorts = "*";
                            policy.AllowedLocalUdpListenerPorts = "*";
                        }
                        else
                        {
                            policy.AllowedRemoteTcpConnectPorts = "*";
                            policy.AllowedRemoteUdpConnectPorts = "*";
                        }
                        fwex.Policy = policy;
                        LearningNewExceptions.Add(fwex);
                    }
                    else
                    {
                        // Known file, add its exceptions, along with other files that belong to this app
                        LearningNewExceptions.AddRange(knownExceptions);
                    }
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
                    foreach (FirewallExceptionV3 ex in LearningNewExceptions)
                    {
                        needsSave = true;
                        ActiveConfig.Service.ActiveProfile.AppExceptions.Add(ex);
                    }

                    LearningNewExceptions = null;
                }

                GlobalInstances.ConfigChangeset = Guid.NewGuid();
            }

            return needsSave;
        }

        private bool TestExceptionList(List<FirewallExceptionV3> testList)
        {
            try
            {
                List<RuleDef> defs = new List<RuleDef>();
                foreach (FirewallExceptionV3 ex in testList)
                {
                    GetRulesForException(ex, defs);
                }

                List<Rule> ruleList = new List<Rule>();
                foreach (RuleDef def in defs)
                {
                    Rule.ConstructRule(ruleList, def);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private TwMessage ProcessCmd(TwMessage req)
        {
            switch (req.Type)
            {
                case MessageType.READ_FW_LOG:
                    {
                        return new TwMessage(MessageType.RESPONSE_OK, GetFwLog());
                    }
                case MessageType.IS_LOCKED:
                    {
                      return new TwMessage(MessageType.RESPONSE_OK, ServiceLocker.Locked);
                    }
                case MessageType.PING:
                    {
                        return new TwMessage(MessageType.RESPONSE_OK);
                    }
                case MessageType.TEST_EXCEPTION:
                    {
                        List<FirewallExceptionV3> testList = req.Arguments[0] as List<FirewallExceptionV3>;
                        if (TestExceptionList(testList))
                            return new TwMessage(MessageType.RESPONSE_OK);
                        else
                            return new TwMessage(MessageType.RESPONSE_ERROR);
                    }
                case MessageType.MODE_SWITCH:
                    {
                        VisibleState.Mode = (TinyWall.Interface.FirewallMode)req.Arguments[0];

                        Policy Firewall = new Policy();
                        if (CommitLearnedRules())
                            ActiveConfig.Service.Save();

                        RebuildApplicationRuleDefs();
                        AssembleActiveRules();
                        MergeActiveRulesIntoWinFirewall(Firewall.GetRules(false));

                        if (
                               (VisibleState.Mode != TinyWall.Interface.FirewallMode.Disabled)
                            && (VisibleState.Mode != TinyWall.Interface.FirewallMode.Learning)
                           )
                        {
                            ActiveConfig.Service.StartupMode = VisibleState.Mode;
                            ActiveConfig.Service.Save();
                        }

                        if (Firewall.LocalPolicyModifyState == LocalPolicyState.GP_OVERRRIDE)
                            return new TwMessage(MessageType.RESPONSE_WARNING);
                        else
                            return new TwMessage(MessageType.RESPONSE_OK);
                    }
                case MessageType.PUT_SETTINGS:
                    {
                        ServerConfiguration newConf = (ServerConfiguration)req.Arguments[0];
                        Guid clientChangeset = (Guid)req.Arguments[1];
                        MessageType resp = (clientChangeset == GlobalInstances.ConfigChangeset) ? MessageType.RESPONSE_OK : MessageType.RESPONSE_WARNING;
                        if (MessageType.RESPONSE_OK == resp)
                        {
                            if (!TestExceptionList(newConf.ActiveProfile.AppExceptions))
                                return new TwMessage(MessageType.RESPONSE_ERROR);

                            ActiveConfig.Service = newConf;
                            GlobalInstances.ConfigChangeset = Guid.NewGuid();
                            ActiveConfig.Service.Save();
                            Policy Firewall = new Policy();
                            ReapplySettings(Firewall.GetRules(false));
                        }
                        return new TwMessage(resp, ActiveConfig.Service, GlobalInstances.ConfigChangeset, VisibleState);
                    }
                case MessageType.GET_SETTINGS:
                    {
                        // Get changeset of client
                        Guid changeset = (Guid)req.Arguments[0];

                        // If our changeset is different, send new settings to client
                        if (changeset != GlobalInstances.ConfigChangeset)
                        {
                            VisibleState.HasPassword = ServiceLocker.HasPassword;
                            VisibleState.Locked = ServiceLocker.Locked;

                            TwMessage ret = new TwMessage(MessageType.RESPONSE_OK,
                                GlobalInstances.ConfigChangeset,
                                ActiveConfig.Service,
                                Utils.DeepClone(VisibleState)
                                );

                            VisibleState.ClientNotifs.Clear();
                            return ret;
                        }
                        else
                        {
                            // Our changeset is the same, so do not send settings again
                            return new TwMessage(MessageType.RESPONSE_OK, GlobalInstances.ConfigChangeset);
                        }
                    }
                case MessageType.REINIT:
                    {
                        if (CommitLearnedRules())
                            ActiveConfig.Service.Save();
                        InitFirewall();
                        return new TwMessage(MessageType.RESPONSE_OK);
                    }
                case MessageType.UNLOCK:
                    {
                        if (ServiceLocker.Unlock((string)req.Arguments[0]))
                            return new TwMessage(MessageType.RESPONSE_OK);
                        else
                            return new TwMessage(MessageType.RESPONSE_ERROR);
                    }
                case MessageType.LOCK:
                    {
                        ServiceLocker.Locked = true;
                        return new TwMessage(MessageType.RESPONSE_OK);
                    }
                case MessageType.GET_PROCESS_PATH:
                    {
                        int pid = (int)req.Arguments[0];
                        string path = Utils.GetPathOfProcess(pid);
                        if (string.IsNullOrEmpty(path))
                            return new TwMessage(MessageType.RESPONSE_ERROR);
                        else
                            return new TwMessage(MessageType.RESPONSE_OK, path);
                    }
                case MessageType.SET_PASSPHRASE:
                    {
                        FileLocker.UnlockFile(ServiceSettings.PasswordFilePath);
                        try
                        {
                            ServiceLocker.SetPass((string)req.Arguments[0]);
                            return new TwMessage(MessageType.RESPONSE_OK);
                        }
                        catch
                        {
                            return new TwMessage(MessageType.RESPONSE_ERROR);
                        }
                        finally
                        {
                            FileLocker.LockFile(ServiceSettings.PasswordFilePath, FileAccess.Read, FileShare.Read);
                        }
                    }
                case MessageType.STOP_SERVICE:
                    {
                        RunService = false;
                        return new TwMessage(MessageType.RESPONSE_OK);
                    }
                case MessageType.STOP_DISABLE:
                    {
                        UninstallRequested = true;
                        RunService = false;
                        return new TwMessage(MessageType.RESPONSE_OK);
                    }
                case MessageType.MINUTE_TIMER:
                    {
                        if (VisibleState.Mode != TinyWall.Interface.FirewallMode.Learning)
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
                        List<FirewallExceptionV3> exs = ActiveConfig.Service.ActiveProfile.AppExceptions;
                        for (int i = exs.Count-1; i >= 0; --i)
                        {
                            // Timer values above zero are the number of minutes to stay active
                            if ((int)exs[i].Timer <= 0)
                                continue;

                            // Did this one expire?
                            if (exs[i].CreationDate.AddMinutes((double)exs[i].Timer) <= DateTime.Now)
                            {
                                // Remove rule
                                string appid = exs[i].Id.ToString();

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
                            ActiveConfig.Service.ActiveProfile.AppExceptions = exs;
                            GlobalInstances.ConfigChangeset = Guid.NewGuid();
                            ActiveConfig.Service.Save();
                        }

                        return new TwMessage(MessageType.RESPONSE_OK);
                    }
                default:
                    {
                        return new TwMessage(MessageType.RESPONSE_ERROR);
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
                    // TODO: deprecated, only need to handle this on Vista
                    EventLog.WriteEntry("Unable to listen for firewall events. Windows Firewall monitoring will be turned off.");
                }

                RunService = true;
                while (RunService)
                {
                    TwMessage msg;
                    Future<TwMessage> future;
                    Q.Dequeue(out msg, out future);

                    TwMessage resp;
                    resp = ProcessCmd(msg);
                    if (null != future)
                        future.Value = resp;
                }
            }
            finally
            {
                if (WFEventWatcher != null)
                    WFEventWatcher.Dispose();

                Cleanup();
                Environment.Exit(0);
            }
        }

        // Entry point for thread that listens to commands from the controller application.
        private TwMessage PipeServerDataReceived(TwMessage req)
        {
            if (((int)req.Type > 2047) && ServiceLocker.Locked)
            {
                // Notify that we need to be unlocked first
                return new TwMessage(MessageType.RESPONSE_LOCKED, 1);
            }
            else
            {
                LastControllerCommandTime = DateTime.Now;

                // Process and wait for response
                Future<TwMessage> future = new Future<TwMessage>();
                Q.Enqueue(req, future);
                TwMessage resp = future.Value;

                // Send response back to pipe
                return resp;
            }
        }

        // This is a list of apps that are allowed to change firewall rules
        private readonly string[] WhitelistedApps = new string[]
                {
#if DEBUG
                    Path.Combine(Path.GetDirectoryName(TinyWall.Interface.Internal.Utils.ExecutablePath), "TinyWall.vshost.exe"),
#endif
                    TinyWall.Interface.Internal.Utils.ExecutablePath,
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "dllhost.exe")
                };
        private void WFEventWatcher_EventRecordWritten(object sender, EventRecordWrittenEventArgs e)
        {
            // Do nothing if the firewall is in disabled mode
            if (VisibleState.Mode == TinyWall.Interface.FirewallMode.Disabled)
                return;

            int propidx = -1;
            MessageType cmd = MessageType.INVALID_COMMAND;
            switch (e.EventRecord.Id)
            {
                case 2003:     // firewall setting changed
                    {
                        propidx = 7;
                        cmd = MessageType.REINIT;
                        break;
                    }
                case 2004:     // rule added
                    {
                        propidx = 22;
                        cmd = MessageType.REINIT;
                        break;
                    }
                case 2005:     // rule changed
                    {
                        propidx = 22;
                        cmd = MessageType.REINIT;
                        break;
                    }
                case 2006:     // rule deleted
                    {
                        propidx = 3;
                        cmd = MessageType.REINIT;
                        break;
                    }
                case 2010:     // network interface changed profile
                    {
                        cmd = MessageType.REINIT;
                        break;
                    }
                case 2032:     // firewall has been reset
                    {
                        propidx = 1;
                        cmd = MessageType.REINIT;
                        break;
                    }
                default:
                    break;
            }

            if ((cmd != MessageType.INVALID_COMMAND) && (!Q.HasMessageType(cmd)))
            {
              /* EVpath has bogus empty value when run inside the IDE, so triggered by itself,
               * it brings the TW service into an infinite REINIT loop.
               * Hence we only allow the REINIT in release builds.
               * */
#if !DEBUG
                if (propidx != -1)
                {
                    // If the rules were changed by an allowed app, do nothing
                    string EVpath = (string)e.EventRecord.Properties[propidx].Value;
                    for (int i = 0; i < WhitelistedApps.Length; ++i)
                    {
                        if (string.Compare(WhitelistedApps[i], EVpath, StringComparison.OrdinalIgnoreCase) == 0)
                            return;
                    }
                    
                    if (string.IsNullOrEmpty(EVpath))
                      EventLog.WriteEntry("Reloading firewall configuration because an external process has modified it.");
                    else
                      EventLog.WriteEntry("Reloading firewall configuration because " + EVpath + " has modified it.");

                }

                Q.Enqueue(new TwMessage(cmd), null);
#endif
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
                VisibleState = new ServerState();

                FileLocker.LockFile(Obsolete.ProfileManager.DBPath, FileAccess.Read, FileShare.Read);
                FileLocker.LockFile(ServiceSettings.PasswordFilePath, FileAccess.Read, FileShare.Read);

                // Lock configuration if we have a password
                ServiceLocker = new ServiceSettings();
                if (ServiceLocker.HasPassword)
                    ServiceLocker.Locked = true;

                // Issue load command
                Q = new BoundedMessageQueue();
                Q.Enqueue(new TwMessage(MessageType.REINIT), null);

                // Start thread that is going to control Windows Firewall
                FirewallWorkerThread = new Thread(new ThreadStart(FirewallWorkerMethod));
                FirewallWorkerThread.IsBackground = true;
                FirewallWorkerThread.Start();

                // Fire up pipe
                GlobalInstances.ServerPipe = new PipeServerEndpoint(new PipeDataReceived(PipeServerDataReceived));

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
            RequestStop();
        }

        private void RequestStop()
        {
            TwMessage req = new TwMessage(MessageType.STOP_SERVICE);
            Future<TwMessage> future = new Future<TwMessage>();
            Q.Enqueue(req, future);
            TwMessage resp = future.Value;
        }

        private void Cleanup()
        {
            // Check all exceptions if any one has expired
            {
                List<FirewallExceptionV3> exs = ActiveConfig.Service.ActiveProfile.AppExceptions;
                for (int i = exs.Count-1; i >= 0; --i)
                {
                    // Did this one expire?
                    if (exs[i].Timer == AppExceptionTimer.Until_Reboot)
                    {
                        // Remove exception
                        exs.RemoveAt(i);
                    }
                }
                ActiveConfig.Service.ActiveProfile.AppExceptions = exs;
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
        }

        // Executed on computer shutdown.
        protected override void OnShutdown()
        {
            RequestStop();
        }

#if DEBUG
        internal void Start(string[] args)
        {
            this.OnStart(args);
        }
#endif
    }
}
