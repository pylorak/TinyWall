using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Buffers.Binary;

namespace pylorak.Windows.NetStat
{
    public static class NetStat
    {
        public static ushort NetworkToHostByteOrder(ushort val)
        {
            return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(val) : val;
        }

        public static TcpTable GetExtendedTcp4Table(bool sorted)
        {
            var tcpRows = new List<TcpRow>();
            int tcpTableLength = 0;

            if (NativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref tcpTableLength, sorted, NativeMethods.AfInet, NativeMethods.TcpTableType.OwnerPidAll, 0) != 0)
            {
                using var tcpTable = new AllocHLocalSafeHandle(tcpTableLength);
                var tableMemPtr = tcpTable.DangerousGetHandle();
                if (NativeMethods.GetExtendedTcpTable(tableMemPtr, ref tcpTableLength, sorted, NativeMethods.AfInet, NativeMethods.TcpTableType.OwnerPidAll, 0) == 0)
                {
                    var table = Marshal.PtrToStructure<NativeMethods.Tcp4Table>(tableMemPtr);
                    var rowPtr = tableMemPtr + Marshal.SizeOf(table.length);
                    var rowSize = Marshal.SizeOf<NativeMethods.Tcp4Row>();
                    tcpRows.Capacity = (int)table.length;
                    for (int i = 0; i < table.length; ++i)
                    {
                        tcpRows.Add(new TcpRow(Marshal.PtrToStructure<NativeMethods.Tcp4Row>(rowPtr)));
                        rowPtr += rowSize;
                    }
                }
            }

            return new TcpTable(tcpRows);
        }

        public static TcpTable GetExtendedTcp6Table(bool sorted)
        {
            var tcpRows = new List<TcpRow>();
            int tcpTableLength = 0;

            if (NativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref tcpTableLength, sorted, NativeMethods.AfInet6, NativeMethods.TcpTableType.OwnerPidAll, 0) != 0)
            {
                using var tcpTable = new AllocHLocalSafeHandle(tcpTableLength);
                var tableMemPtr = tcpTable.DangerousGetHandle();
                if (NativeMethods.GetExtendedTcpTable(tableMemPtr, ref tcpTableLength, sorted, NativeMethods.AfInet6, NativeMethods.TcpTableType.OwnerPidAll, 0) == 0)
                {
                    var table = Marshal.PtrToStructure<NativeMethods.Tcp6Table>(tableMemPtr);
                    var rowPtr = tableMemPtr + Marshal.SizeOf(table.length);
                    var rowSize = Marshal.SizeOf<NativeMethods.Tcp6Row>();
                    tcpRows.Capacity = (int)table.length;
                    for (int i = 0; i < table.length; ++i)
                    {
                        tcpRows.Add(new TcpRow(Marshal.PtrToStructure<NativeMethods.Tcp6Row>(rowPtr)));
                        rowPtr += rowSize;
                    }
                }
            }

            return new TcpTable(tcpRows);
        }

        public static UdpTable GetExtendedUdp4Table(bool sorted)
        {
            var udpRows = new List<UdpRow>();
            int udpTableLength = 0;

            if (NativeMethods.GetExtendedUdpTable(IntPtr.Zero, ref udpTableLength, sorted, NativeMethods.AfInet, NativeMethods.UdpTableType.OwnerPid, 0) != 0)
            {
                using var udpTable = new AllocHLocalSafeHandle(udpTableLength);
                var tableMemPtr = udpTable.DangerousGetHandle();
                if (NativeMethods.GetExtendedUdpTable(tableMemPtr, ref udpTableLength, sorted, NativeMethods.AfInet, NativeMethods.UdpTableType.OwnerPid, 0) == 0)
                {
                    var table = Marshal.PtrToStructure<NativeMethods.Udp4Table>(tableMemPtr);
                    var rowPtr = tableMemPtr + Marshal.SizeOf(table.length);
                    var rowSize = Marshal.SizeOf<NativeMethods.Udp4Row>();
                    udpRows.Capacity = (int)table.length;
                    for (int i = 0; i < table.length; ++i)
                    {
                        udpRows.Add(new UdpRow(Marshal.PtrToStructure<NativeMethods.Udp4Row>(rowPtr)));
                        rowPtr += rowSize;
                    }
                }
            }

            return new UdpTable(udpRows);
        }

        public static UdpTable GetExtendedUdp6Table(bool sorted)
        {
            var udpRows = new List<UdpRow>();
            int udpTableLength = 0;

            if (NativeMethods.GetExtendedUdpTable(IntPtr.Zero, ref udpTableLength, sorted, NativeMethods.AfInet6, NativeMethods.UdpTableType.OwnerPid, 0) != 0)
            {
                using var udpTable = new AllocHLocalSafeHandle(udpTableLength);
                var tableMemPtr = udpTable.DangerousGetHandle();
                if (NativeMethods.GetExtendedUdpTable(tableMemPtr, ref udpTableLength, sorted, NativeMethods.AfInet6, NativeMethods.UdpTableType.OwnerPid, 0) == 0)
                {
                    var table = Marshal.PtrToStructure<NativeMethods.Udp6Table>(tableMemPtr);
                    var rowPtr = tableMemPtr + Marshal.SizeOf(table.length);
                    var rowSize = Marshal.SizeOf<NativeMethods.Udp6Row>();
                    udpRows.Capacity = (int)table.length;
                    for (int i = 0; i < table.length; ++i)
                    {
                        udpRows.Add(new UdpRow(Marshal.PtrToStructure<NativeMethods.Udp6Row>(rowPtr)));
                        rowPtr += rowSize;
                    }
                }
            }

            return new UdpTable(udpRows);
        }
    }
}
