using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using PKSoft.netstat;

using TinyWall.Interface;
using TinyWall.Interface.Internal;

namespace PKSoft
{
    internal partial class ConnectionsForm : Form
    {
        private readonly TinyWallController Controller;
        private readonly Size IconSize = new Size((int)Math.Round(16 * Utils.DpiScalingFactor), (int)Math.Round(16 * Utils.DpiScalingFactor));

        internal ConnectionsForm(TinyWallController ctrl)
        {
            InitializeComponent();
            this.IconList.ImageSize = IconSize;
            this.Icon = Resources.Icons.firewall;
            this.Controller = ctrl;

            this.IconList.Images.Add("store", Resources.Icons.store);
            this.IconList.Images.Add("system", Resources.Icons.windows_small);
            this.IconList.Images.Add("network-drive", Resources.Icons.network_drive_small);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private string GetPathFromPidCached(Dictionary<uint, string> cache, uint pid, string path = null)
        {
            if (cache.ContainsKey(pid))
                return cache[pid];
            else
            {
                string ret = path ?? Utils.GetPathOfProcessUseTwService(pid, GlobalInstances.Controller);
                cache.Add(pid, ret);
                return ret;
            }
        }

        private void UpdateList()
        {
            Future<TwMessage> fwLogRequest = GlobalInstances.Controller.BeginReadFwLog();

            var uwpPackages = new UwpPackage();
            var itemColl = new List<ListViewItem>();
            var procCache = new Dictionary<uint, string>();
            var servicePids = new ServicePidMap();

            // Retrieve IP tables while waiting for log entries

            DateTime now = DateTime.Now;
            TcpTable tcpTable = NetStat.GetExtendedTcp4Table(false);
            foreach (TcpRow tcpRow in tcpTable)
            {
                if ((chkShowListen.Checked && (tcpRow.State == TcpState.Listen))
                  || (chkShowActive.Checked && (tcpRow.State != TcpState.Listen)))
                {
                    string path = GetPathFromPidCached(procCache, tcpRow.ProcessId);
                    ConstructListItem(itemColl, path, null, tcpRow.ProcessId, "TCP", tcpRow.LocalEndPoint, tcpRow.RemoteEndPoint, tcpRow.State.ToString(), now, RuleDirection.Invalid, uwpPackages, servicePids);
                }
            }
            tcpTable = NetStat.GetExtendedTcp6Table(false);
            foreach (TcpRow tcpRow in tcpTable)
            {
                if ((chkShowListen.Checked && (tcpRow.State == TcpState.Listen))
                 || (chkShowActive.Checked && (tcpRow.State != TcpState.Listen)))
                {
                    string path = GetPathFromPidCached(procCache, tcpRow.ProcessId);
                    ConstructListItem(itemColl, path, null, tcpRow.ProcessId, "TCP", tcpRow.LocalEndPoint, tcpRow.RemoteEndPoint, tcpRow.State.ToString(), now, RuleDirection.Invalid, uwpPackages, servicePids);
                }
            }

            if (chkShowListen.Checked)
            {
                IPEndPoint dummyEP = new IPEndPoint(0, 0);
                UdpTable udpTable = NetStat.GetExtendedUdp4Table(false);
                foreach (UdpRow udpRow in udpTable)
                {
                    string path = GetPathFromPidCached(procCache, udpRow.ProcessId);
                    ConstructListItem(itemColl, path, null, udpRow.ProcessId, "UDP", udpRow.LocalEndPoint, dummyEP, "Listen", now, RuleDirection.Invalid, uwpPackages, servicePids);
                }
                udpTable = NetStat.GetExtendedUdp6Table(false);
                foreach (UdpRow udpRow in udpTable)
                {
                    string path = GetPathFromPidCached(procCache, udpRow.ProcessId);
                    ConstructListItem(itemColl, path, null, udpRow.ProcessId, "UDP", udpRow.LocalEndPoint, dummyEP, "Listen", now, RuleDirection.Invalid, uwpPackages, servicePids);
                }
            }

            // Finished reading tables, continues with log processing
            List<FirewallLogEntry> fwLog = GlobalInstances.Controller.EndReadFwLog(fwLogRequest);

            // Show log entries if requested by user
            if (chkShowBlocked.Checked)
            {
                // Try to resolve PIDs heuristically
                var ProcessPathInfoMap = new Dictionary<string, List<ProcessManager.ExtendedProcessEntry>>();
                foreach (var p in ProcessManager.CreateToolhelp32SnapshotExtended())
                {
                    if (string.IsNullOrEmpty(p.ImagePath))
                        continue;

                    var key = p.ImagePath.ToLowerInvariant();
                    if (!ProcessPathInfoMap.ContainsKey(key))
                        ProcessPathInfoMap.Add(key, new List<ProcessManager.ExtendedProcessEntry>());
                    ProcessPathInfoMap[key].Add(p);
                }

                foreach (var e in fwLog)
                {
                    var key = e.AppPath.ToLowerInvariant();
                    if (!ProcessPathInfoMap.ContainsKey(key))
                        continue;

                    var p = ProcessPathInfoMap[key];
                    if ((p.Count == 1) && (p[0].CreationTime < e.Timestamp.ToFileTime()))
                        e.ProcessId = p[0].BaseEntry.th32ProcessID;
                }

                List<FirewallLogEntry> filteredLog = new List<FirewallLogEntry>();
                TimeSpan refSpan = TimeSpan.FromMinutes(5);
                for (int i = 0; i < fwLog.Count; ++i)
                {
                    FirewallLogEntry newEntry = fwLog[i];

                    // Ignore log entries older than refSpan
                    TimeSpan span = now - newEntry.Timestamp;
                    if (span > refSpan)
                        continue;

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

                                for (int j = 0; j < filteredLog.Count; ++j)
                                {
                                    FirewallLogEntry oldEntry = filteredLog[j];
                                    if (oldEntry.Equals(newEntry, false))
                                    {
                                        matchFound = true;
                                        oldEntry.Timestamp = newEntry.Timestamp;
                                        break;
                                    }
                                }

                                if (!matchFound)
                                    filteredLog.Add(newEntry);
                                break;
                            }
                    }
                }

                for (int i = 0; i < filteredLog.Count; ++i)
                {
                    FirewallLogEntry entry = filteredLog[i];
                    entry.AppPath = TinyWall.Interface.Internal.Utils.GetExactPath(entry.AppPath);   // correct path capitalization
                    ConstructListItem(itemColl, entry.AppPath, entry.PackageId, entry.ProcessId, entry.Protocol.ToString(), new IPEndPoint(IPAddress.Parse(entry.LocalIp), entry.LocalPort), new IPEndPoint(IPAddress.Parse(entry.RemoteIp), entry.RemotePort), "Blocked", entry.Timestamp, entry.Direction, uwpPackages, servicePids);
                }
            }

            // Add items to list
            list.BeginUpdate();
            list.Items.Clear();
            list.Items.AddRange(itemColl.ToArray());
            list.EndUpdate();
        }

        private void ConstructListItem(List<ListViewItem> itemColl, string appPath, string packageId, uint procId, string protocol, IPEndPoint localEP, IPEndPoint remoteEP, string state, DateTime ts, RuleDirection dir, UwpPackage uwpList, ServicePidMap servicePidMap)
        {
            try
            {
                ProcessInfo e = new ProcessInfo(procId)
                {
                    ExePath = appPath,
                    Package = uwpList.FindPackage(packageId),
                    Services = servicePidMap.GetServicesInPid(procId)
                };

                // Construct list item
                string name = e.Package.HasValue ? e.Package.Value.Name : System.IO.Path.GetFileName(appPath);
                string title = (procId != 0) ? $"{name} ({procId})" : $"{name}";
                ListViewItem li = new ListViewItem(title);
                li.Tag = e;
                li.ToolTipText = appPath;

                // Add icon
                if (e.Package.HasValue)
                {
                    li.ImageKey = "store";
                }
                else if (appPath == "System")
                {
                    li.ImageKey = "system";
                }
                else if (NetworkPath.IsNetworkPath(appPath))
                {
                    li.ImageKey = "network-drive";
                }
                else if (System.IO.Path.IsPathRooted(appPath) && System.IO.File.Exists(appPath))
                {
                    if (!IconList.Images.ContainsKey(appPath))
                    {
                        // Get icon
                        IconList.Images.Add(appPath, Utils.GetIconContained(appPath, IconSize.Width, IconSize.Height));
                    }
                    li.ImageKey = appPath;
                }

                if (e.Pid == 0)
                    li.SubItems.Add(string.Empty);
                else
                    li.SubItems.Add(string.Join(", ", e.Services.ToArray()));
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
                li.SubItems.Add(ts.ToString("yyyy/MM/dd HH:mm:ss"));
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

            ActiveConfig.Controller.ConnFormColumnWidths.Clear();
            foreach (ColumnHeader col in list.Columns)
                ActiveConfig.Controller.ConnFormColumnWidths.Add(col.Tag as string, col.Width);

            ActiveConfig.Controller.Save();
        }

        private void ConnectionsForm_Load(object sender, EventArgs e)
        {
            Utils.SetDoubleBuffering(list, true);
            list.ListViewItemSorter = new ListViewItemComparer(8, null, false);
            if (ActiveConfig.Controller.ConnFormWindowSize.Width != 0)
                this.Size = ActiveConfig.Controller.ConnFormWindowSize;
            if (ActiveConfig.Controller.ConnFormWindowLoc.X != 0)
            {
                this.Location = ActiveConfig.Controller.ConnFormWindowLoc;
                Utils.FixupFormPosition(this);
            }
            this.WindowState = ActiveConfig.Controller.ConnFormWindowState;
            this.chkShowActive.Checked = ActiveConfig.Controller.ConnFormShowConnections;
            this.chkShowListen.Checked = ActiveConfig.Controller.ConnFormShowOpenPorts;
            this.chkShowBlocked.Checked = ActiveConfig.Controller.ConnFormShowBlocked;

            foreach (ColumnHeader col in list.Columns)
            {
                if (ActiveConfig.Controller.ConnFormColumnWidths.TryGetValue(col.Tag as string, out int width))
                    col.Width = width;
            }

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
                hasPid &= (li.Tag as ProcessInfo).Pid != 0;
            }
            mnuCloseProcess.Enabled = hasPid;
        }

        private void mnuCloseProcess_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem li in list.SelectedItems)
            {
                ProcessInfo pi = li.Tag as ProcessInfo;

                try
                {
                    using (Process proc = Process.GetProcessById(unchecked((int)pi.Pid)))
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
                            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.CouldNotCloseProcess, proc.ProcessName, pi.Pid), PKSoft.Resources.Messages.TinyWall, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
            if (!Controller.EnsureUnlockedServer())
                return;

            var selection = new List<ProcessInfo>();
            foreach (ListViewItem li in list.SelectedItems)
            {
                selection.Add(li.Tag as ProcessInfo);
            }
            Controller.WhitelistProcesses(selection);
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
                string hash = Hasher.HashFile((li.Tag as ProcessInfo).ExePath);
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
                string filename = System.IO.Path.GetFileName((li.Tag as ProcessInfo).ExePath);
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
                string filename = System.IO.Path.GetFileName((li.Tag as ProcessInfo).ExePath);
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

        private void ConnectionsForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.F5)
            {
                btnRefresh_Click(btnRefresh, null);
                e.Handled = true;
            }
        }
    }
}
