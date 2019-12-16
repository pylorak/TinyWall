using System;
using System.Runtime.Serialization;

namespace TinyWall.Interface.Internal
{
    [DataContract(Namespace = "TinyWall")]
    [Serializable]
    public struct TwMessage
    {
        [DataMember(EmitDefaultValue = false)]
        public MessageType Type { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public object[] Arguments { get; set; }

        public TwMessage(MessageType type, params object[] args)
        {
            Type = type;
            Arguments = args;
        }
    }
}
