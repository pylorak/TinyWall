using System;
using System.Collections.Generic;
using System.Collections;
using System.Windows.Forms;

namespace PKSoft
{
    // Implements the manual sorting of items by columns.
    internal class ListViewItemComparer : IComparer<ListViewItem>, IComparer
    {
        private ImageList ImageList;

        internal ListViewItemComparer() { }

        internal ListViewItemComparer(int column, ImageList imageList = null, bool ascending = true)
        {
            Column = column;
            Ascending = ascending;
            ImageList = imageList;
        }

        public int Compare(ListViewItem x, ListViewItem y)
        {
            int order = Ascending ? +1 : -1;

            if (ImageList != null)
            {
                int deletedKey = ImageList.Images.IndexOfKey("deleted");
                if (x.ImageIndex != y.ImageIndex)
                {
                    if (x.ImageIndex == deletedKey)
                        return order * 1;
                    else if (y.ImageIndex == deletedKey)
                        return order * -1;
                }
            }

            return order * String.Compare(x.SubItems[Column].Text, y.SubItems[Column].Text, StringComparison.CurrentCulture);
        }

        int IComparer.Compare(object x, object y)
        {
            var lx = x as ListViewItem;
            var ly = y as ListViewItem;
            return Compare(lx, ly);
        }

        internal int Column { get; private set; } = 0;
        internal bool Ascending { get; set; } = true;
    }
}
