using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.Management;
using System.Threading;
using pylorak.Windows;
using pylorak.Windows.Services;
using pylorak.Windows.WFP;
using pylorak.Windows.WFP.Interop;
using pylorak.Utilities;

namespace pylorak.TinyWall
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
        private FileLocker FileLocker = new FileLocker();
        private HostsFileManager HostsFileManager = new HostsFileManager();

        // Context needed for learning mode
        private FirewallLogWatcher LogWatcher = new FirewallLogWatcher();
        private List<FirewallExceptionV3> LearningNewExceptions = new List<FirewallExceptionV3>();

        // Context for auto rule inheritance
        private readonly object InheritanceGuard = new object();
        private StringBuilder? ProcessStartWatcher_Sbuilder;
        private HashSet<string> UserSubjectExes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);        // All executables with pre-configured rules.
        private Dictionary<string, List<FirewallExceptionV3>> ChildInheritance = new Dictionary<string, List<FirewallExceptionV3>>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, HashSet<string>> ChildInheritedSubjectExes = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);   // Executables that have been already auto-whitelisted due to inheritance
        private ThreadThrottler FirewallThreadThrottler = new ThreadThrottler(Thread.CurrentThread, ThreadPriority.Highest, false);

        private bool RunService = false;
        private bool DisplayCurrentlyOn = true;
        private ServerState VisibleState = new ServerState();

        private Engine WfpEngine = new Engine("TinyWall Session", "", FWPM_SESSION_FLAGS.None, 5000);
        private ManagementEventWatcher ProcessStartWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
        private EventMerger RuleReloadEventMerger = new EventMerger(1000);

        private HashSet<IpAddrMask> LocalSubnetAddreses = new HashSet<IpAddrMask>();
        private HashSet<IpAddrMask> GatewayAddresses = new HashSet<IpAddrMask>();
        private HashSet<IpAddrMask> DnsAddresses = new HashSet<IpAddrMask>();
        private FilterConditionList LocalSubnetFilterConditions = new FilterConditionList();
        private FilterConditionList GatewayFilterConditions = new FilterConditionList();
        private FilterConditionList DnsFilterConditions = new FilterConditionList();

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
                    def.RemoteAddresses = RuleDef.LOCALSUBNET_ID;
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
                        GetRulesForException(ex, rules, rawSocketExceptions, (ulong)FilterWeights.DefaultPermit, (ulong)FilterWeights.Blocklist);
                    }
                }

                // Rules specific to the selected firewall mode
                bool needUserRules = true;
                switch (VisibleState.Mode)
                {
                    case FirewallMode.AllowOutgoing:
                        {
                            // Block everything
                            def = new RuleDef(ModeId, "Block everything", GlobalSubject.Instance, RuleAction.Block, RuleDirection.InOut, Protocol.Any, (ulong)FilterWeights.DefaultBlock);
                            rules.Add(def);

                            // Allow outgoing
                            def = new RuleDef(ModeId, "Allow outbound", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.Out, Protocol.Any, (ulong)FilterWeights.DefaultPermit);
                            rules.Add(def);
                            break;
                        }
                    case FirewallMode.BlockAll:
                        {
                            // We won't need application exceptions
                            needUserRules = false;

                            // Block all
                            def = new RuleDef(ModeId, "Block everything", GlobalSubject.Instance, RuleAction.Block, RuleDirection.InOut, Protocol.Any, (ulong)FilterWeights.DefaultBlock);
                            rules.Add(def);
                            break;
                        }
                    case FirewallMode.Learning:
                        {
                            // Add rule to explicitly allow everything
                            def = new RuleDef(ModeId, "Allow everything", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.InOut, Protocol.Any, (ulong)FilterWeights.DefaultPermit);
                            rules.Add(def);
                            break;
                        }
                    case FirewallMode.Disabled:
                        {
                            // We won't need application exceptions
                            needUserRules = false;

                            // Add rule to explicitly allow everything
                            def = new RuleDef(ModeId, "Allow everything", GlobalSubject.Instance, RuleAction.Allow, RuleDirection.InOut, Protocol.Any, (ulong)FilterWeights.DefaultPermit);
                            rules.Add(def);
                            break;
                        }
                    case FirewallMode.Normal:
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
                        var procTree = new Dictionary<uint, ProcessSnapshotEntry>();
                        foreach (var p in ProcessManager.CreateToolhelp32SnapshotExtended())
                            procTree.Add(p.ProcessId, p);

                        // This list will hold parents that we already checked for a process.
                        // Used to avoid inf. loop when parent-PID info is unreliable.
                        var pidsChecked = new HashSet<uint>();

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
                            for (var parentEntry = procTree[pair.Key]; ;)
                            {
                                long childCreationTime = parentEntry.CreationTime;
                                if (procTree.ContainsKey(parentEntry.ParentProcessId))
                                    parentEntry = procTree[parentEntry.ParentProcessId];
                                else
                                    // We reached top of process tree (with non-existing parent)
                                    break;

                                // Check if what we have is really the parent, or just a reused PID
                                if (parentEntry.CreationTime > childCreationTime)
                                    // We reached the top of the process tree (with non-existing parent)
                                    break;

                                if (parentEntry.ProcessId == 0)
                                    // We reached top of process tree (with idle process)
                                    break;

                                if (pidsChecked.Contains(parentEntry.ProcessId))
                                    // We've been here before, damn it. Avoid looping eternally...
                                    break;

                                pidsChecked.Add(parentEntry.ProcessId);

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
                    if (r.Application is not null)
                        r.Application = PathMapper.Instance.ConvertPathIgnoreErrors(r.Application, PathFormat.NativeNt);
                }

                bool displayBlockActive = ActiveConfig.Service.ActiveProfile.DisplayOffBlock && !DisplayCurrentlyOn;
                if (displayBlockActive)
                {
                    // Modify all allow-rules to only allow local subnet
                    foreach (var r in rules)
                    {
                        if (r.Action == RuleAction.Allow)
                        {
                            r.RemoteAddresses = RuleDef.LOCALSUBNET_ID;
                        }
                    }
                }

                return rules;
            }
        }

        private void InstallRules(List<RuleDef> rules, List<RuleDef> rawSocketExceptions, bool useTransaction)
        {
            Transaction? trx = useTransaction ? WfpEngine.BeginTransaction() : null;
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
                PathMapper.Instance.RebuildCache();

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
            // Local helper methods

            bool addCommonIpFilterCondition(IpFilterCondition cond, FilterConditionList coll)
            {
                if (cond.IsIPv6 == LayerIsV6Stack(layer))
                {
                    coll.Add(cond);
                    return true;
                }
                return false;
            }
            bool addIpFilterCondition(IpAddrMask peerAddr, RemoteOrLocal peerType, FilterConditionList coll)
            {
                if (peerAddr.IsIPv6 == LayerIsV6Stack(layer))
                {
                    coll.Add(new IpFilterCondition(peerAddr.Address, (byte)peerAddr.PrefixLen, peerType));
                    return true;
                }
                return false;
            }
            (ushort, ushort) parseUInt16Range(ReadOnlySpan<char> str)
            {
                if (-1 != str.IndexOf('-'))
                {
                    ReadOnlySpan<char> min, max;
                    using (var enumerator = str.Split('-'))
                    {
                        enumerator.MoveNext(); min = enumerator.Current;
                        enumerator.MoveNext(); max = enumerator.Current;
                    }
                    return (min.DecimalToUInt16(), max.DecimalToUInt16());
                }
                else
                {
                    var port = str.DecimalToUInt16();
                    return (port, port);
                }
            }

            // ---------------------------------------

            using var conditions = new FilterConditionList();

            // Application identity
            if (!Utils.IsNullOrEmpty(r.AppContainerSid))
            {
                System.Diagnostics.Debug.Assert(!r.AppContainerSid.Equals("*"));

                // Skip filter if OS is not supported
                if (!pylorak.Windows.VersionInfo.Win81OrNewer)
                    return;

                if (!LayerIsIcmpError(layer))
                    conditions.Add(new PackageIdFilterCondition(r.AppContainerSid));
                else
                    return;
            }
            else
            {
                if (!Utils.IsNullOrEmpty(r.ServiceName))
                {
                    System.Diagnostics.Debug.Assert(!r.ServiceName.Equals("*"));
                    if (!LayerIsIcmpError(layer))
                        conditions.Add(new ServiceNameFilterCondition(r.ServiceName));
                    else
                        return;
                }

                if (!Utils.IsNullOrEmpty(r.Application))
                {
                    System.Diagnostics.Debug.Assert(!r.Application.Equals("*"));

                    if (!LayerIsIcmpError(layer))
                        conditions.Add(new AppIdFilterCondition(r.Application, false, true));
                    else
                        return;
                }
            }

            // IP address
            if (!Utils.IsNullOrEmpty(r.RemoteAddresses))
            {
                System.Diagnostics.Debug.Assert(!r.RemoteAddresses.Equals("*"));

                bool validAddressFound = false;
                foreach (var ipStr in r.RemoteAddresses.AsSpan().Split(',', SpanSplitOptions.RemoveEmptyEntries))
                {
                    if (ipStr.Equals(RuleDef.LOCALSUBNET_ID, StringComparison.Ordinal))
                    {
                        foreach (var filter in LocalSubnetFilterConditions)
                            validAddressFound |= addCommonIpFilterCondition((IpFilterCondition)filter, conditions);
                    }
                    else if (ipStr.Equals("DefaultGateway", StringComparison.Ordinal))
                    {
                        foreach (var filter in GatewayFilterConditions)
                            validAddressFound |= addCommonIpFilterCondition((IpFilterCondition)filter, conditions);
                    }
                    else if (ipStr.Equals("DNS", StringComparison.Ordinal))
                    {
                        foreach (var filter in DnsFilterConditions)
                            validAddressFound |= addCommonIpFilterCondition((IpFilterCondition)filter, conditions);
                    }
                    else
                    {
                        validAddressFound |= addIpFilterCondition(IpAddrMask.Parse(ipStr), RemoteOrLocal.Remote, conditions);
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

            // Protocol
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

            // Ports
            if (!Utils.IsNullOrEmpty(r.LocalPorts))
            {
                System.Diagnostics.Debug.Assert(!r.LocalPorts.Equals("*"));
                foreach (var p in r.LocalPorts.AsSpan().Split(',', SpanSplitOptions.RemoveEmptyEntries))
                {
                    (var minPort, var maxPort) = parseUInt16Range(p);
                    conditions.Add(new PortFilterCondition(minPort, maxPort, RemoteOrLocal.Local));
                }
            }
            if (!Utils.IsNullOrEmpty(r.RemotePorts))
            {
                System.Diagnostics.Debug.Assert(!r.RemotePorts.Equals("*"));
                foreach (var p in r.RemotePorts.AsSpan().Split(',', SpanSplitOptions.RemoveEmptyEntries))
                {
                    (var minPort, var maxPort) = parseUInt16Range(p);
                    conditions.Add(new PortFilterCondition(minPort, maxPort, RemoteOrLocal.Remote));
                }
            }

            // ICMP
            if (!Utils.IsNullOrEmpty(r.IcmpTypesAndCodes))
            {
                System.Diagnostics.Debug.Assert(!r.IcmpTypesAndCodes.Equals("*"));
                foreach (var e in r.IcmpTypesAndCodes.AsSpan().Split(',', SpanSplitOptions.RemoveEmptyEntries))
                {
                    using var tc = e.Split(':');
                    tc.MoveNext(); var icmpType = tc.Current;

                    if (LayerIsIcmpError(layer))
                    {
                        // ICMP Type
                        if ((icmpType.Length != 0) && icmpType.TryDecimalToUInt16(out ushort icmpTypeVal))
                            conditions.Add(new IcmpErrorTypeFilterCondition(icmpTypeVal));

                        // ICMP Code
                        if (tc.MoveNext())
                        {
                            var icmpCode = tc.Current;
                            if ((icmpCode.Length != 0) && icmpCode.TryDecimalToUInt16(out ushort icmpCodeVal))
                                conditions.Add(new IcmpErrorCodeFilterCondition(icmpCodeVal));
                        }
                    }
                    else
                    {
                        // ICMP Type - note different condition key
                        if ((icmpType.Length != 0) && icmpType.TryDecimalToUInt16(out ushort icmpTypeVal))
                            conditions.Add(new IcmpTypeFilterCondition(icmpTypeVal));

                        // Matching on ICMP Code not possible
                    }
                }
            }

            // Create and install filter
            using (Filter f = new Filter(
                r.ExceptionId.ToString(),
                r.Name,
                TINYWALL_PROVIDER_KEY,
                (r.Action == RuleAction.Allow) ? FilterActions.FWP_ACTION_PERMIT : FilterActions.FWP_ACTION_BLOCK,
                r.Weight,
                conditions
            ))
            {
                f.LayerKey = GetLayerKey(layer);
                f.SublayerKey = GetSublayerKey(layer);

                InstallWfpFilter(f);
            }
        }

        private void InstallRawSocketBlocks()
        {
            InstallRawSocketBlocks(LayerKeyEnum.FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V4);
            InstallRawSocketBlocks(LayerKeyEnum.FWPM_LAYER_ALE_RESOURCE_ASSIGNMENT_V6);
        }

        private void InstallRawSocketBlocks(LayerKeyEnum layer)
        {
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
                f.Conditions.Add(new FlagsFilterCondition(ConditionFlags.FWP_CONDITION_FLAG_IS_RAW_ENDPOINT, FieldMatchType.FWP_MATCH_FLAGS_ANY_SET));

                InstallWfpFilter(f);
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
            FilterActions action = permit ? FilterActions.FWP_ACTION_PERMIT : FilterActions.FWP_ACTION_BLOCK;
            ulong weight = (ulong)(permit ? FilterWeights.UserPermit : FilterWeights.UserBlock);

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
                f.Conditions.Add(new LocalInterfaceCondition(ifAlias));

                InstallWfpFilter(f);
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
                try
                {
                    using var conditions = new FilterConditionList();
                    if (!Utils.IsNullOrEmpty(subj.Application))
                        conditions.Add(new AppIdFilterCondition(subj.Application, false, true));
                    if (!Utils.IsNullOrEmpty(subj.ServiceName))
                        conditions.Add(new ServiceNameFilterCondition(subj.ServiceName));
                    if (conditions.Count == 0)
                        return;

                    using (Filter f = new Filter(
                        "Raw socket permit",
                        string.Empty,
                        TINYWALL_PROVIDER_KEY,
                        FilterActions.FWP_ACTION_PERMIT,
                        (ulong)FilterWeights.RawSocketPermit,
                        conditions
                    ))
                    {
                        f.LayerKey = GetLayerKey(layer);
                        f.SublayerKey = GetSublayerKey(layer);

                        InstallWfpFilter(f);
                    }
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

                // Don't affect loopback traffic
                f.Conditions.Add(new FlagsFilterCondition(ConditionFlags.FWP_CONDITION_FLAG_IS_LOOPBACK | ConditionFlags.FWP_CONDITION_FLAG_IS_IPSEC_SECURED, FieldMatchType.FWP_MATCH_FLAGS_NONE_SET));

                InstallWfpFilter(f);
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
                DatabaseClasses.Application? app = GlobalInstances.AppDatabase.GetApplicationByName(name);
                if (app is null)
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
                        UnrestrictedPolicy pol = (UnrestrictedPolicy)ex.Policy;

                        RuleDef def = new RuleDef(ex.Id, "Full access", ex.Subject, RuleAction.Allow, RuleDirection.InOut, Protocol.Any, permitWeight);
                        if (pol.LocalNetworkOnly)
                            def.RemoteAddresses = RuleDef.LOCALSUBNET_ID;
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
                        TcpUdpPolicy pol = (TcpUdpPolicy)ex.Policy;

                        // Incoming
                        if (!string.IsNullOrEmpty(pol.AllowedLocalTcpListenerPorts) && (pol.AllowedLocalTcpListenerPorts == pol.AllowedLocalUdpListenerPorts))
                        {
                            RuleDef def = new RuleDef(ex.Id, "TCP/UDP Listen Ports", ex.Subject, RuleAction.Allow, RuleDirection.In, Protocol.TcpUdp, permitWeight);
                            if (!string.Equals(pol.AllowedLocalTcpListenerPorts, "*"))
                                def.LocalPorts = pol.AllowedLocalTcpListenerPorts;
                            if (pol.LocalNetworkOnly)
                                def.RemoteAddresses = RuleDef.LOCALSUBNET_ID;
                            results.Add(def);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(pol.AllowedLocalTcpListenerPorts))
                            {
                                RuleDef def = new RuleDef(ex.Id, "TCP Listen Ports", ex.Subject, RuleAction.Allow, RuleDirection.In, Protocol.TCP, permitWeight);
                                if (!string.Equals(pol.AllowedLocalTcpListenerPorts, "*"))
                                    def.LocalPorts = pol.AllowedLocalTcpListenerPorts;
                                if (pol.LocalNetworkOnly)
                                    def.RemoteAddresses = RuleDef.LOCALSUBNET_ID;
                                results.Add(def);
                            }
                            if (!string.IsNullOrEmpty(pol.AllowedLocalUdpListenerPorts))
                            {
                                RuleDef def = new RuleDef(ex.Id, "UDP Listen Ports", ex.Subject, RuleAction.Allow, RuleDirection.In, Protocol.UDP, permitWeight);
                                if (!string.Equals(pol.AllowedLocalUdpListenerPorts, "*"))
                                    def.LocalPorts = pol.AllowedLocalUdpListenerPorts;
                                if (pol.LocalNetworkOnly)
                                    def.RemoteAddresses = RuleDef.LOCALSUBNET_ID;
                                results.Add(def);
                            }
                        }

                        // Outgoing
                        if (!string.IsNullOrEmpty(pol.AllowedRemoteTcpConnectPorts) && (pol.AllowedRemoteTcpConnectPorts == pol.AllowedRemoteUdpConnectPorts))
                        {
                            RuleDef def = new RuleDef(ex.Id, "TCP/UDP Outbound Ports", ex.Subject, RuleAction.Allow, RuleDirection.Out, Protocol.TcpUdp, permitWeight);
                            if (!string.Equals(pol.AllowedRemoteTcpConnectPorts, "*"))
                                def.RemotePorts = pol.AllowedRemoteTcpConnectPorts;
                            if (pol.LocalNetworkOnly)
                                def.RemoteAddresses = RuleDef.LOCALSUBNET_ID;
                            results.Add(def);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(pol.AllowedRemoteTcpConnectPorts))
                            {
                                RuleDef def = new RuleDef(ex.Id, "TCP Outbound Ports", ex.Subject, RuleAction.Allow, RuleDirection.Out, Protocol.TCP, permitWeight);
                                if (!string.Equals(pol.AllowedRemoteTcpConnectPorts, "*"))
                                    def.RemotePorts = pol.AllowedRemoteTcpConnectPorts;
                                if (pol.LocalNetworkOnly)
                                    def.RemoteAddresses = RuleDef.LOCALSUBNET_ID;
                                results.Add(def);
                            }
                            if (!string.IsNullOrEmpty(pol.AllowedRemoteUdpConnectPorts))
                            {
                                RuleDef def = new RuleDef(ex.Id, "UDP Outbound Ports", ex.Subject, RuleAction.Allow, RuleDirection.Out, Protocol.UDP, permitWeight);
                                if (!string.Equals(pol.AllowedRemoteUdpConnectPorts, "*"))
                                    def.RemotePorts = pol.AllowedRemoteUdpConnectPorts;
                                if (pol.LocalNetworkOnly)
                                    def.RemoteAddresses = RuleDef.LOCALSUBNET_ID;
                                results.Add(def);
                            }
                        }
                        break;
                    }
                case PolicyType.RuleList:
                    {
                        // The RuleDefs returned can get modified by the caller.
                        // To avoid changing the original templates we return copies of rules.

                        RuleListPolicy pol = (RuleListPolicy)ex.Policy;
                        foreach (var rule in pol.Rules)
                        {
                            var ruleCopy = rule.ShallowCopy();
                            ruleCopy.SetSubject(ex.Subject);
                            ruleCopy.ExceptionId = ex.Id;
                            ruleCopy.Weight = (rule.Action == RuleAction.Allow) ? permitWeight : blockWeight;
                            results.Add(ruleCopy);
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
            try
            {
                return ServerConfiguration.Load(ConfigSavePath);
            }
            catch { }

            // Load from file failed, prepare default config instead

            var ret = new ServerConfiguration();
            ret.SetActiveProfile(Resources.Messages.Default);

            // Allow recommended exceptions
            DatabaseClasses.AppDatabase db = GlobalInstances.AppDatabase;
            foreach (DatabaseClasses.Application app in db.KnownApplications)
            {
                if (app.HasFlag("TWUI:Special") && app.HasFlag("TWUI:Recommended"))
                {
                    ret.ActiveProfile.SpecialExceptions.Add(app.Name);
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
                HostsFileManager.EnableProtection = ActiveConfig.Service.LockHostsFile;
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
                        string filePath = Path.Combine(Utils.AppDataPath, LastUpdateCheck_FILENAME);
                        if (File.Exists(filePath))
                        {
                            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                            using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                            {
                                LastUpdateCheck_ = DateTime.Parse(sr.ReadLine());
                            }
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
                    string filePath = Path.Combine(Utils.AppDataPath, LastUpdateCheck_FILENAME);
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
                UpdateModule? module = UpdateChecker.GetDatabaseFileModule(VisibleState.Update);
                if (module is not null)
                {
                    if (!string.Equals(module.DownloadHash, Hasher.HashFile(DatabaseClasses.AppDatabase.DBPath), StringComparison.OrdinalIgnoreCase))
                    {
                        GetCompressedUpdate(module, DatabaseUpdateInstall);
                    }
                }

                module = UpdateChecker.GetHostsFileModule(VisibleState.Update);
                if (module is not null)
                {
                    if (!string.Equals(module.DownloadHash, HostsFileManager.GetHostsHash(), StringComparison.OrdinalIgnoreCase))
                    {
                        GetCompressedUpdate(module, HostsUpdateInstall);
                    }
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

            FileLocker.Unlock(DatabaseClasses.AppDatabase.DBPath);
            using (var afu = new AtomicFileUpdater(DatabaseClasses.AppDatabase.DBPath))
            {
                File.Copy(tmpFilePath, afu.TemporaryFilePath, true);
                afu.Commit();
            }
            FileLocker.Lock(DatabaseClasses.AppDatabase.DBPath, FileAccess.Read, FileShare.Read);
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
                        FirewallMode newMode = (FirewallMode)req.Arguments[0];

                        if (CommitLearnedRules())
                            ActiveConfig.Service.Save(ConfigSavePath);

                        try
                        {
                            LogWatcher.Enabled = (FirewallMode.Learning == newMode);
                        }
                        catch (Exception e)
                        {
                            Utils.Log("Cannot enter auto-learn mode. Is the 'eventlog' service running? For details see next log entry.", Utils.LOG_ID_SERVICE);
                            Utils.LogException(e, Utils.LOG_ID_SERVICE);
                            return new TwMessage(MessageType.RESPONSE_ERROR);
                        }

                        VisibleState.Mode = newMode;
                        InstallFirewallRules();

                        if (
                               (VisibleState.Mode != FirewallMode.Disabled)
                            && (VisibleState.Mode != FirewallMode.Learning)
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
                        List<FirewallExceptionV3> exceptions = (List<FirewallExceptionV3>)req.Arguments[0];

                        foreach (var ex in exceptions)
                        {
                            GetRulesForException(ex, rules, rawSocketExceptions, (ulong)FilterWeights.UserPermit, (ulong)FilterWeights.UserBlock);
                        }

                        InstallRules(rules, rawSocketExceptions, true);
                        lock (FirewallThreadThrottler.SynchRoot) { FirewallThreadThrottler.Release(); }

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
                        var pid = (uint)req.Arguments[0];
                        string path = Utils.GetPathOfProcess(pid);
                        if (string.IsNullOrEmpty(path))
                            return new TwMessage(MessageType.RESPONSE_ERROR);
                        else
                            return new TwMessage(MessageType.RESPONSE_OK, path);
                    }
                case MessageType.SET_PASSPHRASE:
                    {
                        FileLocker.Unlock(PasswordManager.PasswordFilePath);
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
                            FileLocker.Lock(PasswordManager.PasswordFilePath, FileAccess.Read, FileShare.Read);
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
                case MessageType.DISPLAY_POWER_EVENT:
                    {
                        bool turnOn = (bool)req.Arguments[0];
                        if (turnOn != DisplayCurrentlyOn)
                        {
                            DisplayCurrentlyOn = turnOn;
                            InstallFirewallRules();
                        }
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

                bool ipConfigurationChanged =
                    !LocalSubnetAddreses.SetEquals(newLocalSubnetAddreses) ||
                    !GatewayAddresses.SetEquals(newGatewayAddresses) ||
                    !DnsAddresses.SetEquals(newDnsAddresses);

                if (ipConfigurationChanged)
                {
                    LocalSubnetAddreses = newLocalSubnetAddreses;
                    GatewayAddresses = newGatewayAddresses;
                    DnsAddresses = newDnsAddresses;

                    LocalSubnetFilterConditions.Clear();
                    GatewayFilterConditions.Clear();
                    DnsFilterConditions.Clear();
                    
                    foreach (var addr in LocalSubnetAddreses)
                        LocalSubnetFilterConditions.Add(new IpFilterCondition(addr.Address, (byte)addr.PrefixLen, RemoteOrLocal.Remote));
                    foreach (var addr in GatewayAddresses)
                        GatewayFilterConditions.Add(new IpFilterCondition(addr.Address, (byte)addr.PrefixLen, RemoteOrLocal.Remote));
                    foreach (var addr in DnsAddresses)
                        DnsFilterConditions.Add(new IpFilterCondition(addr.Address, (byte)addr.PrefixLen, RemoteOrLocal.Remote));
                }

                return ipConfigurationChanged;
            }
        }

        internal static void DeleteWfpObjects(Engine wfp, bool removeLayersAndProvider)
        {
            // WARNING! This method is super-slow if not executed inside a WFP transaction!
            using (var timer = new HierarchicalStopwatch("DeleteWfpObjects()"))
            {
                var layerKeys = (LayerKeyEnum[])Enum.GetValues(typeof(LayerKeyEnum));
                foreach (var layer in layerKeys)
                {
                    Guid layerKey = GetLayerKey(layer);
                    Guid subLayerKey = GetSublayerKey(layer);

                    // Remove filters in the sublayer
                    foreach (var filterKey in wfp.EnumerateFilterKeys(TINYWALL_PROVIDER_KEY, layerKey))
                        wfp.UnregisterFilter(filterKey);

                    // Remove sublayer
                    if (removeLayersAndProvider)
                        try { wfp.UnregisterSublayer(subLayerKey); } catch { }
                }

                // Remove provider
                if (removeLayersAndProvider)
                    try { wfp.UnregisterProvider(TINYWALL_PROVIDER_KEY); } catch { }
            }
        }

        public TinyWallServer()
        {
            // Make sure the very-first command is a REINIT
            Q.Enqueue(new TwMessage(MessageType.REINIT), null);

            // Fire up file protections as soon as possible
            FileLocker.Lock(DatabaseClasses.AppDatabase.DBPath, FileAccess.Read, FileShare.Read);
            FileLocker.Lock(PasswordManager.PasswordFilePath, FileAccess.Read, FileShare.Read);

            // Lock configuration if we have a password
            if (ServiceLocker.HasPassword)
                ServiceLocker.Locked = true;

            LogWatcher.NewLogEntry += (FirewallLogWatcher sender, FirewallLogEntry entry) => { AutoLearnLogEntry(entry); };
            MinuteTimer = new Timer(new TimerCallback(TimerCallback), null, Timeout.Infinite, Timeout.Infinite);

            // Discover network configuration
            ReenumerateAdresses();

            // Fire up pipe
            ServerPipe = new PipeServerEndpoint(new PipeDataReceived(PipeServerDataReceived), "TinyWallController");
        }

        // Entry point for thread that actually issues commands to Windows Firewall.
        // Only one thread (this one) is allowed to issue them.
        public void Run(ServiceBase service)
        {
            using (var timer = new HierarchicalStopwatch("Service Run()"))
            {
                using (var WinDefFirewall = new WindowsFirewall())
                using (var NetworkInterfaceWatcher = new IpInterfaceWatcher())
                using (var WfpEvent = WfpEngine.SubscribeNetEvent(WfpNetEventCallback))
                using (var DisplayOffSubscription = SafeHandlePowerSettingNotification.Create(service.ServiceHandle, PowerSetting.GUID_CONSOLE_DISPLAY_STATE, DeviceNotifFlags.DEVICE_NOTIFY_SERVICE_HANDLE))
                using (var DeviceNotification = SafeHandleDeviceNotification.Create(service.ServiceHandle, DeviceInterfaceClass.GUID_DEVINTERFACE_VOLUME, DeviceNotifFlags.DEVICE_NOTIFY_SERVICE_HANDLE))
                using (var MountPointsWatcher = new RegistryWatcher(@"HKEY_LOCAL_MACHINE\SYSTEM\MountedDevices", true))
                {
                    WfpEngine.CollectNetEvents = true;
                    WfpEngine.EventMatchAnyKeywords = InboundEventMatchKeyword.FWPM_NET_EVENT_KEYWORD_INBOUND_BCAST | InboundEventMatchKeyword.FWPM_NET_EVENT_KEYWORD_INBOUND_MCAST;

                    ProcessStartWatcher.EventArrived += ProcessStartWatcher_EventArrived;
                    NetworkInterfaceWatcher.InterfaceChanged += (object sender, EventArgs args) =>
                    {
                        Q.Enqueue(new TwMessage(MessageType.REENUMERATE_ADDRESSES), null);
                    };
                    RuleReloadEventMerger.Event += (object sender, EventArgs args) =>
                    {
                        Q.Enqueue(new TwMessage(MessageType.RELOAD_WFP_FILTERS), null);
                    };
                    MountPointsWatcher.RegistryChanged += (object sender, EventArgs args) =>
                    {
                        RuleReloadEventMerger.Pulse();
                    };
                    MountPointsWatcher.Enabled = true;
                    service.FinishStateChange();
#if !DEBUG
                    // Basic software health checks
                    TinyWallDoctor.EnsureHealth(Utils.LOG_ID_SERVICE);
#endif

                    MinuteTimer.Change(60000, 60000);
                    RunService = true;
                    while (RunService)
                    {
                        timer.NewSubTask("Message wait");
                        Q.Dequeue(out TwMessage msg, out Future<TwMessage>? future);

                        timer.NewSubTask($"Message {msg.Type}");
                        try
                        {
                            TwMessage resp = ProcessCmd(msg);
                            if (future is not null)
                                future.Value = resp;
                        }
                        catch (Exception e)
                        {
                            Utils.LogException(e, Utils.LOG_ID_SERVICE);
                            if (null != future)
                                future.Value = new TwMessage(MessageType.RESPONSE_ERROR);
                        }
                    }
                }
            }
        }

        private void ProcessStartWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                using (var throttler = new ThreadThrottler(Thread.CurrentThread, ThreadPriority.Highest, true))
                {
                    uint pid = (uint)(e.NewEvent["ProcessID"]);
                    string path = ProcessManager.GetProcessPath(pid, ref ProcessStartWatcher_Sbuilder);

                    // Skip if we have no path
                    if (string.IsNullOrEmpty(path))
                        return;

                    List<FirewallExceptionV3>? newExceptions = null;

                    lock (InheritanceGuard)
                    {
                        // Skip if we have a user-defined rule for this path
                        if (UserSubjectExes.Contains(path))
                            return;

                        // This list will hold parents that we already checked for a process.
                        // Used to avoid infinite loop when parent-PID info is unreliable.
                        var pidsChecked = new HashSet<uint>();

                        // Start walking up the process tree
                        for (var parentPid = pid; ;)
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

                            string parentPath = ProcessManager.GetProcessPath(parentPid, ref ProcessStartWatcher_Sbuilder);
                            if (string.IsNullOrEmpty(parentPath))
                                continue;

                            // Skip if we have already processed this parent-child combination
                            if (ChildInheritedSubjectExes.ContainsKey(path) && ChildInheritedSubjectExes[path].Contains(parentPath))
                                break;

                            if (ChildInheritance.TryGetValue(parentPath, out List<FirewallExceptionV3> exList))
                            {
                                newExceptions ??= new List<FirewallExceptionV3>();

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

        private void WfpNetEventCallback(NetEventData data)
        {
            EventLogEvent eventType = EventLogEvent.ALLOWED;
            if (data.EventType == FWPM_NET_EVENT_TYPE.FWPM_NET_EVENT_TYPE_CLASSIFY_DROP)
                eventType = EventLogEvent.BLOCKED;
            else if (data.EventType == FWPM_NET_EVENT_TYPE.FWPM_NET_EVENT_TYPE_CLASSIFY_ALLOW)
                eventType = EventLogEvent.ALLOWED;
            else
                return;

            FirewallLogEntry entry = new FirewallLogEntry();
            entry.Timestamp = data.timeStamp;
            entry.Event = eventType;

            if (!Utils.IsNullOrEmpty(data.appId))
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
                ((string.Equals(entry.RemoteIp, "127.0.0.1", StringComparison.Ordinal)
                && string.Equals(entry.LocalIp, "127.0.0.1", StringComparison.Ordinal)))
               || // IPv6
                ((string.Equals(entry.RemoteIp, "::1", StringComparison.Ordinal)
                && string.Equals(entry.LocalIp, "::1", StringComparison.Ordinal)))
               )
            {
                // Ignore communication within local machine
                return;
            }

            // Certain things we don't want to whitelist
            if (Utils.IsNullOrEmpty(entry.AppPath)
                || string.Equals(entry.AppPath, "System", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(entry.AppPath, "svchost.exe", StringComparison.InvariantCultureIgnoreCase)
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

                List<FirewallExceptionV3> exceptions = GlobalInstances.AppDatabase.GetExceptionsForApp(newSubject, false, out _);
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

        public void DisplayPowerEvent(bool turnOn)
        {
            Q.Enqueue(new TwMessage(MessageType.DISPLAY_POWER_EVENT, turnOn), null);
        }

        public void MountedVolumesChangedEvent()
        {
            RuleReloadEventMerger.Pulse();
        }

        public void Dispose()
        {
            using (var timer = new HierarchicalStopwatch("TinyWallService.Dispose()"))
            {
                ServerPipe?.Dispose();
                ProcessStartWatcher.Dispose();

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
                    using WaitHandle wh = new AutoResetEvent(false);
                    MinuteTimer.Dispose(wh);
                    wh.WaitOne();
                }

                RuleReloadEventMerger.Dispose();
                LocalSubnetFilterConditions.Dispose();
                GatewayFilterConditions.Dispose();
                DnsFilterConditions.Dispose();
                LogWatcher.Dispose();
                CommitLearnedRules();
                ActiveConfig.Service.Save(ConfigSavePath);
                HostsFileManager.Dispose();
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

        private TinyWallServer? Server;
        private Thread? FirewallWorkerThread;
#if !DEBUG
        private bool IsComputerShuttingDown;
#endif
        internal TinyWallService()
            : base()
        {
            this.AcceptedControls = ServiceAcceptedControl.SERVICE_ACCEPT_SHUTDOWN;
            this.AcceptedControls |= ServiceAcceptedControl.SERVICE_ACCEPT_POWEREVENT;
#if DEBUG
            this.AcceptedControls |= ServiceAcceptedControl.SERVICE_ACCEPT_STOP;
#endif
        }

        public override string ServiceName
        {
            get { return SERVICE_NAME; }
        }

        private void FirewallWorkerMethod()
        {
            try
            {
                using (Server = new TinyWallServer())
                {
                    Server.Run(this);
                }
            }
            finally
            {
#if !DEBUG
                Thread.MemoryBarrier();
                if (!IsComputerShuttingDown)    // cannot set service state if a shutdown is already in progress
                {
                    SetServiceStateReached(ServiceState.Stopped);
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

        private void StopServer()
        {
            Thread.MemoryBarrier();
            Server?.RequestStop();
            FirewallWorkerThread?.Join(10000);
            FinishStateChange();
        }

        // Executed when service is stopped manually.
        protected override void OnStop()
        {
            StopServer();
        }

        // Executed on computer shutdown.
        protected override void OnShutdown()
        {
#if !DEBUG
            IsComputerShuttingDown = true;
#endif
            StartStateChange(ServiceState.StopPending);
        }

        protected override void OnDeviceEvent(DeviceEventData data)
        {
            if ((data.Event == DeviceEventType.DeviceArrival) || (data.Event == DeviceEventType.DeviceRemoveComplete))
            {
                bool pathMapperRebuildNeeded = false;

                if (data.DeviceType == DeviceBroadcastHdrDevType.DBT_DEVTYP_DEVICEINTERFACE)
                {
                    if (data.Class == DeviceInterfaceClass.GUID_DEVINTERFACE_VOLUME)
                    {
                        pathMapperRebuildNeeded = true;
                    }
                }
                else if (data.DeviceType == DeviceBroadcastHdrDevType.DBT_DEVTYP_VOLUME)
                {
                    pathMapperRebuildNeeded = true;
                }

                if (pathMapperRebuildNeeded)
                {
                    Server?.MountedVolumesChangedEvent();
                }
            }
        }

        protected override void OnPowerEvent(PowerEventData data)
        {
            if (data.Event == PowerEventType.PowerSettingChange)
            {
                if (data.Setting == PowerSetting.GUID_CONSOLE_DISPLAY_STATE)
                {
                    if (data.PayloadInt == 0)
                        Server?.DisplayPowerEvent(false);
                    else if (data.PayloadInt == 1)
                        Server?.DisplayPowerEvent(true);
                    else
                    {
                        // Dimming event... ignore
                    }
                }
            }
        }
    }
}
