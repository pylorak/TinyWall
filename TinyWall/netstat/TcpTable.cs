using System.Collections;
using System.Collections.Generic;

namespace PKSoft.netstat
{
    internal class TcpTable : IEnumerable<TcpRow>
    {
        private IEnumerable<TcpRow> tcpRows;

        internal TcpTable(IEnumerable<TcpRow> tcpRows)
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
