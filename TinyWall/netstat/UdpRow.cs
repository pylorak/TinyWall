using System.Net;

namespace PKSoft.netstat
{

    internal class UdpRow
    {
        private IPVersion ipVersion;
        private IPEndPoint localEndPoint;
        private int processId;

        internal UdpRow(SafeNativeMethods.Udp4Row udpRow)
        {
            ipVersion = IPVersion.IPv4;
            this.processId = udpRow.owningPid;

            int localPort = NetStat.PortNetworkToHost(udpRow.localPort);
            long localAddress = udpRow.localAddr;
            this.localEndPoint = new IPEndPoint(localAddress, localPort);
        }
        internal UdpRow(SafeNativeMethods.Udp6Row udpRow)
        {
            ipVersion = IPVersion.IPv6;
            this.processId = udpRow.owningPid;

            int localPort = NetStat.PortNetworkToHost(udpRow.localPort);
            IPAddress localAddress = new IPAddress(udpRow.localAddr);
            this.localEndPoint = new IPEndPoint(localAddress, localPort);
        }

        internal IPEndPoint LocalEndPoint
        {
            get { return this.localEndPoint; }
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
