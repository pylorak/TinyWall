using System;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace PKSoft.netstat
{
    internal static class SafeNativeMethods
    {
        internal const int AfInet = 2;
        internal const int AfInet6 = 23;

        private const string DllName = "iphlpapi.dll";

        [DllImport(DllName, SetLastError = true)]
        internal static extern uint GetExtendedTcpTable(IntPtr tcpTable, ref int tcpTableLength, [MarshalAs(UnmanagedType.Bool)] bool sort, int ipVersion, TcpTableType tcpTableType, int reserved);

        [DllImport(DllName, SetLastError = true)]
        internal static extern uint GetExtendedUdpTable(IntPtr udpTable, ref int udpTableLength, [MarshalAs(UnmanagedType.Bool)] bool sort, int ipVersion, UdpTableType udpTableType, int reserved);

        internal enum TcpTableType
        {
            BasicListener,
            BasicConnections,
            BasicAll,
            OwnerPidListener,
            OwnerPidConnections,
            OwnerPidAll,
            OwnerModuleListener,
            OwnerModuleConnections,
            OwnerModuleAll,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Tcp4Table
        {
            internal uint length;
            internal Tcp4Row row;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Tcp4Row
        {
            internal TcpState state;
            internal uint localAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            internal byte[] localPort;
            internal uint remoteAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            internal byte[] remotePort;
            internal uint owningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Tcp6Table
        {
            internal uint length;
            internal Tcp6Row row;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct Tcp6Row
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            internal byte[] localAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            internal byte[] localScopeId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            internal byte[] localPort;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            internal byte[] remoteAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            internal byte[] remoteScopeId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            internal byte[] remotePort;

            internal TcpState state;
            internal uint owningPid;
        }

        internal enum UdpTableType
        {
            Basic,
            OwnerPid,
            OwnerModule,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Udp4Table
        {
            internal uint length;
            internal Udp4Row row;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Udp4Row
        {
            internal uint localAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            internal byte[] localPort;
            internal uint owningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Udp6Table
        {
            internal uint length;
            internal Udp6Row row;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Udp6Row
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            internal byte[] localAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            internal byte[] localScopeId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            internal byte[] localPort;

            internal uint owningPid;
        }
    }
}
