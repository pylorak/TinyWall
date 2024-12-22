using System.Net;
using System.Net.NetworkInformation;

namespace pylorak.Windows.NetStat
{
    public class TcpRow
    {
        public IPVersion IPVersion { get; private set; }
        public IPEndPoint LocalEndPoint { get; private set; }
        public IPEndPoint RemoteEndPoint { get; private set; }
        public TcpState State { get; private set; }
        public uint ProcessId { get; private set; }

        internal TcpRow(NativeMethods.Tcp4Row tcpRow)
        {
            IPVersion = IPVersion.IPv4;
            State = tcpRow.state;
            ProcessId = tcpRow.owningPid;

            int localPort = NetStat.NetworkToHostByteOrder((ushort)tcpRow.localPort);
            long localAddress = tcpRow.localAddr;
            LocalEndPoint = new IPEndPoint(localAddress, localPort);

            int remotePort = NetStat.NetworkToHostByteOrder((ushort)tcpRow.remotePort);
            long remoteAddress = tcpRow.remoteAddr;
            RemoteEndPoint = new IPEndPoint(remoteAddress, remotePort);
        }
        internal TcpRow(NativeMethods.Tcp6Row tcpRow)
        {
            IPVersion = IPVersion.IPv6;
            State = tcpRow.state;
            ProcessId = tcpRow.owningPid;

            var localPort = NetStat.NetworkToHostByteOrder((ushort)tcpRow.localPort);
            var localAddress = new IPAddress(tcpRow.localAddr);
            LocalEndPoint = new IPEndPoint(localAddress, localPort);

            var remotePort = NetStat.NetworkToHostByteOrder((ushort)tcpRow.remotePort);
            var remoteAddress = new IPAddress(tcpRow.remoteAddr);
            RemoteEndPoint = new IPEndPoint(remoteAddress, remotePort);
        }
    }
}
