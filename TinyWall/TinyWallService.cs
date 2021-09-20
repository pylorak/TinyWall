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
using WFPdotNet;
using WFPdotNet.Interop;

namespace PKSoft
{
    public sealed class TinyWallServer : IDisposable
    {
        private enum FilterWeights : ulong
        {
            Blocklist = 9000000,
            RawSocketPermit = 8000000,
            RawSocketBlock = 7000000,
            UserBlock = 6000000,
            UserPermit = 5000000,
            DefaultPermit = 4000000,
            DefaultBlock = 3000000,
        }

        private static readonly Guid TINYWALL_PROVIDER_KEY = new Guid("{66CA412C-4453-4F1E-A973-C16E433E34D0}");

        private BoundedMessageQueue Q = new BoundedMessageQueue();
        private PipeServerEndpoint ServerPipe;

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
        private readonly StringBuilder ProcessStartWatcher_Sbuilder = new StringBuilder();
        private HashSet<string> UserSubjectExes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);        // All executables with pre-configured rules.
        private Dictionary<string, List<FirewallExceptionV3>> ChildInheritance = new Dictionary<string, List<FirewallExceptionV3>>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, HashSet<string>> ChildInheritedSubjectExes = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);   // Executables that have been already auto-whitelisted due to inheritance
        private ThreadThrottler FirewallThreadThrottler;

        private bool RunService = false;
        private ServerState VisibleState = new ServerState();

        private Engine WfpEngine;
        private ManagementEventWatcher ProcessStartWatcher;
        private IpInterfaceWatcher NetworkInterfaceWatcher;

        private HashSet<IpAddrMask> LocalSubnetAddreses = new HashSet<IpAddrMask>();
        private HashSet<IpAddrMask> GatewayAddresses = new HashSet<IpAddrMask>();
        private HashSet<IpAddrMask> DnsAddresses = new HashSet<IpAddrMask>();

        private List<RuleDef> AssembleActiveRules(List<RuleDef> rawSocketExceptions)
        {
            using (var timer = new HierarchicalStopwatch("AssembleActiveRules()"))
            {
                List<RuleDef> rules = new List<RuleDef>();
                Guid ModeId = Guid.NewGuid();
                RuleDef def;


                // Do we want to let local traffic through?
                if (ActiveConfig.Service.ActiveProfile.AllowLocalSubnet)
                {
                    def = new RuleDef(ModeId, "Allow local subnet", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.InOut, Protocol.Any, (ulong)FilterWeights.DefaultPermit);
                    def.RemoteAddresses = "LocalSubnet";
                    rules.Add(def);
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
                            rules.Add(def);

                            // Block rest
                            def = new RuleDef(ModeId, "Block incoming", GlobalSubject.Instance, RuleAction.Block, RuleDirection.In, Protocol.Any, (ulong)FilterWeights.DefaultBlock);
                            rules.Add(def);
                            break;
                        }
                    case TinyWall.Interface.FirewallMode.BlockAll:
                        {
                            // We won't need application exceptions
                            needUserRules = false;

                            // Block all
                            def = new RuleDef(ModeId, "Block everything", GlobalSubject.Instance, RuleAction.Block, RuleDirection.InOut, Protocol.Any, (ulong)FilterWeights.DefaultBlock);
                            rules.Add(def);
                            break;
                        }
                    case TinyWall.Interface.FirewallMode.Learning:
                        {
                            // Add rule to explicitly allow everything
                            def = new RuleDef(ModeId, "Allow everything", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.InOut, Protocol.Any, (ulong)FilterWeights.DefaultPermit);
                            rules.Add(def);
                            break;
                        }
                    case TinyWall.Interface.FirewallMode.Disabled:
                        {
                            // We won't need application exceptions
                            needUserRules = false;

                            // Add rule to explicitly allow everything
                            def = new RuleDef(ModeId, "Allow everything", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.InOut, Protocol.Any, (ulong)FilterWeights.DefaultPermit);
                            rules.Add(def);
                            break;
                        }
                    case TinyWall.Interface.FirewallMode.Normal:
                        {
                            // Block all by default
                            def = new RuleDef(ModeId, "Block everything", GlobalSubject.Instance, RuleAction.Block, RuleDirection.InOut, Protocol.Any, (ulong)FilterWeights.DefaultBlock);
                            rules.Add(def);
                            break;
                        }
                }

                if (needUserRules)
                {
                    List<FirewallExceptionV3> UserExceptions = new List<FirewallExceptionV3>();

                    // Add rule for our own binary
                    UserExceptions.Add(new FirewallExceptionV3(
                        new ExecutableSubject(ProcessManager.ExecutablePath),
                        new TcpUdpPolicy()
                        {
                            AllowedRemoteTcpConnectPorts = "443"
                        }
                    ));

                    // Collect all applications exceptions
                    UserExceptions.AddRange(ActiveConfig.Service.ActiveProfile.AppExceptions);

                    // Collect all special exceptions
                    ActiveConfig.Service.ActiveProfile.SpecialExceptions.Remove("TinyWall");    // TODO: Deprecated: Needed due to old configs. Remove in future version.
                    foreach (string appName in ActiveConfig.Service.ActiveProfile.SpecialExceptions)
                        UserExceptions.AddRange(CollectExceptionsForAppByName(appName));

                    // Convert exceptions to rules
                    foreach (FirewallExceptionV3 ex in UserExceptions)
                    {
                        if (ex.Subject is ExecutableSubject exe)
                        {
                            string exePath = exe.ExecutablePath;
                            UserSubjectExes.Add(exePath);
                            if (ex.ChildProcessesInherit)
                            {
                                // We might have multiple rules with the same exePath, so we maintain a list of exceptions
                                if (!ChildInheritance.ContainsKey(exePath))
                                    ChildInheritance.Add(exePath, new List<FirewallExceptionV3>());
                                ChildInheritance[exePath].Add(ex);
                            }
                        }

                        GetRulesForException(ex, rules, rawSocketExceptions, (ulong)FilterWeights.UserPermit, (ulong)FilterWeights.UserBlock);
                    }

                    if (ChildInheritance.Count != 0)
                    {
                        timer.NewSubTask("Rule inheritance processing");

                        StringBuilder sbuilder = new StringBuilder(1024);
                        Dictionary<int, ProcessManager.ExtendedProcessEntry> procTree = new Dictionary<int, ProcessManager.ExtendedProcessEntry>();
                        foreach (var p in ProcessManager.CreateToolhelp32SnapshotExtended())
                        {
                            var p2 = p;
                            if (!string.IsNullOrEmpty(p.ImagePath))
                                p2.ImagePath = p.ImagePath;
                            procTree.Add(p2.BaseEntry.th32ProcessID, p2);
                        }

                        // This list will hold parents that we already checked for a process.
                        // Used to avoid inf. loop when parent-PID info is unreliable.
                        HashSet<int> pidsChecked = new HashSet<int>();

                        foreach (var pair in procTree)
                        {
                            pidsChecked.Clear();

                            string procPath = pair.Value.ImagePath;

                            // Skip if we have no path
                            if (string.IsNullOrEmpty(procPath))
                                continue;

                            // Skip if we have a user-defined rule for this path
                            if (UserSubjectExes.Contains(procPath))
                                continue;

                            // Start walking up the process tree
                            for (ProcessManager.ExtendedProcessEntry parentEntry = procTree[pair.Key]; ;)
                            {
                                long childCreationTime = parentEntry.CreationTime;
                                if (procTree.ContainsKey(parentEntry.BaseEntry.th32ParentProcessID))
                                    parentEntry = procTree[parentEntry.BaseEntry.th32ParentProcessID];
                                else
                                    // We reached top of process tree (with non-existing parent)
                                    break;

                                // Check if what we have is really the parent, or just a reused PID
                                if (parentEntry.CreationTime > childCreationTime)
                                    // We reached the top of the process tree (with non-existing parent)
                                    break;

                                if (parentEntry.BaseEntry.th32ProcessID == 0)
                                    // We reached top of process tree (with idle process)
                                    break;

                                if (pidsChecked.Contains(parentEntry.BaseEntry.th32ProcessID))
                                    // We've been here before, damn it. Avoid looping eternally...
                                    break;

                                pidsChecked.Add(parentEntry.BaseEntry.th32ProcessID);

                                if (string.IsNullOrEmpty(parentEntry.ImagePath))
                                    // We cannot get the path, so let's skip this parent
                                    continue;

                                if (ChildInheritedSubjectExes.ContainsKey(procPath) && ChildInheritedSubjectExes[procPath].Contains(parentEntry.ImagePath))
                                    // We have already processed this parent-child combination
                                    break;

                                if (ChildInheritance.TryGetValue(parentEntry.ImagePath, out List<FirewallExceptionV3> exList))
                                {
                                    var subj = new ExecutableSubject(procPath);
                                    foreach (var userEx in exList)
                                        GetRulesForException(new FirewallExceptionV3(subj, userEx.Policy), rules, rawSocketExceptions, (ulong)FilterWeights.UserPermit, (ulong)FilterWeights.UserBlock);

                                    if (!ChildInheritedSubjectExes.ContainsKey(procPath))
                                        ChildInheritedSubjectExes.Add(procPath, new HashSet<string>());
                                    ChildInheritedSubjectExes[procPath].Add(parentEntry.ImagePath);
                                    break;
                                }
                            }
                        }
                    }   // if (ChildInheritance ...
                }

                // Convert all paths to kernel-format
                foreach (var r in rules)
                {
                    r.Application = PathMapper.Instance.ConvertPathIgnoreErrors(r.Application, PathFormat.NativeNt);
                }
                foreach (var r in rawSocketExceptions)
                {
                    r.Application = PathMapper.Instance.ConvertPathIgnoreErrors(r.Application, PathFormat.NativeNt);
                }

                return rules;
            }
        }

        private void InstallRules(List<RuleDef> rules, List<RuleDef> rawSocketExceptions, bool useTransaction)
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
                    InstallWsl2Filters(ActiveConfig.Service.ActiveProfile.HasSpecialException("WSL_2"));
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
            using (var timer = new HierarchicalStopwatch("InstallFirewallRules()"))
            {
                LastRuleReloadTime = DateTime.Now;

                List<RuleDef> rules;
                List<RuleDef> rawSocketExceptions = new List<RuleDef>();
                lock (InheritanceGuard)
                {
                    UserSubjectExes.Clear();
                    ChildInheritance.Clear();
                    ChildInheritedSubjectExes.Clear();
                    rules = AssembleActiveRules(rawSocketExceptions);

                    try
                    {
                        if (ChildInheritance.Count > 0)
                            ProcessStartWatcher.Start();
                        else
                            ProcessStartWatcher.Stop();
                    }
                    catch
                    {
                        // TODO: Add nonce-flag and log only if it has not been logged already
                        // Utils.Log("WMI error. Subprocess monitoring will be disabled.", Utils.LOG_ID_SERVICE);
                    }
                }

                timer.NewSubTask("WFP transaction acquire");
                using (Transaction trx = WfpEngine.BeginTransaction())
                {
                    timer.NewSubTask("WFP preparation");
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

                    timer.NewSubTask("Installing rules");
                    InstallRules(rules, rawSocketExceptions, false);

                    timer.NewSubTask("WFP transaction commit");
                    trx.Commit();
                }
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
            try
            {

                if (!string.IsNullOrEmpty(r.AppContainerSid))
                {
                    System.Diagnostics.Debug.Assert(!r.AppContainerSid.Equals("*"));

                    // Skip filter if OS is not supported
                    if (!TinyWall.Interface.VersionInfo.Win81OrNewer)
                        return;

                    if (!LayerIsIcmpError(layer))
                        conditions.Add(new PackageIdFilterCondition(r.AppContainerSid));
                    else
                        return;
                }
                else
                {
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
                            conditions.Add(new AppIdFilterCondition(r.Application, false, true));
                        else
                            return;
                    }
                }

                if (!string.IsNullOrEmpty(r.RemoteAddresses))
                {
                    System.Diagnostics.Debug.Assert(!r.RemoteAddresses.Equals("*"));

                    bool validAddressFound = false;
                    string[] addresses = r.RemoteAddresses.Split(',');

                    void addIpFilterCondition(IpAddrMask peerAddr, RemoteOrLocal peerType)
                    {
                        if (peerAddr.IsIPv6 == LayerIsV6Stack(layer))
                        {
                            validAddressFound = true;
                            conditions.Add(new IpFilterCondition(peerAddr.Address, (byte)peerAddr.PrefixLen, peerType));
                        }
                    }

                    foreach (var ipStr in addresses)
                    {
                        if (ipStr == "LocalSubnet")
                        {
                            foreach (var addr in LocalSubnetAddreses)
                                addIpFilterCondition(addr, RemoteOrLocal.Remote);
                        }
                        else if (ipStr == "DefaultGateway")
                        {
                            foreach (var addr in GatewayAddresses)
                                addIpFilterCondition(addr, RemoteOrLocal.Remote);
                        }
                        else if (ipStr == "DNS")
                        {
                            foreach (var addr in DnsAddresses)
                                addIpFilterCondition(addr, RemoteOrLocal.Remote);
                        }
                        else
                        {
                            addIpFilterCondition(IpAddrMask.Parse(ipStr), RemoteOrLocal.Remote);
                        }
                    }

                    if (!validAddressFound)
                    {
                        // Break. We don't want to add this filter to this layer.
                        return;
                    }
                }

                // We never want to affect loopback traffic
                conditions.Add(new FlagsFilterCondition(ConditionFlags.FWP_CONDITION_FLAG_IS_LOOPBACK, FieldMatchType.FWP_MATCH_FLAGS_NONE_SET));

                if (r.Protocol != Protocol.Any)
                {
                    if (LayerIsAleAuthConnect(layer) || LayerIsAleAuthRecvAccept(layer))
                    {
                        if (r.Protocol == Protocol.TcpUdp)
                        {
                            conditions.Add(new ProtocolFilterCondition((byte)Protocol.TCP));
                            conditions.Add(new ProtocolFilterCondition((byte)Protocol.UDP));
                        }
                        else
                            conditions.Add(new ProtocolFilterCondition((byte)r.Protocol));
                    }
                }
                if (!string.IsNullOrEmpty(r.LocalPorts))
                {
                    System.Diagnostics.Debug.Assert(!r.LocalPorts.Equals("*"));
                    string[] ports = r.LocalPorts.Split(',');
                    foreach (var p in ports)
                        conditions.Add(new PortFilterCondition(p, RemoteOrLocal.Local));
                }
                if (!string.IsNullOrEmpty(r.RemotePorts))
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
                                cv.value.uint16 = icmpType;
                                conditions.Add(new FilterCondition(ConditionKeys.FWPM_CONDITION_ICMP_TYPE, FieldMatchType.FWP_MATCH_EQUAL, cv));
                            }
                            // ICMP Code
                            if ((tc.Length > 1) && !string.IsNullOrEmpty(tc[1]) && ushort.TryParse(tc[1], out ushort icmpCode))
                            {
                                FWP_CONDITION_VALUE0 cv = new FWP_CONDITION_VALUE0();
                                cv.type = FWP_DATA_TYPE.FWP_UINT16;
                                cv.value.uint16 = icmpCode;
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
                                cv.value.uint16 = icmpType;
                                conditions.Add(new FilterCondition(ConditionKeys.FWPM_CONDITION_ORIGINAL_ICMP_TYPE, FieldMatchType.FWP_MATCH_EQUAL, cv));
                            }
                            // Matching on ICMP Code not possible
                        }
                    }
                }

                using (Filter f = new Filter(
                    r.ExceptionId.ToString(),
                    r.Name,
                    TINYWALL_PROVIDER_KEY,
                    (r.Action == RuleAction.Allow) ? FilterActions.FWP_ACTION_PERMIT : FilterActions.FWP_ACTION_BLOCK,
                    r.Weight
                ))
                {
                    f.LayerKey = GetLayerKey(layer);
                    f.SublayerKey = GetSublayerKey(layer);
                    f.Conditions.AddRange(conditions);

                    InstallWfpFilter(f);
                }
            }
            finally
            {
                for (int i = 0; i < conditions.Count; ++i)
                    conditions[i].Dispose();
            }
        }

        private void InstallRawSocketBlocks()
        {
            InstallRawSocketBlocks(LayerKeyEnum.FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V4);
            InstallRawSocketBlocks(LayerKeyEnum.FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V6);
        }

        private void InstallRawSocketBlocks(LayerKeyEnum layer)
        {
            List<FilterCondition> conditions = new List<FilterCondition>();

            try
            {
                conditions.Add(new FlagsFilterCondition(ConditionFlags.FWP_CONDITION_FLAG_IS_RAW_ENDPOINT, FieldMatchType.FWP_MATCH_FLAGS_ANY_SET));

                using (Filter f = new Filter(
                    "Raw socket block",
                    string.Empty,
                    TINYWALL_PROVIDER_KEY,
                    FilterActions.FWP_ACTION_BLOCK,
                    (ulong)FilterWeights.RawSocketBlock
                ))
                {
                    f.LayerKey = GetLayerKey(layer);
                    f.SublayerKey = GetSublayerKey(layer);
                    f.Conditions.AddRange(conditions);

                    InstallWfpFilter(f);
                }
            }
            finally
            {
                for (int i = 0; i < conditions.Count; ++i)
                    conditions[i].Dispose();
            }
        }

        private void InstallWsl2Filters(bool permit)
        {
            const string ifAlias = "vEthernet (WSL)";
            try
            {
                if (LocalInterfaceCondition.InterfaceAliasExists(ifAlias))
                {
                    InstallWsl2Filters(permit, ifAlias, LayerKeyEnum.FWPM_LAYER_ALE_AUTH_CONNECT_V4);
                    InstallWsl2Filters(permit, ifAlias, LayerKeyEnum.FWPM_LAYER_ALE_AUTH_CONNECT_V6);
                    InstallWsl2Filters(permit, ifAlias, LayerKeyEnum.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V4);
                    InstallWsl2Filters(permit, ifAlias, LayerKeyEnum.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V6);
                    InstallWsl2Filters(permit, ifAlias, LayerKeyEnum.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V4);
                    InstallWsl2Filters(permit, ifAlias, LayerKeyEnum.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V6);
                    InstallWsl2Filters(permit, ifAlias, LayerKeyEnum.FWPM_LAYER_INBOUND_ICMP_ERROR_V4);
                    InstallWsl2Filters(permit, ifAlias, LayerKeyEnum.FWPM_LAYER_INBOUND_ICMP_ERROR_V6);
                }
            }
            catch { }
        }

        private void InstallWsl2Filters(bool permit, string ifAlias, LayerKeyEnum layer)
        {
            List<FilterCondition> conditions = new List<FilterCondition>();

            FilterActions action = permit ? FilterActions.FWP_ACTION_PERMIT : FilterActions.FWP_ACTION_BLOCK;
            ulong weight = (ulong)(permit ? FilterWeights.UserPermit : FilterWeights.UserBlock);

            try
            {
                conditions.Add(new LocalInterfaceCondition(ifAlias));

                using (Filter f = new Filter(
                    "Allow WSL2",
                    string.Empty,
                    TINYWALL_PROVIDER_KEY,
                    action,
                    weight
                ))
                {
                    f.LayerKey = GetLayerKey(layer);
                    f.SublayerKey = GetSublayerKey(layer);
                    f.Conditions.AddRange(conditions);

                    InstallWfpFilter(f);
                }
            }
            finally
            {
                for (int i = 0; i < conditions.Count; ++i)
                    conditions[i].Dispose();
            }
        }

        private void InstallRawSocketPermits(List<RuleDef> rawSocketExceptions)
        {
            InstallRawSocketPermits(rawSocketExceptions, LayerKeyEnum.FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V4);
            InstallRawSocketPermits(rawSocketExceptions, LayerKeyEnum.FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V6);
        }

        private void InstallRawSocketPermits(List<RuleDef> rawSocketExceptions, LayerKeyEnum layer)
        {
            foreach (var subj in rawSocketExceptions)
            {
                List<FilterCondition> conditions = new List<FilterCondition>();

                try
                {
                    if (!string.IsNullOrEmpty(subj.Application))
                        conditions.Add(new AppIdFilterCondition(subj.Application, false, true));
                    if (!string.IsNullOrEmpty(subj.ServiceName))
                        conditions.Add(new ServiceNameFilterCondition(subj.ServiceName));

                    if (conditions.Count == 0)
                        return;

                    using (Filter f = new Filter(
                        "Raw socket permit",
                        string.Empty,
                        TINYWALL_PROVIDER_KEY,
                        FilterActions.FWP_ACTION_PERMIT,
                        (ulong)FilterWeights.RawSocketPermit
                    ))
                    {
                        f.LayerKey = GetLayerKey(layer);
                        f.SublayerKey = GetSublayerKey(layer);
                        f.Conditions.AddRange(conditions);

                        InstallWfpFilter(f);
                    }
                }
                catch { }
                finally
                {
                    for (int i = 0; i < conditions.Count; ++i)
                        conditions[i].Dispose();
                }
            }
        }

        private void InstallPortScanProtection()
        {
            InstallPortScanProtection(LayerKeyEnum.FWPM_LAYER_INBOUND_TRANSPORT_V4_DISCARD, BuiltinCallouts.FWPM_CALLOUT_WFP_TRANSPORT_LAYER_V4_SILENT_DROP);
            InstallPortScanProtection(LayerKeyEnum.FWPM_LAYER_INBOUND_TRANSPORT_V6_DISCARD, BuiltinCallouts.FWPM_CALLOUT_WFP_TRANSPORT_LAYER_V6_SILENT_DROP);
        }

        private void InstallPortScanProtection(LayerKeyEnum layer, Guid callout)
        {
            List<FilterCondition> conditions = new List<FilterCondition>();

            try
            {
                // Don't affect loopback traffic
                conditions.Add(new FlagsFilterCondition(ConditionFlags.FWP_CONDITION_FLAG_IS_LOOPBACK | ConditionFlags.FWP_CONDITION_FLAG_IS_IPSEC_SECURED, FieldMatchType.FWP_MATCH_FLAGS_NONE_SET));

                using (Filter f = new Filter(
                    "Port Scanning Protection",
                    string.Empty,
                    TINYWALL_PROVIDER_KEY,
                    FilterActions.FWP_ACTION_CALLOUT_TERMINATING,
                    (ulong)FilterWeights.Blocklist
                ))
                {
                    f.LayerKey = GetLayerKey(layer);
                    f.SublayerKey = GetSublayerKey(layer);
                    f.CalloutKey = callout;
                    f.Conditions.AddRange(conditions);

                    InstallWfpFilter(f);
                }
            }
            finally
            {
                for (int i = 0; i < conditions.Count; ++i)
                    conditions[i].Dispose();
            }
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
            // Also, relevant info:
            // https://networkengineering.stackexchange.com/questions/58903/how-to-handle-icmp-in-ipv6-or-icmpv6-in-ipv4

            if ((r.Direction & RuleDirection.Out) != 0)
            {
                ConstructFilter(r, LayerKeyEnum.FWPM_LAYER_ALE_AUTH_CONNECT_V6);
                ConstructFilter(r, LayerKeyEnum.FWPM_LAYER_ALE_AUTH_CONNECT_V4);

                if ((r.Protocol == Protocol.Any) || (r.Protocol == Protocol.ICMPv6))
                    ConstructFilter(r, LayerKeyEnum.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V6);
                if ((r.Protocol == Protocol.Any) || (r.Protocol == Protocol.ICMPv4))
                    ConstructFilter(r, LayerKeyEnum.FWPM_LAYER_OUTBOUND_ICMP_ERROR_V4);
            }
            if ((r.Direction & RuleDirection.In) != 0)
            { 
                ConstructFilter(r, LayerKeyEnum.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V6);
                ConstructFilter(r, LayerKeyEnum.FWPM_LAYER_ALE_AUTH_RECV_ACCEPT_V4);

                if ((r.Protocol == Protocol.Any) || (r.Protocol == Protocol.ICMPv6))
                    ConstructFilter(r, LayerKeyEnum.FWPM_LAYER_INBOUND_ICMP_ERROR_V6);
                if ((r.Protocol == Protocol.Any) || (r.Protocol == Protocol.ICMPv4))
                    ConstructFilter(r, LayerKeyEnum.FWPM_LAYER_INBOUND_ICMP_ERROR_V4);
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

        private void GetRulesForException(FirewallExceptionV3 ex, List<RuleDef> results, List<RuleDef> rawSocketExceptions, ulong permitWeight, ulong blockWeight)
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
                        results.Add(def);
                        break;
                    }
                case PolicyType.Unrestricted:
                    {
                        RuleDef def = new RuleDef(ex.Id, "Full access", ex.Subject, RuleAction.Allow, RuleDirection.InOut, Protocol.Any, permitWeight);
                        if ((ex.Policy as UnrestrictedPolicy).LocalNetworkOnly)
                            def.RemoteAddresses = "LocalSubnet";
                        results.Add(def);

                        if (rawSocketExceptions != null)
                        {
                            // Make exception for promiscuous mode
                            rawSocketExceptions.Add(def);
                        }

                        break;
                    }
                case PolicyType.TcpUdpOnly:
                    {
                        TcpUdpPolicy pol = ex.Policy as TcpUdpPolicy;

                        // Incoming
                        if (!string.IsNullOrEmpty(pol.AllowedLocalTcpListenerPorts) && (pol.AllowedLocalTcpListenerPorts == pol.AllowedLocalUdpListenerPorts))
                        {
                            RuleDef def = new RuleDef(ex.Id, "TCP/UDP Listen Ports", ex.Subject, RuleAction.Allow, RuleDirection.In, Protocol.TcpUdp, permitWeight);
                            if (!pol.AllowedLocalTcpListenerPorts.Equals("*"))
                                def.LocalPorts = pol.AllowedLocalTcpListenerPorts;
                            if (pol.LocalNetworkOnly)
                                def.RemoteAddresses = "LocalSubnet";
                            results.Add(def);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(pol.AllowedLocalTcpListenerPorts))
                            {
                                RuleDef def = new RuleDef(ex.Id, "TCP Listen Ports", ex.Subject, RuleAction.Allow, RuleDirection.In, Protocol.TCP, permitWeight);
                                if (!pol.AllowedLocalTcpListenerPorts.Equals("*"))
                                    def.LocalPorts = pol.AllowedLocalTcpListenerPorts;
                                if (pol.LocalNetworkOnly)
                                    def.RemoteAddresses = "LocalSubnet";
                                results.Add(def);
                            }
                            if (!string.IsNullOrEmpty(pol.AllowedLocalUdpListenerPorts))
                            {
                                RuleDef def = new RuleDef(ex.Id, "UDP Listen Ports", ex.Subject, RuleAction.Allow, RuleDirection.In, Protocol.UDP, permitWeight);
                                if (!pol.AllowedLocalUdpListenerPorts.Equals("*"))
                                    def.LocalPorts = pol.AllowedLocalUdpListenerPorts;
                                if (pol.LocalNetworkOnly)
                                    def.RemoteAddresses = "LocalSubnet";
                                results.Add(def);
                            }
                        }

                        // Outgoing
                        if (!string.IsNullOrEmpty(pol.AllowedRemoteTcpConnectPorts) && (pol.AllowedRemoteTcpConnectPorts == pol.AllowedRemoteUdpConnectPorts))
                        {
                            RuleDef def = new RuleDef(ex.Id, "TCP/UDP Outbound Ports", ex.Subject, RuleAction.Allow, RuleDirection.Out, Protocol.TcpUdp, permitWeight);
                            if (!pol.AllowedRemoteTcpConnectPorts.Equals("*"))
                                def.RemotePorts = pol.AllowedRemoteTcpConnectPorts;
                            if (pol.LocalNetworkOnly)
                                def.RemoteAddresses = "LocalSubnet";
                            results.Add(def);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(pol.AllowedRemoteTcpConnectPorts))
                            {
                                RuleDef def = new RuleDef(ex.Id, "TCP Outbound Ports", ex.Subject, RuleAction.Allow, RuleDirection.Out, Protocol.TCP, permitWeight);
                                if (!pol.AllowedRemoteTcpConnectPorts.Equals("*"))
                                    def.RemotePorts = pol.AllowedRemoteTcpConnectPorts;
                                if (pol.LocalNetworkOnly)
                                    def.RemoteAddresses = "LocalSubnet";
                                results.Add(def);
                            }
                            if (!string.IsNullOrEmpty(pol.AllowedRemoteUdpConnectPorts))
                            {
                                RuleDef def = new RuleDef(ex.Id, "UDP Outbound Ports", ex.Subject, RuleAction.Allow, RuleDirection.Out, Protocol.UDP, permitWeight);
                                if (!pol.AllowedRemoteUdpConnectPorts.Equals("*"))
                                    def.RemotePorts = pol.AllowedRemoteUdpConnectPorts;
                                if (pol.LocalNetworkOnly)
                                    def.RemoteAddresses = "LocalSubnet";
                                results.Add(def);
                            }
                        }
                        break;
                    }
                case PolicyType.RuleList:
                    {
                        RuleListPolicy pol = ex.Policy as RuleListPolicy;
                        foreach (var rule in pol.Rules)
                        {
                            rule.SetSubject(ex.Subject);
                            rule.ExceptionId = ex.Id;
                            rule.Weight = (rule.Action == RuleAction.Allow) ? permitWeight : blockWeight;
                            results.Add(rule);
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

        // This method completely reinitializes the firewall.
        private void InitFirewall()
        {
            using (var timer = new HierarchicalStopwatch("InitFirewall()"))
            {
                LoadDatabase();
                ActiveConfig.Service = LoadServerConfig();
                VisibleState.Mode = ActiveConfig.Service.StartupMode;
                GlobalInstances.ServerChangeset = Guid.NewGuid();

                ReapplySettings();
                InstallFirewallRules();
            }
        }


        // This method reapplies all firewall settings.
        private void ReapplySettings()
        {
            using (var timer = new HierarchicalStopwatch("ReapplySettings()"))
            {
                HostsFileManager.EnableProtection(ActiveConfig.Service.LockHostsFile);
                if (ActiveConfig.Service.Blocklists.EnableBlocklists
                    && ActiveConfig.Service.Blocklists.EnableHostsBlocklist)
                    HostsFileManager.EnableHostsFile();
                else
                    HostsFileManager.DisableHostsFile();
            }
        }

        private void LoadDatabase()
        {
            using (var timer = new HierarchicalStopwatch("LoadDatabase()"))
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
        }

        private DateTime? LastUpdateCheck_ = null;
        private const string LastUpdateCheck_FILENAME = "updatecheck";
        private DateTime LastUpdateCheck
        {
            get
            {
                if (!LastUpdateCheck_.HasValue)
                {
                    try
                    {
                        string filePath = Path.Combine(ServiceSettings21.AppDataPath, LastUpdateCheck_FILENAME);
                        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                        {
                            LastUpdateCheck_ = DateTime.Parse(sr.ReadLine());
                        }
                    }
                    catch { }
                }

                if (!LastUpdateCheck_.HasValue)
                    LastUpdateCheck_ = DateTime.MinValue;
                if (LastUpdateCheck_.Value > DateTime.Now)
                    LastUpdateCheck_ = DateTime.MinValue;

                return LastUpdateCheck_.Value;
            }

            set
            {
                LastUpdateCheck_ = value;

                try
                {
                    string filePath = Path.Combine(ServiceSettings21.AppDataPath, LastUpdateCheck_FILENAME);
                    using (var afu = new AtomicFileUpdater(filePath))
                    {
                        using (FileStream fs = new FileStream(afu.TemporaryFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                        {
                            sw.WriteLine(value.ToString("O"));
                        }
                        afu.Commit();
                    }
                }
                catch { }
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
                LastUpdateCheck = DateTime.Now;
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
                Utils.LogException(e, Utils.LOG_ID_SERVICE);
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
            using (var afu = new AtomicFileUpdater(DatabaseClasses.AppDatabase.DBPath))
            {
                File.Copy(tmpFilePath, afu.TemporaryFilePath, true);
                afu.Commit();
            }
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

                ActiveConfig.Service.ActiveProfile.AddExceptions(LearningNewExceptions);
                LearningNewExceptions.Clear();
            }

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
                        FirewallMode newMode = (TinyWall.Interface.FirewallMode)req.Arguments[0];

                        if (CommitLearnedRules())
                            ActiveConfig.Service.Save(ConfigSavePath);

                        try
                        {
                            LogWatcher.Enabled = (FirewallMode.Learning == newMode);
                        }
                        catch(Exception e)
                        {
                            Utils.Log("Cannot enter auto-learn mode. Is the 'eventlog' service running? For details see next log entry.", Utils.LOG_ID_SERVICE);
                            Utils.LogException(e, Utils.LOG_ID_SERVICE);
                            return new TwMessage(MessageType.RESPONSE_ERROR);
                        }

                        VisibleState.Mode = newMode;
                        InstallFirewallRules();

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
                                Utils.LogException(e, Utils.LOG_ID_SERVICE);
                            }
                        }
                        VisibleState.HasPassword = ServiceLocker.HasPassword;
                        VisibleState.Locked = ServiceLocker.Locked;
                        return new TwMessage(resp, ActiveConfig.Service, GlobalInstances.ServerChangeset, VisibleState);
                    }
                case MessageType.ADD_TEMPORARY_EXCEPTION:
                    {
                        List<RuleDef> rules = new List<RuleDef>();
                        List<RuleDef> rawSocketExceptions = new List<RuleDef>();
                        List<FirewallExceptionV3> exceptions = req.Arguments[0] as List<FirewallExceptionV3>;

                        foreach (var ex in exceptions)
                        {
                            GetRulesForException(ex, rules, rawSocketExceptions, (ulong)FilterWeights.UserPermit, (ulong)FilterWeights.UserBlock);
                        }

                        InstallRules(rules, rawSocketExceptions, true);
                        lock (FirewallThreadThrottler.SynchRoot) {FirewallThreadThrottler.Release();}

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
                case MessageType.RELOAD_WFP_FILTERS:
                    {
                        InstallFirewallRules();
                        return new TwMessage(MessageType.RESPONSE_OK);
                    }
                case MessageType.UNLOCK:
                    {
                        bool success = ServiceLocker.Unlock((string)req.Arguments[0]);
                        if (success)
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
                        if (ReenumerateAdresses())  // returns true if anything changed
                            InstallFirewallRules();
                        return new TwMessage(MessageType.RESPONSE_OK);
                    }
                default:
                    {
                        return new TwMessage(MessageType.RESPONSE_ERROR);
                    }
            }
        }

        private bool ReenumerateAdresses()
        {
            using (var timer = new HierarchicalStopwatch("NIC enumeration"))
            {
                HashSet<IpAddrMask> newLocalSubnetAddreses = new HashSet<IpAddrMask>();
                HashSet<IpAddrMask> newGatewayAddresses = new HashSet<IpAddrMask>();
                HashSet<IpAddrMask> newDnsAddresses = new HashSet<IpAddrMask>();

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

                        newLocalSubnetAddreses.Add(am.Subnet);
                    }

                    foreach (var uni in props.GatewayAddresses)
                    {
                        IpAddrMask am = new IpAddrMask(uni);
                        newGatewayAddresses.Add(am);
                    }

                    foreach (var uni in props.DnsAddresses)
                    {
                        IpAddrMask am = new IpAddrMask(uni);
                        newDnsAddresses.Add(am);
                    }
                }

                newLocalSubnetAddreses.Add(new IpAddrMask(IPAddress.Parse("255.255.255.255")));
                newLocalSubnetAddreses.Add(IpAddrMask.LinkLocal);
                newLocalSubnetAddreses.Add(IpAddrMask.IPv6LinkLocal);
                newLocalSubnetAddreses.Add(IpAddrMask.LinkLocalMulticast);
                newLocalSubnetAddreses.Add(IpAddrMask.AdminScopedMulticast);
                newLocalSubnetAddreses.Add(IpAddrMask.IPv6LinkLocalMulticast);

                if (!LocalSubnetAddreses.SetEquals(newLocalSubnetAddreses)
                    || !GatewayAddresses.SetEquals(newGatewayAddresses)
                    || !DnsAddresses.SetEquals(newDnsAddresses))
                {
                    LocalSubnetAddreses = newLocalSubnetAddreses;
                    GatewayAddresses = newGatewayAddresses;
                    DnsAddresses = newDnsAddresses;
                    return true;
                }

                return false;
            }
        }

        internal static void DeleteWfpObjects(Engine wfp, bool removeLayersAndProvider)
        {
            // WARNING! This method is super-slow if not executed inside a WFP transaction!

            using (var timer = new HierarchicalStopwatch("DeleteWfpObjects()"))
            {
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
            }
        }

        // Entry point for thread that actually issues commands to Windows Firewall.
        // Only one thread (this one) is allowed to issue them.
        public void Run()
        {
            using (var timer = new HierarchicalStopwatch("Service Run()"))
            {
                timer.NewSubTask("Init 1");

                FirewallThreadThrottler = new ThreadThrottler(Thread.CurrentThread, ThreadPriority.Highest, false, true);
                MinuteTimer = new Timer(new TimerCallback(TimerCallback), null, 60000, 60000);
                LogWatcher.NewLogEntry += (FirewallLogWatcher sender, FirewallLogEntry entry) =>
                {
                    AutoLearnLogEntry(entry);
                };

                // Fire up file protections as soon as possible
                FileLocker.LockFile(DatabaseClasses.AppDatabase.DBPath, FileAccess.Read, FileShare.Read);
                FileLocker.LockFile(PasswordManager.PasswordFilePath, FileAccess.Read, FileShare.Read);

#if !DEBUG
                // Basic software health checks
                TinyWallDoctor.EnsureHealth(Utils.LOG_ID_SERVICE);
#endif

                // Lock configuration if we have a password
                if (ServiceLocker.HasPassword)
                    ServiceLocker.Locked = true;

                // Discover network configuration
                ReenumerateAdresses();

                // Issue load command
                Q.Enqueue(new TwMessage(MessageType.REINIT), null);

                // If mount points change, we need to update WFP rules due to Win32->Kernel path format mapping
                PathMapper.Instance.MountPointsChanged += (object sender, EventArgs args) =>
                {
                    Q.Enqueue(new TwMessage(MessageType.RELOAD_WFP_FILTERS), null);
                };

                // Fire up pipe
                ServerPipe = new PipeServerEndpoint(new PipeDataReceived(PipeServerDataReceived), "TinyWallController");

                // Make sure event collection is enabled
                timer.NewSubTask("WFP option session");
                using (WfpEngine = new Engine("TinyWall Option Session", "", FWPM_SESSION_FLAGS.None, 5000))
                {
                    WfpEngine.CollectNetEvents = true;
                    WfpEngine.EventMatchAnyKeywords = InboundEventMatchKeyword.FWPM_NET_EVENT_KEYWORD_INBOUND_BCAST | InboundEventMatchKeyword.FWPM_NET_EVENT_KEYWORD_INBOUND_MCAST;
                }

                timer.NewSubTask("Init 2");
                using (WindowsFirewall WinDefFirewall = new WindowsFirewall())
                using (ProcessStartWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace")))
                using (NetworkInterfaceWatcher = new IpInterfaceWatcher())
                using (WfpEngine = new Engine("TinyWall Session", "", FWPM_SESSION_FLAGS.None, 5000))
                using (var WfpEvent = WfpEngine.SubscribeNetEvent(WfpNetEventCallback, null))
                {
                    ProcessStartWatcher.EventArrived += ProcessStartWatcher_EventArrived;
                    NetworkInterfaceWatcher.InterfaceChanged += NetworkInterfaceWatcher_EventArrived;

                    RunService = true;
                    while (RunService)
                    {
                        timer.NewSubTask("Message wait");
                        Q.Dequeue(out TwMessage msg, out Future<TwMessage> future);

                        timer.NewSubTask($"Message {msg.Type}");
                        try
                        {
                            TwMessage resp = ProcessCmd(msg);
                            if (null != future)
                                future.Value = resp;
                        }
                        catch (Exception e)
                        {
                            Utils.LogException(e, Utils.LOG_ID_SERVICE);
                            if (null != future)
                                future.Value = new TwMessage(MessageType.RESPONSE_ERROR, null);
                        }
                    }
                }
            }
        }

        private void NetworkInterfaceWatcher_EventArrived(IpInterfaceWatcher sender)
        {
            Q.Enqueue(new TwMessage(MessageType.REENUMERATE_ADDRESSES), null);
        }

        private void ProcessStartWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                using (var throttler = new ThreadThrottler(Thread.CurrentThread, ThreadPriority.Highest, true, false))
                {
                    uint pid = (uint)(e.NewEvent["ProcessID"]);
                    string path = ProcessManager.GetProcessPath(unchecked((int)pid), ProcessStartWatcher_Sbuilder);

                    // Skip if we have no path
                    if (string.IsNullOrEmpty(path))
                        return;

                    List<FirewallExceptionV3> newExceptions = null;

                    lock (InheritanceGuard)
                    {
                        // Skip if we have a user-defined rule for this path
                        if (UserSubjectExes.Contains(path))
                            return;

                        // This list will hold parents that we already checked for a process.
                        // Used to avoid inf. loop when parent-PID info is unreliable.
                        HashSet<int> pidsChecked = new HashSet<int>();

                        // Start walking up the process tree
                        for (int parentPid = unchecked((int)pid); ;)
                        {
                            if (!ProcessManager.GetParentProcess(parentPid, ref parentPid))
                                // We reached the top of the process tree (with non-existent parent)
                                break;

                            if (parentPid == 0)
                                // We reached top of process tree (with idle process)
                                break;

                            if (pidsChecked.Contains(parentPid))
                                // We've been here before, damn it. Avoid looping eternally...
                                break;

                            pidsChecked.Add(parentPid);

                            string parentPath = ProcessManager.GetProcessPath(parentPid, ProcessStartWatcher_Sbuilder);
                            if (string.IsNullOrEmpty(parentPath))
                                continue;

                            // Skip if we have already processed this parent-child combination
                            if (ChildInheritedSubjectExes.ContainsKey(path) && ChildInheritedSubjectExes[path].Contains(parentPath))
                                break;

                            if (ChildInheritance.TryGetValue(parentPath, out List<FirewallExceptionV3> exList))
                            {
                                if (newExceptions == null)
                                    newExceptions = new List<FirewallExceptionV3>();

                                foreach (var userEx in exList)
                                    newExceptions.Add(new FirewallExceptionV3(new ExecutableSubject(path), userEx.Policy));

                                if (!ChildInheritedSubjectExes.ContainsKey(path))
                                    ChildInheritedSubjectExes.Add(path, new HashSet<string>());
                                ChildInheritedSubjectExes[path].Add(parentPath);
                                break;
                            }
                        }
                    }

                    if (newExceptions != null)
                    {
                        lock (FirewallThreadThrottler.SynchRoot) { FirewallThreadThrottler.Request(); }
                        Q.Enqueue(new TwMessage(MessageType.ADD_TEMPORARY_EXCEPTION, newExceptions), null);
                    }
                }
            }
            finally
            {
                e.NewEvent.Dispose();
            }
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
                entry.AppPath = PathMapper.Instance.ConvertPathIgnoreErrors(data.appId, PathFormat.Win32);
            else
                entry.AppPath = "System";
            entry.PackageId = data.packageId;
            entry.RemoteIp = data.remoteAddr?.ToString();
            entry.LocalIp = data.localAddr?.ToString();
            if (data.remotePort.HasValue)
                entry.RemotePort = data.remotePort.Value;
            if (data.direction.HasValue)
                entry.Direction = data.direction == FwpmDirection.FWP_DIRECTION_OUT ? RuleDirection.Out : RuleDirection.In;
            if (data.ipProtocol.HasValue)
                entry.Protocol = (Protocol)data.ipProtocol;
            if (data.localPort.HasValue)
                entry.LocalPort = data.localPort.Value;

            // Replace invalid IP strings with the "unspecified address" IPv6 specifier
            if (string.IsNullOrEmpty(entry.RemoteIp))
                entry.RemoteIp = "::";
            if (string.IsNullOrEmpty(entry.LocalIp))
                entry.LocalIp = "::";

            lock (FirewallLogEntries)
            {
                FirewallLogEntries.Enqueue(entry);
            }
        }

        private void AutoLearnLogEntry(FirewallLogEntry entry)
        {
            if (  // IPv4
                ((entry.RemoteIp.Equals("127.0.0.1", StringComparison.Ordinal)
                && entry.LocalIp.Equals("127.0.0.1", StringComparison.Ordinal)))
               || // IPv6
                ((entry.RemoteIp.Equals("::1", StringComparison.Ordinal)
                && entry.LocalIp.Equals("::1", StringComparison.Ordinal)))
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
            using (var timer = new HierarchicalStopwatch("TinyWallService.Dispose()"))
            {
                ServerPipe?.Dispose();

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

                LogWatcher.Dispose();
                CommitLearnedRules();
                ActiveConfig.Service.Save(ConfigSavePath);
                FileLocker.UnlockAll();

                FirewallThreadThrottler?.Dispose();

#if !DEBUG
            // Basic software health checks
            TinyWallDoctor.EnsureHealth(Utils.LOG_ID_SERVICE);
#else
                using (var wfp = new Engine("TinyWall Cleanup Session", "", FWPM_SESSION_FLAGS.None, 5000))
                using (var trx = wfp.BeginTransaction())
                {
                    DeleteWfpObjects(wfp, true);
                    trx.Commit();
                }
#endif
                GlobalInstances.Cleanup();
                PathMapper.Instance.Dispose();
            }
        }
    }


    internal sealed class TinyWallService : ServiceBase
    {
        internal readonly static string[] ServiceDependencies = new string[]
        {
            "Schedule",
            "Winmgmt",
            "BFE"
        };

        internal const string SERVICE_NAME = "TinyWall";
        internal const string SERVICE_DISPLAY_NAME = "TinyWall Service";

        private TinyWallServer Server;
        private Thread FirewallWorkerThread;
        private bool IsComputerShuttingDown;

        internal TinyWallService()
        {
            this.CanShutdown = true;
#if DEBUG
            this.CanStop = true;
#else
            this.CanStop = false;
#endif
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
                Thread.MemoryBarrier();
                if (!IsComputerShuttingDown)    // cannot set service state if a shutdown is already in progress
                {
                    // Set service state to stopped or else we will be restarted by the SCM when our process ends
                    using (var srvManager = new ServiceControlManager())
                    {
                        srvManager.SetServiceState(ServiceName, ServiceHandle, ServiceState.SERVICE_STOPPED, 0);
                    }
                }
                Process.GetCurrentProcess().Kill();
#endif
            }
        }

        // Entry point for Windows service.
        protected override void OnStart(string[] args)
        {
            // Initialization on a new thread prevents stalling the SCM
            FirewallWorkerThread = new Thread(new ThreadStart(FirewallWorkerMethod));
            FirewallWorkerThread.Name = "ServiceMain";
            FirewallWorkerThread.Start();
        }

        private void StopServer(bool computerShutdown)
        {
            IsComputerShuttingDown = computerShutdown;
            Thread.MemoryBarrier();
            Server.RequestStop();
            FirewallWorkerThread.Join(10000);
        }

        // Executed when service is stopped manually.
        protected override void OnStop()
        {
            StopServer(false);
        }

        // Executed on computer shutdown.
        protected override void OnShutdown()
        {
            StopServer(true);
        }

#if DEBUG
        internal void Start(string[] args)
        {
            this.OnStart(args);
        }
#endif
    }
}
