using System;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Linq;
using pylorak.Windows;
using pylorak.Utilities;

namespace pylorak.TinyWall
{
    internal partial class ProcessesForm : Form
    {
        internal readonly List<ProcessInfo> Selection = new();
        private readonly BackgroundTask IconScanner = new();
        private readonly Size IconSize = new Size((int)Math.Round(16 * Utils.DpiScalingFactor), (int)Math.Round(16 * Utils.DpiScalingFactor));

        internal ProcessesForm(bool multiSelect)
        {
            InitializeComponent();
            Utils.SetRightToLeft(this);
            this.IconList.ImageSize = IconSize;
            this.listView.MultiSelect = multiSelect;
            this.Icon = Resources.Icons.firewall;
            this.btnOK.Image = GlobalInstances.ApplyBtnIcon;
            this.btnCancel.Image = GlobalInstances.CancelBtnIcon;

            this.IconList.Images.Add("store", Resources.Icons.store);
            this.IconList.Images.Add("system", Resources.Icons.windows_small);
            this.IconList.Images.Add("network-drive", Resources.Icons.network_drive_small);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listView.SelectedItems.Count; ++i)
            {
                this.Selection.Add((ProcessInfo)listView.SelectedItems[i].Tag);
            }
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnOK.Enabled = listView.SelectedItems.Count > 0;
        }

        private void listView_DoubleClick(object sender, EventArgs e)
        {
            if (btnOK.Enabled)
            {
                btnOK_Click(btnOK, EventArgs.Empty);
            }
        }

        private void ProcessesForm_Load(object sender, EventArgs ev)
        {
            this.Icon = Resources.Icons.firewall;
            if (ActiveConfig.Controller.ProcessesFormWindowSize.Width != 0)
                this.Size = ActiveConfig.Controller.ProcessesFormWindowSize;
            if (ActiveConfig.Controller.ProcessesFormWindowLoc.X != 0)
            {
                this.Location = ActiveConfig.Controller.ProcessesFormWindowLoc;
                Utils.FixupFormPosition(this);
            }
            this.WindowState = ActiveConfig.Controller.ProcessesFormWindowState;

            foreach (ColumnHeader col in listView.Columns)
            {
                if (ActiveConfig.Controller.ProcessesFormColumnWidths.TryGetValue((string)col.Tag, out int width))
                    col.Width = width;
            }

            const string TEMP_ICON_KEY = ".exe";
            IconList.Images.Add(TEMP_ICON_KEY, Utils.GetIconContained(".exe", IconSize.Width, IconSize.Height));
            IconScanner.CancelTask();

            List<ListViewItem> itemColl = new List<ListViewItem>();
            UwpPackage packages = new UwpPackage();
            ServicePidMap service_pids = new ServicePidMap();
            Process[] procs = Process.GetProcesses();

            for (int i = 0; i < procs.Length; ++i)
            {
                using (Process p = procs[i])
                {
                    try
                    {
                        var pid = unchecked((uint)p.Id);
                        var e = ProcessInfo.Create(pid, packages, service_pids);

                        if (string.IsNullOrEmpty(e.Path))
                            continue;

                        // Scan list of already added items to prevent duplicates
                        bool skip = false;
                        for (int j = 0; j < itemColl.Count; ++j)
                        {
                            ProcessInfo opi = (ProcessInfo)itemColl[j].Tag;
                            if ((e.Package == opi.Package) && (e.Path == opi.Path) && (e.Services.SetEquals(opi.Services)))
                            {
                                skip = true;
                                break;
                            }
                        }
                        if (skip)
                            continue;

                        // Add list item
                        ListViewItem li = new ListViewItem(e.Package.HasValue ? e.Package.Value.Name : p.ProcessName);
                        li.SubItems.Add(string.Join(", ", e.Services.ToArray()));
                        li.SubItems.Add(e.Path);
                        li.Tag = e;
                        itemColl.Add(li);

                        // Add icon
                        if (e.Package.HasValue)
                        {
                            li.ImageKey = "store";
                        }
                        else if (e.Path == "System")
                        {
                            li.ImageKey = "system";
                        }
                        else if (NetworkPath.IsNetworkPath(e.Path))
                        {
                            li.ImageKey = "network-drive";
                        }
                        else
                        {
                            // Real icon will be loaded later asynchronously, for now just assign a generic icon
                            li.ImageKey = TEMP_ICON_KEY;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            Utils.SetDoubleBuffering(listView, true);
            listView.BeginUpdate();
            listView.ListViewItemSorter = new ListViewItemComparer(0);
            listView.Items.AddRange(itemColl.ToArray());
            listView.EndUpdate();

            // Load process icons asynchronously
            IconScanner.Restart(() =>
            {
                var st = Stopwatch.StartNew();
                var loaded_icons = new HashSet<string>();
                foreach (var li in itemColl)
                {
                    IconScanner.CancellationToken.ThrowIfCancellationRequested();

                    var icon_path = (li.Tag as ProcessInfo)!.Path;
                    if (li.ImageKey.Equals(TEMP_ICON_KEY))
                    {
                        var is_icon_new = !loaded_icons.Contains(icon_path);
                        var icon = is_icon_new ? Utils.GetIconContained(icon_path, IconSize.Width, IconSize.Height) : null;
                        loaded_icons.Add(icon_path);

                        if (!is_icon_new || (is_icon_new && (icon is not null)))
                        {
                            listView.BeginInvoke((MethodInvoker)delegate
                            {
                                if (is_icon_new)
                                    IconList.Images.Add(icon_path, icon);
                                li.ImageKey = icon_path;

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

        private void listView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            var oldSorter = (ListViewItemComparer)listView.ListViewItemSorter;
            var newSorter = new ListViewItemComparer(e.Column);
            if ((oldSorter != null) && (oldSorter.Column == newSorter.Column))
                newSorter.Ascending = !oldSorter.Ascending;

            listView.ListViewItemSorter = newSorter;
        }

        private void ProcessesForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ActiveConfig.Controller.ProcessesFormWindowState = this.WindowState;
            if (this.WindowState == FormWindowState.Normal)
            {
                ActiveConfig.Controller.ProcessesFormWindowSize = this.Size;
                ActiveConfig.Controller.ProcessesFormWindowLoc = this.Location;
            }
            else
            {
                ActiveConfig.Controller.ProcessesFormWindowSize = this.RestoreBounds.Size;
                ActiveConfig.Controller.ProcessesFormWindowLoc = this.RestoreBounds.Location;
            }

            ActiveConfig.Controller.ProcessesFormColumnWidths.Clear();
            foreach (ColumnHeader col in listView.Columns)
                ActiveConfig.Controller.ProcessesFormColumnWidths.Add((string)col.Tag, col.Width);

            ActiveConfig.Controller.Save();
            IconScanner.Dispose();
        }
    }
}
