using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace pylorak.Utilities
{
    public class IpAddrMask : IEquatable<IpAddrMask>
    {
        private static readonly byte[] MaskByteBitsLookup = new byte[]
        { 0x00, 0x80, 0xC0, 0xE0, 0xF0, 0xF8, 0xFC, 0xFE, 0xFF };

        private static readonly byte[] LinkLocalBytes = new byte[]
        { 169, 254, 0, 0 };

        private static readonly byte[] IPv6LinkLocalBytes = new byte[]
        { 0xFE, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        private static readonly byte[] MulticastBytes = new byte[]
        { 224, 0, 0, 0 };

        private static readonly byte[] AdminScopedMulticastBytes = new byte[]
        { 239, 192, 0, 0 };

        private static readonly byte[] IPv6MulticastBytes = new byte[]
        { 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        private static readonly byte[] IPv6LinkLocalMulticastBytes = new byte[]
        { 0xFF, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        public IPAddress Address;
        public int PrefixLen;

        private static int CountBits(int value)
        {
            int count = 0;
            while (value != 0)
            {
                count++;
                value &= value - 1;
            }
            return count;
        }

        public IpAddrMask(IPAddress addr, int prefixLen)
        {
            Address = addr;
            PrefixLen = prefixLen;
        }

        public IpAddrMask(IPAddress addr)
        {
            Address = addr;
            PrefixLen = IsIPv4 ? 32 : 128;
        }

        public IpAddrMask(GatewayIPAddressInformation addrInfo) : this (addrInfo.Address)
        {
        }

        public IpAddrMask(UnicastIPAddressInformation addrInfo)
        {
            Address = addrInfo.Address;
            PrefixLen = addrInfo.PrefixLength;
        }

        public bool IsIPv4
        {
            get
            {
                return (Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            }
        }

        public bool IsIPv6
        {
            get
            {
                return (Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
            }
        }

        public bool IsLoopback
        {
            get
            {
                return
                        (IsIPv4 && Address.Equals(IPAddress.Loopback))
                    ||  (IsIPv6 && Address.Equals(IPAddress.IPv6Loopback))
                    ;
            }
        }

        public bool IsMulticast
        {
            get
            {
                IpAddrMask multicastSubnet = IsIPv4 ? Multicast : IPv6Multicast;
                return multicastSubnet.ContainsHost(this.Address);
            }
        }

        public bool IsLinkLocal
        {
            get
            {
                IpAddrMask linkLocalSubnet = IsIPv4 ? LinkLocal : IPv6LinkLocal;
                IpAddrMask linkLocalMulticastSubnet = IsIPv4 ? LinkLocalMulticast : IPv6LinkLocalMulticast;
                return linkLocalSubnet.ContainsHost(this.Address) || linkLocalMulticastSubnet.ContainsHost(this.Address);
            }
        }

        public bool ContainsHost(IPAddress host)
        {
            if (Address.AddressFamily != host.AddressFamily)
                throw new ArgumentException("Parameter must be of the same AddressFamily as this instance.");

            IpAddrMask other = new IpAddrMask(host, this.PrefixLen);
            return this.Subnet.Equals(other.Subnet);
        }

        public IPAddress SubnetMask
        {
            get
            {
                int nBytes = IsIPv4 ? 4 : 16;
                byte[] rb = new byte[nBytes];
                int prefix = PrefixLen;
                for (int i = 0; i < rb.Length; ++i)
                {
                    int s = (prefix < 8) ? prefix : 8;
                    rb[i] = MaskByteBitsLookup[s];
                    prefix -= s;
                }
                return new IPAddress(rb);
            }
        }

        public IpAddrMask Subnet
        {
            get
            {
                byte[] addr = Address.GetAddressBytes();
                byte[] mask = SubnetMask.GetAddressBytes();
                byte[] ret = new byte[addr.Length];
                for (int i = 0; i < ret.Length; ++i)
                {
                    ret[i] = (byte)(addr[i] & mask[i]);
                }
                return new IpAddrMask(new IPAddress(ret), PrefixLen);
            }
        }

        public IpAddrMask SubnetFirstIp
        {
            get
            {
                byte[] addr = Address.GetAddressBytes();
                byte[] mask = SubnetMask.GetAddressBytes();
                byte[] ret = new byte[addr.Length];
                for (int i = 0; i < ret.Length; ++i)
                {
                    ret[i] = (byte)(addr[i] & mask[i]);
                }
                return new IpAddrMask(new IPAddress(ret));
            }
        }

        public IpAddrMask SubnetLastIp
        {
            get
            {
                byte[] addr = Address.GetAddressBytes();
                byte[] mask = SubnetMask.GetAddressBytes();
                byte[] ret = new byte[addr.Length];
                for (int i = 0; i < ret.Length; ++i)
                {
                    ret[i] = (byte)(addr[i] | ~mask[i]);
                }
                return new IpAddrMask(new IPAddress(ret));
            }
        }

        public bool Equals(IpAddrMask other)
        {
            if (other == null)
                return false;

            return (PrefixLen == other.PrefixLen) && Address.Equals(other.Address);
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            IpAddrMask other = obj as IpAddrMask;
            if (other == null)
                return false;
            else
                return Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = 2068274603;
            hashCode = hashCode * -1521134295 + EqualityComparer<IPAddress>.Default.GetHashCode(Address);
            hashCode = hashCode * -1521134295 + PrefixLen.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            if (IsIPv4)
            {
                if (PrefixLen == 32)
                    return Address.ToString();
                else
                    return $"{Address.ToString()}/{PrefixLen}";
            }
            else
            {
                string addr = Address.ToString();
                if (addr.Contains("%"))
                    return addr;
                else if (PrefixLen == 128)
                    return addr;
                else
                    return $"{addr}/{PrefixLen}";
            }
        }

        public static IpAddrMask Parse(string str)
        {
            return Parse(str.AsSpan());
        }

        public static IpAddrMask Parse(ReadOnlySpan<char> str)
        {
            int prefix;
            IPAddress addr;

            int slash = str.IndexOf('/');
            if (slash == -1)
            {
                addr = IPAddress.Parse(str.ToString()); // TODO: Parse from Span directly and don't convert to string
                prefix = addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
            }
            else
            {
                var addrSpan = str.Slice(0, slash);
                addr = IPAddress.Parse(addrSpan.ToString()); // TODO: Parse from Span directly and don't convert to string
                var prefixSpan = str.Slice(slash + 1);
                prefix = prefixSpan.DecimalToInt32(); // TODO: Use int.Parse() when available
            }

            return new IpAddrMask(addr, prefix);
        }

        public static IpAddrMask IPv6Loopback
        {
            get { return new IpAddrMask(IPAddress.IPv6Loopback, 128); }
        }

        public static IpAddrMask Loopback
        {
            get { return new IpAddrMask(IPAddress.Loopback, 8); }
        }

        public static IpAddrMask LinkLocal
        {
            get { return new IpAddrMask(new IPAddress(LinkLocalBytes), 16); }
        }

        public static IpAddrMask IPv6LinkLocal
        {
            get { return new IpAddrMask(new IPAddress(IPv6LinkLocalBytes), 64); }
        }

        public static IpAddrMask Multicast
        {
            get { return new IpAddrMask(new IPAddress(MulticastBytes), 4); }
        }

        public static IpAddrMask AdminScopedMulticast
        {
            // This returns an IP range that encompasses both local-scoped and orgnaization-scopes
            // addresses, plus a bit more...
            get { return new IpAddrMask(new IPAddress(AdminScopedMulticastBytes), 10); }
        }

        public static IpAddrMask IPv6Multicast
        {
            get { return new IpAddrMask(new IPAddress(IPv6MulticastBytes), 8); }
        }

        public static IpAddrMask LinkLocalMulticast
        {
            get { return new IpAddrMask(new IPAddress(MulticastBytes), 24); }
        }

        public static IpAddrMask IPv6LinkLocalMulticast
        {
            get { return new IpAddrMask(new IPAddress(IPv6LinkLocalMulticastBytes), 16); }
        }
    }
}
