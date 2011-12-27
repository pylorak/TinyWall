using System.Net;
using System.Net.NetworkInformation;

namespace PKSoft.netstat
{
    internal class TcpRow
    {
        private IPVersion ipVersion;
        private IPEndPoint localEndPoint;
        private IPEndPoint remoteEndPoint;
        private TcpState state;
        private int processId;

        internal TcpRow(SafeNativeMethods.Tcp4Row tcpRow)
        {
            ipVersion = IPVersion.IPv4;
            this.state = tcpRow.state;
            this.processId = tcpRow.owningPid;

            int localPort = NetStat.PortNetworkToHost(tcpRow.localPort);
            long localAddress = tcpRow.localAddr;
            this.localEndPoint = new IPEndPoint(localAddress, localPort);

            int remotePort = NetStat.PortNetworkToHost(tcpRow.remotePort);
            long remoteAddress = tcpRow.remoteAddr;
            this.remoteEndPoint = new IPEndPoint(remoteAddress, remotePort);
        }
        internal TcpRow(SafeNativeMethods.Tcp6Row tcpRow)
        {
            ipVersion = IPVersion.IPv6;
            this.state = tcpRow.state;
            this.processId = tcpRow.owningPid;

            int localPort = NetStat.PortNetworkToHost(tcpRow.localPort);
            IPAddress localAddress = new IPAddress(tcpRow.localAddr);
            this.localEndPoint = new IPEndPoint(localAddress, localPort);

            int remotePort = NetStat.PortNetworkToHost(tcpRow.remotePort);
            IPAddress remoteAddress = new IPAddress(tcpRow.remoteAddr);
            this.remoteEndPoint = new IPEndPoint(remoteAddress, remotePort);
        }

        internal IPEndPoint LocalEndPoint
        {
            get { return this.localEndPoint; }
        }

        internal IPEndPoint RemoteEndPoint
        {
            get { return this.remoteEndPoint; }
        }

        internal TcpState State
        {
            get { return this.state; }
        }

        internal int ProcessId
        {
            get { return this.processId; }
        }

        internal IPVersion IPVersion
        {
            get { return this.ipVersion; }
        }
    }
}
