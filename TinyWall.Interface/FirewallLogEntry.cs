using System;


namespace TinyWall.Interface
{
    public enum EventLogEvent
    {
        BLOCKED,
        ALLOWED,
        ALLOWED_LISTEN = 5154,
        ALLOWED_CONNECTION = 5156,
        ALLOWED_LOCAL_BIND = 5158,
        BLOCKED_LISTEN = 5155,
        BLOCKED_CONNECTION = 5157,
        BLOCKED_PACKET = 5152,
        BLOCKED_LOCAL_BIND = 5159
    }

    [Flags]
    public enum RuleDirection
    {
        Invalid = 0,
        In = 1,
        Out = 2,
        InOut = In | Out
    }

    [Serializable]
    public class FirewallLogEntry : IEquatable<FirewallLogEntry>
    {
        public DateTime Timestamp;
        public EventLogEvent Event;
        public uint ProcessId;
        public Protocol Protocol;
        public RuleDirection Direction;
        public string LocalIp;
        public string RemoteIp;
        public int LocalPort;
        public int RemotePort;
        public string AppPath;
        public string PackageId;

        public override bool Equals(object obj)
        {
            if (obj is FirewallLogEntry other)
                return Equals(other, true);
            else
                return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                const int OFFSET_BASIS = unchecked((int)2166136261u);
                const int FNV_PRIME = 16777619;

                int hash = OFFSET_BASIS;
                hash = (hash ^ Timestamp.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ Event.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ ProcessId.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ Protocol.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ Direction.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ LocalIp.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ RemoteIp.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ LocalPort.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ RemotePort.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ AppPath.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ PackageId.GetHashCode()) * FNV_PRIME;

                return hash;
            }
        }

        public bool Equals(FirewallLogEntry obj, bool timestampMustMatch)
        {
            if (null == obj) return false;

            // Return true if the fields match.
            bool eventMatch =
                this.RemoteIp.Equals(obj.RemoteIp) &&
                (this.RemotePort == obj.RemotePort) &&
                (this.Event == obj.Event) &&
                (this.ProcessId == obj.ProcessId) &&
                (this.Protocol == obj.Protocol) &&
                (this.Direction == obj.Direction) &&
                this.LocalIp.Equals(obj.LocalIp) &&
                this.AppPath.Equals(obj.AppPath) &&
                this.AppPath.Equals(obj.PackageId) &&
                (this.LocalPort == obj.LocalPort);

            if (timestampMustMatch)
                return this.Timestamp.Equals(obj.Timestamp) && eventMatch;
            else
                return eventMatch;
        }

        public bool Equals(FirewallLogEntry other)
        {
            return this.Equals(other, true);
        }
    }
}
