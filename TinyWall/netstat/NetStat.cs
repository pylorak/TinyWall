using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PKSoft.netstat
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

            IntPtr tcpTable = IntPtr.Zero;
            int tcpTableLength = 0;

            if (SafeNativeMethods.GetExtendedTcpTable(tcpTable, ref tcpTableLength, sorted, SafeNativeMethods.AfInet, SafeNativeMethods.TcpTableType.OwnerPidAll, 0) != 0)
            {
                try
                {
                    tcpTable = Marshal.AllocHGlobal(tcpTableLength);
                    if (SafeNativeMethods.GetExtendedTcpTable(tcpTable, ref tcpTableLength, true, SafeNativeMethods.AfInet, SafeNativeMethods.TcpTableType.OwnerPidAll, 0) == 0)
                    {
                        SafeNativeMethods.Tcp4Table table = (SafeNativeMethods.Tcp4Table)Marshal.PtrToStructure(tcpTable, typeof(SafeNativeMethods.Tcp4Table));

                        IntPtr rowPtr = (IntPtr)((long)tcpTable + Marshal.SizeOf(table.length));
                        for (int i = 0; i < table.length; ++i)
                        {
                            tcpRows.Add(new TcpRow((SafeNativeMethods.Tcp4Row)Marshal.PtrToStructure(rowPtr, typeof(SafeNativeMethods.Tcp4Row))));
                            rowPtr = (IntPtr)(rowPtr.ToInt64() + Marshal.SizeOf(typeof(SafeNativeMethods.Tcp4Row)));
                        }
                    }
                }
                finally
                {
                    if (tcpTable != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(tcpTable);
                    }
                }
            }

            return new TcpTable(tcpRows);
        }

        internal static TcpTable GetExtendedTcp6Table(bool sorted)
        {
            List<TcpRow> tcpRows = new List<TcpRow>();

            IntPtr tcpTable = IntPtr.Zero;
            int tcpTableLength = 0;

            if (SafeNativeMethods.GetExtendedTcpTable(tcpTable, ref tcpTableLength, sorted, SafeNativeMethods.AfInet6, SafeNativeMethods.TcpTableType.OwnerPidAll, 0) != 0)
            {
                try
                {
                    tcpTable = Marshal.AllocHGlobal(tcpTableLength);
                    if (SafeNativeMethods.GetExtendedTcpTable(tcpTable, ref tcpTableLength, true, SafeNativeMethods.AfInet6, SafeNativeMethods.TcpTableType.OwnerPidAll, 0) == 0)
                    {
                        SafeNativeMethods.Tcp6Table table = (SafeNativeMethods.Tcp6Table)Marshal.PtrToStructure(tcpTable, typeof(SafeNativeMethods.Tcp6Table));

                        IntPtr rowPtr = (IntPtr)((long)tcpTable + Marshal.SizeOf(table.length));
                        for (int i = 0; i < table.length; ++i)
                        {
                            tcpRows.Add(new TcpRow((SafeNativeMethods.Tcp6Row)Marshal.PtrToStructure(rowPtr, typeof(SafeNativeMethods.Tcp6Row))));
                            rowPtr = (IntPtr)(rowPtr.ToInt64() + Marshal.SizeOf(typeof(SafeNativeMethods.Tcp6Row)));
                        }
                    }
                }
                finally
                {
                    if (tcpTable != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(tcpTable);
                    }
                }
            }

            return new TcpTable(tcpRows);
        }

        internal static UdpTable GetExtendedUdp4Table(bool sorted)
        {
            List<UdpRow> udpRows = new List<UdpRow>();

            IntPtr udpTable = IntPtr.Zero;
            int udpTableLength = 0;

            if (SafeNativeMethods.GetExtendedUdpTable(udpTable, ref udpTableLength, sorted, SafeNativeMethods.AfInet, SafeNativeMethods.UdpTableType.OwnerPid, 0) != 0)
            {
                try
                {
                    udpTable = Marshal.AllocHGlobal(udpTableLength);
                    if (SafeNativeMethods.GetExtendedUdpTable(udpTable, ref udpTableLength, true, SafeNativeMethods.AfInet, SafeNativeMethods.UdpTableType.OwnerPid, 0) == 0)
                    {
                        SafeNativeMethods.Udp4Table table = (SafeNativeMethods.Udp4Table)Marshal.PtrToStructure(udpTable, typeof(SafeNativeMethods.Udp4Table));

                        IntPtr rowPtr = (IntPtr)((long)udpTable + Marshal.SizeOf(table.length));
                        for (int i = 0; i < table.length; ++i)
                        {
                            udpRows.Add(new UdpRow((SafeNativeMethods.Udp4Row)Marshal.PtrToStructure(rowPtr, typeof(SafeNativeMethods.Udp4Row))));
                            rowPtr = (IntPtr)(rowPtr.ToInt64() + Marshal.SizeOf(typeof(SafeNativeMethods.Udp4Row)));
                        }
                    }
                }
                finally
                {
                    if (udpTable != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(udpTable);
                    }
                }
            }

            return new UdpTable(udpRows);
        }

        internal static UdpTable GetExtendedUdp6Table(bool sorted)
        {
            List<UdpRow> udpRows = new List<UdpRow>();

            IntPtr udpTable = IntPtr.Zero;
            int udpTableLength = 0;

            if (SafeNativeMethods.GetExtendedUdpTable(udpTable, ref udpTableLength, sorted, SafeNativeMethods.AfInet6, SafeNativeMethods.UdpTableType.OwnerPid, 0) != 0)
            {
                try
                {
                    udpTable = Marshal.AllocHGlobal(udpTableLength);
                    if (SafeNativeMethods.GetExtendedUdpTable(udpTable, ref udpTableLength, true, SafeNativeMethods.AfInet6, SafeNativeMethods.UdpTableType.OwnerPid, 0) == 0)
                    {
                        SafeNativeMethods.Udp6Table table = (SafeNativeMethods.Udp6Table)Marshal.PtrToStructure(udpTable, typeof(SafeNativeMethods.Udp6Table));

                        IntPtr rowPtr = (IntPtr)((long)udpTable + Marshal.SizeOf(table.length));
                        for (int i = 0; i < table.length; ++i)
                        {
                            udpRows.Add(new UdpRow((SafeNativeMethods.Udp6Row)Marshal.PtrToStructure(rowPtr, typeof(SafeNativeMethods.Udp6Row))));
                            rowPtr = (IntPtr)(rowPtr.ToInt64() + Marshal.SizeOf(typeof(SafeNativeMethods.Udp6Row)));
                        }
                    }
                }
                finally
                {
                    if (udpTable != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(udpTable);
                    }
                }
            }

            return new UdpTable(udpRows);
        }

    }
}
