using System;

namespace pylorak.TinyWall
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

    public sealed record FirewallLogEntry : IEquatable<FirewallLogEntry>
    {
        public DateTime Timestamp;
        public EventLogEvent Event;
        public uint ProcessId;
        public Protocol Protocol;
        public RuleDirection Direction;
        public string? LocalIp;
        public string? RemoteIp;
        public int LocalPort;
        public int RemotePort;
        public string? AppPath;
        public string? PackageId;

        public int GetHashCode(bool includeTimestamp)
        {
            unchecked
            {
                const int OFFSET_BASIS = unchecked((int)2166136261u);
                const int FNV_PRIME = 16777619;

                int hash = OFFSET_BASIS;
                if (includeTimestamp)
                    hash = (hash ^ Timestamp.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ Event.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ ProcessId.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ Protocol.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ Direction.GetHashCode()) * FNV_PRIME;
                if (LocalIp is not null)
                    hash = (hash ^ LocalIp.GetHashCode()) * FNV_PRIME;
                if (RemoteIp is not null)
                    hash = (hash ^ RemoteIp.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ LocalPort.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ RemotePort.GetHashCode()) * FNV_PRIME;
                if (AppPath is not null)
                    hash = (hash ^ AppPath.GetHashCode()) * FNV_PRIME;
                if (PackageId is not null)
                    hash = (hash ^ PackageId.GetHashCode()) * FNV_PRIME;

                return hash;
            }
        }

        public override int GetHashCode()
        {
            return GetHashCode(true);
        }

        public bool Equals(FirewallLogEntry? obj, bool includeTimestamp)
        {
            if (obj is null) return false;

            // Return true if the fields match.
            return
                (!includeTimestamp || (Timestamp == obj.Timestamp)) &&
                (Event == obj.Event) &&
                (ProcessId == obj.ProcessId) &&
                (Protocol == obj.Protocol) &&
                (Direction == obj.Direction) &&
                string.Equals(LocalIp, obj.LocalIp) &&
                string.Equals(RemoteIp, obj.RemoteIp) &&
                (LocalPort == obj.LocalPort) &&
                (RemotePort == obj.RemotePort) &&
                string.Equals(AppPath, obj.AppPath) &&
                string.Equals(PackageId, obj.PackageId);
        }

        public bool Equals(FirewallLogEntry? other)
        {
            return Equals(other, true);
        }
    }
}
