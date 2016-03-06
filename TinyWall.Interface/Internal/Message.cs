using System;
using System.Runtime.Serialization;

namespace TinyWall.Interface.Internal
{
    [Serializable]
    public struct TwMessage
    {
        public MessageType Type { get; set; }

        public object[] Arguments { get; set; }

        public TwMessage(MessageType type, params object[] args)
        {
            Type = type;
            Arguments = args;
        }
    }
}
