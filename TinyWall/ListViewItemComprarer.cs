using System;
using System.Collections;
using System.Windows.Forms;

namespace PKSoft
{
    // Implements the manual sorting of items by columns.
    internal class ListViewItemComparer : IComparer
    {
        private int col;
        private bool ascending = true;

        internal ListViewItemComparer()
        {
            col = 0;
        }
        internal ListViewItemComparer(int column)
        {
            col = column;
        }
        public int Compare(object x, object y)
        {
            if (ascending)
                return String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text, StringComparison.CurrentCulture);
            else
                return -String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text, StringComparison.CurrentCulture);
        }

        internal int Column
        {
            get { return col; }
        }

        internal bool Ascending
        {
            get { return ascending; }
            set { ascending = value; }
        }
    }
}
