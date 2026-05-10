using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;

namespace pylorak.Windows
{
    /// <summary>
    /// Direct P/Invoke wrapper for GetAdaptersAddresses.
    /// Replaces NetworkInterface.GetAllNetworkInterfaces() which leaks ~15KB of
    /// native memory per call through iphlpapi!GetPerAdapterInfo -> DNSAPI!Dns_AllocZero.
    /// See: Unity issue UUM-52888, dotnet/runtime#50323.
    /// </summary>
    public static class NetworkAdapterEnumerator
    {
        [SuppressUnmanagedCodeSecurity]
        private static class SafeNativeMethods
        {
            [DllImport("iphlpapi.dll")]
            internal static extern uint GetAdaptersAddresses(
            uint Family, uint Flags, IntPtr Reserved,
            IntPtr AdapterAddresses, ref uint SizePointer);
        }

        private const uint AF_UNSPEC = 0;
        private const uint GAA_FLAG_INCLUDE_GATEWAYS = 0x0080;
        private const uint ERROR_BUFFER_OVERFLOW = 111;
        private const uint ERROR_NO_DATA = 232;
        private const uint ERROR_ADDRESS_NOT_ASSOCIATED = 1228;
        private const uint ERROR_SUCCESS = 0;
        private const int IF_OPER_STATUS_UP = 1;
        private const short FAMILY_INET = 2;
        private const short FAMILY_INET6 = 23;

        #region Native structs - layout computed by CLR, no manual offsets

        [StructLayout(LayoutKind.Sequential)]
        private struct SOCKET_ADDRESS
        {
            public IntPtr lpSockaddr;
            public int iSockaddrLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IP_ADAPTER_ADDRESSES
        {
            public ulong Alignment;
            public IntPtr Next;
            public IntPtr AdapterName;
            public IntPtr FirstUnicastAddress;
            public IntPtr FirstAnycastAddress;
            public IntPtr FirstMulticastAddress;
            public IntPtr FirstDnsServerAddress;
            public IntPtr DnsSuffix;
            public IntPtr Description;
            public IntPtr FriendlyName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] PhysicalAddress;
            public uint PhysicalAddressLength;
            public uint Flags;
            public uint Mtu;
            public uint IfType;
            public int OperStatus;
            public uint Ipv6IfIndex;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public uint[] ZoneIndices;
            public IntPtr FirstPrefix;
            public ulong TransmitLinkSpeed;
            public ulong ReceiveLinkSpeed;
            public IntPtr FirstWinsServerAddress;
            public IntPtr FirstGatewayAddress;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IP_ADAPTER_UNICAST_ADDRESS
        {
            public ulong Alignment;
            public IntPtr Next;
            public SOCKET_ADDRESS Address;
            public int PrefixOrigin;
            public int SuffixOrigin;
            public int DadState;
            public uint ValidLifetime;
            public uint PreferredLifetime;
            public uint LeaseLifetime;
            public byte OnLinkPrefixLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IP_ADAPTER_LINKED_ADDRESS
        {
            public ulong Alignment;
            public IntPtr Next;
            public SOCKET_ADDRESS Address;
        }

        #endregion

        public struct UnicastEntry
        {
            public IPAddress Address;
            public int PrefixLength;
        }

        public static bool EnumerateActiveAdapters(
            out HashSet<UnicastEntry> unicastAddresses,
            out HashSet<IPAddress> gatewayAddresses,
            out HashSet<IPAddress> dnsAddresses)
        {
            unicastAddresses = new HashSet<UnicastEntry>();
            gatewayAddresses = new HashSet<IPAddress>();
            dnsAddresses = new HashSet<IPAddress>();

            uint size = 15*1024;    // starting size of 15 KiB recommended by Microsoft to reduce chances of retries
            const int MAX_ALLOCATION_RETRIES = 3;
            using var buffer = SafeHGlobalHandle.Alloc(size);
            for (int i = 0; i < MAX_ALLOCATION_RETRIES; ++i)
            {
                uint result = SafeNativeMethods.GetAdaptersAddresses(AF_UNSPEC, GAA_FLAG_INCLUDE_GATEWAYS, IntPtr.Zero, buffer.DangerousGetHandle(), ref size);
                switch (result)
                {
                    case ERROR_SUCCESS:
                        {
                            IntPtr adapterPtr = buffer.DangerousGetHandle();
                            while (adapterPtr != IntPtr.Zero)
                            {
                                var adapter = Marshal.PtrToStructure<IP_ADAPTER_ADDRESSES>(adapterPtr);

                                if (adapter.OperStatus == IF_OPER_STATUS_UP)
                                {
                                    ReadUnicastAddresses(adapter.FirstUnicastAddress, unicastAddresses);
                                    ReadLinkedAddresses(adapter.FirstGatewayAddress, gatewayAddresses);
                                    ReadLinkedAddresses(adapter.FirstDnsServerAddress, dnsAddresses);
                                }

                                adapterPtr = adapter.Next;
                            }

                            return true;
                        }
                    case ERROR_BUFFER_OVERFLOW:
                        // Need larger buffer
                        buffer.ForgetAndResize(size);
                        break;
                    case ERROR_NO_DATA:
                    case ERROR_ADDRESS_NOT_ASSOCIATED:
                        // No IP addresses associated
                        return true;
                    default:
                        // Error condition
                        return false;
                }
            }

            // Ran out of MAX_ALLOCATION_RETRIES
            return false;
        }

        private static void ReadUnicastAddresses(IntPtr ptr, HashSet<UnicastEntry> result)
        {
            while (ptr != IntPtr.Zero)
            {
                var uni = Marshal.PtrToStructure<IP_ADAPTER_UNICAST_ADDRESS>(ptr);
                var ip = ReadIPAddress(uni.Address.lpSockaddr);
                if (ip != null)
                {
                    result.Add(new UnicastEntry
                    {
                        Address = ip,
                        PrefixLength = uni.OnLinkPrefixLength
                    });
                }
                ptr = uni.Next;
            }
        }

        private static void ReadLinkedAddresses(IntPtr ptr, HashSet<IPAddress> result)
        {
            while (ptr != IntPtr.Zero)
            {
                var entry = Marshal.PtrToStructure<IP_ADAPTER_LINKED_ADDRESS>(ptr);
                var ip = ReadIPAddress(entry.Address.lpSockaddr);
                if (ip != null)
                    result.Add(ip);
                ptr = entry.Next;
            }
        }

        private static IPAddress ReadIPAddress(IntPtr sockAddrPtr)
        {
            if (sockAddrPtr == IntPtr.Zero)
                return null;

            short family = Marshal.ReadInt16(sockAddrPtr, 0);

            if (family == FAMILY_INET)
            {
                // sockaddr_in: family(2) + port(2) + addr(4)
                var addr = new byte[4];
                Marshal.Copy(IntPtr.Add(sockAddrPtr, 4), addr, 0, 4);
                return new IPAddress(addr);
            }

            if (family == FAMILY_INET6)
            {
                // sockaddr_in6: family(2) + port(2) + flowinfo(4) + addr(16) + scope_id(4)
                var addr = new byte[16];
                Marshal.Copy(IntPtr.Add(sockAddrPtr, 8), addr, 0, 16);
                long scopeId = (uint)Marshal.ReadInt32(sockAddrPtr, 24);
                return new IPAddress(addr, scopeId);
            }

            return null;
        }
    }
}
