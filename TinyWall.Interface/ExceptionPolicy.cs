using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TinyWall.Interface
{
    public enum PolicyType
    {
        Invalid,
        HardBlock,
        Unrestricted,
        TcpUdpOnly,
        RuleList
    }


    // -----------------------------------------------------------------------

    [DataContract(Namespace = "TinyWall")]
    [Serializable]
    public abstract class ExceptionPolicy
    {
        public abstract PolicyType PolicyType { get; }

        public abstract bool MergeRulesTo(ref ExceptionPolicy other);
    }

    // -----------------------------------------------------------------------

    [DataContract(Namespace = "TinyWall")]
    [Serializable]
    public class HardBlockPolicy : ExceptionPolicy
    {
        public static HardBlockPolicy Instance { get; } = new HardBlockPolicy();

        public override PolicyType PolicyType
        {
            get
            {
                return PolicyType.HardBlock;
            }
        }

        public override bool MergeRulesTo(ref ExceptionPolicy other)
        {
            switch(other.PolicyType)
            {
                case PolicyType.RuleList:
                case PolicyType.HardBlock:
                case PolicyType.TcpUdpOnly:
                case PolicyType.Unrestricted:
                    other = HardBlockPolicy.Instance;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return true;
        }
    }

    // -----------------------------------------------------------------------

    [DataContract(Namespace = "TinyWall")]
    [Serializable]
    public class UnrestrictedPolicy : ExceptionPolicy
    {
        public override PolicyType PolicyType
        {
            get
            {
                return PolicyType.Unrestricted;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public bool LocalNetworkOnly { get; set; }

        public override bool MergeRulesTo(ref ExceptionPolicy other)
        {
            switch (other.PolicyType)
            {
                case PolicyType.Unrestricted:
                    UnrestrictedPolicy pol = other as UnrestrictedPolicy;
                    pol.LocalNetworkOnly |= this.LocalNetworkOnly;
                    break;
                case PolicyType.HardBlock:
                    other = HardBlockPolicy.Instance;
                    break;
                case PolicyType.RuleList:
                case PolicyType.TcpUdpOnly:
                    /* No action on purpose */
                    break;
                default:
                    throw new NotImplementedException();
            }

            return true;
        }
    }

    // -----------------------------------------------------------------------

    [DataContract(Namespace = "TinyWall")]
    [Serializable]
    public class TcpUdpPolicy : ExceptionPolicy
    {
        public override PolicyType PolicyType
        {
            get
            {
                return PolicyType.TcpUdpOnly;
            }
        }

        public TcpUdpPolicy(bool unrestricted = false)
        {
            if (unrestricted)
            {
                AllowedRemoteTcpConnectPorts = "*";
                AllowedRemoteUdpConnectPorts = "*";
                AllowedLocalTcpListenerPorts = "*";
                AllowedLocalUdpListenerPorts = "*";
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public bool LocalNetworkOnly { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string AllowedRemoteTcpConnectPorts { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string AllowedRemoteUdpConnectPorts { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string AllowedLocalTcpListenerPorts { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string AllowedLocalUdpListenerPorts { get; set; }

        public override bool MergeRulesTo(ref ExceptionPolicy other)
        {
            switch (other.PolicyType)
            {
                case PolicyType.RuleList:
                    /* We could merge into 'other' but we won't, so that
                     * this policy stays separately editable in a UI.
                     * */
                    return false;
                case PolicyType.HardBlock:
                    other = HardBlockPolicy.Instance;
                    break;
                case PolicyType.TcpUdpOnly:
                    return MergeRulesTo(other as TcpUdpPolicy);
                case PolicyType.Unrestricted:
                    other = this;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return true;
        }

        private bool MergeRulesTo(TcpUdpPolicy other)
        {
            if (this.LocalNetworkOnly != other.LocalNetworkOnly)
                return false;

            other.AllowedRemoteTcpConnectPorts = MergeStringList(this.AllowedRemoteTcpConnectPorts, other.AllowedRemoteTcpConnectPorts);
            other.AllowedRemoteUdpConnectPorts = MergeStringList(this.AllowedRemoteUdpConnectPorts, other.AllowedRemoteUdpConnectPorts);
            other.AllowedLocalTcpListenerPorts = MergeStringList(this.AllowedLocalTcpListenerPorts, other.AllowedLocalTcpListenerPorts);
            other.AllowedLocalUdpListenerPorts = MergeStringList(this.AllowedLocalUdpListenerPorts, other.AllowedLocalUdpListenerPorts);

            return true;
        }

        private static string MergeStringList(string str1, string str2)
        {
            if (str1 == null)
                return str2;
            if (str2 == null)
                return str1;

            // We allow the union of the two rules.
            // If any of the two rules allowed all ports (*), we just put 
            // a wildcard into the new merged rule too.
            // Otherwise, we just join the two port lists.

            string[] list1 = str1.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string elem in list1)
            {
                if (elem.Equals("*"))
                    return "*";
            }

            string[] list2 = str2.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string elem in list2)
            {
                if (elem.Equals("*"))
                    return "*";
            }

            List<string> mergedList = new List<string>();
            mergedList.AddRange(list1);
            mergedList.AddRange(list2);
            mergedList.Sort();
            return string.Join(",", mergedList.Distinct().ToArray());
        }
    }


    // -----------------------------------------------------------------------

    [DataContract(Namespace = "TinyWall")]
    [Serializable]
    public class RuleListPolicy : ExceptionPolicy
    {
        public override PolicyType PolicyType
        {
            get
            {
                return PolicyType.RuleList;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public List<RuleDef> Rules { get; set; }

        public override bool MergeRulesTo(ref ExceptionPolicy other)
        {
            switch (other.PolicyType)
            {
                case PolicyType.RuleList:
                    RuleListPolicy polRules = other as RuleListPolicy;
                    polRules.Rules.AddRange(this.Rules);
                    break;
                case PolicyType.TcpUdpOnly:
                    return false;
                case PolicyType.HardBlock:
                    other = HardBlockPolicy.Instance;
                    break;
                case PolicyType.Unrestricted:
                    other = this;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return true;
        }
    }
}
