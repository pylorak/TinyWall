using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace pylorak.Windows.NetStat
{
    internal static class NetStat
    {
        internal static int PortNetworkToHost(byte[] port)
        {
            return (port[0] << 8) + (port[1]) + (port[2] << 24) + (port[3] << 16);
        }

        internal static TcpTable GetExtendedTcp4Table(bool sorted)
        {
            List<TcpRow> tcpRows = new List<TcpRow>();

            int tcpTableLength = 0;

            if (SafeNativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref tcpTableLength, sorted, SafeNativeMethods.AfInet, SafeNativeMethods.TcpTableType.OwnerPidAll, 0) != 0)
            {
                using (var tcpTable = new AllocHLocalSafeHandle(tcpTableLength))
                {
                    IntPtr tableMemPtr = tcpTable.DangerousGetHandle();
                    if (SafeNativeMethods.GetExtendedTcpTable(tableMemPtr, ref tcpTableLength, true, SafeNativeMethods.AfInet, SafeNativeMethods.TcpTableType.OwnerPidAll, 0) == 0)
                    {
                        SafeNativeMethods.Tcp4Table table = Marshal.PtrToStructure<SafeNativeMethods.Tcp4Table>(tableMemPtr);

                        IntPtr rowPtr = (IntPtr)((long)tableMemPtr + Marshal.SizeOf(table.length));
                        for (int i = 0; i < table.length; ++i)
                        {
                            tcpRows.Add(new TcpRow(Marshal.PtrToStructure<SafeNativeMethods.Tcp4Row>(rowPtr)));
                            rowPtr = (IntPtr)(rowPtr.ToInt64() + Marshal.SizeOf(typeof(SafeNativeMethods.Tcp4Row)));
                        }
                    }
                }
            }

            return new TcpTable(tcpRows);
        }

        internal static TcpTable GetExtendedTcp6Table(bool sorted)
        {
            List<TcpRow> tcpRows = new List<TcpRow>();

            int tcpTableLength = 0;

            if (SafeNativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref tcpTableLength, sorted, SafeNativeMethods.AfInet6, SafeNativeMethods.TcpTableType.OwnerPidAll, 0) != 0)
            {
                using (var tcpTable = new AllocHLocalSafeHandle(tcpTableLength))
                {
                    IntPtr tableMemPtr = tcpTable.DangerousGetHandle();
                    if (SafeNativeMethods.GetExtendedTcpTable(tableMemPtr, ref tcpTableLength, true, SafeNativeMethods.AfInet6, SafeNativeMethods.TcpTableType.OwnerPidAll, 0) == 0)
                    {
                        SafeNativeMethods.Tcp6Table table = Marshal.PtrToStructure<SafeNativeMethods.Tcp6Table>(tableMemPtr);

                        IntPtr rowPtr = (IntPtr)((long)tableMemPtr + Marshal.SizeOf(table.length));
                        for (int i = 0; i < table.length; ++i)
                        {
                            tcpRows.Add(new TcpRow(Marshal.PtrToStructure<SafeNativeMethods.Tcp6Row>(rowPtr)));
                            rowPtr = (IntPtr)(rowPtr.ToInt64() + Marshal.SizeOf(typeof(SafeNativeMethods.Tcp6Row)));
                        }
                    }
                }
            }

            return new TcpTable(tcpRows);
        }

        internal static UdpTable GetExtendedUdp4Table(bool sorted)
        {
            List<UdpRow> udpRows = new List<UdpRow>();

            int udpTableLength = 0;

            if (SafeNativeMethods.GetExtendedUdpTable(IntPtr.Zero, ref udpTableLength, sorted, SafeNativeMethods.AfInet, SafeNativeMethods.UdpTableType.OwnerPid, 0) != 0)
            {
                using (var udpTable = new AllocHLocalSafeHandle(udpTableLength))
                {
                    IntPtr tableMemPtr = udpTable.DangerousGetHandle();
                    if (SafeNativeMethods.GetExtendedUdpTable(tableMemPtr, ref udpTableLength, true, SafeNativeMethods.AfInet, SafeNativeMethods.UdpTableType.OwnerPid, 0) == 0)
                    {
                        SafeNativeMethods.Udp4Table table = Marshal.PtrToStructure<SafeNativeMethods.Udp4Table>(tableMemPtr);

                        IntPtr rowPtr = (IntPtr)((long)tableMemPtr + Marshal.SizeOf(table.length));
                        for (int i = 0; i < table.length; ++i)
                        {
                            udpRows.Add(new UdpRow(Marshal.PtrToStructure<SafeNativeMethods.Udp4Row>(rowPtr)));
                            rowPtr = (IntPtr)(rowPtr.ToInt64() + Marshal.SizeOf(typeof(SafeNativeMethods.Udp4Row)));
                        }
                    }
                }
            }

            return new UdpTable(udpRows);
        }

        internal static UdpTable GetExtendedUdp6Table(bool sorted)
        {
            List<UdpRow> udpRows = new List<UdpRow>();

            int udpTableLength = 0;

            if (SafeNativeMethods.GetExtendedUdpTable(IntPtr.Zero, ref udpTableLength, sorted, SafeNativeMethods.AfInet6, SafeNativeMethods.UdpTableType.OwnerPid, 0) != 0)
            {
                using (var udpTable = new AllocHLocalSafeHandle(udpTableLength))
                {
                    IntPtr tableMemPtr = udpTable.DangerousGetHandle();
                    if (SafeNativeMethods.GetExtendedUdpTable(tableMemPtr, ref udpTableLength, true, SafeNativeMethods.AfInet6, SafeNativeMethods.UdpTableType.OwnerPid, 0) == 0)
                    {
                        SafeNativeMethods.Udp6Table table = Marshal.PtrToStructure<SafeNativeMethods.Udp6Table>(tableMemPtr);

                        IntPtr rowPtr = (IntPtr)((long)tableMemPtr + Marshal.SizeOf(table.length));
                        for (int i = 0; i < table.length; ++i)
                        {
                            udpRows.Add(new UdpRow(Marshal.PtrToStructure<SafeNativeMethods.Udp6Row>(rowPtr)));
                            rowPtr = (IntPtr)(rowPtr.ToInt64() + Marshal.SizeOf(typeof(SafeNativeMethods.Udp6Row)));
                        }
                    }
                }
            }

            return new UdpTable(udpRows);
        }

    }
}
