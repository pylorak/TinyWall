using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PKSoft.netstat;

namespace PKSoft
{
    internal partial class ConnectionsForm : Form
    {
        internal ConnectionsForm()
        {
            InitializeComponent();
            this.Icon = Icons.firewall;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void UpdateList()
        {
            List<ListViewItem> itemColl = new List<ListViewItem>();

            TcpTable tcpTable = NetStat.GetExtendedTcp4Table(false);
            foreach (TcpRow tcpRow in tcpTable)
            {
                if (chkShowListen.Checked || (tcpRow.State != TcpState.Listen))
                    ConstructListItem(itemColl, tcpRow.ProcessId, "TCP/IPv4", tcpRow.LocalEndPoint, tcpRow.RemoteEndPoint, tcpRow.State.ToString());
            }
            tcpTable = NetStat.GetExtendedTcp6Table(false);
            foreach (TcpRow tcpRow in tcpTable)
            {
                if (chkShowListen.Checked || (tcpRow.State != TcpState.Listen))
                    ConstructListItem(itemColl, tcpRow.ProcessId, "TCP/IPv6", tcpRow.LocalEndPoint, tcpRow.RemoteEndPoint, tcpRow.State.ToString());
            }

            if (chkShowListen.Checked)
            {
                IPEndPoint dummyEP = new IPEndPoint(0, 0);
                UdpTable udpTable = NetStat.GetExtendedUdp4Table(false);
                foreach (UdpRow udpRow in udpTable)
                {
                    ConstructListItem(itemColl, udpRow.ProcessId, "UDP/IPv4", udpRow.LocalEndPoint, dummyEP, "Listen");
                }
                udpTable = NetStat.GetExtendedUdp6Table(false);
                foreach (UdpRow udpRow in udpTable)
                {
                    ConstructListItem(itemColl, udpRow.ProcessId, "UDP/IPv6", udpRow.LocalEndPoint, dummyEP, "Listen");
                }
            }

            list.SuspendLayout();
            list.Items.Clear();
            list.Items.AddRange(itemColl.ToArray());
            list.ResumeLayout(false);
        }

        private void ConstructListItem(List<ListViewItem> itemColl, int procId, string protocol, IPEndPoint localEP, IPEndPoint remoteEP, string state)
        {
            // Get process
            Process proc = null;
            try
            {
                proc = Process.GetProcessById(procId);
            }
            catch (ArgumentException)
            {
                // Process ID has become invalid,
                // do not add item to collection.
                return;
            }

            ListViewItem li = new ListViewItem(proc.ProcessName + " (" + proc.Id + ")");
            li.Tag = proc.Id;

            try
            {
                // Get path
                string path = proc.MainModule.FileName;
                li.ToolTipText = path;

                // Get icon
                if (!IconList.Images.ContainsKey(path))
                {
                    IconList.Images.Add(path, Utils.GetIcon(path, 16, 16));
                }
                li.ImageKey = path;
            }
            catch
            {
                // If anything goes wrong above, well, we won't have an icon.
                // Who cares...
                // Notably, proc.MainModule.FileName throws often for some system processes
            }

            li.SubItems.Add(protocol);
            li.SubItems.Add(localEP.Port.ToString().PadLeft(5));
            li.SubItems.Add(localEP.Address.ToString());
            li.SubItems.Add(remoteEP.Port.ToString().PadLeft(5));
            li.SubItems.Add(remoteEP.Address.ToString());
            li.SubItems.Add(state);
            itemColl.Add(li);
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            UpdateList();
        }

        private void list_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            list.ListViewItemSorter = new ListViewItemComparer(e.Column);
        }

        private void chkShowListen_CheckedChanged(object sender, EventArgs e)
        {
            UpdateList();
        }

        private void ConnectionsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SettingsManager.ControllerConfig.ConnFormWindowState = this.WindowState;

            if (this.WindowState == FormWindowState.Normal)
            {
                SettingsManager.ControllerConfig.ConnFormWindowSize = this.Size;
                SettingsManager.ControllerConfig.ConnFormWindowLoc = this.Location;
            }
            else
            {
                SettingsManager.ControllerConfig.ConnFormWindowSize = this.RestoreBounds.Size;
                SettingsManager.ControllerConfig.ConnFormWindowLoc = this.RestoreBounds.Location;
            }

            SettingsManager.ControllerConfig.Save();
        }

        private void ConnectionsForm_Load(object sender, EventArgs e)
        {
            this.Size = SettingsManager.ControllerConfig.ConnFormWindowSize;
            this.Location = SettingsManager.ControllerConfig.ConnFormWindowLoc;
            this.WindowState = SettingsManager.ControllerConfig.ConnFormWindowState;
            UpdateList();
        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (list.SelectedIndices.Count < 1)
                e.Cancel = true;
        }

        private void mnuCloseProcess_Click(object sender, EventArgs e)
        {
            ListViewItem li = list.SelectedItems[0];
            int pid = (int)li.Tag;

            try
            {
                using (Process proc = Process.GetProcessById(pid))
                {
                    try
                    {
                        if (!proc.CloseMainWindow())
                        {
                            proc.Kill();
                        }
                        if (!proc.WaitForExit(5000))
                            throw new Exception();
                        else
                            UpdateList();
                    }
                    catch (InvalidOperationException)
                    {
                        // The process has already exited. Fine, that's just what we want :)
                    }
                    catch
                    {
                        // The process has already exited. Fine, that's just what we want :)
                        MessageBox.Show(this, "Could not close selected process. Elevate TinyWall and try again.", "Cannot close", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
            }
            catch
            {
                // The app has probably already quit. Leave it at that.
            }
        }
    }


    // Implements the manual sorting of items by columns.
    internal class ListViewItemComparer : IComparer
    {
        private int col;
        internal ListViewItemComparer()
        {
            col = 0;
        }
        internal ListViewItemComparer(int column)
        {
            col = column;
        }
        public int Compare(object x, object y)
        {
            return String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text);
        }
    }

}
