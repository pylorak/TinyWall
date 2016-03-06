using System;
using System.Collections.Generic;
using TinyWall.Interface.Internal;

namespace TinyWall.Interface
{
    [Serializable]
    public class UpdateModule
    {
        public string Component;
        public string ComponentVersion;
        public string DownloadHash;
        public string UpdateURL;
    }

    [Serializable]
    public class UpdateDescriptor
    {
        public string MagicWord = "TinyWall Update Descriptor";
        public UpdateModule[] Modules;
    }

    [Serializable]
    public class ServerState
    {
        public bool HasPassword = false;
        public bool Locked = false;
        public UpdateDescriptor Update = null;
        public FirewallMode Mode = FirewallMode.Unknown;
        public List<MessageType> ClientNotifs = new List<MessageType>();
    }
}
