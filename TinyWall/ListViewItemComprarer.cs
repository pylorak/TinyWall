using System;
using System.Collections.Generic;
using System.Collections;
using System.Windows.Forms;

namespace pylorak.TinyWall
{
    // Implements the manual sorting of items by columns.
    internal class ListViewItemComparer : IComparer<ListViewItem>, IComparer
    {
        private readonly ImageList? ImageList;

        internal ListViewItemComparer(int column, ImageList? imageList = null, bool ascending = true)
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
            if ((x is ListViewItem lx) && (y is ListViewItem ly))
                return Compare(lx, ly);
            else
                throw new ArgumentException($"Both arguments must by of type {nameof(ListViewItem)}.");
        }

        internal int Column { get; private set; } = 0;
        internal bool Ascending { get; set; } = true;
    }
}
