using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System.Drawing;
using PKSoft.netstat;

using TinyWall.Interface;
using TinyWall.Interface.Internal;

namespace PKSoft
{
    internal partial class ConnectionsForm : Form
    {
        private List<FirewallLogEntry> FwLogEntries = new List<FirewallLogEntry>();
        private TinyWallController Controller = null;
        private Size IconSize = new Size((int)Math.Round(16 * Utils.DpiScalingFactor), (int)Math.Round(16 * Utils.DpiScalingFactor));

        internal ConnectionsForm(TinyWallController ctrl)
        {
            InitializeComponent();
            this.IconList.ImageSize = IconSize;
            this.Icon = Resources.Icons.firewall;
            this.Controller = ctrl;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private string GetPathFromPidCached(Dictionary<int, string> cache, int pid, string path = null)
        {
            if (cache.ContainsKey(pid))
                return cache[pid];
            else
            {
                string ret = null;
                if (path == null)
                    ret = Utils.GetPathOfProcessUseTwService(pid, GlobalInstances.Controller);
                else
                    ret = path;

                cache.Add(pid, ret);
                return ret;
            }
        }

        private void UpdateList()
        {
            List<ListViewItem> itemColl = new List<ListViewItem>();
            Dictionary<int, string> procCache = new Dictionary<int, string>();

            Future<TwMessage> fwLogRequest = GlobalInstances.Controller.BeginReadFwLog();

            // Retrieve IP tables while waiting for log entries

            DateTime now = DateTime.Now;
            TcpTable tcpTable = NetStat.GetExtendedTcp4Table(false);
            foreach (TcpRow tcpRow in tcpTable)
            {
                if ((chkShowListen.Checked && (tcpRow.State == TcpState.Listen))
                  || (chkShowActive.Checked && (tcpRow.State != TcpState.Listen)))
                {
                    string path = GetPathFromPidCached(procCache, tcpRow.ProcessId);
                    ConstructListItem(itemColl, path, tcpRow.ProcessId, "TCP", tcpRow.LocalEndPoint, tcpRow.RemoteEndPoint, tcpRow.State.ToString(), now);
                }
            }
            tcpTable = NetStat.GetExtendedTcp6Table(false);
            foreach (TcpRow tcpRow in tcpTable)
            {
                if ((chkShowListen.Checked && (tcpRow.State == TcpState.Listen))
                 || (chkShowActive.Checked && (tcpRow.State != TcpState.Listen)))
                {
                    string path = GetPathFromPidCached(procCache, tcpRow.ProcessId);
                    ConstructListItem(itemColl, path, tcpRow.ProcessId, "TCP", tcpRow.LocalEndPoint, tcpRow.RemoteEndPoint, tcpRow.State.ToString(), now);
                }
            }

            if (chkShowListen.Checked)
            {
                IPEndPoint dummyEP = new IPEndPoint(0, 0);
                UdpTable udpTable = NetStat.GetExtendedUdp4Table(false);
                foreach (UdpRow udpRow in udpTable)
                {
                    string path = GetPathFromPidCached(procCache, udpRow.ProcessId);
                    ConstructListItem(itemColl, path, udpRow.ProcessId, "UDP", udpRow.LocalEndPoint, dummyEP, "Listen", now);
                }
                udpTable = NetStat.GetExtendedUdp6Table(false);
                foreach (UdpRow udpRow in udpTable)
                {
                    string path = GetPathFromPidCached(procCache, udpRow.ProcessId);
                    ConstructListItem(itemColl, path, udpRow.ProcessId, "UDP", udpRow.LocalEndPoint, dummyEP, "Listen", now);
                }
            }

            // Finished reading tables, continues with log processing

            // Remove log entries older than 2 minutes
            now = DateTime.Now;
            TimeSpan refSpan = TimeSpan.FromMinutes(5);
            for (int i = FwLogEntries.Count - 1; i >= 0; --i)
            {
                TimeSpan span = now - FwLogEntries[i].Timestamp;
                if (span > refSpan)
                    FwLogEntries.RemoveAt(i);
            }

            // Add new log entries
            List<FirewallLogEntry> fwLogEntry = GlobalInstances.Controller.EndReadFwLog(fwLogRequest);
            for (int i = 0; i < fwLogEntry.Count; ++i)
            {
                FirewallLogEntry newEntry = fwLogEntry[i];
                switch (newEntry.Event)
                {
                    case EventLogEvent.ALLOWED_LISTEN:
                    case EventLogEvent.ALLOWED_CONNECTION:
                    case EventLogEvent.ALLOWED_LOCAL_BIND:
                    case EventLogEvent.ALLOWED:
                        {
                            newEntry.Event = EventLogEvent.ALLOWED;
                            break;
                        }
                    case EventLogEvent.BLOCKED_LISTEN:
                    case EventLogEvent.BLOCKED_CONNECTION:
                    case EventLogEvent.BLOCKED_LOCAL_BIND:
                    case EventLogEvent.BLOCKED_PACKET:
                    case EventLogEvent.BLOCKED:
                        {
                            bool matchFound = false;
                            newEntry.Event = EventLogEvent.BLOCKED;

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
                            break;
                        }
                }
            }

            // Show log entries if requested by user
            if (chkShowBlocked.Checked)
            {
                for (int i = 0; i < FwLogEntries.Count; ++i)
                {
                    FirewallLogEntry entry = FwLogEntries[i];
                    ConstructListItem(itemColl, entry.AppPath, (int)entry.ProcessID, entry.Protocol.ToString(), new IPEndPoint(IPAddress.Parse(entry.SourceIP), entry.SourcePort), new IPEndPoint(IPAddress.Parse(entry.DestinationIP), entry.DestinationPort), "Blocked", entry.Timestamp, entry.Direction);
                }
            }

            // Add items to list
            list.SuspendLayout();
            list.Items.Clear();
            list.Items.AddRange(itemColl.ToArray());
            list.ResumeLayout(false);
        }

        private void ConstructListItem(List<ListViewItem> itemColl, string appPath, int procId, string protocol, IPEndPoint localEP, IPEndPoint remoteEP, string state, DateTime ts, RuleDirection dir = RuleDirection.Invalid)
        {
            try
            {

                // Construct list item
                string name = System.IO.Path.GetFileName(appPath);
                string title = (procId != 0) ? $"{name} ({procId})" : name;
                ListViewItem li = new ListViewItem(title);
                li.Tag = procId;
                li.ToolTipText = appPath;

                if (System.IO.Path.IsPathRooted(appPath) && System.IO.File.Exists(appPath))
                {
                    if (!IconList.Images.ContainsKey(appPath))
                    {
                        // Get icon
                        IconList.Images.Add(appPath, Utils.GetIconContained(appPath, IconSize.Width, IconSize.Height));
                    }
                    li.ImageKey = appPath;
                }

                li.SubItems.Add(protocol);
                li.SubItems.Add(localEP.Port.ToString(CultureInfo.InvariantCulture).PadLeft(5));
                li.SubItems.Add(localEP.Address.ToString());
                li.SubItems.Add(remoteEP.Port.ToString(CultureInfo.InvariantCulture).PadLeft(5));
                li.SubItems.Add(remoteEP.Address.ToString());
                li.SubItems.Add(state);
                switch (dir)
                {
                    case RuleDirection.In:
                        li.SubItems.Add(PKSoft.Resources.Messages.TrafficIn);
                        break;
                    case RuleDirection.Out:
                        li.SubItems.Add(PKSoft.Resources.Messages.TrafficOut);
                        break;
                    default:
                        li.SubItems.Add(string.Empty);
                        break;
                }
                li.SubItems.Add(ts.ToString(CultureInfo.InvariantCulture));
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
            ActiveConfig.Controller.ConnFormWindowState = this.WindowState;
            if (this.WindowState == FormWindowState.Normal)
            {
                ActiveConfig.Controller.ConnFormWindowSize = this.Size;
                ActiveConfig.Controller.ConnFormWindowLoc = this.Location;
            }
            else
            {
                ActiveConfig.Controller.ConnFormWindowSize = this.RestoreBounds.Size;
                ActiveConfig.Controller.ConnFormWindowLoc = this.RestoreBounds.Location;
            }

            ActiveConfig.Controller.ConnFormShowConnections = this.chkShowActive.Checked;
            ActiveConfig.Controller.ConnFormShowOpenPorts = this.chkShowListen.Checked;
            ActiveConfig.Controller.ConnFormShowBlocked = this.chkShowBlocked.Checked;

            ActiveConfig.Controller.Save();
        }

        private void ConnectionsForm_Load(object sender, EventArgs e)
        {
            list.ListViewItemSorter = new ListViewItemComparer(0);
            this.Size = ActiveConfig.Controller.ConnFormWindowSize;
            this.Location = ActiveConfig.Controller.ConnFormWindowLoc;
            this.WindowState = ActiveConfig.Controller.ConnFormWindowState;
            this.chkShowActive.Checked = ActiveConfig.Controller.ConnFormShowConnections;
            this.chkShowListen.Checked = ActiveConfig.Controller.ConnFormShowOpenPorts;
            this.chkShowBlocked.Checked = ActiveConfig.Controller.ConnFormShowBlocked;
            UpdateList();
        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (list.SelectedIndices.Count < 1)
                e.Cancel = true;

            // Don't allow Kill if we don't have a PID
            bool hasPid = true;
            foreach (ListViewItem li in list.SelectedItems)
            {
                hasPid &= ((int)li.Tag != 0);
            }
            mnuCloseProcess.Enabled = hasPid;
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
            List<FirewallExceptionV3> exceptions = new List<FirewallExceptionV3>();

            foreach (ListViewItem li in list.SelectedItems)
            {
                string path = li.ToolTipText;
                if (string.IsNullOrEmpty(path))
                    continue;

                // Check if we already have an exception for this file
                bool found = false;
                foreach (var ex in exceptions)
                {
                    if (ex.Subject is ExecutableSubject exe)
                    {
                        if (exe.ExecutablePath.Equals(path, StringComparison.InvariantCultureIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (found)
                    continue;

                // Try to recognize app based on this file
                ExecutableSubject subject = ExceptionSubject.Construct(path, null) as ExecutableSubject;
                exceptions.AddRange(GlobalInstances.AppDatabase.GetExceptionsForApp(subject, true, out DatabaseClasses.Application dummyApp));
            }

            Controller.AddExceptionList(exceptions);
        }

        private void mnuCopyRemoteAddress_Click(object sender, EventArgs e)
        {
            ListViewItem li = list.SelectedItems[0];
            string clipboardData = li.SubItems[5].Text;

            IDataObject dataObject = new DataObject();
            dataObject.SetData(DataFormats.UnicodeText, false, clipboardData);
            try
            {
                Clipboard.SetDataObject(dataObject, true, 20, 100);
            }
            catch
            {
                // Fail silently :(
            }
        }

        private void mnuVirusTotal_Click(object sender, EventArgs e)
        {
            try
            {
                ListViewItem li = list.SelectedItems[0];

                const string urlTemplate = @"https://www.virustotal.com/latest-scan/{0}";
                string hash = Hasher.HashFile(li.ToolTipText);
                string url = string.Format(CultureInfo.InvariantCulture, urlTemplate, hash);
                Utils.StartProcess(url, string.Empty, false);
            }
            catch
            {
                MessageBox.Show(this, PKSoft.Resources.Messages.CannotGetPathOfProcess, PKSoft.Resources.Messages.TinyWall, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            }
        }

        private void mnuProcessLibrary_Click(object sender, EventArgs e)
        {
            try
            {
                ListViewItem li = list.SelectedItems[0];

                const string urlTemplate = @"http://www.processlibrary.com/search/?q={0}";
                string filename = System.IO.Path.GetFileName(li.ToolTipText);
                string url = string.Format(CultureInfo.InvariantCulture, urlTemplate, filename);
                Utils.StartProcess(url, string.Empty, false);
            }
            catch
            {
                MessageBox.Show(this, PKSoft.Resources.Messages.CannotGetPathOfProcess, PKSoft.Resources.Messages.TinyWall, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        private void mnuFileNameOnTheWeb_Click(object sender, EventArgs e)
        {
            try
            {
                ListViewItem li = list.SelectedItems[0];

                const string urlTemplate = @"www.google.com/search?q={0}";
                string filename = System.IO.Path.GetFileName(li.ToolTipText);
                string url = string.Format(CultureInfo.InvariantCulture, urlTemplate, filename);
                Utils.StartProcess(url, string.Empty, false);
            }
            catch
            {
                MessageBox.Show(this, PKSoft.Resources.Messages.CannotGetPathOfProcess, PKSoft.Resources.Messages.TinyWall, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void mnuRemoteAddressOnTheWeb_Click(object sender, EventArgs e)
        {
            try
            {
                ListViewItem li = list.SelectedItems[0];

                const string urlTemplate = @"www.google.com/search?q={0}";
                string address = li.SubItems[5].Text;
                string url = string.Format(CultureInfo.InvariantCulture, urlTemplate, address);
                Utils.StartProcess(url, string.Empty, false);
            }
            catch
            {
                MessageBox.Show(this, PKSoft.Resources.Messages.CannotGetPathOfProcess, PKSoft.Resources.Messages.TinyWall, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}
