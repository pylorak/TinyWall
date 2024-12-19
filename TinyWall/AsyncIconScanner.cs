using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Windows.Forms;
using pylorak.Utilities;
using Windows.ApplicationModel.Store.Preview.InstallControl;

namespace pylorak.TinyWall
{
    internal class AsyncIconScanner : Disposable
    {
        public delegate string IconPathDelegate(ListViewItem li);

        private readonly BackgroundTask ScannerTask = new();
        private readonly ConcurrentDictionary<string, int> LoadedIcons = new();
        private readonly IconPathDelegate PathExtractor;
        public int TemporaryIconIdx { get; }

        /// <summary>Constructor.</summary>
        /// <param name="pathExtractor">A delegate that returns the path to an icon file from an instance of ListViewItem.</param>
        /// <param name="tempIconIdx">An icon will only be loaded for ListViewItem instances that have this value for their ImageIndex property.</param>
        internal AsyncIconScanner(IconPathDelegate pathExtractor, int tempIconIdx)
        {
            PathExtractor = pathExtractor;
            TemporaryIconIdx = tempIconIdx;
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
                    if (!string.IsNullOrWhiteSpace(icon_path) && (li.ImageIndex == TemporaryIconIdx))
                    {
                        var is_icon_new = !LoadedIcons.TryGetValue(icon_path, out int icon_idx);
                        var icon = is_icon_new ? Utils.GetIconContained(icon_path, iconSize.Width, iconSize.Height) : null;

                        if (!is_icon_new || (is_icon_new && (icon is not null)))
                        {
                            listView.BeginInvoke((MethodInvoker)delegate
                            {
                                if (is_icon_new)
                                {
                                    imageList.Images.Add(icon_path, icon);
                                    icon_idx = imageList.Images.IndexOfKey(icon_path);
                                    LoadedIcons.TryAdd(icon_path, icon_idx);
                                }
                                li.ImageIndex = icon_idx;

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
                listView.BeginInvoke((MethodInvoker)delegate { listView.Refresh(); });
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
