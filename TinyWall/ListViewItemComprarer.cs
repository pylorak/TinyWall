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
            int order = Ascending ? +1 : -1;

            ListViewItem lx = x as ListViewItem;
            ListViewItem ly = y as ListViewItem;

            if (lx.ImageKey != ly.ImageKey)
            {
                if (lx.ImageKey == "deleted")
                    return order * 1;
                else if (ly.ImageKey == "deleted")
                    return order * -1;
            }

            return order * String.Compare(lx.SubItems[Column].Text, ly.SubItems[Column].Text, StringComparison.CurrentCulture);
        }

        internal int Column { get; private set; } = 0;
        internal bool Ascending { get; set; } = true;
    }
}
