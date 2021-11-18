using System.Collections;
using System.Collections.Generic;

namespace PKSoft.netstat
{
    internal class UdpTable : IEnumerable<UdpRow>
    {
        private IEnumerable<UdpRow> udpRows;

        internal UdpTable(IEnumerable<UdpRow> udpRows)
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
