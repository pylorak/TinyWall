using System;
using System.IO;
using System.ServiceProcess;
using System.Collections.Generic;
using PKSoft.WindowsFirewall;

namespace PKSoft
{
    [Serializable]
    public class RuleDef
    {
        private const string EPHEMERAL_PORT_RANGE = "1025-65535";
        private const string EPHEMERAL_TOKEN = "EPHEMERAL";

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name;
        public string ExceptionId;
        public PacketAction Action;

        public string Application;
        public string ServiceName;
        public string LocalPorts;
        public string RemotePorts;
        public string LocalAddresses;
        public string RemoteAddresses;
        public string IcmpTypesAndCodes;

        public Protocol Protocol;
        public RuleDirection Direction;

        public RuleDef()
        {
        }

        public RuleDef(string exid, string name, PacketAction action, RuleDirection direction, Protocol protocol)
        {
            this.Name = name;
            this.ExceptionId = exid;
            this.Action = action;
            this.Direction = direction;
            this.Protocol = protocol;
        }

        internal void ConstructRule(List<Rule> ruleset)
        {
            if (!string.IsNullOrEmpty(ServiceName))
            {
                // Check if service exists
                using (ServiceController sc = new ServiceController(ServiceName))
                { }
            }

            if (this.Protocol == WindowsFirewall.Protocol.TcpUdp)
            {
                RuleDef pProt;

                // For TCP
                pProt = Utils.DeepClone(this);
                pProt.ExceptionId += "[TCP]";
                pProt.Protocol = WindowsFirewall.Protocol.TCP;
                pProt.ConstructRule(ruleset);

                // For UDP
                pProt = Utils.DeepClone(this);
                pProt.ExceptionId += "[UDP]";
                pProt.Protocol = WindowsFirewall.Protocol.UDP;
                pProt.ConstructRule(ruleset);
            }
            else if (this.Protocol == WindowsFirewall.Protocol.ICMP)
            {
                RuleDef pProt;

                // For ICMPv4
                pProt = Utils.DeepClone(this);
                pProt.ExceptionId += "[ICMPv4]";
                pProt.Protocol = WindowsFirewall.Protocol.ICMPv4;
                pProt.ConstructRule(ruleset);

                // For ICMPv6
                pProt = Utils.DeepClone(this);
                pProt.ExceptionId += "[ICMPv6]";
                pProt.Protocol = WindowsFirewall.Protocol.ICMPv6;
                pProt.ConstructRule(ruleset);
            }
            else if (this.Direction == WindowsFirewall.RuleDirection.InOut)
            {
                RuleDef pDir;

                // For IN
                pDir = Utils.DeepClone(this);
                pDir.ExceptionId += "[in]";
                pDir.Direction = WindowsFirewall.RuleDirection.In;
                pDir.ConstructRule(ruleset);

                // For OUT
                pDir = Utils.DeepClone(this);
                pDir.ExceptionId += "[out]";
                pDir.Direction = WindowsFirewall.RuleDirection.Out;
                pDir.ConstructRule(ruleset);
            }
            else
            {
                Rule r = new Rule(ExceptionId + " " + this.Name, string.Empty, ProfileType.All, this.Direction, this.Action, this.Protocol);
                if (!string.IsNullOrEmpty(this.Application))
                    r.ApplicationName = this.Application;
                if (!string.IsNullOrEmpty(this.ServiceName))
                    r.ServiceName = this.ServiceName;
                if (!string.IsNullOrEmpty(this.LocalAddresses))
                    r.LocalAddresses = this.LocalAddresses;
                if (!string.IsNullOrEmpty(this.RemoteAddresses))
                    r.RemoteAddresses = this.RemoteAddresses;
                if (!string.IsNullOrEmpty(this.IcmpTypesAndCodes))
                    r.IcmpTypesAndCodes = this.IcmpTypesAndCodes;

                if (!string.IsNullOrEmpty(this.LocalPorts))
                {
                    this.LocalPorts = this.LocalPorts.Replace(EPHEMERAL_TOKEN, EPHEMERAL_PORT_RANGE);
                    r.LocalPorts = this.LocalPorts;
                }
                if (!string.IsNullOrEmpty(this.RemotePorts))
                {
                    this.RemotePorts = this.RemotePorts.Replace(EPHEMERAL_TOKEN, EPHEMERAL_PORT_RANGE);
                    r.RemotePorts = this.RemotePorts;
                }

                ruleset.Add(r);
            }
        }
    }
}
