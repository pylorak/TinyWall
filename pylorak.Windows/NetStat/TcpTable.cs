using System.Collections;
using System.Collections.Generic;

namespace pylorak.Windows.NetStat
{
    public readonly struct TcpTable : IEnumerable<TcpRow>
    {
        private readonly IEnumerable<TcpRow> tcpRows;

        public TcpTable(IEnumerable<TcpRow> tcpRows)
        {
            this.tcpRows = tcpRows;
        }

        public IEnumerator<TcpRow> GetEnumerator()
        {
            return this.tcpRows.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.tcpRows.GetEnumerator();
        }
    }
}
