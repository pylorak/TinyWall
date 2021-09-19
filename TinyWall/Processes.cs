using System;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Linq;

namespace PKSoft
{
    internal partial class ProcessesForm : Form
    {
        internal readonly List<ProcessInfo> Selection = new List<ProcessInfo>();
        private readonly Size IconSize = new Size((int)Math.Round(16 * Utils.DpiScalingFactor), (int)Math.Round(16 * Utils.DpiScalingFactor));

        internal ProcessesForm(bool multiSelect)
        {
            InitializeComponent();
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
                this.Selection.Add(listView.SelectedItems[i].Tag as ProcessInfo);
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
                btnOK_Click(btnOK, null);
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
                if (ActiveConfig.Controller.ProcessesFormColumnWidths.TryGetValue(col.Tag as string, out int width))
                    col.Width = width;
            }

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
                        ProcessInfo e = new ProcessInfo(p.Id, packages, service_pids);

                        if (string.IsNullOrEmpty(e.ExePath))
                            continue;

                        // Scan list of already added items to prevent duplicates
                        bool skip = false;
                        for (int j = 0; j < itemColl.Count; ++j)
                        {
                            ProcessInfo opi = itemColl[j].Tag as ProcessInfo;
                            if ((e.Package == opi.Package) && (e.ExePath == opi.ExePath) && (e.Services.SetEquals(opi.Services)))
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
                        li.SubItems.Add(e.ExePath);
                        li.Tag = e;
                        itemColl.Add(li);

                        // Add icon
                        if (e.Package.HasValue)
                        {
                            li.ImageKey = "store";
                        }
                        else if (e.ExePath == "System")
                        {
                            li.ImageKey = "system";
                        }
                        else if (TinyWall.Interface.Internal.NetworkPath.IsNetworkPath(e.ExePath))
                        {
                            li.ImageKey = "network-drive";
                        }
                        else if (System.IO.Path.IsPathRooted(e.ExePath) && System.IO.File.Exists(e.ExePath))
                        {
                            if (!IconList.Images.ContainsKey(e.ExePath))
                                IconList.Images.Add(e.ExePath, Utils.GetIconContained(e.ExePath, IconSize.Width, IconSize.Height));
                            li.ImageKey = e.ExePath;
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
        }

        private void listView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListViewItemComparer oldSorter = listView.ListViewItemSorter as ListViewItemComparer;
            ListViewItemComparer newSorter = new ListViewItemComparer(e.Column);
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
                ActiveConfig.Controller.ProcessesFormColumnWidths.Add(col.Tag as string, col.Width);

            ActiveConfig.Controller.Save();
        }
    }
}
