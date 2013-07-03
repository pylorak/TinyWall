using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace PKSoft
{
    internal partial class ProcessesForm : Form
    {
        internal string SelectedPath;

        internal static FirewallException ChooseProcess(IWin32Window parent = null)
        {
            using (ProcessesForm pf = new ProcessesForm())
            {
                if (pf.ShowDialog(parent) == DialogResult.Cancel)
                    return null;

                FirewallException ex = new FirewallException(pf.SelectedPath, null, true);
                return ex;
            }
        }

        internal ProcessesForm()
        {
            InitializeComponent();
            this.Icon = Resources.Icons.firewall;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.SelectedPath = listView.SelectedItems[0].Tag as string;
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

        private void ProcessesForm_Load(object sender, EventArgs e)
        {
            this.Icon = Resources.Icons.firewall;

            List<ListViewItem> itemColl = new List<ListViewItem>();

            Process[] procs = Process.GetProcesses();
            for (int i = 0; i < procs.Length; ++i)
            {
                using (Process p = procs[i])
                {
                    try
                    {
                        string ProcPath = Utils.GetPathOfProcessUseTwService(p);
                        if (string.IsNullOrEmpty(ProcPath))
                            continue;

                        // Scan list of already added items to prevent duplicates
                        bool skip = false;
                        for (int j = 0; j < listView.Items.Count; ++j)
                        {
                            if (listView.Items[j].SubItems[0].Text.ToUpperInvariant().Equals(p.ProcessName.ToUpperInvariant()) &&
                                listView.Items[j].SubItems[1].Text.ToUpperInvariant().Equals(ProcPath.ToUpperInvariant())
                                )
                            {
                                skip = true;
                                break;
                            }
                        }
                        if (skip)
                            continue;

                        // Add icon
                        if (System.IO.Path.IsPathRooted(ProcPath) && System.IO.File.Exists(ProcPath))
                        {
                            if (!IconList.Images.ContainsKey(ProcPath))
                                IconList.Images.Add(ProcPath, Utils.GetIcon(ProcPath, 16, 16));
                        }

                        // Add list item
                        ListViewItem li = new ListViewItem(p.ProcessName);
                        li.ImageKey = ProcPath;
                        li.SubItems.Add(ProcPath);
                        li.Tag = ProcPath;
                        itemColl.Add(li);
                    }
                    catch
                    {
                    }
                }
            }

            listView.SuspendLayout();
            listView.ListViewItemSorter = new ListViewItemComparer(0);
            listView.Items.AddRange(itemColl.ToArray());
            listView.ResumeLayout(true);
        }

        private void listView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListViewItemComparer oldSorter = listView.ListViewItemSorter as ListViewItemComparer;
            ListViewItemComparer newSorter = new ListViewItemComparer(e.Column);
            if ((oldSorter != null) && (oldSorter.Column == newSorter.Column))
                newSorter.Ascending = !oldSorter.Ascending;

            listView.ListViewItemSorter = newSorter;
        }
    }
}
