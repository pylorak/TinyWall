using System.Collections;
using System.Collections.Generic;

namespace pylorak.Windows.NetStat
{
    public readonly struct UdpTable : IEnumerable<UdpRow>
    {
        private readonly IEnumerable<UdpRow> udpRows;

        public UdpTable(IEnumerable<UdpRow> udpRows)
        {
            this.udpRows = udpRows;
        }

        public IEnumerator<UdpRow> GetEnumerator()
        {
            return this.udpRows.GetEnumerator();
        }

         IEnumerator IEnumerable.GetEnumerator()
        {
            return this.udpRows.GetEnumerator();
        }
    }
}
