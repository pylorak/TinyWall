using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PKSoft.netstat;

namespace PKSoft
{
    internal partial class ConnectionsForm : Form
    {
        List<FirewallLogEntry> FwLogEntries = new List<FirewallLogEntry>();

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

            ReqResp fwLogRequest = GlobalInstances.CommunicationMan.QueueMessage(new Message(TinyWallCommands.READ_FW_LOG));

            // Retrieve IP tables while waiting for log entries

            TcpTable tcpTable = NetStat.GetExtendedTcp4Table(false);
            foreach (TcpRow tcpRow in tcpTable)
            {
                if ( (chkShowListen.Checked && (tcpRow.State == TcpState.Listen))
                  || (chkShowActive.Checked && (tcpRow.State != TcpState.Listen)))
                    ConstructListItem(itemColl, tcpRow.ProcessId, "TCP", tcpRow.LocalEndPoint, tcpRow.RemoteEndPoint, tcpRow.State.ToString());
            }
            tcpTable = NetStat.GetExtendedTcp6Table(false);
            foreach (TcpRow tcpRow in tcpTable)
            {
                if ((chkShowListen.Checked && (tcpRow.State == TcpState.Listen))
                 || (chkShowActive.Checked && (tcpRow.State != TcpState.Listen)))
                    ConstructListItem(itemColl, tcpRow.ProcessId, "TCP", tcpRow.LocalEndPoint, tcpRow.RemoteEndPoint, tcpRow.State.ToString());
            }

            if (chkShowListen.Checked)
            {
                IPEndPoint dummyEP = new IPEndPoint(0, 0);
                UdpTable udpTable = NetStat.GetExtendedUdp4Table(false);
                foreach (UdpRow udpRow in udpTable)
                {
                    ConstructListItem(itemColl, udpRow.ProcessId, "UDP", udpRow.LocalEndPoint, dummyEP, "Listen");
                }
                udpTable = NetStat.GetExtendedUdp6Table(false);
                foreach (UdpRow udpRow in udpTable)
                {
                    ConstructListItem(itemColl, udpRow.ProcessId, "UDP", udpRow.LocalEndPoint, dummyEP, "Listen");
                }
            }

            // Finished reading tables, continues with log processing

            // Remove log entries older than 2 minutes
            DateTime now = DateTime.Now;
            TimeSpan refSpan = TimeSpan.FromMinutes(2);
            for (int i = FwLogEntries.Count - 1; i >= 0; --i)
            {
                TimeSpan span = now - FwLogEntries[i].Timestamp;
                if (span > refSpan)
                    FwLogEntries.RemoveAt(i);
            }

            // Add new log entries
            Message resp = fwLogRequest.GetResponse();
            List<FirewallLogEntry> fwLogEntry = resp.Arguments[0] as List<FirewallLogEntry>;
            for (int i = 0; i < fwLogEntry.Count; ++i)
            {
                bool matchFound = false;
                FirewallLogEntry newEntry = fwLogEntry[i];
                for (int j = 0; j < FwLogEntries.Count; ++j)
                {
                    FirewallLogEntry oldEntry = FwLogEntries[j];
                    if (oldEntry.Equals(newEntry, false))
                    {
                        matchFound = true;
                        oldEntry.Timestamp = newEntry.Timestamp;
                        break;
                    }
                }

                if (!matchFound)
                    FwLogEntries.Add(newEntry);
            }

            // Show log entries if requested by user
            if (chkShowBlocked.Checked)
            {
                for (int i = 0; i < FwLogEntries.Count; ++i)
                {
                    FirewallLogEntry entry = FwLogEntries[i];
                    ConstructListItem(itemColl, (int)entry.ProcessID, entry.Protocol.ToString(), new IPEndPoint(IPAddress.Parse(entry.SourceIP), entry.SourcePort), new IPEndPoint(IPAddress.Parse(entry.DestinationIP), entry.DestinationPort), "Blocked");
                }
            }

            // Add items to list
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
            li.SubItems.Add(localEP.Port.ToString(CultureInfo.InvariantCulture).PadLeft(5));
            li.SubItems.Add(localEP.Address.ToString());
            li.SubItems.Add(remoteEP.Port.ToString(CultureInfo.InvariantCulture).PadLeft(5));
            li.SubItems.Add(remoteEP.Address.ToString());
            li.SubItems.Add(state);
            itemColl.Add(li);
        }

        private void list_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            list.ListViewItemSorter = new ListViewItemComparer(e.Column);
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            UpdateList();
        }

        private void chkShowListen_CheckedChanged(object sender, EventArgs e)
        {
            UpdateList();
        }

        private void chkShowBlocked_CheckedChanged(object sender, EventArgs e)
        {
            UpdateList();
        }

        private void chkShowActive_CheckedChanged(object sender, EventArgs e)
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

            SettingsManager.ControllerConfig.ConnFormShowConnections = this.chkShowActive.Checked;
            SettingsManager.ControllerConfig.ConnFormShowOpenPorts = this.chkShowListen.Checked;
            SettingsManager.ControllerConfig.ConnFormShowBlocked = this.chkShowBlocked.Checked;

            SettingsManager.ControllerConfig.Save();
        }

        private void ConnectionsForm_Load(object sender, EventArgs e)
        {
            this.Size = SettingsManager.ControllerConfig.ConnFormWindowSize;
            this.Location = SettingsManager.ControllerConfig.ConnFormWindowLoc;
            this.WindowState = SettingsManager.ControllerConfig.ConnFormWindowState;
            this.chkShowActive.Checked = SettingsManager.ControllerConfig.ConnFormShowConnections;
            this.chkShowListen.Checked = SettingsManager.ControllerConfig.ConnFormShowOpenPorts;
            this.chkShowBlocked.Checked = SettingsManager.ControllerConfig.ConnFormShowBlocked;
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
                            throw new ApplicationException();
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
            return String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text, StringComparison.CurrentCulture);
        }
    }

}
