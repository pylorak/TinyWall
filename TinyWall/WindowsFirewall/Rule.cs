using System;

using NetFwTypeLib;

namespace PKSoft.WindowsFirewall
{
    /// <summary>
    /// Represents a single firewall Rule.
    /// </summary>
    internal class Rule
    {
        private static readonly Type tNetFwRule = Type.GetTypeFromProgID("HNetCfg.FwRule");
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

        internal Rule(string name, string desc, ProfileType profile, RuleDirection dir, PKSoft.WindowsFirewall.PacketAction action, Protocol protocol)
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
        internal PacketAction Action
        {
            get { return (PacketAction)fwRule.Action; }
            set { fwRule.Action = (NET_FW_ACTION_)value; }
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
            get { return (RuleDirection)fwRule.Direction; }
            set { fwRule.Direction = (NET_FW_RULE_DIRECTION_)value; }
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
    }
}
