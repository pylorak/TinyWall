using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using TinyWall.Interface.Internal;

namespace TinyWall.Interface
{
    [DataContract(Namespace = "TinyWall")]
    public class UpdateModule
    {
        [DataMember]
        public string Component;
        [DataMember]
        public string ComponentVersion;
        [DataMember]
        public string DownloadHash;
        [DataMember]
        public string UpdateURL;
    }

    [DataContract(Namespace = "TinyWall")]
    public class UpdateDescriptor
    {
        [DataMember]
        public string MagicWord = "TinyWall Update Descriptor";
        [DataMember]
        public UpdateModule[] Modules;
    }

    public class ServerState
    {
        public bool HasPassword = false;
        public bool Locked = false;
        public UpdateDescriptor Update = null;
        public FirewallMode Mode = FirewallMode.Unknown;
        public List<MessageType> ClientNotifs = new List<MessageType>();
    }
}
