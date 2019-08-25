using System;
using System.Collections;
using System.Windows.Forms;

namespace PKSoft
{
    // Implements the manual sorting of items by columns.
    internal class ListViewItemComparer : IComparer
    {
        internal ListViewItemComparer() { }

        internal ListViewItemComparer(int column, bool ascending = true)
        {
            Column = column;
            Ascending = ascending;
        }

        public int Compare(object x, object y)
        {
            if (Ascending)
                return String.Compare(((ListViewItem)x).SubItems[Column].Text, ((ListViewItem)y).SubItems[Column].Text, StringComparison.CurrentCulture);
            else
                return -String.Compare(((ListViewItem)x).SubItems[Column].Text, ((ListViewItem)y).SubItems[Column].Text, StringComparison.CurrentCulture);
        }

        internal int Column { get; private set; } = 0;
        internal bool Ascending { get; set; } = true;
    }
}
