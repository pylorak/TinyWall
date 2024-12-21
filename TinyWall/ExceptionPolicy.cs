using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace pylorak.TinyWall
{
    public class ExceptionPolicyConverter : PolymorphicJsonConverter<ExceptionPolicy>
    {
        public override string DiscriminatorPropertyName => "PolicyType";

        public override ExceptionPolicy? DeserializeDerived(ref Utf8JsonReader reader, int discriminator)
        {
            var ret = (PolicyType)discriminator switch
            {
                PolicyType.HardBlock => (ExceptionPolicy?)JsonSerializer.Deserialize<HardBlockPolicy>(ref reader, SourceGenerationContext.Default.HardBlockPolicy),
                PolicyType.Unrestricted => (ExceptionPolicy?)JsonSerializer.Deserialize<UnrestrictedPolicy>(ref reader, SourceGenerationContext.Default.UnrestrictedPolicy),
                PolicyType.TcpUdpOnly => (ExceptionPolicy?)JsonSerializer.Deserialize<TcpUdpPolicy>(ref reader, SourceGenerationContext.Default.TcpUdpPolicy),
                PolicyType.RuleList => (ExceptionPolicy?)JsonSerializer.Deserialize<RuleListPolicy>(ref reader, SourceGenerationContext.Default.RuleListPolicy),
                _ => throw new JsonException($"Tried to deserialize unsupported type with discriminator {(PolicyType)discriminator}."),
            };
            return ret;
        }

        public override void SerializeDerived(Utf8JsonWriter writer, ExceptionPolicy value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case HardBlockPolicy typedVal:
                    JsonSerializer.Serialize<HardBlockPolicy>(writer, typedVal, SourceGenerationContext.Default.HardBlockPolicy); break;
                case UnrestrictedPolicy typedVal:
                    JsonSerializer.Serialize<UnrestrictedPolicy>(writer, typedVal, SourceGenerationContext.Default.UnrestrictedPolicy); break;
                case TcpUdpPolicy typedVal:
                    JsonSerializer.Serialize<TcpUdpPolicy>(writer, typedVal, SourceGenerationContext.Default.TcpUdpPolicy); break;
                case RuleListPolicy typedVal:
                    JsonSerializer.Serialize<RuleListPolicy>(writer, typedVal, SourceGenerationContext.Default.RuleListPolicy); break;
                default:
                    throw new JsonException($"Tried to serialize unsupported type {value.GetType()}.");
            }
        }
    }

    // TODO: Get rid of PolicyType and use type patterns instead
    public enum PolicyType
    {
        Invalid,
        HardBlock,
        Unrestricted,
        TcpUdpOnly,
        RuleList
    }


    // -----------------------------------------------------------------------

    [JsonConverter(typeof(ExceptionPolicyConverter))]
    [DataContract(Namespace = "TinyWall")]
    public abstract class ExceptionPolicy
    {
        [JsonPropertyOrder(-1)]
        public abstract PolicyType PolicyType { get; }

        public abstract bool MergeRulesTo(ref ExceptionPolicy other);
    }

    // -----------------------------------------------------------------------

    [DataContract(Namespace = "TinyWall")]
    public class HardBlockPolicy : ExceptionPolicy
    {
        public static HardBlockPolicy Instance { get; } = new HardBlockPolicy();

        public override PolicyType PolicyType => PolicyType.HardBlock;

        public override bool MergeRulesTo(ref ExceptionPolicy other)
        {
            other = other.PolicyType switch
            {
                PolicyType.RuleList or 
                    PolicyType.HardBlock or
                    PolicyType.TcpUdpOnly or 
                    PolicyType.Unrestricted => HardBlockPolicy.Instance,
                PolicyType.Invalid => throw new InvalidOperationException(),
                _ => throw new NotImplementedException(),
            };
            return true;
        }
    }

    // -----------------------------------------------------------------------

    [DataContract(Namespace = "TinyWall")]
    public class UnrestrictedPolicy : ExceptionPolicy
    {
        public override PolicyType PolicyType => PolicyType.Unrestricted;

        [DataMember(EmitDefaultValue = false)]
        public bool LocalNetworkOnly { get; set; }

        public override bool MergeRulesTo(ref ExceptionPolicy target)
        {
            switch (target.PolicyType)
            {
                case PolicyType.Unrestricted:
                {
                    var other = (UnrestrictedPolicy)target;
                    other.LocalNetworkOnly &= this.LocalNetworkOnly;
                    break;
                }
                case PolicyType.HardBlock:
                    // No change to target
                    break;
                case PolicyType.RuleList:
                    this.LocalNetworkOnly = false;
                    target = this;
                    break;
                case PolicyType.TcpUdpOnly:
                {
                    var other = (TcpUdpPolicy)target;
                    this.LocalNetworkOnly &= other.LocalNetworkOnly;
                    target = this;
                    break;
                }
                default:
                    throw new NotImplementedException();
            }

            return true;
        }
    }

    // -----------------------------------------------------------------------

    [DataContract(Namespace = "TinyWall")]
    public class TcpUdpPolicy : ExceptionPolicy
    {
        public override PolicyType PolicyType => PolicyType.TcpUdpOnly;

        public TcpUdpPolicy() :
            this(unrestricted: false)
        { }

        public TcpUdpPolicy(bool unrestricted)
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
        public string? AllowedRemoteTcpConnectPorts { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string? AllowedRemoteUdpConnectPorts { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string? AllowedLocalTcpListenerPorts { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string? AllowedLocalUdpListenerPorts { get; set; }

        public override bool MergeRulesTo(ref ExceptionPolicy target)
        {
            switch (target.PolicyType)
            {
                case PolicyType.RuleList:
                    /* We could merge into 'other' but we won't, so that
                     * this policy stays separately editable in a UI.
                     * */
                    return false;
                case PolicyType.HardBlock:
                    // No change to target
                    break;
                case PolicyType.TcpUdpOnly:
                    return MergeRulesTo((TcpUdpPolicy)target);
                case PolicyType.Unrestricted:
                    var other = (UnrestrictedPolicy)target;
                    other.LocalNetworkOnly &= this.LocalNetworkOnly;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return true;
        }

        private bool MergeRulesTo(TcpUdpPolicy other)
        {
            other.LocalNetworkOnly &= this.LocalNetworkOnly;
            other.AllowedRemoteTcpConnectPorts = MergeStringList(this.AllowedRemoteTcpConnectPorts, other.AllowedRemoteTcpConnectPorts);
            other.AllowedRemoteUdpConnectPorts = MergeStringList(this.AllowedRemoteUdpConnectPorts, other.AllowedRemoteUdpConnectPorts);
            other.AllowedLocalTcpListenerPorts = MergeStringList(this.AllowedLocalTcpListenerPorts, other.AllowedLocalTcpListenerPorts);
            other.AllowedLocalUdpListenerPorts = MergeStringList(this.AllowedLocalUdpListenerPorts, other.AllowedLocalUdpListenerPorts);

            return true;
        }

        private static readonly char[] LIST_SEPARATORS = new[]{ ',' };
        private static string? MergeStringList(string? str1, string? str2)
        {
            if (str1 == null)
                return str2;
            if (str2 == null)
                return str1;

            // We allow the union of the two rules.
            // If any of the two rules allowed all ports (*), we just put 
            // a wildcard into the new merged rule too.
            // Otherwise, we just join the two port lists.

            string[] list1 = str1.Split(LIST_SEPARATORS, StringSplitOptions.RemoveEmptyEntries);
            foreach (string elem in list1)
            {
                if (elem.Equals("*"))
                    return "*";
            }

            string[] list2 = str2.Split(LIST_SEPARATORS, StringSplitOptions.RemoveEmptyEntries);
            foreach (string elem in list2)
            {
                if (elem.Equals("*"))
                    return "*";
            }

            var mergedList = new List<string>();
            mergedList.AddRange(list1);
            mergedList.AddRange(list2);
            mergedList.Sort();
            return string.Join(",", mergedList.Distinct().ToArray());
        }
    }


    // -----------------------------------------------------------------------

    [DataContract(Namespace = "TinyWall")]
    public class RuleListPolicy : ExceptionPolicy
    {
        public override PolicyType PolicyType => PolicyType.RuleList;

        [DataMember(EmitDefaultValue = false)]
        public List<RuleDef> Rules { get; set; } = new List<RuleDef>();

        public override bool MergeRulesTo(ref ExceptionPolicy target)
        {
            switch (target.PolicyType)
            {
                case PolicyType.RuleList:
                {
                    var other = (RuleListPolicy)target;
                    other.Rules.AddRange(this.Rules);
                    break;
                }
                case PolicyType.TcpUdpOnly:
                    return false;
                case PolicyType.HardBlock:
                    // No change to target
                    break;
                case PolicyType.Unrestricted:
                {
                    var other = (UnrestrictedPolicy)target;
                    other.LocalNetworkOnly = false;
                    break;
                }
                default:
                    throw new NotImplementedException();
            }

            return true;
        }
    }
}
