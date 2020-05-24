namespace TinyWall.Interface
{
    // Actually valid protocol numbers and interpretations can be found on:
    // http://www.iana.org/assignments/protocol-numbers/protocol-numbers.xml
    public enum Protocol
    {
        HOPOPT = 0,
        ICMPv4 = 1,
        ICMPv6 = 58,
        IGMP = 2,
        TCP = 6,
        UDP = 17,
        GRE = 47,
        ESP = 50,
        AH = 51,

        // Virtual protocols (>1023)
        Any = 1024,
        TcpUdp
    }
}
