using System.Net;

namespace pylorak.Windows.NetStat
{
    public class UdpRow
    {
        public IPVersion IPVersion { get; private set; }
        public IPEndPoint LocalEndPoint { get; private set; }
        public uint ProcessId { get; private set; }

        internal UdpRow(NativeMethods.Udp4Row udpRow)
        {
            IPVersion = IPVersion.IPv4;
            ProcessId = udpRow.owningPid;

            var localPort = NetStat.PortNetworkToHost(udpRow.localPort);
            var localAddress = udpRow.localAddr;
            LocalEndPoint = new IPEndPoint(localAddress, localPort);
        }
        internal UdpRow(NativeMethods.Udp6Row udpRow)
        {
            IPVersion = IPVersion.IPv6;
            ProcessId = udpRow.owningPid;

            var localPort = NetStat.PortNetworkToHost(udpRow.localPort);
            var localAddress = new IPAddress(udpRow.localAddr);
            LocalEndPoint = new IPEndPoint(localAddress, localPort);
        }
    }
}
