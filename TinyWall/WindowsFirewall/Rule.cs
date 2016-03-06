using System;
using System.Collections.Generic;
using System.ServiceProcess;
using TinyWall.Interface;
using NetFwTypeLib;

namespace PKSoft.WindowsFirewall
{
    /// <summary>
    /// Represents a single firewall Rule.
    /// </summary>
    internal sealed class Rule
    {
        private static readonly Type tNetFwRule = Type.GetTypeFromProgID("HNetCfg.FwRule");
        private const string EPHEMERAL_PORT_RANGE = "1025-65535";
        private const string EPHEMERAL_TOKEN = "EPHEMERAL";

        private INetFwRule fwRule;

        internal INetFwRule Handle
        {
            get { return fwRule; }
        }

        /// <summary>
        /// Creates an Instance of the <see>Rule</see> class from a INetFwRule.
        /// </summary>
        /// <param name="FwRule"></param>
        internal Rule(INetFwRule FwRule)
        {
            fwRule = FwRule;
        }

        internal Rule()
        {
            fwRule = (INetFwRule)Activator.CreateInstance(tNetFwRule);
        }

        internal Rule(string name, string desc, ProfileType profile, RuleDirection dir, RuleAction action, Protocol protocol)
            : this()
        {
            this.Name = name;
            this.Description = desc;
            if (protocol != Protocol.Any)
                this.Protocol = protocol;
            this.Direction = dir;
            this.Profiles = profile;
            this.Action = action;
            this.Enabled = true;
        }

        /// <summary>
        /// Accesses the Action property of this rule. 
        /// </summary>
        internal RuleAction Action
        {
            get { return ActionFw2Tw(fwRule.Action); }
            set { fwRule.Action = ActionTw2Fw(value); }
        }
        /// <summary>
        ///  Accesses the ApplicationName property for this rule. 
        /// </summary>
        internal string ApplicationName
        {
            get { return fwRule.ApplicationName; }
            set { fwRule.ApplicationName = value; }
        }

        /// <summary>
        /// Accesses the Description property for this rule. 
        /// </summary>
        /// <remarks>
        /// This property is optional. The string must not contain the "|" character.
        /// </remarks>
        internal string Description
        {
            get { return fwRule.Description; }
            set { fwRule.Description = value; }
        }

        /// <summary>
        /// Accesses the Direction property for this rule. 
        /// </summary>
        internal RuleDirection Direction
        {
            get { return DirectionFw2Tw(fwRule.Direction); }
            set { fwRule.Direction = DirectionTw2Fw(value); }
        }

        internal static NET_FW_RULE_DIRECTION_ DirectionTw2Fw(RuleDirection v)
        {
            switch (v)
            {
                case RuleDirection.In:
                    return NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
                case RuleDirection.Out:
                    return NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                default:
                    throw new ArgumentException();
            }
        }

        internal static RuleDirection DirectionFw2Tw(NET_FW_RULE_DIRECTION_ v)
        {
            switch (v)
            {
                case NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN:
                    return RuleDirection.In;
                case NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT:
                    return RuleDirection.Out;
                default:
                    throw new NotSupportedException();
            }
        }

        internal static NET_FW_ACTION_ ActionTw2Fw(RuleAction v)
        {
            switch (v)
            {
                case RuleAction.Allow:
                    return NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                case RuleAction.Block:
                    return NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                default:
                    throw new ArgumentException();
            }
        }

        internal static RuleAction ActionFw2Tw(NET_FW_ACTION_ v)
        {
            switch (v)
            {
                case NET_FW_ACTION_.NET_FW_ACTION_ALLOW:
                    return RuleAction.Allow;
                case NET_FW_ACTION_.NET_FW_ACTION_BLOCK:
                    return RuleAction.Block;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Accesses the EdgeTraversal property for this rule. 
        /// </summary>
        internal bool EdgeTraversal
        {
            get { return fwRule.EdgeTraversal; }
            set { fwRule.EdgeTraversal = value; }
        }
        /// <summary>
        /// Accesses the Enabled property for this rule. 
        /// </summary>
        internal bool Enabled
        {
            get { return fwRule.Enabled; }
            set { fwRule.Enabled = value; }
        }
        /// <summary>
        /// Accesses the Grouping property for this rule. 
        /// </summary>
        internal string Grouping
        {
            get { return fwRule.Grouping; }
            set { fwRule.Grouping = value; }
        }
        /// <summary>
        /// Accesses the IcmpTypesAndCodes property for this rule. 
        /// </summary>
        internal string IcmpTypesAndCodes
        {
            get { return fwRule.IcmpTypesAndCodes; }
            set { fwRule.IcmpTypesAndCodes = value; }
        }
        /// <summary>
        /// Accesses the Interfaces property for this rule. 
        /// </summary>
        internal string[] Interfaces
        {
            get { return (string[])fwRule.Interfaces; }
            set { fwRule.Interfaces = value; }
        }
        /// <summary>
        /// Accesses the InterfaceTypes property for this rule. 
        /// </summary>
        internal string InterfaceTypes
        {
            get { return fwRule.InterfaceTypes; }
            set { fwRule.InterfaceTypes = value;}
        }
        /// <summary>
        /// Accesses the LocalAddresses property for this rule. 
        /// </summary>
        internal string LocalAddresses
        {
            get { return fwRule.LocalAddresses; }
            set {fwRule.LocalAddresses = value; }
        }
        /// <summary>
        /// Accesses the LocalPorts property of this rule. 
        /// </summary>
        internal string LocalPorts
        {
            get { return fwRule.LocalPorts; }
            set { fwRule.LocalPorts = value;}
        }
        /// <summary>
        /// Accesses the Name property for this rule. 
        /// </summary>
        internal string Name
        {
            get { return fwRule.Name; }
            set {fwRule.Name = value; }
        }

        /// <summary>
        /// Accesses the Profiles property for this rule. 
        /// </summary>
        internal ProfileType Profiles
        {
            get {return (ProfileType)fwRule.Profiles; }
            set { fwRule.Profiles = (int)value; }
        }
        /// <summary>
        /// Accesses the Protocol property for this rule. 
        /// </summary>
        internal Protocol Protocol
        {
            get {return (Protocol)fwRule.Protocol; }
            set { fwRule.Protocol = (int)value; }
        }

        /// <summary>
        /// Accesses the RemoteAddresses property of this rule. 
        /// </summary>
        internal string RemoteAddresses
        {
            get {return fwRule.RemoteAddresses; }
            set { fwRule.RemoteAddresses = value; }
        }

        /// <summary>
        /// Accesses the RemotePorts property for this rule. 
        /// </summary>
        internal string RemotePorts
        {
            get { return fwRule.RemotePorts; }
            set {fwRule.RemotePorts = value; }
        }
        /// <summary>
        /// Accesses the ServiceaName property for this rule. 
        /// </summary>
        internal string ServiceName
        {
            get { return fwRule.serviceName; }
            set { fwRule.serviceName = value; }
        }

        internal static void ConstructRule(List<Rule> ruleset, RuleDef rd)
        {
            if (!string.IsNullOrEmpty(rd.ServiceName))
            {
                // Check if service exists
                using (ServiceController sc = new ServiceController(rd.ServiceName))
                { }
            }

            if (rd.Protocol == Protocol.TcpUdp)
            {
                RuleDef pProt;

                // For TCP
                pProt = rd.DeepCopy();
                pProt.Name = "[TCP]"+ pProt.Name;
                pProt.Protocol = Protocol.TCP;
                ConstructRule(ruleset, pProt);

                // For UDP
                pProt = rd.DeepCopy();
                pProt.Name = "[UDP]" + pProt.Name;
                pProt.Protocol = Protocol.UDP;
                ConstructRule(ruleset, pProt);
            }
            else if (rd.Protocol == Protocol.ICMP)
            {
                RuleDef pProt;

                // For ICMPv4
                pProt = rd.DeepCopy();
                pProt.Name = "[ICMPv4]" + pProt.Name;
                pProt.Protocol = Protocol.ICMPv4;
                ConstructRule(ruleset, pProt);

                // For ICMPv6
                pProt = rd.DeepCopy();
                pProt.Name = "[ICMPv6]" + pProt.Name;
                pProt.Protocol = Protocol.ICMPv6;
                ConstructRule(ruleset, pProt);
            }
            else if (rd.Direction == RuleDirection.InOut)
            {
                RuleDef pDir;

                // For IN
                pDir = rd.DeepCopy();
                pDir.Name = "[in]" + pDir.Name;
                pDir.Direction = RuleDirection.In;
                ConstructRule(ruleset, pDir);

                // For OUT
                pDir = rd.DeepCopy();
                pDir.Name = "[out]" + pDir.Name;
                pDir.Direction = RuleDirection.Out;
                ConstructRule(ruleset, pDir);
            }
            else
            {
                Rule r = new Rule(rd.ExceptionId + " " + rd.Name, string.Empty, ProfileType.All, rd.Direction, rd.Action, rd.Protocol);
                if (!string.IsNullOrEmpty(rd.Application))
                    r.ApplicationName = rd.Application;
                if (!string.IsNullOrEmpty(rd.ServiceName))
                    r.ServiceName = rd.ServiceName;
                if (!string.IsNullOrEmpty(rd.LocalAddresses))
                    r.LocalAddresses = rd.LocalAddresses;
                if (!string.IsNullOrEmpty(rd.RemoteAddresses))
                    r.RemoteAddresses = rd.RemoteAddresses;
                if (!string.IsNullOrEmpty(rd.IcmpTypesAndCodes))
                    r.IcmpTypesAndCodes = rd.IcmpTypesAndCodes;

                if (!string.IsNullOrEmpty(rd.LocalPorts))
                {
                    rd.LocalPorts = rd.LocalPorts.Replace(EPHEMERAL_TOKEN, EPHEMERAL_PORT_RANGE);
                    r.LocalPorts = rd.LocalPorts;
                }
                if (!string.IsNullOrEmpty(rd.RemotePorts))
                {
                    // Windows Vista does not support port ranges. On Vista, we replace EPHEMERAL with no port restriction.
                    Version Win7Version = new Version(6, 1, 0, 0);
                    if (!rd.RemotePorts.Contains(EPHEMERAL_TOKEN) || (Environment.OSVersion.Version >= Win7Version))
                    {
                        rd.RemotePorts = rd.RemotePorts.Replace(EPHEMERAL_TOKEN, EPHEMERAL_PORT_RANGE);
                        r.RemotePorts = rd.RemotePorts;
                    }
                }

                ruleset.Add(r);
            }
        }
    }
}
