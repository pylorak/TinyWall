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

    public enum RuleDirection
    {
        Invalid,
        In,
        Out,
        InOut
    }

    [Serializable]
    public class FirewallLogEntry : IEquatable<FirewallLogEntry>
    {
        public DateTime Timestamp;
        public EventLogEvent Event;
        public UInt64 ProcessID;
        public Protocol Protocol;
        public RuleDirection Direction;
        public string SourceIP;
        public string DestinationIP;
        public int SourcePort;
        public int DestinationPort;
        public string AppPath;

        public override bool Equals(object obj)
        {
            FirewallLogEntry other = obj as FirewallLogEntry;
            if (null == other)
                return false;

            return this.Equals(other, true);
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
                hash = (hash ^ ProcessID.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ Protocol.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ Direction.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ SourceIP.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ DestinationIP.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ SourcePort.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ DestinationPort.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ AppPath.GetHashCode()) * FNV_PRIME;

                return hash;
            }
        }

        public bool Equals(FirewallLogEntry obj, bool timestampMustMatch)
        {
            if (null == obj) return false;

            // Return true if the fields match.
            bool eventMatch =
                this.DestinationIP.Equals(obj.DestinationIP) &&
                (this.DestinationPort == obj.DestinationPort) &&
                (this.Event == obj.Event) &&
                (this.ProcessID == obj.ProcessID) &&
                (this.Protocol == obj.Protocol) &&
                (this.Direction == obj.Direction) &&
                this.SourceIP.Equals(obj.SourceIP) &&
                this.AppPath.Equals(obj.AppPath) &&
                (this.SourcePort == obj.SourcePort);

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
