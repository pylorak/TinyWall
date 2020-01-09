using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.Management;
using System.ServiceProcess;
using System.Threading;
using TinyWall.Interface;
using TinyWall.Interface.Internal;
using NetFwTypeLib;
using WFPdotNet;
using WFPdotNet.Interop;

namespace PKSoft
{
    public sealed class TinyWallServer : IDisposable
    {
        private enum FilterWeights : ulong
        {
            Blocklist = 9000000,
            RawSockePermit = 8000000,
            RawSocketBlock = 7000000,
            UserBlock = 6000000,
            UserPermit = 5000000,
            DefaultPermit = 4000000,
            DefaultBlock = 3000000,
        }

        private static readonly Guid TINYWALL_PROVIDER_KEY = new Guid("{66CA412C-4453-4F1E-A973-C16E433E34D0}");

        private BoundedMessageQueue Q = new BoundedMessageQueue();

        private Timer MinuteTimer;
        private DateTime LastControllerCommandTime = DateTime.Now;
        private DateTime LastRuleReloadTime = DateTime.Now;
        private CircularBuffer<FirewallLogEntry> FirewallLogEntries = new CircularBuffer<FirewallLogEntry>(500);
        private PasswordManager ServiceLocker = new PasswordManager();

        // Context needed for learning mode
        private FirewallLogWatcher LogWatcher = new FirewallLogWatcher();
        private List<FirewallExceptionV3> LearningNewExceptions = new List<FirewallExceptionV3>();

        // Context for auto rule inheritance
        private readonly object InheritanceGuard = new object();
        private readonly StringBuilder ProcessStartWatcher_Sbuilder = new StringBuilder(1024);
        private HashSet<string> UserSubjectExes = new HashSet<string>();        // All executables with pre-configured rules.
        private Dictionary<string, FirewallExceptionV3> ChildInheritance = new Dictionary<string, FirewallExceptionV3>();
        private Dictionary<string, HashSet<string>> ChildInheritedSubjectExes = new Dictionary<string, HashSet<string>>();   // Executables that have been already auto-whitelisted due to inheritance
        private ThreadThrottler FirewallThreadThrottler;

        private bool RunService = false;
        private ServerState VisibleState = new ServerState();
        private DateTime LastUpdateCheck = DateTime.MinValue;

        private Engine WfpEngine;
        private DevicePathMapper NtPathMapper = new DevicePathMapper();

        private List<IpAddrMask> InterfaceAddreses = new List<IpAddrMask>();
        private List<IpAddrMask> GatewayAddresses = new List<IpAddrMask>();
        private List<IpAddrMask> DnsAddresses = new List<IpAddrMask>();

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
                tmp.RemoteAddresses = "255.255.255.255";
                ExpandRule(tmp, results);
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
            else if (Utils.EqualsCaseInsensitive(r.RemoteAddresses, "DNS"))
            {
                foreach (var addr in DnsAddresses)
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

        private List<RuleDef> AssembleActiveRules(List<ExceptionSubject> rawSocketExceptions)
        {
            List<RuleDef> rules = new List<RuleDef>();
            Guid ModeId = Guid.NewGuid();
            RuleDef def;


            // Do we want to let local traffic through?
            if (ActiveConfig.Service.ActiveProfile.AllowLocalSubnet)
            {
                def = new RuleDef(ModeId, "Allow local subnet", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.InOut, Protocol.Any, (ulong)FilterWeights.DefaultPermit);
                def.RemoteAddresses = "LocalSubnet";
                ExpandRule(def, rules);
            }

            // Do we want to block known malware ports?
            if (ActiveConfig.Service.Blocklists.EnableBlocklists && ActiveConfig.Service.Blocklists.EnablePortBlocklist)
            {
                List<FirewallExceptionV3> exceptions = new List<FirewallExceptionV3>();
                exceptions.AddRange(CollectExceptionsForAppByName("Malware Ports"));
                foreach (FirewallExceptionV3 ex in exceptions)
                {
                    ex.RegenerateId();
                    GetRulesForException(ex, rules, null, (ulong)FilterWeights.DefaultPermit, (ulong)FilterWeights.Blocklist);
                }
            }

            // Rules specific to the selected firewall mode
            bool needUserRules = true;
            switch (VisibleState.Mode)
            {
                case TinyWall.Interface.FirewallMode.AllowOutgoing:
                    {
                        // Add rule to explicitly allow outgoing connections
                        def = new RuleDef(ModeId, "Allow outbound", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.Out, Protocol.Any, (ulong)FilterWeights.DefaultPermit);
                        ExpandRule(def, rules);

                        // Block rest
                        def = new RuleDef(ModeId, "Block incoming", GlobalSubject.Instance, RuleAction.Block, RuleDirection.In, Protocol.Any, (ulong)FilterWeights.DefaultBlock);
                        ExpandRule(def, rules);
                        break;
                    }
                case TinyWall.Interface.FirewallMode.BlockAll:
                    {
                        // We won't need application exceptions
                        needUserRules = false;

                        // Block all
                        def = new RuleDef(ModeId, "Block everything", GlobalSubject.Instance, RuleAction.Block, RuleDirection.InOut, Protocol.Any, (ulong)FilterWeights.DefaultBlock);
                        ExpandRule(def, rules);
                        break;
                    }
                case TinyWall.Interface.FirewallMode.Learning:
                    {
                        // Add rule to explicitly allow everything
                        def = new RuleDef(ModeId, "Allow everything", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.InOut, Protocol.Any, (ulong)FilterWeights.DefaultPermit);
                        ExpandRule(def, rules);
                        break;
                    }
                case TinyWall.Interface.FirewallMode.Disabled:
                    {
                        // We won't need application exceptions
                        needUserRules = false;

                        // Add rule to explicitly allow everything
                        def = new RuleDef(ModeId, "Allow everything", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.InOut, Protocol.Any, (ulong)FilterWeights.DefaultPermit);
                        ExpandRule(def, rules);
                        break;
                    }
                case TinyWall.Interface.FirewallMode.Normal:
                    {
                        // Block all by default
                        def = new RuleDef(ModeId, "Block everything", GlobalSubject.Instance, RuleAction.Block, RuleDirection.InOut, Protocol.Any, (ulong)FilterWeights.DefaultBlock);
                        ExpandRule(def, rules);
                        break;
                    }
            }

            if (needUserRules)
            {
                List<FirewallExceptionV3> UserExceptions = new List<FirewallExceptionV3>();

                // Collect all applications exceptions
                UserExceptions.AddRange(ActiveConfig.Service.ActiveProfile.AppExceptions);

                // Collect all special exceptions
                foreach (string appName in ActiveConfig.Service.ActiveProfile.SpecialExceptions)
                    UserExceptions.AddRange(CollectExceptionsForAppByName(appName));

                // Convert exceptions to rules
                foreach (FirewallExceptionV3 ex in UserExceptions)
                {
                    if (ex.Subject is ExecutableSubject exe)
                    {
                        string exePath = exe.ExecutablePath.ToLowerInvariant();
                        UserSubjectExes.Add(exePath);
                        if (ex.ChildProcessesInherit)
                        {
                            ChildInheritance.Add(exePath, ex);
                        }
                    }

                    GetRulesForException(ex, rules, rawSocketExceptions, (ulong)FilterWeights.UserPermit, (ulong)FilterWeights.UserBlock);
                }

                if (ChildInheritance.Count != 0)
                {
                    StringBuilder sbuilder = new StringBuilder(1024);
                    Dictionary<int, ProcessManager.PROCESSENTRY32> procTree = new Dictionary<int, ProcessManager.PROCESSENTRY32>();
                    foreach (var p in ProcessManager.CreateToolhelp32Snapshot())
                    {
                        var p2 = p;
                        string tmpPath = ProcessManager.GetProcessPath(p.th32ProcessID, sbuilder);
                        if (!string.IsNullOrEmpty(tmpPath))
                            p2.szExeFile = tmpPath.ToLowerInvariant();
                        procTree.Add(p2.th32ProcessID, p2);
                    }

                    foreach (var pair in procTree)
                    {
                        string procPath = pair.Value.szExeFile;

                        // Skip if we have no path
                        if (string.IsNullOrEmpty(procPath))
                            continue;

                        // Skip if we have a user-defined rule for this path
                        if (UserSubjectExes.Contains(procPath))
                            continue;

                        // Start walking up the process tree
                        for (ProcessManager.PROCESSENTRY32 parentEntry = procTree[pair.Key]; ;)
                        {
                            if (procTree.ContainsKey(parentEntry.th32ParentProcessID))
                                parentEntry = procTree[parentEntry.th32ParentProcessID];
                            else
                                // We reached top of process tree (with non-existing parent)
                                break;

                            if (parentEntry.th32ProcessID == 0)
                                // We reached top of process tree (with idle process)
                                break;

                            if (string.IsNullOrEmpty(parentEntry.szExeFile))
                                // We cannot get the path, so let's skip this parent
                                continue;

                            if (ChildInheritedSubjectExes.ContainsKey(procPath) && ChildInheritedSubjectExes[procPath].Contains(parentEntry.szExeFile))
                                // We have already processed this parent-child combination
                                break;

                            if (ChildInheritance.TryGetValue(parentEntry.szExeFile, out FirewallExceptionV3 userEx))
                            {
                                FirewallExceptionV3 ex = Utils.DeepClone(userEx);
                                ex.Subject = new ExecutableSubject(procPath);
                                GetRulesForException(ex, rules, rawSocketExceptions, (ulong)FilterWeights.UserPermit, (ulong)FilterWeights.UserBlock);
                                if (!ChildInheritedSubjectExes.ContainsKey(procPath))
                                    ChildInheritedSubjectExes.Add(procPath, new HashSet<string>());
                                ChildInheritedSubjectExes[procPath].Add(parentEntry.szExeFile);
                                break;
                            }
                        }
                    }
                }   // if (ChildInheritance ...
            }

            return rules;
        }

        private void InstallRules(List<RuleDef> rules, List<ExceptionSubject> rawSocketExceptions, bool useTransaction)
        {
            Transaction trx = useTransaction ? WfpEngine.BeginTransaction() : null;
            try
            {

                // Add new rules
                foreach (RuleDef r in rules)
                {
                    try
                    {
                        ConstructFilter(r);
                    }
                    catch { }
                }

                // Built-in protections
                if (VisibleState.Mode != FirewallMode.Disabled)
                {
                    InstallRawSocketPermits(rawSocketExceptions);
                }

                trx?.Commit();
            }
            finally
            {
                trx?.Dispose();
            }

        }

        private void InstallFirewallRules()
        {
            LastRuleReloadTime = DateTime.Now;

            List<RuleDef> rules;
            List<ExceptionSubject> rawSocketExceptions = new List<ExceptionSubject>();
            lock (InheritanceGuard)
            {
                UserSubjectExes.Clear();
                ChildInheritance.Clear();
                ChildInheritedSubjectExes.Clear();
                rules = AssembleActiveRules(rawSocketExceptions);
            }

            using (Transaction trx = WfpEngine.BeginTransaction())
            {
                // Remove all existing WFP objects
                DeleteWfpObjects(WfpEngine, true);

                // Install provider
                var provider = new FWPM_PROVIDER0();
                provider.displayData.name = "Karoly Pados";
                provider.displayData.description = "TinyWall Provider";
                provider.serviceName = TinyWallService.SERVICE_NAME;
                provider.flags = FWPM_PROVIDER_FLAGS.FWPM_PROVIDER_FLAG_PERSISTENT;
                provider.providerKey = TINYWALL_PROVIDER_KEY;
                Guid providerKey = WfpEngine.RegisterProvider(ref provider);
                Debug.Assert(TINYWALL_PROVIDER_KEY == providerKey);

                // Install sublayers
                var layerKeys = (LayerKeyEnum[])Enum.GetValues(typeof(LayerKeyEnum));
                foreach (var layer in layerKeys)
                {
                    Guid slKey = GetSublayerKey(layer);
                    var wfpSublayer = new Sublayer($"TinyWall Sublayer for {layer.ToString()}");
                    wfpSublayer.Weight = ushort.MaxValue >> 4;
                    wfpSublayer.SublayerKey = slKey;
                    wfpSublayer.ProviderKey = TINYWALL_PROVIDER_KEY;
                    wfpSublayer.Flags = FWPM_SUBLAYER_FLAGS.FWPM_SUBLAYER_FLAG_PERSISTENT;
                    WfpEngine.RegisterSublayer(wfpSublayer);
                }

                // Add standard protections
                if (VisibleState.Mode != FirewallMode.Disabled)
                {
                    InstallPortScanProtection();
                    InstallRawSocketBlocks();
                }

                InstallRules(rules, rawSocketExceptions, false);

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
            FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V4,
            FWPM_LAYER_INBOUND_TRANSPORT_V6_DISCARD,
            FWPM_LAYER_INBOUND_TRANSPORT_V4_DISCARD,
            FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V6,
            FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V4,
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
                case LayerKeyEnum.FWPM_LAYER_INBOUND_TRANSPORT_V6_DISCARD:
                    return WfpSublayerKeys.FWPM_LAYER_INBOUND_TRANSPORT_V6_DISCARD;
                case LayerKeyEnum.FWPM_LAYER_INBOUND_TRANSPORT_V4_DISCARD:
                    return WfpSublayerKeys.FWPM_LAYER_INBOUND_TRANSPORT_V4_DISCARD;
                case LayerKeyEnum.FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V6:
                    return WfpSublayerKeys.FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V6;
                case LayerKeyEnum.FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V4:
                    return WfpSublayerKeys.FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V4;
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
                case LayerKeyEnum.FWPM_LAYER_INBOUND_TRANSPORT_V6_DISCARD:
                    return LayerKeys.FWPM_LAYER_INBOUND_TRANSPORT_V6_DISCARD;
                case LayerKeyEnum.FWPM_LAYER_INBOUND_TRANSPORT_V4_DISCARD:
                    return LayerKeys.FWPM_LAYER_INBOUND_TRANSPORT_V4_DISCARD;
                case LayerKeyEnum.FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V6:
                    return LayerKeys.FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V6;
                case LayerKeyEnum.FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V4:
                    return LayerKeys.FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V4;
                default:
                    throw new ArgumentException("Invalid or not support layerEnum.");
            }
        }

        private void InstallWfpFilter(Filter f)
        {
            try
            {
                f.FilterKey = Guid.NewGuid();
                f.Flags = FilterFlags.FWPM_FILTER_FLAG_PERSISTENT;
                WfpEngine.RegisterFilter(f);

                f.FilterKey = Guid.NewGuid();
                f.Flags = FilterFlags.FWPM_FILTER_FLAG_BOOTTIME;
                WfpEngine.RegisterFilter(f);
            }
            catch { }
        }

        private void ConstructFilter(RuleDef r, LayerKeyEnum layer)
        {
            List<FilterCondition> conditions = new List<FilterCondition>();


            if (!string.IsNullOrEmpty(r.ServiceName))
            {
                System.Diagnostics.Debug.Assert(!r.ServiceName.Equals("*"));
                if (!LayerIsIcmpError(layer))
                    conditions.Add(new ServiceNameFilterCondition(r.ServiceName));
                else
                    return;
            }

            if (!string.IsNullOrEmpty(r.Application))
            {
                System.Diagnostics.Debug.Assert(!r.Application.Equals("*"));
                if (!LayerIsIcmpError(layer))
                    conditions.Add(new AppIdFilterCondition(r.Application));
                else
                    return;
            }

            if (!string.IsNullOrEmpty(r.RemoteAddresses) && !LayerIsAleAuthListen(layer))
            {
                System.Diagnostics.Debug.Assert(!r.RemoteAddresses.Equals("*"));

                IpAddrMask remote = IpAddrMask.Parse(r.RemoteAddresses);
                if (remote.IsIPv6 == LayerIsV6Stack(layer))
                    conditions.Add(new IpFilterCondition(remote.Address, (byte)remote.PrefixLen, RemoteOrLocal.Remote));
                else
                    // Break. We don't want to add this filter to this layer.
                    return;
            }

            // We never want to affect loopback traffic
            conditions.Add(new FlagsFilterCondition(ConditionFlags.FWP_CONDITION_FLAG_IS_LOOPBACK, FieldMatchType.FWP_MATCH_FLAGS_NONE_SET));

            if (r.Protocol != Protocol.Any)
            {
                System.Diagnostics.Debug.Assert(r.Protocol != Protocol.ICMP);
                System.Diagnostics.Debug.Assert(r.Protocol != Protocol.TcpUdp);
                if (LayerIsAleAuthConnect(layer) || LayerIsAleAuthRecvAccept(layer))
                    conditions.Add(new ProtocolFilterCondition((byte)r.Protocol));
            }
            if (!string.IsNullOrEmpty(r.LocalPorts))
            {
                System.Diagnostics.Debug.Assert(!r.LocalPorts.Equals("*"));
                string[] ports = r.LocalPorts.Split(',');
                foreach (var p in ports)
                    conditions.Add(new PortFilterCondition(p, RemoteOrLocal.Local));
            }
            if (!string.IsNullOrEmpty(r.RemotePorts) && !LayerIsAleAuthListen(layer))
            {
                System.Diagnostics.Debug.Assert(!r.RemotePorts.Equals("*"));
                string[] ports = r.RemotePorts.Split(',');
                foreach (var p in ports)
                    conditions.Add(new PortFilterCondition(p, RemoteOrLocal.Remote));
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
                            conditions.Add(new FilterCondition(ConditionKeys.FWPM_CONDITION_ICMP_TYPE, FieldMatchType.FWP_MATCH_EQUAL, cv));
                        }
                        // ICMP Code
                        if ((tc.Length > 1) && !string.IsNullOrEmpty(tc[1]) && ushort.TryParse(tc[1], out ushort icmpCode))
                        {
                            FWP_CONDITION_VALUE0 cv = new FWP_CONDITION_VALUE0();
                            cv.type = FWP_DATA_TYPE.FWP_UINT16;
                            cv.uint16 = icmpCode;
                            conditions.Add(new FilterCondition(ConditionKeys.FWPM_CONDITION_ICMP_CODE, FieldMatchType.FWP_MATCH_EQUAL, cv));
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
                            conditions.Add(new FilterCondition(ConditionKeys.FWPM_CONDITION_ORIGINAL_ICMP_TYPE, FieldMatchType.FWP_MATCH_EQUAL, cv));
                        }
                        // Matching on ICMP Code not possible
                    }
                }
            }

            Filter f = new Filter(
                r.ExceptionId.ToString(),
                r.Name,
                TINYWALL_PROVIDER_KEY,
                (r.Action == RuleAction.Allow) ? FilterActions.FWP_ACTION_PERMIT : FilterActions.FWP_ACTION_BLOCK,
                r.Weight
            );
            f.LayerKey = GetLayerKey(layer);
            f.SublayerKey = GetSublayerKey(layer);
            f.Conditions.AddRange(conditions);

            InstallWfpFilter(f);
        }

        private void InstallRawSocketBlocks()
        {
            InstallRawSocketBlocks(LayerKeyEnum.FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V4);
            InstallRawSocketBlocks(LayerKeyEnum.FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V6);
        }

        private void InstallRawSocketBlocks(LayerKeyEnum layer)
        {
            Filter f = new Filter(
                "Raw socket block",
                string.Empty,
                TINYWALL_PROVIDER_KEY,
                FilterActions.FWP_ACTION_BLOCK,
                (ulong)FilterWeights.RawSocketBlock
            );
            f.LayerKey = GetLayerKey(layer);
            f.SublayerKey = GetSublayerKey(layer);
            f.Conditions.Add(new FlagsFilterCondition(ConditionFlags.FWP_CONDITION_FLAG_IS_RAW_ENDPOINT, FieldMatchType.FWP_MATCH_FLAGS_ANY_SET));

            InstallWfpFilter(f);
        }

        private void InstallRawSocketPermits(List<ExceptionSubject> rawSocketExceptions)
        {
            InstallRawSocketPermits(rawSocketExceptions, LayerKeyEnum.FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V4);
            InstallRawSocketPermits(rawSocketExceptions, LayerKeyEnum.FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V6);
        }

        private void InstallRawSocketPermits(List<ExceptionSubject> rawSocketExceptions, LayerKeyEnum layer)
        {
            foreach (var subj in rawSocketExceptions)
            {
                try
                {
                    Filter f = new Filter(
                        "Raw socket permit",
                        string.Empty,
                        TINYWALL_PROVIDER_KEY,
                        FilterActions.FWP_ACTION_PERMIT,
                        (ulong)FilterWeights.RawSockePermit
                    );
                    f.LayerKey = GetLayerKey(layer);
                    f.SublayerKey = GetSublayerKey(layer);

                    if ((subj is ExecutableSubject exe) && !string.IsNullOrEmpty(exe.ExecutablePath))
                        f.Conditions.Add(new AppIdFilterCondition(exe.ExecutablePath));
                    if ((subj is ServiceSubject srv) && !string.IsNullOrEmpty(srv.ServiceName))
                        f.Conditions.Add(new ServiceNameFilterCondition(srv.ServiceName));

                    InstallWfpFilter(f);
                }
                catch { }
            }
        }

        private void InstallPortScanProtection()
        {
            InstallPortScanProtection(LayerKeyEnum.FWPM_LAYER_INBOUND_TRANSPORT_V4_DISCARD, BuiltinCallouts.FWPM_CALLOUT_WFP_TRANSPORT_LAYER_V4_SILENT_DROP);
            InstallPortScanProtection(LayerKeyEnum.FWPM_LAYER_INBOUND_TRANSPORT_V6_DISCARD, BuiltinCallouts.FWPM_CALLOUT_WFP_TRANSPORT_LAYER_V6_SILENT_DROP);
        }

        private void InstallPortScanProtection(LayerKeyEnum layer, Guid callout)
        {
            Filter f = new Filter(
                "Port Scanning Protection",
                string.Empty,
                TINYWALL_PROVIDER_KEY,
                FilterActions.FWP_ACTION_CALLOUT_TERMINATING,
                (ulong)FilterWeights.Blocklist
            );
            f.LayerKey = GetLayerKey(layer);
            f.SublayerKey = GetSublayerKey(layer);
            f.CalloutKey = callout;

            // Don't affect loopback traffic
            f.Conditions.Add(new FlagsFilterCondition(ConditionFlags.FWP_CONDITION_FLAG_IS_LOOPBACK | ConditionFlags.FWP_CONDITION_FLAG_IS_IPSEC_SECURED, FieldMatchType.FWP_MATCH_FLAGS_NONE_SET));

            InstallWfpFilter(f);
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

        private void ConstructFilter(RuleDef r)
        {
            switch (r.Direction)
            {
                case RuleDirection.Out:
                    ConstructFilter(r, LayerKeyEnum.FWPM_LAYER_ALE_AUTH_CONNECT_V6);
                    ConstructFilter(r, LayerKeyEnum.FWPM_LAYER_ALE_AUTH_CONNECT_V4);
                    if ((r.Protocol == Protocol.Any) || (r.Protocol == Protocol.ICMPv4) || (r.Protocol == Protocol.ICMPv6))
                    {
                        ConstructFilter(r, LayerKeyEnum.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V6);
                        ConstructFilter(r, LayerKeyEnum.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V4);
                    }
                    break;
                case RuleDirection.In:
                    ConstructFilter(r, LayerKeyEnum.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V6);
                    ConstructFilter(r, LayerKeyEnum.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V4);
                    if ((r.Protocol == Protocol.Any) || (r.Protocol == Protocol.ICMPv4) || (r.Protocol == Protocol.ICMPv6))
                    {
                        ConstructFilter(r, LayerKeyEnum.FWPM_LAYER_INBOUND_ICMP_ERROR_V6);
                        ConstructFilter(r, LayerKeyEnum.FWPM_LAYER_INBOUND_ICMP_ERROR_V4);
                    }
                    break;
                default:
                    throw new ArgumentException("Unsupported direction parameter.");
            }
        }

        private List<FirewallExceptionV3> CollectExceptionsForAppByName(string name)
        {
            List<FirewallExceptionV3> exceptions = new List<FirewallExceptionV3>();

            try
            {
                // Retrieve database entry for appName
                DatabaseClasses.Application app = GlobalInstances.AppDatabase.GetApplicationByName(name);
                if (app == null)
                    return exceptions;

                // Create rules
                foreach (DatabaseClasses.SubjectIdentity id in app.Components)
                {
                    try
                    {
                        List<ExceptionSubject> foundSubjects = id.SearchForFile();
                        foreach (var subject in foundSubjects)
                        {
                            exceptions.Add(id.InstantiateException(subject));
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return exceptions;
        }

        private void GetRulesForException(FirewallExceptionV3 ex, List<RuleDef> results, List<ExceptionSubject> rawSocketExceptions, ulong permitWeight, ulong blockWeight)
        {
            if (ex.Id == Guid.Empty)
            {
                // Do not let the service crash if a rule cannot be constructed 
#if DEBUG
                throw new InvalidOperationException("Firewall exception specification must have an ID.");
#else
                ex.RegenerateId();
                GlobalInstances.ServerChangeset = Guid.NewGuid();
#endif
            }

            switch (ex.Policy.PolicyType)
            {
                case PolicyType.HardBlock:
                    {
                        RuleDef def = new RuleDef(ex.Id, "Block", ex.Subject, RuleAction.Block, RuleDirection.InOut, Protocol.Any, blockWeight);
                        ExpandRule(def, results);
                        break;
                    }
                case PolicyType.Unrestricted:
                    {
                        RuleDef def = new RuleDef(ex.Id, "Full access", ex.Subject, RuleAction.Allow, RuleDirection.InOut, Protocol.Any, permitWeight);
                        if ((ex.Policy as UnrestrictedPolicy).LocalNetworkOnly)
                            def.RemoteAddresses = "LocalSubnet";
                        ExpandRule(def, results);

                        if (rawSocketExceptions != null)
                        {
                            // Make exception for promiscuous mode
                            rawSocketExceptions.Add(ex.Subject);
                        }

                        break;
                    }
                case PolicyType.TcpUdpOnly:
                    {
                        TcpUdpPolicy pol = ex.Policy as TcpUdpPolicy;
                        if (!string.IsNullOrEmpty(pol.AllowedLocalTcpListenerPorts))
                        {
                            RuleDef def = new RuleDef(ex.Id, "TCP Listen Ports", ex.Subject, RuleAction.Allow, RuleDirection.In, Protocol.TCP, permitWeight);
                            if (!pol.AllowedLocalTcpListenerPorts.Equals("*"))
                                def.LocalPorts = pol.AllowedLocalTcpListenerPorts;
                            if (pol.LocalNetworkOnly)
                                def.RemoteAddresses = "LocalSubnet";
                            ExpandRule(def, results);
                        }
                        if (!string.IsNullOrEmpty(pol.AllowedLocalUdpListenerPorts))
                        {
                            RuleDef def = new RuleDef(ex.Id, "UDP Listen Ports", ex.Subject, RuleAction.Allow, RuleDirection.In, Protocol.UDP, permitWeight);
                            if (!pol.AllowedLocalUdpListenerPorts.Equals("*"))
                                def.LocalPorts = pol.AllowedLocalUdpListenerPorts;
                            if (pol.LocalNetworkOnly)
                                def.RemoteAddresses = "LocalSubnet";
                            ExpandRule(def, results);
                        }
                        if (!string.IsNullOrEmpty(pol.AllowedRemoteTcpConnectPorts))
                        {
                            RuleDef def = new RuleDef(ex.Id, "TCP Outbound Ports", ex.Subject, RuleAction.Allow, RuleDirection.Out, Protocol.TCP, permitWeight);
                            if (!pol.AllowedRemoteTcpConnectPorts.Equals("*"))
                                def.RemotePorts = pol.AllowedRemoteTcpConnectPorts;
                            if (pol.LocalNetworkOnly)
                                def.RemoteAddresses = "LocalSubnet";
                            ExpandRule(def, results);
                        }
                        if (!string.IsNullOrEmpty(pol.AllowedRemoteUdpConnectPorts))
                        {
                            RuleDef def = new RuleDef(ex.Id, "UDP Outbound Ports", ex.Subject, RuleAction.Allow, RuleDirection.Out, Protocol.UDP, permitWeight);
                            if (!pol.AllowedRemoteUdpConnectPorts.Equals("*"))
                                def.RemotePorts = pol.AllowedRemoteUdpConnectPorts;
                            if (pol.LocalNetworkOnly)
                                def.RemoteAddresses = "LocalSubnet";
                            ExpandRule(def, results);
                        }
                        break;
                    }
                case PolicyType.RuleList:
                    {
                        RuleListPolicy pol = ex.Policy as RuleListPolicy;
                        foreach (var rule in pol.Rules)
                        {
                            rule.ExceptionId = ex.Id;
                            rule.Weight = (rule.Action == RuleAction.Allow) ? permitWeight : blockWeight;
                            ExpandRule(rule, results);
                        }
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

        private static INetFwPolicy2 GetFwPolicy2()
        {
            Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            return (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);
        }

        private static INetFwRule CreateFwRule(string name, NET_FW_ACTION_ action, NET_FW_RULE_DIRECTION_ dir)
        {
            Type tNetFwRule = Type.GetTypeFromProgID("HNetCfg.FwRule");
            INetFwRule rule = (INetFwRule)Activator.CreateInstance(tNetFwRule);

            rule.Name = name;
            rule.Action = action;
            rule.Direction = dir;
            rule.Grouping = "TinyWall";
            rule.Profiles = (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE | (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC | (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN;
            rule.Enabled = true;
            if ((NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN == dir) && (NET_FW_ACTION_.NET_FW_ACTION_ALLOW == action))
                rule.EdgeTraversal = true;

            return rule;
        }

        private void DisableMpsSvc()
        {
            try
            {
                INetFwPolicy2 fwPolicy2 = GetFwPolicy2();

                // Disable Windows Firewall notifications
                fwPolicy2.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = true;
                fwPolicy2.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = true;
                fwPolicy2.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = true;

                // Add new rules
                string newRuleId = $"TinyWall Compat [{Utils.RandomString(6)}]";
                fwPolicy2.Rules.Add(CreateFwRule(newRuleId, NET_FW_ACTION_.NET_FW_ACTION_ALLOW, NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN));
                fwPolicy2.Rules.Add(CreateFwRule(newRuleId, NET_FW_ACTION_.NET_FW_ACTION_ALLOW, NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT));

                // Remove earlier rules
                INetFwRules rules = fwPolicy2.Rules;
                foreach (INetFwRule rule in rules)
                {
                    string ruleName = rule.Name;
                    if (!string.IsNullOrEmpty(ruleName) && ruleName.Contains("TinyWall") && (ruleName != newRuleId))
                        rules.Remove(rule.Name);
                }
            }
            catch { }
        }

        private void RestoreMpsSvc()
        {
            try
            {
                INetFwPolicy2 fwPolicy2 = GetFwPolicy2();

                // Enable Windows Firewall notifications
                fwPolicy2.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = false;
                fwPolicy2.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = false;
                fwPolicy2.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = false;

                // Remove earlier rules
                INetFwRules rules = fwPolicy2.Rules;
                foreach (INetFwRule rule in rules)
                {
                    if ((rule.Grouping != null) && rule.Grouping.Equals("TinyWall"))
                        rules.Remove(rule.Name);
                }
            }
            catch { }
        }

        // This method completely reinitializes the firewall.
        private void InitFirewall()
        {
            LoadDatabase();
            ActiveConfig.Service = LoadServerConfig();
            VisibleState.Mode = ActiveConfig.Service.StartupMode;
            GlobalInstances.ServerChangeset = Guid.NewGuid();

            ReapplySettings();
            InstallFirewallRules();
            ThreadPool.QueueUserWorkItem((WaitCallback)delegate (object state) { DisableMpsSvc(); });
        }


        // This method reapplies all firewall settings.
        private void ReapplySettings()
        {
            HostsFileManager.EnableProtection(ActiveConfig.Service.LockHostsFile);
            if (ActiveConfig.Service.Blocklists.EnableBlocklists
                && ActiveConfig.Service.Blocklists.EnableHostsBlocklist)
                HostsFileManager.EnableHostsFile();
            else
                HostsFileManager.DisableHostsFile();
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

        private void UpdaterMethod()
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
                return;
            }
            finally
            {
                LastUpdateCheck = DateTime.Now;    // TODO do not invalidate client config just because LastUpdateCheck
                GlobalInstances.ServerChangeset = Guid.NewGuid();
                ActiveConfig.Service.Save(ConfigSavePath);
            }

            if (VisibleState.Update == null)
                return;

            try
            {
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
            catch (Exception e)
            {
                Utils.LogCrash(e, Utils.LOG_ID_SERVICE);
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
            GlobalInstances.ServerChangeset = Guid.NewGuid();
        }

        internal void TimerCallback(Object state)
        {
            // This timer is called every minute.

            if (!Q.HasMessageType(MessageType.MINUTE_TIMER))
                Q.Enqueue(new TwMessage(MessageType.MINUTE_TIMER), null);
        }

        private List<FirewallLogEntry> GetFwLog()
        {
            List<FirewallLogEntry> entries = new List<FirewallLogEntry>();
            lock (FirewallLogEntries)
            {
                entries.AddRange(FirewallLogEntries);
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

                ActiveConfig.Service.ActiveProfile.AppExceptions.AddRange(LearningNewExceptions);
                LearningNewExceptions.Clear();
            }

            ActiveConfig.Service.ActiveProfile.Normalize();
            GlobalInstances.ServerChangeset = Guid.NewGuid();
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

                        LogWatcher.Enabled = (VisibleState.Mode == FirewallMode.Learning);

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
                        MessageType resp = (clientChangeset == GlobalInstances.ServerChangeset) ? MessageType.RESPONSE_OK : MessageType.RESPONSE_WARNING;
                        if (MessageType.RESPONSE_OK == resp)
                        {
                            try
                            {
                                ActiveConfig.Service = newConf;
                                GlobalInstances.ServerChangeset = Guid.NewGuid();
                                ActiveConfig.Service.Save(ConfigSavePath);
                                ReapplySettings();
                                InstallFirewallRules();
                            }
                            catch (Exception e)
                            {
                                Utils.LogCrash(e, Utils.LOG_ID_SERVICE);
                            }
                        }
                        VisibleState.HasPassword = ServiceLocker.HasPassword;
                        VisibleState.Locked = ServiceLocker.Locked;
                        return new TwMessage(resp, ActiveConfig.Service, GlobalInstances.ServerChangeset, VisibleState);
                    }
                case MessageType.ADD_TEMPORARY_EXCEPTION:
                    {
                        List<RuleDef> rules = new List<RuleDef>();
                        List<ExceptionSubject> rawSocketExceptions = new List<ExceptionSubject>();
                        List<FirewallExceptionV3> exceptions = req.Arguments[0] as List<FirewallExceptionV3>;

                        foreach (var ex in exceptions)
                        {
                            GetRulesForException(ex, rules, rawSocketExceptions, (ulong)FilterWeights.UserPermit, (ulong)FilterWeights.UserBlock);
                        }

                        InstallRules(rules, rawSocketExceptions, true);
                        FirewallThreadThrottler.Release();

                        return new TwMessage(MessageType.RESPONSE_OK);
                    }
                case MessageType.GET_SETTINGS:
                    {
                        // Get changeset of client
                        Guid changeset = (Guid)req.Arguments[0];

                        // If our changeset is different, send new settings to client
                        if (changeset != GlobalInstances.ServerChangeset)
                        {
                            VisibleState.HasPassword = ServiceLocker.HasPassword;
                            VisibleState.Locked = ServiceLocker.Locked;

                            TwMessage ret = new TwMessage(MessageType.RESPONSE_OK,
                                GlobalInstances.ServerChangeset,
                                ActiveConfig.Service,
                                VisibleState
                                );

                            VisibleState.ClientNotifs.Clear();
                            return ret;
                        }
                        else
                        {
                            // Our changeset is the same, so do not send settings again
                            return new TwMessage(MessageType.RESPONSE_OK, GlobalInstances.ServerChangeset);
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
                        bool success = ServiceLocker.Unlock((string)req.Arguments[0]);
                        GlobalInstances.ServerChangeset = Guid.NewGuid();
                        if (success)
                            return new TwMessage(MessageType.RESPONSE_OK);
                        else
                            return new TwMessage(MessageType.RESPONSE_ERROR);
                    }
                case MessageType.LOCK:
                    {
                        ServiceLocker.Locked = true;
                        GlobalInstances.ServerChangeset = Guid.NewGuid();
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
                        FileLocker.UnlockFile(PasswordManager.PasswordFilePath);
                        try
                        {
                            ServiceLocker.SetPass((string)req.Arguments[0]);
                            GlobalInstances.ServerChangeset = Guid.NewGuid();
                            return new TwMessage(MessageType.RESPONSE_OK);
                        }
                        catch
                        {
                            return new TwMessage(MessageType.RESPONSE_ERROR);
                        }
                        finally
                        {
                            FileLocker.LockFile(PasswordManager.PasswordFilePath, FileAccess.Read, FileShare.Read);
                        }
                    }
                case MessageType.STOP_SERVICE:
                    {
                        RunService = false;
                        return new TwMessage(MessageType.RESPONSE_OK);
                    }
                case MessageType.MINUTE_TIMER:
                    {
                        bool needsSave = false;

                        // Check for inactivity and lock if necessary
                        if (DateTime.Now - LastControllerCommandTime > TimeSpan.FromMinutes(10))
                        {
                            Q.Enqueue(new TwMessage(MessageType.LOCK), null);
                        }

                        // Check all exceptions if any has expired
                        List<FirewallExceptionV3> exs = ActiveConfig.Service.ActiveProfile.AppExceptions;
                        for (int i = exs.Count - 1; i >= 0; --i)
                        {
                            // Timer values above zero are the number of minutes to stay active
                            if ((int)exs[i].Timer <= 0)
                                continue;

                            // Did this one expire?
                            if (exs[i].CreationDate.AddMinutes((double)exs[i].Timer) <= DateTime.Now)
                            {
                                // Remove exception
                                exs.RemoveAt(i);
                                needsSave = true;
                            }
                        }
                        if (needsSave)
                        {
                            ActiveConfig.Service.ActiveProfile.AppExceptions = exs;
                            GlobalInstances.ServerChangeset = Guid.NewGuid();
                            ActiveConfig.Service.Save(ConfigSavePath);
                            InstallFirewallRules();
                        }

                        // Periodically reload all rules.
                        // This is needed to clear out temprary rules
                        // added due to child-process rule inheritance.
                        if (DateTime.Now - LastRuleReloadTime > TimeSpan.FromMinutes(30))
                        {
                            InstallFirewallRules();
                        }

                        // Check for updates once every 2 days
                        if (ActiveConfig.Service.AutoUpdateCheck)
                        {
                            if (DateTime.Now - LastUpdateCheck >= TimeSpan.FromDays(2))
                            {
#if !DEBUG
                                UpdaterMethod();
#endif
                            }
                        }

                        return new TwMessage(MessageType.RESPONSE_OK);
                    }
                case MessageType.REENUMERATE_ADDRESSES:
                    {
                        InterfaceAddreses.Clear();
                        GatewayAddresses.Clear();
                        DnsAddresses.Clear();

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

                            foreach (var uni in props.DnsAddresses)
                            {
                                IpAddrMask am = new IpAddrMask(uni);
                                DnsAddresses.Add(am);
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

        internal static void DeleteWfpObjects(Engine wfp, bool removeLayersAndProvider)
        {
            // WARNING! This method is super-slow if not executed inside a WFP transaction!

#if DEBUG
            Stopwatch watch = new Stopwatch();
            watch.Start();
#endif

            var layerSet = new HashSet<Guid>();
            var layerKeys = (LayerKeyEnum[])Enum.GetValues(typeof(LayerKeyEnum));
            foreach (var layer in layerKeys)
                layerSet.Add(GetSublayerKey(layer));

            FilterCollection filters = wfp.GetFilters(false);
            foreach (var f in filters)
            {
                // Remove filter if created by TinyWall
                // Remove filter if in a TinyWall layer (created by a 3rd party)
                if ((TINYWALL_PROVIDER_KEY == f.ProviderKey) || layerSet.Contains(f.SublayerKey))
                    wfp.UnregisterFilter(f.FilterKey);
            }

            if (removeLayersAndProvider)
            {
                // Remove all sublayers
                SublayerCollection subLayers = wfp.GetSublayers();
                foreach (var sl in subLayers)
                {
                    if (TINYWALL_PROVIDER_KEY == sl.ProviderKey)
                        wfp.UnregisterSublayer(sl.SublayerKey);
                }

                // Remove provider, ignore if not found
                try { wfp.UnregisterProvider(TINYWALL_PROVIDER_KEY); }
                catch { }
            }

#if DEBUG
            watch.Stop();
            Debug.WriteLine($"DeleteWfpObjects completed in {watch.ElapsedMilliseconds} ms.");
#endif
        }

        // Entry point for thread that actually issues commands to Windows Firewall.
        // Only one thread (this one) is allowed to issue them.
        public void Run()
        {
            FirewallThreadThrottler = new ThreadThrottler(Thread.CurrentThread, ThreadPriority.Highest);
            MinuteTimer = new Timer(new TimerCallback(TimerCallback), null, 60000, 60000);
            LogWatcher.NewLogEntry += LogWatcher_NewLogEntry;

            // Fire up file protections as soon as possible
            FileLocker.LockFile(DatabaseClasses.AppDatabase.DBPath, FileAccess.Read, FileShare.Read);
            FileLocker.LockFile(PasswordManager.PasswordFilePath, FileAccess.Read, FileShare.Read);

#if !DEBUG
            // Basic software health checks
            try { TinyWallDoctor.EnsureHealth(); }
            catch { }
#endif

            // Lock configuration if we have a password
            if (ServiceLocker.HasPassword)
                ServiceLocker.Locked = true;

            // Issue load command
            Q.Enqueue(new TwMessage(MessageType.REENUMERATE_ADDRESSES), null);
            Q.Enqueue(new TwMessage(MessageType.REINIT), null);

            // Fire up pipe
            GlobalInstances.ServerPipe = new PipeServerEndpoint(new PipeDataReceived(PipeServerDataReceived));

            // Listen to network configuration changes
            WqlEventQuery StartQuery = new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace");
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            // Make sure event collection is enabled
            using (WfpEngine = new Engine("TinyWall Option Session", "", FWPM_SESSION_FLAGS.None, 5000))
            {
                WfpEngine.CollectNetEvents = true;
                WfpEngine.EventMatchAnyKeywords = InboundEventMatchKeyword.FWPM_NET_EVENT_KEYWORD_INBOUND_BCAST | InboundEventMatchKeyword.FWPM_NET_EVENT_KEYWORD_INBOUND_MCAST;
            }

            using (ManagementEventWatcher ProcessStartWatcher = new ManagementEventWatcher(StartQuery))
            using (WfpEngine = new Engine("TinyWall Session", "", FWPM_SESSION_FLAGS.None, 5000))
            using (var WfpEvent = WfpEngine.SubscribeNetEvent(WfpNetEventCallback, null))
            {
                ProcessStartWatcher.EventArrived += ProcessStartWatcher_EventArrived;
                ProcessStartWatcher.Start();

                RunService = true;
                while (RunService)
                {
                    Q.Dequeue(out TwMessage msg, out Future<TwMessage> future);

                    TwMessage resp;
                    resp = ProcessCmd(msg);
                    if (null != future)
                        future.Value = resp;
                }
            }
        }

        private void ProcessStartWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            using (var throttler = new ThreadThrottler(ThreadPriority.Highest, true))
            {
                uint pid = (uint)(e.NewEvent["ProcessID"]);
                string path = ProcessManager.GetProcessPath(unchecked((int)pid), ProcessStartWatcher_Sbuilder)?.ToLowerInvariant();
                List<FirewallExceptionV3> newExceptions = new List<FirewallExceptionV3>();

                // Skip if we have no path
                if (string.IsNullOrEmpty(path))
                    return;

                lock (InheritanceGuard)
                {
                    // Skip if we have a user-defined rule for this path
                    if (UserSubjectExes.Contains(path))
                        return;

                    // Start walking up the process tree
                    for (int parentPid = unchecked((int)pid); ;)
                    {
                        if (!ProcessManager.GetParentProcess(parentPid, ref parentPid))
                            // We reached top of process tree (with non-existent paretn)
                            break;

                        if (parentPid == 0)
                            // We reached top of process tree (with idle process)
                            break;

                        string parentPath = ProcessManager.GetProcessPath(parentPid, ProcessStartWatcher_Sbuilder)?.ToLowerInvariant();
                        if (string.IsNullOrEmpty(parentPath))
                            continue;

                        // Skip if we have already processed this parent-child combination
                        if (ChildInheritedSubjectExes.ContainsKey(path) && ChildInheritedSubjectExes[path].Contains(parentPath))
                            break;

                        if (ChildInheritance.TryGetValue(parentPath, out FirewallExceptionV3 userEx))
                        {
                            FirewallExceptionV3 ex = new FirewallExceptionV3(new ExecutableSubject(path), userEx.Policy);
                            newExceptions.Add(ex);

                            if (!ChildInheritedSubjectExes.ContainsKey(path))
                                ChildInheritedSubjectExes.Add(path, new HashSet<string>());
                            ChildInheritedSubjectExes[path].Add(parentPath);
                            break;
                        }
                    }
                }

                if (newExceptions.Count != 0)
                {
                    FirewallThreadThrottler.Request();
                    Q.Enqueue(new TwMessage(MessageType.ADD_TEMPORARY_EXCEPTION, newExceptions), null);
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
                entry.AppPath = NtPathMapper.FromNtPath(data.appId);
            else
                entry.AppPath = "System";
            entry.DestinationIP = data.remoteAddr?.ToString();
            entry.SourceIP = data.localAddr?.ToString();
            if (data.remotePort.HasValue)
                entry.DestinationPort = data.remotePort.Value;
            if (data.direction.HasValue)
                entry.Direction = data.direction == FwpmDirection.FWP_DIRECTION_OUT ? RuleDirection.Out : RuleDirection.In;
            if (data.ipProtocol.HasValue)
                entry.Protocol = (Protocol)data.ipProtocol;
            if (data.localPort.HasValue)
                entry.SourcePort = data.localPort.Value;

            // Replace invalid IP strings with the "unspecified address" IPv6 specifier
            if (string.IsNullOrEmpty(entry.DestinationIP))
                entry.DestinationIP = "::";
            if (string.IsNullOrEmpty(entry.SourceIP))
                entry.SourceIP = "::";

            lock (FirewallLogEntries)
            {
                FirewallLogEntries.Enqueue(entry);
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
            if (string.IsNullOrEmpty(entry.AppPath)
                || entry.AppPath.Equals("System", StringComparison.InvariantCultureIgnoreCase)
                || entry.AppPath.EndsWith("svchost.exe", StringComparison.InvariantCultureIgnoreCase)
                )
                return;

            ExecutableSubject newSubject = new ExecutableSubject(entry.AppPath);

            lock (LearningNewExceptions)
            {
                for (int j = 0; j < LearningNewExceptions.Count; ++j)
                {
                    if (LearningNewExceptions[j].Subject.Equals(newSubject))
                        // Already in LearningNewExceptions, nothing to do
                        return;
                }

                List<FirewallExceptionV3> exceptions = GlobalInstances.AppDatabase.GetExceptionsForApp(newSubject, false, out DatabaseClasses.Application app);
                LearningNewExceptions.AddRange(exceptions);
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
            if (((int)req.Type > 4095))
            {
                // We cannot receive this from the client
                return new TwMessage(MessageType.RESPONSE_ERROR);
            }
            else
            {
                LastControllerCommandTime = DateTime.Now;

                // Process and wait for response
                Future<TwMessage> future = new Future<TwMessage>();
                Q.Enqueue(req, future);

                // Send response back to pipe
                return future.Value;
            }
        }

        public void RequestStop()
        {
            TwMessage req = new TwMessage(MessageType.STOP_SERVICE);
            Future<TwMessage> future = new Future<TwMessage>();
            Q.Enqueue(req, future);
            future.WaitValue();
        }

        public void Dispose()
        {
            // Check all exceptions if any one has expired
            {
                List<FirewallExceptionV3> exs = ActiveConfig.Service.ActiveProfile.AppExceptions;
                for (int i = exs.Count - 1; i >= 0; --i)
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

            if (MinuteTimer != null)
            {
                using (WaitHandle wh = new AutoResetEvent(false))
                {
                    MinuteTimer.Dispose(wh);
                    wh.WaitOne();
                }
                MinuteTimer = null;
            }

            NtPathMapper.Dispose();
            LogWatcher.Dispose();
            CommitLearnedRules();
            ActiveConfig.Service.Save(ConfigSavePath);
            FileLocker.UnlockAll();

            RestoreMpsSvc();

            FirewallThreadThrottler?.Dispose();

#if !DEBUG
            // Basic software health checks
            try { TinyWallDoctor.EnsureHealth(); }
            catch { }
#else
            using (var wfp = new Engine("TinyWall Cleanup Session", "", FWPM_SESSION_FLAGS.None, 5000))
            using (var trx = wfp.BeginTransaction())
            {
                DeleteWfpObjects(wfp, true);
                trx.Commit();
            }
#endif
        }
    }


    internal sealed class TinyWallService : ServiceBase
    {
        internal readonly static string[] ServiceDependencies = new string[]
        {
            "eventlog",
            "Winmgmt"
        };

        internal const string SERVICE_NAME = "TinyWall";
        internal const string SERVICE_DISPLAY_NAME = "TinyWall Service";

        private TinyWallServer Server;
        private Thread FirewallWorkerThread;

        internal TinyWallService()
        {
            this.CanShutdown = true;
#if DEBUG
            this.CanStop = true;
#else
            this.CanStop = false;
#endif
        }
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Utils.LogCrash(e.ExceptionObject as Exception, Utils.LOG_ID_SERVICE);
        }

        private void FirewallWorkerMethod()
        {
            try
            {
                using (Server = new TinyWallServer())
                {
                    Server.Run();
                }
            }
            finally
            {
#if !DEBUG
                // Set service state to stopped or else we will be restarted by the SCM when our process ends
                using (var srvManager = new ScmWrapper.ServiceControlManager())
                {
                    srvManager.SetServiceState(ServiceName, ServiceHandle, ScmWrapper.State.SERVICE_STOPPED, 0);
                }
                Process.GetCurrentProcess().Kill();
#endif
            }
        }

        // Entry point for Windows service.
        protected override void OnStart(string[] args)
        {
#if !DEBUG
            // Register an unhandled exception handler
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
#endif
            // Initialization on a new thread prevents stalling the SCM
            FirewallWorkerThread = new Thread(new ThreadStart(FirewallWorkerMethod));
            FirewallWorkerThread.Start();
        }

        private void StopServer()
        {
            Server.RequestStop();
            FirewallWorkerThread.Join(10000);
        }

        // Executed when service is stopped manually.
        protected override void OnStop()
        {
            StopServer();
        }

        // Executed on computer shutdown.
        protected override void OnShutdown()
        {
            StopServer();
        }

#if DEBUG
        internal void Start(string[] args)
        {
            this.OnStart(args);
        }
#endif
    }
}
