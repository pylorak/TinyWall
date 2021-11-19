using System;
using System.Runtime.Serialization;

namespace pylorak.TinyWall
{
    [DataContract(Namespace = "TinyWall")]
    public class RuleDef
    {
        public static readonly string LOCALSUBNET_ID = "LocalSubnet";

        [DataMember]
        public string? Name;
        [DataMember(EmitDefaultValue = false)]
        public Guid ExceptionId;
        [DataMember]
        public RuleAction Action;

        [DataMember(EmitDefaultValue = false)]
        public string? AppContainerSid;
        [DataMember(EmitDefaultValue = false)]
        public string? Application;
        [DataMember(EmitDefaultValue = false)]
        public string? ServiceName;
        [DataMember(EmitDefaultValue = false)]
        public string? LocalPorts;
        [DataMember(EmitDefaultValue = false)]
        public string? RemotePorts;
        [DataMember(EmitDefaultValue = false)]
        public string? LocalAddresses;
        [DataMember(EmitDefaultValue = false)]
        public string? RemoteAddresses;
        [DataMember(EmitDefaultValue = false)]
        public string? IcmpTypesAndCodes;

        [DataMember]
        public Protocol Protocol;
        [DataMember]
        public RuleDirection Direction;

        public ulong Weight;

        public RuleDef()
        { }

        public RuleDef ShallowCopy()
        {
            var copy = new RuleDef();
            copy.Name = this.Name;
            copy.ExceptionId = this.ExceptionId;
            copy.Action = this.Action;
            copy.Application = this.Application;
            copy.ServiceName = this.ServiceName;
            copy.AppContainerSid = this.AppContainerSid;
            copy.LocalPorts = this.LocalPorts;
            copy.RemotePorts = this.RemotePorts;
            copy.LocalAddresses = this.LocalAddresses;
            copy.RemoteAddresses = this.RemoteAddresses;
            copy.IcmpTypesAndCodes = this.IcmpTypesAndCodes;
            copy.Protocol = this.Protocol;
            copy.Direction = this.Direction;
            copy.Weight = this.Weight;
            return copy;
        }

        public void SetSubject(ExceptionSubject subject)
        {
            if (subject != null)
            {
                switch (subject)
                {
                    case ServiceSubject service:
                        this.Application = service.ExecutablePath;
                        this.ServiceName = service.ServiceName;
                        this.AppContainerSid = null;
                        break;
                    case ExecutableSubject exe:
                        this.Application = exe.ExecutablePath;
                        this.ServiceName = null;
                        this.AppContainerSid = null;
                        break;
                    case AppContainerSubject uwp:
                        this.Application = null;
                        this.ServiceName = null;
                        this.AppContainerSid = uwp.Sid;
                        break;
                    case GlobalSubject _:
                        this.Application = null;
                        this.ServiceName = null;
                        this.AppContainerSid = null;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public RuleDef(Guid exceptionId, string name, ExceptionSubject subject, RuleAction action, RuleDirection direction, Protocol protocol, ulong weight)
        {
            SetSubject(subject);
            this.Name = name;
            this.ExceptionId = exceptionId;
            this.Action = action;
            this.Direction = direction;
            this.Protocol = protocol;
            this.Weight = weight;
        }
    }
}
