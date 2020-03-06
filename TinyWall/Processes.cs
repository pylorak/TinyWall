using System;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using TinyWall.Interface;

namespace PKSoft
{
    internal partial class ProcessesForm : Form
    {
        internal class EntryDetails
        {
            public string ExePath;
            public UwpPackage.Package? Package;
        }

        private readonly List<EntryDetails> Selection = new List<EntryDetails>();
        private readonly Size IconSize = new Size((int)Math.Round(16 * Utils.DpiScalingFactor), (int)Math.Round(16 * Utils.DpiScalingFactor));

        internal static List<EntryDetails> ChooseProcess(IWin32Window parent, bool multiSelect)
        {
            using (ProcessesForm pf = new ProcessesForm(multiSelect))
            {
                List<EntryDetails> pathList = new List<EntryDetails>();

                if (pf.ShowDialog(parent) == DialogResult.Cancel)
                    return pathList;

                pathList.AddRange(pf.Selection);
                return pathList;
            }
        }

        internal ProcessesForm(bool multiSelect)
        {
            InitializeComponent();
            this.IconList.ImageSize = IconSize;
            this.listView.MultiSelect = multiSelect;
            this.Icon = Resources.Icons.firewall;
            this.btnOK.Image = GlobalInstances.ApplyBtnIcon;
            this.btnCancel.Image = GlobalInstances.CancelBtnIcon;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listView.SelectedItems.Count; ++i)
            {
                this.Selection.Add(listView.SelectedItems[i].Tag as EntryDetails);
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
            this.Size = ActiveConfig.Controller.ProcessesFormWindowSize;
            this.Location = ActiveConfig.Controller.ProcessesFormWindowLoc;
            this.WindowState = ActiveConfig.Controller.ProcessesFormWindowState;

            List<ListViewItem> itemColl = new List<ListViewItem>();
            UwpPackage packages = new UwpPackage();

            Process[] procs = Process.GetProcesses();
            for (int i = 0; i < procs.Length; ++i)
            {
                using (Process p = procs[i])
                {
                    try
                    {
                        EntryDetails e = new EntryDetails();

                        e.ExePath = Utils.GetPathOfProcessUseTwService(p.Id, GlobalInstances.Controller);
                        if (string.IsNullOrEmpty(e.ExePath))
                            continue;

                        // Scan list of already added items to prevent duplicates
                        bool skip = false;
                        for (int j = 0; j < itemColl.Count; ++j)
                        {
                            if (itemColl[j].SubItems[1].Text.ToUpperInvariant().Equals(e.ExePath.ToUpperInvariant()))
                            {
                                skip = true;
                                break;
                            }
                        }
                        if (skip)
                            continue;

                        // Add icon
                        if (System.IO.Path.IsPathRooted(e.ExePath) && System.IO.File.Exists(e.ExePath))
                        {
                            if (!IconList.Images.ContainsKey(e.ExePath))
                                IconList.Images.Add(e.ExePath, Utils.GetIconContained(e.ExePath, IconSize.Width, IconSize.Height));
                        }

                        // Detect AppContainers
                        e.Package = packages.FindPackage(ProcessManager.GetAppContainerSid(p.Id));

                        // Add list item
                        ListViewItem li = new ListViewItem(p.ProcessName);
                        li.ImageKey = e.ExePath;
                        li.SubItems.Add(e.ExePath);
                        li.Tag = e;
                        itemColl.Add(li);
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

            ActiveConfig.Controller.Save();
        }
    }
}
