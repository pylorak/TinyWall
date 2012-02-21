using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using PKSoft.netstat;

namespace PKSoft
{
    internal partial class ConnectionsForm : Form
    {
        private struct ProcInfo
        {
            internal int pid;
            internal string name;
            internal string path;
        }

        private List<FirewallLogEntry> FwLogEntries = new List<FirewallLogEntry>();
        private MainForm MainForm = null;

        internal ConnectionsForm(MainForm form)
        {
            InitializeComponent();
            this.Icon = Resources.Icons.firewall;
            this.MainForm = form;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void UpdateList()
        {
            List<ListViewItem> itemColl = new List<ListViewItem>();
            Dictionary<int, ProcInfo> procCache = new Dictionary<int, ProcInfo>();

            ReqResp fwLogRequest = GlobalInstances.CommunicationMan.QueueMessage(new Message(TinyWallCommands.READ_FW_LOG));

            // Retrieve IP tables while waiting for log entries

            TcpTable tcpTable = NetStat.GetExtendedTcp4Table(false);
            foreach (TcpRow tcpRow in tcpTable)
            {
                if ( (chkShowListen.Checked && (tcpRow.State == TcpState.Listen))
                  || (chkShowActive.Checked && (tcpRow.State != TcpState.Listen)))
                    ConstructListItem(itemColl, procCache, tcpRow.ProcessId, "TCP", tcpRow.LocalEndPoint, tcpRow.RemoteEndPoint, tcpRow.State.ToString());
            }
            tcpTable = NetStat.GetExtendedTcp6Table(false);
            foreach (TcpRow tcpRow in tcpTable)
            {
                if ((chkShowListen.Checked && (tcpRow.State == TcpState.Listen))
                 || (chkShowActive.Checked && (tcpRow.State != TcpState.Listen)))
                    ConstructListItem(itemColl, procCache, tcpRow.ProcessId, "TCP", tcpRow.LocalEndPoint, tcpRow.RemoteEndPoint, tcpRow.State.ToString());
            }

            if (chkShowListen.Checked)
            {
                IPEndPoint dummyEP = new IPEndPoint(0, 0);
                UdpTable udpTable = NetStat.GetExtendedUdp4Table(false);
                foreach (UdpRow udpRow in udpTable)
                {
                    ConstructListItem(itemColl, procCache, udpRow.ProcessId, "UDP", udpRow.LocalEndPoint, dummyEP, "Listen");
                }
                udpTable = NetStat.GetExtendedUdp6Table(false);
                foreach (UdpRow udpRow in udpTable)
                {
                    ConstructListItem(itemColl, procCache, udpRow.ProcessId, "UDP", udpRow.LocalEndPoint, dummyEP, "Listen");
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
                    ConstructListItem(itemColl, procCache, (int)entry.ProcessID, entry.Protocol.ToString(), new IPEndPoint(IPAddress.Parse(entry.SourceIP), entry.SourcePort), new IPEndPoint(IPAddress.Parse(entry.DestinationIP), entry.DestinationPort), "Blocked");
                }
            }

            // Add items to list
            list.SuspendLayout();
            list.Items.Clear();
            list.Items.AddRange(itemColl.ToArray());
            list.ResumeLayout(false);
        }

        private void ConstructListItem(List<ListViewItem> itemColl, Dictionary<int, ProcInfo> procCache, int procId, string protocol, IPEndPoint localEP, IPEndPoint remoteEP, string state)
        {
            try
            {
                // Get process information
                ProcInfo pi = new ProcInfo();
                if (procCache.ContainsKey(procId))
                    pi = procCache[procId];
                else
                {
                    using (Process proc = Process.GetProcessById(procId))
                    {
                        pi.pid = procId;
                        pi.name = proc.ProcessName;
                        pi.path = Utils.GetProcessMainModulePath(proc);
                        if (string.IsNullOrEmpty(pi.path))
                        {
                            // We couldn't extract path of process
                            if (pi.name.Equals("System", StringComparison.OrdinalIgnoreCase))
                                pi.path = "System";
                            else
                                pi.path = string.Empty;
                        }
                    }
                    procCache.Add(procId, pi);
                }

                // Construct list item
                ListViewItem li = new ListViewItem(string.Format(CultureInfo.CurrentCulture, "{0}({1})", pi.name, pi.pid));
                li.Tag = pi.pid;
                li.ToolTipText = pi.path;

                if (System.IO.Path.IsPathRooted(pi.path))
                {
                    if (!IconList.Images.ContainsKey(pi.path))
                    {
                        // Get icon
                        IconList.Images.Add(pi.path, Utils.GetIcon(pi.path, 16, 16));
                    }
                    li.ImageKey = pi.path;
                }

                li.SubItems.Add(protocol);
                li.SubItems.Add(localEP.Port.ToString(CultureInfo.InvariantCulture).PadLeft(5));
                li.SubItems.Add(localEP.Address.ToString());
                li.SubItems.Add(remoteEP.Port.ToString(CultureInfo.InvariantCulture).PadLeft(5));
                li.SubItems.Add(remoteEP.Address.ToString());
                li.SubItems.Add(state);
                itemColl.Add(li);
            }
            catch
            {
                // Most probably process ID has become invalid,
                // but we also catch other errors too.
                // Simply do not add item to the list.
                return;
            }
        }

        private void list_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListViewItemComparer oldSorter = list.ListViewItemSorter as ListViewItemComparer;
            ListViewItemComparer newSorter = new ListViewItemComparer(e.Column);
            if ((oldSorter != null) && (oldSorter.Column == newSorter.Column))
                newSorter.Ascending = !oldSorter.Ascending;

            list.ListViewItemSorter = newSorter;
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
            foreach (ListViewItem li in list.SelectedItems)
            {
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
                            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.CouldNotCloseProcess, proc.ProcessName, pid), PKSoft.Resources.Messages.TinyWall, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        }
                    }
                }
                catch
                {
                    // The app has probably already quit. Leave it at that.
                }
            }
        }

        private void mnuUnblock_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem li in list.SelectedItems)
            {
                string path = li.ToolTipText;
                if (string.IsNullOrEmpty(path))
                    continue;

                try
                {
                    AppExceptionSettings ex = new AppExceptionSettings(path);
                    ex.ServiceName = string.Empty;
                    ex.TryRecognizeApp(true);

                    if (ex.Recognized.Value)
                    {
                        List<AppExceptionSettings> exceptions = AppExceptionSettings.CheckForAppDependencies(this, ex);
                        for (int i = 0; i < exceptions.Count; ++i)
                            SettingsManager.CurrentZone.AppExceptions = Utils.ArrayAddItem(SettingsManager.CurrentZone.AppExceptions, exceptions[i]);
                    }
                    else
                    {
                        SettingsManager.CurrentZone.AppExceptions = Utils.ArrayAddItem(SettingsManager.CurrentZone.AppExceptions, ex);
                    }
                }
                catch
                {
                    MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.CouldNotWhitelistProcess, path), PKSoft.Resources.Messages.TinyWall, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }

            SettingsManager.CurrentZone.Normalize();
            MainForm.ApplyFirewallSettings(null, SettingsManager.CurrentZone, true);
        }
    }
}
