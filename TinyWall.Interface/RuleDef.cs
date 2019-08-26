using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace TinyWall.Interface
{
    [Serializable]
    [DataContract(Namespace = "TinyWall")]
    public class RuleDef
    {
        [XmlAttribute]
        [DataMember]
        public string Name;
        [XmlIgnore]
        [DataMember(EmitDefaultValue = false)]
        public Guid ExceptionId;
        [DataMember]
        public RuleAction Action;

        [DataMember(EmitDefaultValue = false)]
        public string Application;
        [DataMember(EmitDefaultValue = false)]
        public string ServiceName;
        [DataMember(EmitDefaultValue = false)]
        public string LocalPorts;
        [DataMember(EmitDefaultValue = false)]
        public string RemotePorts;
        [DataMember(EmitDefaultValue = false)]
        public string LocalAddresses;
        [DataMember(EmitDefaultValue = false)]
        public string RemoteAddresses;
        [DataMember(EmitDefaultValue = false)]
        public string IcmpTypesAndCodes;

        [DataMember]
        public Protocol Protocol;
        [DataMember]
        public RuleDirection Direction;

        public ulong Weight;

        public RuleDef()
        {
        }

        public RuleDef DeepCopy()
        {
            RuleDef copy = new RuleDef();
            copy.Name = this.Name;
            copy.ExceptionId = this.ExceptionId;
            copy.Action = this.Action;
            copy.Application = this.Application;
            copy.ServiceName = this.ServiceName;
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

        public RuleDef(Guid exceptionId, string name, ExceptionSubject subject, RuleAction action, RuleDirection direction, Protocol protocol, ulong weight)
        {
            if (subject != null)
            {
                ExecutableSubject exe = subject as ExecutableSubject;
                ServiceSubject service = subject as ServiceSubject;

                switch (subject.SubjectType)
                {
                    case SubjectType.Executable:
                        this.Application = exe.ExecutablePath;
                        break;
                    case SubjectType.Service:
                        this.Application = service.ExecutablePath;
                        this.ServiceName = service.ServiceName;
                        break;
                    case SubjectType.Global:
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            this.Name = name;
            this.ExceptionId = exceptionId;
            this.Action = action;
            this.Direction = direction;
            this.Protocol = protocol;
            this.Weight = weight;
        }
    }
}
