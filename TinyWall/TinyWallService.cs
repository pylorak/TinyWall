using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Threading;
using TinyWall.Interface;
using TinyWall.Interface.Internal;
using NetFwTypeLib;
using WFPdotNet;
using WFPdotNet.Interop;

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
        private List<FirewallLogEntry> FirewallLogEntries = new List<FirewallLogEntry>();
        private ServiceSettings ServiceLocker = null;

        // Context needed for learning mode
        FirewallLogWatcher LogWatcher;
        List<FirewallExceptionV3> LearningNewExceptions = new List<FirewallExceptionV3>();
        
        private bool UninstallRequested = false;
        private bool RunService = false;

        private ServerState VisibleState = null;

        private Engine WfpEngine = null;
        private Guid ProviderKey = Guid.Empty;
        private Guid DynamicSublayerKey = Guid.Empty;
        private List<Filter> ActiveWfpFilters = new List<Filter>();

        private List<IpAddrMask> InterfaceAddreses = new List<IpAddrMask>();
        private List<IpAddrMask> GatewayAddresses = new List<IpAddrMask>();

        private void ExpandRule(RuleDef r, List<RuleDef> results)
        {
            if (r.Direction == RuleDirection.InOut)
            {
                RuleDef tmp = r.DeepCopy();
                tmp.Direction = RuleDirection.In;
                ExpandRule(tmp, results);

                tmp = r.DeepCopy();
                tmp.Direction = RuleDirection.Out;
                ExpandRule(tmp, results);
            }
            else if (r.Protocol == Protocol.TcpUdp)
            {
                RuleDef tmp = r.DeepCopy();
                tmp.Protocol = Protocol.TCP;
                ExpandRule(tmp, results);

                tmp = r.DeepCopy();
                tmp.Protocol = Protocol.UDP;
                ExpandRule(tmp, results);
            }
            else if (r.Protocol == Protocol.ICMP)
            {
                RuleDef tmp = r.DeepCopy();
                tmp.Protocol = Protocol.ICMPv4;
                ExpandRule(tmp, results);

                tmp = r.DeepCopy();
                tmp.Protocol = Protocol.ICMPv6;
                ExpandRule(tmp, results);
            }
            else if (!string.IsNullOrEmpty(r.IcmpTypesAndCodes) && r.IcmpTypesAndCodes.Contains(","))
            {
                string[] list = r.IcmpTypesAndCodes.Split(',');
                foreach (var e in list)
                {
                    RuleDef tmp = r.DeepCopy();
                    tmp.IcmpTypesAndCodes = e;
                    ExpandRule(tmp, results);
                }
            }
            else if (!string.IsNullOrEmpty(r.RemoteAddresses) && r.RemoteAddresses.Contains(","))
            {
                string[] addresses = r.RemoteAddresses.Split(',');
                foreach (var addr in addresses)
                {
                    RuleDef tmp = r.DeepCopy();
                    tmp.RemoteAddresses = addr.Trim();
                    ExpandRule(tmp, results);
                }
            }
            else if (Utils.EqualsCaseInsensitive(r.RemoteAddresses, "LocalSubnet"))
            {
                RuleDef tmp;
                foreach (var addr in InterfaceAddreses)
                {
                    tmp = r.DeepCopy();
                    tmp.RemoteAddresses = addr.SubnetFirstIp.ToString();
                    ExpandRule(tmp, results);
                }
                tmp = r.DeepCopy();
                tmp.RemoteAddresses = IpAddrMask.LinkLocal.ToString();
                ExpandRule(tmp, results);
                tmp = r.DeepCopy();
                tmp.RemoteAddresses = IpAddrMask.IPv6LinkLocal.ToString();
                ExpandRule(tmp, results);
                tmp = r.DeepCopy();
                tmp.RemoteAddresses = IpAddrMask.LinkLocalMulticast.ToString();
                ExpandRule(tmp, results);
                tmp = r.DeepCopy();
                tmp.RemoteAddresses = IpAddrMask.IPv6LinkLocalMulticast.ToString();
                ExpandRule(tmp, results);
            }
            else if (Utils.EqualsCaseInsensitive(r.RemoteAddresses, "DefaultGateway"))
            {
                foreach (var addr in GatewayAddresses)
                {
                    RuleDef tmp = r.DeepCopy();
                    tmp.RemoteAddresses = addr.Address.ToString();
                    ExpandRule(tmp, results);
                }
            }
            else
            {
                results.Add(r);
            }
        }

        private List<RuleDef> ExpandRules(List<RuleDef> rules)
        {
            List<RuleDef> results = new List<RuleDef>();

            foreach (var r in rules)
                ExpandRule(r, results);

            return results;
        }

        private static int BlockingRulesFirstComparison(RuleDef a, RuleDef b)
        {
            int x = (a.Action == RuleAction.Block) ? 0 : 1;
            int y = (a.Action == RuleAction.Block) ? 0 : 1;
            return x.CompareTo(y);
        }

        private List<RuleDef> AssembleActiveRules()
        {
            List<RuleDef> ActiveRules = new List<RuleDef>();
            ActiveRules.AddRange(RebuildApplicationRuleDefs());
            ActiveRules.AddRange(RebuildSpecialRuleDefs());
            ActiveRules.Sort(BlockingRulesFirstComparison);

            Guid ModeId = Guid.NewGuid();
            RuleDef def;

            // Do we want to let local traffic through?
            if (ActiveConfig.Service.ActiveProfile.AllowLocalSubnet)
            {
                def = new RuleDef(ModeId, "Allow local subnet", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.InOut, Protocol.Any);
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
                        ActiveRules.Add(new RuleDef(ModeId, "Allow outbound", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.Out, Protocol.Any));
                        break;
                    }
                case TinyWall.Interface.FirewallMode.BlockAll:
                    {
                        // Remove all exceptions
                        ActiveRules.Clear();
                        break;
                    }
                case TinyWall.Interface.FirewallMode.Disabled:
                    {
                        // Remove all rules
                        ActiveRules.Clear();

                        // Add rule to explicitly allow everything
                        ActiveRules.Add(new RuleDef(ModeId, "Allow everything", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.InOut, Protocol.Any));
                        break;
                    }
                case TinyWall.Interface.FirewallMode.Learning:
                    {
                        // Remove all rules
                        ActiveRules.Clear();

                        // Add rule to explicitly allow everything
                        ActiveRules.Add(new RuleDef(ModeId, "Allow everything", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.InOut, Protocol.Any));

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

            // Add a rule to deny all traffic. Denial rules have priority, so this will disable all traffic.
            def = new RuleDef(ModeId, "Block all traffic", GlobalSubject.Instance, RuleAction.Block, RuleDirection.InOut, Protocol.Any);
            ActiveRules.Add(def);

            return ExpandRules(ActiveRules);
        }

        private void InstallFirewallRules()
        {
            List<RuleDef> rules = AssembleActiveRules();

            using (Transaction trx = WfpEngine.BeginTransaction())
            {
                // Remove old rules
                for (int i = ActiveWfpFilters.Count - 1; i >= 0; --i)
                {
                    try
                    {
                        WfpEngine.UnregisterFilter(ActiveWfpFilters[i].FilterKey);
                        ActiveWfpFilters.RemoveAt(i);
                    }
                    catch { }
                }

                System.Diagnostics.Debug.Assert(ActiveWfpFilters.Count == 0);

                // Add new rules
                uint filterWeight = uint.MaxValue; 
                foreach (RuleDef r in rules)
                {
                    --filterWeight;
                     ConstructFilter(r, filterWeight);
                }

                trx.Commit();
            }
        }

        private enum LayerKeyEnum
        {
            FWPM_LAYER_OUTBOUND_ICMP_ERROR_V6,
            FWPM_LAYER_OUTBOUND_ICMP_ERROR_V4,
            FWPM_LAYER_INBOUND_ICMP_ERROR_V6,
            FWPM_LAYER_INBOUND_ICMP_ERROR_V4,
            FWPM_LAYER_ALE_AUTH_CONNECT_V6,
            FWPM_LAYER_ALE_AUTH_CONNECT_V4,
            FWPM_LAYER_ALE_AUTH_LISTEN_V6,
            FWPM_LAYER_ALE_AUTH_LISTEN_V4,
            FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V6,
            FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V4
        }

        private static Guid GetSublayerKey(LayerKeyEnum layer)
        {
            switch (layer)
            {
                case LayerKeyEnum.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V6:
                    return WfpSublayerKeys.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V6;
                case LayerKeyEnum.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V4:
                    return WfpSublayerKeys.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V4;
                case LayerKeyEnum.FWPM_LAYER_INBOUND_ICMP_ERROR_V6:
                    return WfpSublayerKeys.FWPM_LAYER_INBOUND_ICMP_ERROR_V6;
                case LayerKeyEnum.FWPM_LAYER_INBOUND_ICMP_ERROR_V4:
                    return WfpSublayerKeys.FWPM_LAYER_INBOUND_ICMP_ERROR_V4;
                case LayerKeyEnum.FWPM_LAYER_ALE_AUTH_CONNECT_V6:
                    return WfpSublayerKeys.FWPM_LAYER_ALE_AUTH_CONNECT_V6;
                case LayerKeyEnum.FWPM_LAYER_ALE_AUTH_CONNECT_V4:
                    return WfpSublayerKeys.FWPM_LAYER_ALE_AUTH_CONNECT_V4;
                case LayerKeyEnum.FWPM_LAYER_ALE_AUTH_LISTEN_V6:
                    return WfpSublayerKeys.FWPM_LAYER_ALE_AUTH_LISTEN_V6;
                case LayerKeyEnum.FWPM_LAYER_ALE_AUTH_LISTEN_V4:
                    return WfpSublayerKeys.FWPM_LAYER_ALE_AUTH_LISTEN_V4;
                case LayerKeyEnum.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V6:
                    return WfpSublayerKeys.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V6;
                case LayerKeyEnum.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V4:
                    return WfpSublayerKeys.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V4;
                default:
                    throw new ArgumentException("Invalid or not support layerEnum.");
            }
        }

        private static Guid GetLayerKey(LayerKeyEnum layer)
        {
            switch (layer)
            {
                case LayerKeyEnum.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V6:
                    return LayerKeys.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V6;
                case LayerKeyEnum.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V4:
                    return LayerKeys.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V4;
                case LayerKeyEnum.FWPM_LAYER_INBOUND_ICMP_ERROR_V6:
                    return LayerKeys.FWPM_LAYER_INBOUND_ICMP_ERROR_V6;
                case LayerKeyEnum.FWPM_LAYER_INBOUND_ICMP_ERROR_V4:
                    return LayerKeys.FWPM_LAYER_INBOUND_ICMP_ERROR_V4;
                case LayerKeyEnum.FWPM_LAYER_ALE_AUTH_CONNECT_V6:
                    return LayerKeys.FWPM_LAYER_ALE_AUTH_CONNECT_V6;
                case LayerKeyEnum.FWPM_LAYER_ALE_AUTH_CONNECT_V4:
                    return LayerKeys.FWPM_LAYER_ALE_AUTH_CONNECT_V4;
                case LayerKeyEnum.FWPM_LAYER_ALE_AUTH_LISTEN_V6:
                    return LayerKeys.FWPM_LAYER_ALE_AUTH_LISTEN_V6;
                case LayerKeyEnum.FWPM_LAYER_ALE_AUTH_LISTEN_V4:
                    return LayerKeys.FWPM_LAYER_ALE_AUTH_LISTEN_V4;
                case LayerKeyEnum.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V6:
                    return LayerKeys.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V6;
                case LayerKeyEnum.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V4:
                    return LayerKeys.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V4;
                default:
                    throw new ArgumentException("Invalid or not support layerEnum.");
            }
        }

        private void ConstructFilter(RuleDef r, uint filterWeight, LayerKeyEnum layer)
        {
            Filter f = new Filter(
                r.ExceptionId.ToString(),
                r.Name,
                ProviderKey,
                (r.Action == RuleAction.Allow) ? FilterActions.FWP_ACTION_PERMIT : FilterActions.FWP_ACTION_BLOCK,
                filterWeight
            );
            f.FilterKey = Guid.NewGuid();
            f.LayerKey = GetLayerKey(layer);
            f.SublayerKey = GetSublayerKey(layer);

            // We never want to affect loopback traffic
            if (VersionInfo.Win8OrNewer)
                f.Conditions.Add(new FlagsFilterCondition(ConditionFlags.FWP_CONDITION_FLAG_IS_LOOPBACK | ConditionFlags.FWP_CONDITION_FLAG_IS_APPCONTAINER_LOOPBACK | ConditionFlags.FWP_CONDITION_FLAG_IS_NON_APPCONTAINER_LOOPBACK, FieldMatchType.FWP_MATCH_FLAGS_NONE_SET));
            else
                f.Conditions.Add(new FlagsFilterCondition(ConditionFlags.FWP_CONDITION_FLAG_IS_LOOPBACK, FieldMatchType.FWP_MATCH_FLAGS_NONE_SET));


            if (!string.IsNullOrEmpty(r.Application))
            {
                System.Diagnostics.Debug.Assert(!r.Application.Equals("*"));
                if (!LayerIsIcmpError(layer))
                    f.Conditions.Add(new AppIdFilterCondition(r.Application));
                else
                    return;
            }
            if (r.Protocol != Protocol.Any)
            {
                System.Diagnostics.Debug.Assert(r.Protocol != Protocol.ICMP);
                System.Diagnostics.Debug.Assert(r.Protocol != Protocol.TcpUdp);
                if (LayerIsAleAuthConnect(layer) || LayerIsAleAuthRecvAccept(layer))
                    f.Conditions.Add(new ProtocolFilterCondition((byte)r.Protocol));
            }
            if (!string.IsNullOrEmpty(r.LocalPorts))
            {
                System.Diagnostics.Debug.Assert(!r.LocalPorts.Equals("*"));
                string[] ports = r.LocalPorts.Split(',');
                foreach (var p in ports)
                {
                    f.Conditions.Add(new PortFilterCondition(p, RemoteOrLocal.Local));
                }
            }
            if (!string.IsNullOrEmpty(r.RemotePorts) && !LayerIsAleAuthListen(layer))
            {
                System.Diagnostics.Debug.Assert(!r.RemotePorts.Equals("*"));
                string[] ports = r.RemotePorts.Split(',');
                foreach (var p in ports)
                {
                    f.Conditions.Add(new PortFilterCondition(p, RemoteOrLocal.Remote));
                }
            }
            if (!string.IsNullOrEmpty(r.RemoteAddresses) && !LayerIsAleAuthListen(layer))
            {
                System.Diagnostics.Debug.Assert(!r.RemoteAddresses.Equals("*"));

                IpAddrMask remote = IpAddrMask.Parse(r.RemoteAddresses);
                if (remote.IsIPv6 == LayerIsV6Stack(layer))
                    f.Conditions.Add(new IpFilterCondition(remote.Address, (byte)remote.PrefixLen, RemoteOrLocal.Remote));
                else
                    // Break. We don't want to add this filter to this layer.
                    return;
            }
            if (!string.IsNullOrEmpty(r.IcmpTypesAndCodes))
            {
                System.Diagnostics.Debug.Assert(!r.IcmpTypesAndCodes.Equals("*"));
                string[] list = r.IcmpTypesAndCodes.Split(',');
                foreach (var e in list)
                {
                    string[] tc = e.Split(':');

                    if (LayerIsIcmpError(layer))
                    {
                        // ICMP Type
                        if (!string.IsNullOrEmpty(tc[0]) && ushort.TryParse(tc[0], out ushort icmpType))
                        {
                            FWP_CONDITION_VALUE0 cv = new FWP_CONDITION_VALUE0();
                            cv.type = FWP_DATA_TYPE.FWP_UINT16;
                            cv.uint16 = icmpType;
                            f.Conditions.Add(new FilterCondition(ConditionKeys.FWPM_CONDITION_ICMP_TYPE, FieldMatchType.FWP_MATCH_EQUAL, cv));
                        }
                        // ICMP Code
                        if ((tc.Length > 1) && !string.IsNullOrEmpty(tc[1]) && ushort.TryParse(tc[1], out ushort icmpCode))
                        {
                            FWP_CONDITION_VALUE0 cv = new FWP_CONDITION_VALUE0();
                            cv.type = FWP_DATA_TYPE.FWP_UINT16;
                            cv.uint16 = icmpCode;
                            f.Conditions.Add(new FilterCondition(ConditionKeys.FWPM_CONDITION_ICMP_CODE, FieldMatchType.FWP_MATCH_EQUAL, cv));
                        }
                    }
                    else
                    {
                        // ICMP Type - note different condition key
                        if (!string.IsNullOrEmpty(tc[0]) && ushort.TryParse(tc[0], out ushort icmpType))
                        {
                            FWP_CONDITION_VALUE0 cv = new FWP_CONDITION_VALUE0();
                            cv.type = FWP_DATA_TYPE.FWP_UINT16;
                            cv.uint16 = icmpType;
                            f.Conditions.Add(new FilterCondition(ConditionKeys.FWPM_CONDITION_ORIGINAL_ICMP_TYPE, FieldMatchType.FWP_MATCH_EQUAL, cv));
                        }
                        // Matching on ICMP Code not possible
                    }
                }
            }

            try
            {
                WfpEngine.RegisterFilter(f);
                ActiveWfpFilters.Add(f);
            }
            catch { }
        }

        private static bool LayerIsAleAuthConnect(LayerKeyEnum layer)
        {
            return
                (layer == LayerKeyEnum.FWPM_LAYER_ALE_AUTH_CONNECT_V4) ||
                (layer == LayerKeyEnum.FWPM_LAYER_ALE_AUTH_CONNECT_V6);
        }

        private static bool LayerIsAleAuthRecvAccept(LayerKeyEnum layer)
        {
            return
                (layer == LayerKeyEnum.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V6) ||
                (layer == LayerKeyEnum.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V4);
        }

        private static bool LayerIsAleAuthListen(LayerKeyEnum layer)
        {
            return
                (layer == LayerKeyEnum.FWPM_LAYER_ALE_AUTH_LISTEN_V6) ||
                (layer == LayerKeyEnum.FWPM_LAYER_ALE_AUTH_LISTEN_V4);
        }

        private static bool LayerIsIcmpError(LayerKeyEnum layer)
        {
            return
                (layer == LayerKeyEnum.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V6) ||
                (layer == LayerKeyEnum.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V4) ||
                (layer == LayerKeyEnum.FWPM_LAYER_INBOUND_ICMP_ERROR_V6) ||
                (layer == LayerKeyEnum.FWPM_LAYER_INBOUND_ICMP_ERROR_V4);
        }

        private static bool LayerIsV6Stack(LayerKeyEnum layer)
        {
            return
                (layer == LayerKeyEnum.FWPM_LAYER_ALE_AUTH_CONNECT_V6) ||
                (layer == LayerKeyEnum.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V6) ||
                (layer == LayerKeyEnum.FWPM_LAYER_ALE_AUTH_LISTEN_V6) ||
                (layer == LayerKeyEnum.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V6) ||
                (layer == LayerKeyEnum.FWPM_LAYER_INBOUND_ICMP_ERROR_V6);
        }

        private void ConstructFilter(RuleDef r, uint filterWeight)
        {
            switch (r.Direction)
            {
                case RuleDirection.Out:
                    ConstructFilter(r, filterWeight, LayerKeyEnum.FWPM_LAYER_ALE_AUTH_CONNECT_V6);
                    ConstructFilter(r, filterWeight, LayerKeyEnum.FWPM_LAYER_ALE_AUTH_CONNECT_V4);
                    if ((r.Protocol == Protocol.Any) || (r.Protocol == Protocol.ICMPv4) || (r.Protocol == Protocol.ICMPv6))
                    {
                        ConstructFilter(r, filterWeight, LayerKeyEnum.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V6);
                        ConstructFilter(r, filterWeight, LayerKeyEnum.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V4);
                    }
                    break;
                case RuleDirection.In:
                    ConstructFilter(r, filterWeight, LayerKeyEnum.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V6);
                    ConstructFilter(r, filterWeight, LayerKeyEnum.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V4);
                    if ((r.Protocol == Protocol.Any) || (r.Protocol == Protocol.ICMPv4) || (r.Protocol == Protocol.ICMPv6))
                    {
                        ConstructFilter(r, filterWeight, LayerKeyEnum.FWPM_LAYER_INBOUND_ICMP_ERROR_V6);
                        ConstructFilter(r, filterWeight, LayerKeyEnum.FWPM_LAYER_INBOUND_ICMP_ERROR_V4);
                    }
                    if ((r.Protocol == Protocol.Any) || (r.Protocol == Protocol.TCP))
                    {
                        ConstructFilter(r, filterWeight, LayerKeyEnum.FWPM_LAYER_ALE_AUTH_LISTEN_V6);
                        ConstructFilter(r, filterWeight, LayerKeyEnum.FWPM_LAYER_ALE_AUTH_LISTEN_V4);
                    }
                    break;
                default:
                    throw new ArgumentException("Unsupported direction parameter.");
            }
        }

        private List<RuleDef> RebuildSpecialRuleDefs()
        {
            // We will collect all our rules into this list
            List<RuleDef> SpecialRules = new List<RuleDef>();

            // Iterate all enabled special exceptions
            foreach (string appName in ActiveConfig.Service.ActiveProfile.SpecialExceptions)
            {
                try
                {

                    // Retrieve database entry for appName
                    DatabaseClasses.Application app = GlobalInstances.AppDatabase.GetApplicationByName(appName);
                    if (app == null)
                        continue;

                    // Create rules
                    foreach (DatabaseClasses.SubjectIdentity id in app.Components)
                    {
                        try
                        {
                            List<ExceptionSubject> foundSubjects = id.SearchForFile();
                            foreach (var subject in foundSubjects)
                            {
                                try
                                {
                                    FirewallExceptionV3 ex = id.InstantiateException(subject);
                                    ex.RegenerateId();
                                    GetRulesForException(ex, SpecialRules);
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }

            return SpecialRules;
        }

        private List<RuleDef> RebuildApplicationRuleDefs()
        {
            // We will collect all our rules into this list
            List<RuleDef> AppExRules = new List<RuleDef>();

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

            return AppExRules;
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
                        foreach (var rule in pol.Rules)
                            rule.ExceptionId = ex.Id;
                        ruleset.AddRange(pol.Rules);
                        break;
                    }
            }
        }

        private static string ConfigSavePath
        {
            get
            {
                return Path.Combine(Utils.AppDataPath, "config");
            }
        }

        private static ServerConfiguration LoadServerConfig()
        {
            ServerConfiguration ret = null;

            try
            {
                ret = ServerConfiguration.Load(ConfigSavePath);
            }
            catch { }

            if (ret == null)
            {
                // Try again by loading config file from older versions
                try
                {
                    var oldSettings = ServiceSettings21.Load();
                    ret = oldSettings.ToNewFormat();
                }
                catch { }
            }

            if (ret == null)
            {
                ret = new ServerConfiguration();
                ret.SetActiveProfile(PKSoft.Resources.Messages.Default);

                // Allow recommended exceptions
                DatabaseClasses.AppDatabase db = GlobalInstances.AppDatabase;
                foreach (DatabaseClasses.Application app in db.KnownApplications)
                {
                    if (app.HasFlag("TWUI:Special") && app.HasFlag("TWUI:Recommended"))
                    {
                        ret.ActiveProfile.SpecialExceptions.Add(app.Name);
                    }
                }
            }

            return ret;
        }

        private static void EnableMpsSvcNotifications(bool enable)
        {
            Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);
            NET_FW_PROFILE_TYPE2_ fwCurrentProfileTypes = (NET_FW_PROFILE_TYPE2_)fwPolicy2.CurrentProfileTypes;
            fwPolicy2.set_NotificationsDisabled(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN, !enable);
            fwPolicy2.set_NotificationsDisabled(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE, !enable);
            fwPolicy2.set_NotificationsDisabled(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC, !enable);
        }

        // This method completely reinitializes the firewall.
        private void InitFirewall()
        {
            EnableMpsSvcNotifications(false);

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

                barrier.Wait();
                // --- THREAD BARRIER ---
            }

            ActiveConfig.Service = LoadServerConfig();
            GlobalInstances.ConfigChangeset = Guid.NewGuid();
            VisibleState.Mode = ActiveConfig.Service.StartupMode;

            ReapplySettings();
        }


        // This method reapplies all firewall settings.
        private void ReapplySettings()
        {
            InstallFirewallRules();

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
                GlobalInstances.AppDatabase = DatabaseClasses.AppDatabase.Load(DatabaseClasses.AppDatabase.DBPath);
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
                ActiveConfig.Service.Save(ConfigSavePath);
            }

            if (VisibleState.Update == null)
                return;

            UpdateModule module = UpdateChecker.GetDatabaseFileModule(VisibleState.Update);
            if (!module.DownloadHash.Equals(Hasher.HashFile(DatabaseClasses.AppDatabase.DBPath), StringComparison.OrdinalIgnoreCase))
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

            FileLocker.UnlockFile(DatabaseClasses.AppDatabase.DBPath);
            File.Copy(tmpFilePath, DatabaseClasses.AppDatabase.DBPath, true);
            FileLocker.LockFile(DatabaseClasses.AppDatabase.DBPath, FileAccess.Read, FileShare.Read);
            NotifyController(MessageType.DATABASE_UPDATED);
            Q.Enqueue(new TwMessage(MessageType.REINIT), null);
        }

        private void NotifyController(MessageType msg)
        {
            VisibleState.ClientNotifs.Add(msg);
            GlobalInstances.ConfigChangeset = Guid.NewGuid();
        }

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
            List<FirewallLogEntry> entries = new List<FirewallLogEntry>();
            lock (FirewallLogEntries)
            {
                entries.AddRange(FirewallLogEntries);
                FirewallLogEntries.Clear();
            }
            return entries;
        }

        private bool CommitLearnedRules()
        {
            lock (LearningNewExceptions)
            {
                bool needSave = (LearningNewExceptions.Count > 0);
                if (!needSave)
                    return false;

                foreach (FirewallExceptionV3 ex in LearningNewExceptions)
                    ActiveConfig.Service.ActiveProfile.AppExceptions.Add(ex);

                LearningNewExceptions.Clear();
            }

            ActiveConfig.Service.ActiveProfile.Normalize();
            GlobalInstances.ConfigChangeset = Guid.NewGuid();
            return true;
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
                case MessageType.MODE_SWITCH:
                    {
                        VisibleState.Mode = (TinyWall.Interface.FirewallMode)req.Arguments[0];

                        if (CommitLearnedRules())
                            ActiveConfig.Service.Save(ConfigSavePath);

                        InstallFirewallRules();

                        if (VisibleState.Mode == FirewallMode.Learning)
                        {
                            if (LogWatcher == null)
                            {
                                LogWatcher = new FirewallLogWatcher();
                                LogWatcher.NewLogEntry += LogWatcher_NewLogEntry;
                            }
                        }
                        else
                        {
                            LogWatcher?.Dispose();
                            LogWatcher = null;
                        }

                        if (
                               (VisibleState.Mode != TinyWall.Interface.FirewallMode.Disabled)
                            && (VisibleState.Mode != TinyWall.Interface.FirewallMode.Learning)
                           )
                        {
                            ActiveConfig.Service.StartupMode = VisibleState.Mode;
                            ActiveConfig.Service.Save(ConfigSavePath);
                        }

                        return new TwMessage(MessageType.RESPONSE_OK);
                    }
                case MessageType.PUT_SETTINGS:
                    {
                        ServerConfiguration newConf = (ServerConfiguration)req.Arguments[0];
                        Guid clientChangeset = (Guid)req.Arguments[1];
                        MessageType resp = (clientChangeset == GlobalInstances.ConfigChangeset) ? MessageType.RESPONSE_OK : MessageType.RESPONSE_WARNING;
                        if (MessageType.RESPONSE_OK == resp)
                        {
                            try
                            {
                                ActiveConfig.Service = newConf;
                                GlobalInstances.ConfigChangeset = Guid.NewGuid();
                                ActiveConfig.Service.Save(ConfigSavePath);
                                ReapplySettings();
                            }
                            catch (Exception e)
                            {
                                Utils.LogCrash(e);
                            }
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
                            ActiveConfig.Service.Save(ConfigSavePath);

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
                        bool needsSave = false;

                        // Check all exceptions if any one has expired
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
                                string exId = exs[i].Id.ToString();

                                // Search for the exception identifier in the rule name.
                                // Remove rules with a match.
                                for (int j = ActiveWfpFilters.Count-1; j >= 0; --j)
                                {
                                    if (ActiveWfpFilters[j].DisplayName.Contains(exId))
                                    {
                                        try
                                        {
                                            WfpEngine.UnregisterFilter(ActiveWfpFilters[j].FilterKey);
                                            ActiveWfpFilters.RemoveAt(j);
                                        }
                                        catch { }
                                    }
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
                            ActiveConfig.Service.Save(ConfigSavePath);
                        }

                        return new TwMessage(MessageType.RESPONSE_OK);
                    }
                case MessageType.REENUMERATE_ADDRESSES:
                    {
                        InterfaceAddreses.Clear();
                        GatewayAddresses.Clear();

                        NetworkInterface[] coll = NetworkInterface.GetAllNetworkInterfaces();
                        foreach (var iface in coll)
                        {
                            if (iface.OperationalStatus != OperationalStatus.Up)
                                continue;

                            var props = iface.GetIPProperties();

                            foreach (var uni in props.UnicastAddresses)
                            {
                                IpAddrMask am = new IpAddrMask(uni);
                                if (am.IsLoopback || am.IsLinkLocal)
                                    continue;

                                InterfaceAddreses.Add(am);
                            }

                            foreach (var uni in props.GatewayAddresses)
                            {
                                IpAddrMask am = new IpAddrMask(uni);
                                GatewayAddresses.Add(am);
                            }
                        }
                        return new TwMessage(MessageType.RESPONSE_OK);
                    }
                default:
                    {
                        return new TwMessage(MessageType.RESPONSE_ERROR);
                    }
            }
        }

        private void LogWatcher_NewLogEntry(FirewallLogWatcher sender, FirewallLogEntry entry)
        {
            AutoLearnLogEntry(entry);
        }

        // Entry point for thread that actually issues commands to Windows Firewall.
        // Only one thread (this one) is allowed to issue them.
        private void FirewallWorkerMethod()
        {
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            Guid TINYWALL_PROVIDER_KEY = new Guid("{66CA412C-4453-4F1E-A973-C16E433E34D0}");
            using (WfpEngine = new Engine("TinyWall Session", "", FWPM_SESSION_FLAGS.FWPM_SESSION_FLAG_DYNAMIC, 5000))
            using (var WfpEvent = WfpEngine.SubscribeNetEvent(WfpNetEventCallback, null))
            {
                // Check if TinyWall session already exists, and if it does, use it
                ProviderCollection coll = WfpEngine.GetProviders();
                foreach (FWPM_PROVIDER0 p in coll)
                {
                    if (0 == p.providerKey.CompareTo(TINYWALL_PROVIDER_KEY))
                        ProviderKey = TINYWALL_PROVIDER_KEY;
                }

                // Install a temporary provider if needed
                if (0 != ProviderKey.CompareTo(TINYWALL_PROVIDER_KEY))
                {
                    var provider = new FWPM_PROVIDER0();
                    provider.displayData.name = "TinyWall Temporary Provider";
                    provider.serviceName = TinyWallService.SERVICE_NAME;
                    ProviderKey = WfpEngine.RegisterProvider(ref provider);
                }

                // Check if the necessary sublayers are installed
                var layerKeys = (LayerKeyEnum[])Enum.GetValues(typeof(LayerKeyEnum));
                var subLayers = WfpEngine.GetSublayers();
                foreach (var layer in layerKeys)
                {
                    Guid slKey = GetSublayerKey(layer);

                    bool found = false;
                    foreach (var sub in subLayers)
                    {
                        if (slKey == sub.SublayerKey)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        // Install missing layer
                        var wfpSublayer = new Sublayer($"TinyWall Sublayer for {layer.ToString()}");
                        wfpSublayer.Weight = ushort.MaxValue >> 4;
                        wfpSublayer.SublayerKey = slKey;
                        wfpSublayer.ProviderKey = ProviderKey;
                        WfpEngine.RegisterSublayer(wfpSublayer);
                    }
                }


                try
                {
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
                    try
                    {
                        Cleanup();
                    }
                    finally { }
                }
            }
        }

        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            Q.Enqueue(new TwMessage(MessageType.REENUMERATE_ADDRESSES), null);
        }

        private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
            Q.Enqueue(new TwMessage(MessageType.REENUMERATE_ADDRESSES), null);
        }

        private void WfpNetEventCallback(object context, NetEventData data)
        {
            EventLogEvent eventType = EventLogEvent.ALLOWED;
            if (data.EventType == FWPM_NET_EVENT_TYPE.FWPM_NET_EVENT_TYPE_CLASSIFY_DROP)
                eventType = EventLogEvent.BLOCKED;
            else if (data.EventType == FWPM_NET_EVENT_TYPE.FWPM_NET_EVENT_TYPE_CLASSIFY_ALLOW)
                eventType = EventLogEvent.ALLOWED;
            else
                return;

            FirewallLogEntry entry = new FirewallLogEntry();
            entry.Timestamp = DateTime.Now;
            entry.Event = eventType;

            if (!string.IsNullOrEmpty(data.appId))
                entry.AppPath = Utils.DevicePathMapper.FromDevicePath(data.appId);
            else
                entry.AppPath = "System";
            entry.DestinationIP = data.remoteAddr?.ToString();
            if (data.remotePort.HasValue)
                entry.DestinationPort = data.remotePort.Value;
            entry.DestinationIP = data.remoteAddr?.ToString();
            if (data.direction.HasValue)
                entry.Direction = data.direction == FwpmDirection.FWP_DIRECTION_OUT ? RuleDirection.Out : RuleDirection.In;
            if (data.ipProtocol.HasValue)
                entry.Protocol = (Protocol)data.ipProtocol;
            if (data.localPort.HasValue)
                entry.SourcePort = data.localPort.Value;
            entry.SourceIP = data.localAddr?.ToString();

            // Replace invalid IP strings with the "unspecified address" IPv6 specifier
            if (string.IsNullOrEmpty(entry.DestinationIP))
                entry.DestinationIP = "::";
            if (string.IsNullOrEmpty(entry.SourceIP))
                entry.SourceIP = "::";

            // Maximum number of allowed entries
            const int MAX_ENTRIES = 1000;

            lock (FirewallLogEntries)
            {
                // Safe guard against using up all memory
                if (FirewallLogEntries.Count >= MAX_ENTRIES)
                {
                    // Keep the latest MAX_ENTRIES entries
                    int overLimit = FirewallLogEntries.Count - MAX_ENTRIES;
                    FirewallLogEntries.RemoveRange(0, overLimit);
                }

                FirewallLogEntries.Add(entry);
            }
        }

        private void AutoLearnLogEntry(FirewallLogEntry entry)
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
                return;
            }

            // Certain things we don't want to whitelist
            if (string.IsNullOrEmpty(entry.AppPath) || entry.AppPath.Equals("System", StringComparison.InvariantCultureIgnoreCase))
                return;

            ExecutableSubject newSubject = new ExecutableSubject(entry.AppPath);

            lock (LearningNewExceptions)
            {
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
                    return;

                List<FirewallExceptionV3> exceptions = GlobalInstances.AppDatabase.GetExceptionsForApp(newSubject, false, out DatabaseClasses.Application app);
                if (app == null)
                {
                    System.Diagnostics.Debug.Assert(exceptions.Count == 1);

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
                    LearningNewExceptions.AddRange(exceptions);
                }
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

                FileLocker.LockFile(DatabaseClasses.AppDatabase.DBPath, FileAccess.Read, FileShare.Read);
                FileLocker.LockFile(ServiceSettings.PasswordFilePath, FileAccess.Read, FileShare.Read);

                // Lock configuration if we have a password
                ServiceLocker = new ServiceSettings();
                if (ServiceLocker.HasPassword)
                    ServiceLocker.Locked = true;

                // Issue load command
                Q = new BoundedMessageQueue();
                Q.Enqueue(new TwMessage(MessageType.REENUMERATE_ADDRESSES), null);
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
            FirewallWorkerThread.Join(5000);
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
            EnableMpsSvcNotifications(true);

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

            LogWatcher?.Dispose();
            LogWatcher = null;
            CommitLearnedRules();

            ActiveConfig.Service.Save(ConfigSavePath);

#if !DEBUG
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
#endif
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
