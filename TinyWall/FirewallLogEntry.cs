using System;
using System.Collections;

namespace pylorak.TinyWall
{
    public enum FirewallLogEvent
    {
        Invalid,
        ClassifyAllow,
        ClassifyDrop
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
        public FirewallLogEvent Event;
        public uint ProcessId;
        public Protocol Protocol;
        public RuleDirection Direction;
        public byte[]? LocalIp;
        public byte[]? RemoteIp;
        public int LocalPort;
        public int RemotePort;
        public string? AppPath;
        public string? PackageId;
        public FilterGroup FilterGroup;

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
                    hash = (hash ^ Utils.GetArrayHashCode(LocalIp)) * FNV_PRIME;
                if (RemoteIp is not null)
                    hash = (hash ^ Utils.GetArrayHashCode(RemoteIp)) * FNV_PRIME;
                hash = (hash ^ LocalPort.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ RemotePort.GetHashCode()) * FNV_PRIME;
                if (AppPath is not null)
                    hash = (hash ^ AppPath.GetHashCode()) * FNV_PRIME;
                if (PackageId is not null)
                    hash = (hash ^ PackageId.GetHashCode()) * FNV_PRIME;
                hash = (hash ^ FilterGroup.GetHashCode()) * FNV_PRIME;

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
                StructuralComparisons.StructuralEqualityComparer.Equals(LocalIp, obj.LocalIp) &&
                StructuralComparisons.StructuralEqualityComparer.Equals(RemoteIp, obj.RemoteIp) &&
                (LocalPort == obj.LocalPort) &&
                (RemotePort == obj.RemotePort) &&
                string.Equals(AppPath, obj.AppPath) &&
                string.Equals(PackageId, obj.PackageId) &&
                FilterGroup == obj.FilterGroup;
        }

        public bool Equals(FirewallLogEntry? other)
        {
            return Equals(other, true);
        }
    }
}
