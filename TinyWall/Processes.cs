using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace PKSoft
{
    internal partial class ProcessesForm : Form
    {
        internal string SelectedPath;

        internal static AppExceptionSettings ChooseProcess(IWin32Window parent)
        {
            using (ProcessesForm pf = new ProcessesForm())
            {
                if (pf.ShowDialog(parent) == DialogResult.Cancel)
                    return null;

                AppExceptionSettings ex = new AppExceptionSettings(pf.SelectedPath);
                ex.ServiceName = string.Empty;
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
                Process p = procs[i];
                try
                {
                    string ProcPath = p.MainModule.FileName;

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
                    if (!IconList.Images.ContainsKey(ProcPath))
                        IconList.Images.Add(ProcPath, Utils.GetIcon(p.MainModule.FileName, 16, 16));

                    // Add list item
                    ListViewItem li = new ListViewItem(p.ProcessName);
                    li.ImageKey = ProcPath;
                    li.SubItems.Add(p.MainModule.FileName);
                    li.Tag = ProcPath;
                    itemColl.Add(li);
                }
                catch
                {
                }
            }

            listView.SuspendLayout();
            listView.Items.AddRange(itemColl.ToArray());
            listView.ResumeLayout(true);
        }
    }
}
