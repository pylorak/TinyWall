using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Windows.Forms;
using pylorak.Utilities;
using System.Collections.ObjectModel;

namespace pylorak.TinyWall
{
    internal class AsyncIconScanner : Disposable
    {
        public delegate string? IconPathDelegate(ListViewItem li);

        private readonly BackgroundTask ScannerTask = new();
        private readonly ConcurrentDictionary<string, int> LoadedIcons = new();
        public readonly IconPathDelegate PathExtractor;
        public ReadOnlyCollection<int> TemporaryIconIdx { get; }

        /// <summary>Constructor.</summary>
        /// <param name="pathExtractor">A delegate that returns the path to an icon file from an instance of ListViewItem.</param>
        /// <param name="tempIconIdx">An icon will only be loaded for ListViewItem instances that have this value for their ImageIndex property.</param>
        internal AsyncIconScanner(IconPathDelegate pathExtractor, params int[] tempIconIdx)
        {
            PathExtractor = pathExtractor;
            TemporaryIconIdx = new(tempIconIdx);
        }

        internal void Rescan(List<ListViewItem> listItems, ListView listView, ImageList imageList)
        {
            ScannerTask.Restart(() =>
            {
                var st = Stopwatch.StartNew();
                var iconSize = imageList.ImageSize;
                foreach (var li in listItems)
                {
                    ScannerTask.CancellationToken.ThrowIfCancellationRequested();

                    var icon_path = PathExtractor(li);
                    if (!Utils.IsNullOrEmpty(icon_path) && TemporaryIconIdx.Contains(li.ImageIndex))
                    {
                        var is_icon_new = !LoadedIcons.TryGetValue(icon_path, out int icon_idx);
                        var icon = is_icon_new ? Utils.GetIconContained(icon_path, iconSize.Width, iconSize.Height) : null;

                        if (!is_icon_new || (icon is not null))
                        {
                            listView.BeginInvoke((MethodInvoker)delegate
                            {
                                if (listView.IsDisposed)
                                {
                                    icon?.Dispose();
                                    return;
                                }

                                try
                                {
                                    if (is_icon_new)
                                    {
                                        if (!imageList.Images.ContainsKey(icon_path))
                                        {
                                            imageList.Images.Add(icon_path, icon);
                                            _ = imageList.Handle;  // Ensure native HIMAGELIST has independent copy
                                        }
                                        icon?.Dispose();  // Safe: managed HBITMAP freed, imageList already has copy
                                        icon_idx = imageList.Images.IndexOfKey(icon_path);
                                        LoadedIcons.TryAdd(icon_path, icon_idx);
                                    }
                                    li.ImageIndex = icon_idx;
                                }
                                // Ignore any errors during icon loading, it is purely aesthetical
                                catch { }

                                // Live-update listview, but throttle to conserve CPU since this is pretty expensive
                                if (st.ElapsedMilliseconds >= 200)
                                {
                                    st.Restart();
                                    listView.Refresh();
                                }
                            });
                        }
                    }
                }
                listView.BeginInvoke((MethodInvoker)delegate { if (!listView.IsDisposed) listView.Refresh(); });
            });
        }

        internal void CancelScan()
        {
            ScannerTask.CancelTask();
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
                ScannerTask.Dispose();

            base.Dispose(disposing);
        }
    }
}
