using pylorak.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace pylorak.TinyWall
{
    internal partial class ProcessesForm : Form
    {
        internal readonly List<ProcessInfo> Selection = new();

        private readonly Size _iconSize = new((int)Math.Round(16 * Utils.DpiScalingFactor),
            (int)Math.Round(16 * Utils.DpiScalingFactor));

        private string _searchItem = string.Empty;

        internal ProcessesForm(bool multiSelect)
        {
            InitializeComponent();
            Utils.SetRightToLeft(this);
            IconList.ImageSize = _iconSize;
            listView.MultiSelect = multiSelect;
            Icon = Resources.Icons.firewall;
            btnOK.Image = GlobalInstances.ApplyBtnIcon;
            btnCancel.Image = GlobalInstances.CancelBtnIcon;

            IconList.Images.Add("store", Resources.Icons.store);
            IconList.Images.Add("system", Resources.Icons.windows_small);
            IconList.Images.Add("network-drive", Resources.Icons.network_drive_small);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listView.SelectedItems.Count; ++i)
            {
                Selection.Add((ProcessInfo)listView.SelectedItems[i].Tag);
            }

            DialogResult = DialogResult.OK;
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

        private async void ProcessesForm_Load(object sender, EventArgs ev)
        {
            Icon = Resources.Icons.firewall;
            if (ActiveConfig.Controller.ProcessesFormWindowSize.Width != 0)
                Size = ActiveConfig.Controller.ProcessesFormWindowSize;
            if (ActiveConfig.Controller.ProcessesFormWindowLoc.X != 0)
            {
                Location = ActiveConfig.Controller.ProcessesFormWindowLoc;
                Utils.FixupFormPosition(this);
            }

            WindowState = ActiveConfig.Controller.ProcessesFormWindowState;

            await UpdateListAsync();
        }

        private async Task UpdateListAsync()
        {
            Utils.Invoke(this, () => {
                lblPleaseWait.Visible = true;
                Enabled = false;
            });

            // Load column widths ahead of time to avoid accessing UI thread during background processing
            Dictionary<string, int> columnWidths = new Dictionary<string, int>();
            foreach (ColumnHeader col in listView.Columns)
            {
                if (ActiveConfig.Controller.ProcessesFormColumnWidths.TryGetValue((string)col.Tag, out int width))
                    columnWidths[(string)col.Tag] = width;
            }

            // Move heavy operations to background thread
            var items = await Task.Run(() => {
                List<ListViewItem> itemColl = new List<ListViewItem>();
                
                var packageList = new UwpPackageList();
                ServicePidMap servicePids = new ServicePidMap();

                Process[] procs = Process.GetProcesses();

                if (!string.IsNullOrWhiteSpace(_searchItem))
                    procs = procs.Where(p => p.ProcessName.ToLower().Contains(_searchItem.ToLower())).ToArray();

                foreach (var t in procs)
                {
                    // Check if we need to cancel the operation
                    if (!this.IsHandleCreated) break;
                    
                    using Process p = t;
                    try
                    {
                        var pid = unchecked((uint)p.Id);
                        var e = ProcessInfo.Create(pid, packageList, servicePids);

                        if (string.IsNullOrEmpty(e.Path))
                            continue;

                        // Scan list of already added items to prevent duplicates
                        bool skip = itemColl.Select(t1 => (ProcessInfo)t1.Tag).Any(opi =>
                            (e.Package == opi.Package) && (e.Path == opi.Path) && (e.Services.SetEquals(opi.Services)));

                        if (skip)
                            continue;

                        // Create list item without adding icons initially (icons need to be added on UI thread)
                        ListViewItem li = new ListViewItem(e.Package.HasValue ? e.Package.Value.Name : p.ProcessName);
                        li.SubItems.Add(string.Join(", ", e.Services.ToArray()));
                        li.SubItems.Add(e.Path);
                        li.Tag = e;
                        itemColl.Add(li);
                    }
                    catch
                    {
                        // ignored
                    }
                }

                return itemColl;
            });

            // Update UI on main thread
            Utils.Invoke(this, () => {
                // Apply column widths
                foreach (ColumnHeader col in listView.Columns)
                {
                    if (columnWidths.TryGetValue((string)col.Tag, out int width))
                        col.Width = width;
                }

                Utils.SetDoubleBuffering(listView, true);
                listView.BeginUpdate();
                listView.Items.Clear();
                listView.ListViewItemSorter = new ListViewItemComparer(0);

                // Add items to the list view
                listView.Items.AddRange(items.ToArray());

                // Now add icons after items are added to the control
                foreach (ListViewItem li in listView.Items)
                {
                    var e = (ProcessInfo)li.Tag;
                    
                    // Add icon
                    if (e.Package.HasValue)
                    {
                        li.ImageKey = @"store";
                    }
                    else if (e.Path == "System")
                    {
                        li.ImageKey = @"system";
                    }
                    else if (NetworkPath.IsNetworkPath(e.Path))
                    {
                        li.ImageKey = @"network-drive";
                    }
                    else if (System.IO.Path.IsPathRooted(e.Path) && System.IO.File.Exists(e.Path))
                    {
                        if (!IconList.Images.ContainsKey(e.Path))
                            IconList.Images.Add(e.Path,
                                Utils.GetIconContained(e.Path, _iconSize.Width, _iconSize.Height));
                        li.ImageKey = e.Path;
                    }
                }

                listView.EndUpdate();

                lblPleaseWait.Visible = false;
                Enabled = true;
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
        }

        private void txtBxSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Enter or Keys.Return)
            {
                btnSearch.PerformClick();
            }
        }

        private async void btnClear_Click(object sender, EventArgs e)
        {
            _searchItem = string.Empty;
            txtBxSearch.Text = string.Empty;

            await UpdateListAsync();
        }

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBxSearch.Text))
            {
                return;
            }

            _searchItem = txtBxSearch.Text.ToLower();

            await UpdateListAsync();
        }
    }
}