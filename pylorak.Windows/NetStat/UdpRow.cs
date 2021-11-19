using System.Net;

namespace pylorak.Windows.NetStat
{

    internal class UdpRow
    {
        private IPVersion ipVersion;
        private IPEndPoint localEndPoint;
        private uint processId;

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

        internal uint ProcessId
        {
            get { return this.processId; }
        }

        internal IPVersion IPVersion
        {
            get { return this.ipVersion; }
        }
    }
}
