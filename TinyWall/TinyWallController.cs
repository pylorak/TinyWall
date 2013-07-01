using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
using System.Management;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Samples;


namespace PKSoft
{
    internal sealed class TinyWallController : ApplicationContext, IMessageFilter
    {
        #region Vom Windows Form-Designer generierter Code

        private System.ComponentModel.IContainer components = new System.ComponentModel.Container();

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TinyWallController));
            this.Tray = new System.Windows.Forms.NotifyIcon(this.components);
            this.TrayMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuTrafficRate = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuMode = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuModeNormal = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuModeBlockAll = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuModeAllowOutgoing = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuModeDisabled = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuModeLearn = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuManage = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuConnections = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuLock = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuElevate = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuAllowLocalSubnet = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuEnableHostsBlocklist = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuWhitelistByExecutable = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuWhitelistByProcess = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuWhitelistByWindow = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuQuit = new System.Windows.Forms.ToolStripMenuItem();
            this.ofd = new System.Windows.Forms.OpenFileDialog();
            this.TrayMenu.SuspendLayout();
            // 
            // Tray
            // 
            resources.ApplyResources(this.Tray, "Tray");
            this.Tray.Icon = global::PKSoft.Resources.Icons.firewall;
            this.Tray.Visible = false;
            this.Tray.BalloonTipClicked += new System.EventHandler(this.Tray_BalloonTipClicked);
            this.Tray.MouseClick += new System.Windows.Forms.MouseEventHandler(this.Tray_MouseClick);
            // 
            // TrayMenu
            // 
            this.TrayMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuTrafficRate,
            this.toolStripMenuItem1,
            this.mnuMode,
            this.mnuManage,
            this.mnuConnections,
            this.mnuLock,
            this.mnuElevate,
            this.toolStripMenuItem2,
            this.mnuAllowLocalSubnet,
            this.mnuEnableHostsBlocklist,
            this.toolStripMenuItem3,
            this.mnuWhitelistByExecutable,
            this.mnuWhitelistByProcess,
            this.mnuWhitelistByWindow,
            this.toolStripMenuItem5,
            this.mnuQuit});
            this.TrayMenu.Name = "TrayMenu";
            resources.ApplyResources(this.TrayMenu, "TrayMenu");
            this.TrayMenu.Opening += new System.ComponentModel.CancelEventHandler(this.TrayMenu_Opening);
            // 
            // mnuTrafficRate
            // 
            this.mnuTrafficRate.AccessibleRole = System.Windows.Forms.AccessibleRole.StaticText;
            this.mnuTrafficRate.Image = global::PKSoft.Resources.Icons.info;
            this.mnuTrafficRate.Name = "mnuTrafficRate";
            resources.ApplyResources(this.mnuTrafficRate, "mnuTrafficRate");
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            resources.ApplyResources(this.toolStripMenuItem1, "toolStripMenuItem1");
            // 
            // mnuMode
            // 
            this.mnuMode.AccessibleRole = System.Windows.Forms.AccessibleRole.ButtonMenu;
            this.mnuMode.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuModeNormal,
            this.mnuModeBlockAll,
            this.mnuModeAllowOutgoing,
            this.mnuModeDisabled,
            this.mnuModeLearn});
            this.mnuMode.Name = "mnuMode";
            resources.ApplyResources(this.mnuMode, "mnuMode");
            // 
            // mnuModeNormal
            // 
            this.mnuModeNormal.Name = "mnuModeNormal";
            resources.ApplyResources(this.mnuModeNormal, "mnuModeNormal");
            this.mnuModeNormal.Click += new System.EventHandler(this.mnuModeNormal_Click);
            // 
            // mnuModeBlockAll
            // 
            this.mnuModeBlockAll.Name = "mnuModeBlockAll";
            resources.ApplyResources(this.mnuModeBlockAll, "mnuModeBlockAll");
            this.mnuModeBlockAll.Click += new System.EventHandler(this.mnuModeBlockAll_Click);
            // 
            // mnuModeAllowOutgoing
            // 
            this.mnuModeAllowOutgoing.Name = "mnuModeAllowOutgoing";
            resources.ApplyResources(this.mnuModeAllowOutgoing, "mnuModeAllowOutgoing");
            this.mnuModeAllowOutgoing.Click += new System.EventHandler(this.mnuAllowOutgoing_Click);
            // 
            // mnuModeDisabled
            // 
            this.mnuModeDisabled.Name = "mnuModeDisabled";
            resources.ApplyResources(this.mnuModeDisabled, "mnuModeDisabled");
            this.mnuModeDisabled.Click += new System.EventHandler(this.mnuModeDisabled_Click);
            // 
            // mnuModeLearn
            // 
            this.mnuModeLearn.Name = "mnuModeLearn";
            resources.ApplyResources(this.mnuModeLearn, "mnuModeLearn");
            this.mnuModeLearn.Click += new System.EventHandler(this.mnuModeLearn_Click);
            // 
            // mnuManage
            // 
            this.mnuManage.Image = global::PKSoft.Resources.Icons.manage;
            this.mnuManage.Name = "mnuManage";
            resources.ApplyResources(this.mnuManage, "mnuManage");
            this.mnuManage.Click += new System.EventHandler(this.mnuManage_Click);
            // 
            // mnuConnections
            // 
            this.mnuConnections.Image = global::PKSoft.Resources.Icons.connections;
            this.mnuConnections.Name = "mnuConnections";
            resources.ApplyResources(this.mnuConnections, "mnuConnections");
            this.mnuConnections.Click += new System.EventHandler(this.mnuConnections_Click);
            // 
            // mnuLock
            // 
            this.mnuLock.Image = global::PKSoft.Resources.Icons.lock_small;
            this.mnuLock.Name = "mnuLock";
            resources.ApplyResources(this.mnuLock, "mnuLock");
            this.mnuLock.Click += new System.EventHandler(this.mnuLock_Click);
            // 
            // mnuElevate
            // 
            this.mnuElevate.Image = global::PKSoft.Resources.Icons.w7uacshield;
            this.mnuElevate.Name = "mnuElevate";
            resources.ApplyResources(this.mnuElevate, "mnuElevate");
            this.mnuElevate.Click += new System.EventHandler(this.mnuElevate_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            resources.ApplyResources(this.toolStripMenuItem2, "toolStripMenuItem2");
            // 
            // mnuAllowLocalSubnet
            // 
            this.mnuAllowLocalSubnet.Name = "mnuAllowLocalSubnet";
            resources.ApplyResources(this.mnuAllowLocalSubnet, "mnuAllowLocalSubnet");
            this.mnuAllowLocalSubnet.Click += new System.EventHandler(this.mnuAllowLocalSubnet_Click);
            // 
            // mnuEnableHostsBlocklist
            // 
            this.mnuEnableHostsBlocklist.Name = "mnuEnableHostsBlocklist";
            resources.ApplyResources(this.mnuEnableHostsBlocklist, "mnuEnableHostsBlocklist");
            this.mnuEnableHostsBlocklist.Click += new System.EventHandler(this.mnuEnableHostsBlocklist_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            resources.ApplyResources(this.toolStripMenuItem3, "toolStripMenuItem3");
            // 
            // mnuWhitelistByExecutable
            // 
            this.mnuWhitelistByExecutable.Image = global::PKSoft.Resources.Icons.executable;
            this.mnuWhitelistByExecutable.Name = "mnuWhitelistByExecutable";
            resources.ApplyResources(this.mnuWhitelistByExecutable, "mnuWhitelistByExecutable");
            this.mnuWhitelistByExecutable.Click += new System.EventHandler(this.mnuWhitelistByExecutable_Click);
            // 
            // mnuWhitelistByProcess
            // 
            this.mnuWhitelistByProcess.Image = global::PKSoft.Resources.Icons.process;
            this.mnuWhitelistByProcess.Name = "mnuWhitelistByProcess";
            resources.ApplyResources(this.mnuWhitelistByProcess, "mnuWhitelistByProcess");
            this.mnuWhitelistByProcess.Click += new System.EventHandler(this.mnuWhitelistByProcess_Click);
            // 
            // mnuWhitelistByWindow
            // 
            this.mnuWhitelistByWindow.Image = global::PKSoft.Resources.Icons.window;
            this.mnuWhitelistByWindow.Name = "mnuWhitelistByWindow";
            resources.ApplyResources(this.mnuWhitelistByWindow, "mnuWhitelistByWindow");
            this.mnuWhitelistByWindow.Click += new System.EventHandler(this.mnuWhitelistByWindow_Click);
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            resources.ApplyResources(this.toolStripMenuItem5, "toolStripMenuItem5");
            // 
            // mnuQuit
            // 
            this.mnuQuit.Image = global::PKSoft.Resources.Icons.exit;
            this.mnuQuit.Name = "mnuQuit";
            resources.ApplyResources(this.mnuQuit, "mnuQuit");
            this.mnuQuit.Click += new System.EventHandler(this.mnuQuit_Click);
            // 
            // ofd
            // 
            resources.ApplyResources(this.ofd, "ofd");
            this.TrayMenu.ResumeLayout(false);
        }

        private System.Windows.Forms.NotifyIcon Tray;
        private System.Windows.Forms.ContextMenuStrip TrayMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem mnuQuit;
        private System.Windows.Forms.ToolStripMenuItem mnuMode;
        private System.Windows.Forms.ToolStripMenuItem mnuModeNormal;
        private System.Windows.Forms.ToolStripMenuItem mnuModeBlockAll;
        private System.Windows.Forms.ToolStripMenuItem mnuModeDisabled;
        private System.Windows.Forms.ToolStripMenuItem mnuManage;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem5;
        private System.Windows.Forms.ToolStripMenuItem mnuWhitelistByExecutable;
        private System.Windows.Forms.ToolStripMenuItem mnuWhitelistByProcess;
        private System.Windows.Forms.ToolStripMenuItem mnuWhitelistByWindow;
        private System.Windows.Forms.ToolStripMenuItem mnuLock;
        private System.Windows.Forms.ToolStripMenuItem mnuElevate;
        private System.Windows.Forms.ToolStripMenuItem mnuConnections;
        private System.Windows.Forms.ToolStripMenuItem mnuModeAllowOutgoing;
        private System.Windows.Forms.OpenFileDialog ofd;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem mnuAllowLocalSubnet;
        private System.Windows.Forms.ToolStripMenuItem mnuEnableHostsBlocklist;
        private System.Windows.Forms.ToolStripMenuItem mnuTrafficRate;
        private System.Windows.Forms.ToolStripMenuItem mnuModeLearn;

        #endregion

        private MouseInterceptor MouseInterceptor;
        private SettingsForm ShownSettings;
        private ServiceState FirewallState;
        private DateTime LastUpdateNotification = DateTime.MinValue;

        // Traffic rate monitoring
        private System.Threading.Timer TrafficTimer = null;
        private const int TRAFFIC_TIMER_INTERVAL = 2;
        private ulong bytesRxTotal = 0;
        private ulong bytesTxTotal = 0;
        private ulong WmiTsSys100Ns = 0;
        private string rxDisplay = string.Empty;
        private string txDisplay = string.Empty;

        private EventHandler<AnyEventArgs> BalloonClickedCallback;
        private object BalloonClickedCallbackArgument;
        private SynchronizationContext SyncCtx;

        private Hotkey HotKeyWhitelistExecutable;
        private Hotkey HotKeyWhitelistProcess;
        private Hotkey HotKeyWhitelistWindow;

        private CmdLineArgs StartupOpts;

        private bool m_Locked;
        private bool Locked
        {
            get { return m_Locked; }
            set
            {
                m_Locked = value;
                if (m_Locked)
                {
                    mnuLock.Text = PKSoft.Resources.Messages.Unlock;
                }
                else
                {
                    mnuLock.Text = PKSoft.Resources.Messages.Lock;
                }
            }
        }

        public TinyWallController(CmdLineArgs opts)
        {
            this.StartupOpts = opts;

            ActiveConfig.Controller = ControllerSettings.Load();
            try
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(ActiveConfig.Controller.Language);
                System.Windows.Forms.Application.CurrentCulture = Thread.CurrentThread.CurrentUICulture;
            }
            catch { }

            System.Windows.Forms.Application.AddMessageFilter(this);
            InitializeComponent();
            InitController();

            Tray.ContextMenuStrip = TrayMenu;
            Tray.Visible = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                // Manually added
                if (HotKeyWhitelistExecutable != null) HotKeyWhitelistExecutable.Dispose();
                if (HotKeyWhitelistProcess != null) HotKeyWhitelistProcess.Dispose();
                if (HotKeyWhitelistWindow != null) HotKeyWhitelistWindow.Dispose();
                if (MouseInterceptor != null) MouseInterceptor.Dispose();

                using (WaitHandle wh = new AutoResetEvent(false))
                {
                    TrafficTimer.Dispose(wh);
                    wh.WaitOne();
                }

                components.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void ExitThreadCore()
        {
            Tray.Visible = false; // should remove lingering tray icon!
            base.ExitThreadCore();
        }
        private void VerifyUpdates()
        {
            try
            {
                UpdateDescriptor descriptor = FirewallState.Update;
                if (descriptor != null)
                {
                    UpdateModule MainAppModule = UpdateChecker.GetMainAppModule(descriptor);
                    if (new Version(MainAppModule.ComponentVersion) > new Version(System.Windows.Forms.Application.ProductVersion))
                    {
                        Utils.Invoke(SyncCtx, (SendOrPostCallback)delegate(object o)
                        {
                            string prompt = string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.UpdateAvailableBubble, MainAppModule.ComponentVersion);
                            ShowBalloonTip(prompt, ToolTipIcon.Info, 5000, StartUpdate, MainAppModule.UpdateURL);
                        });
                    }
                }
            }
            catch
            {
                // This is an automatic update check in the background.
                // If we fail (for whatever reason, no internet, server down etc.),
                // we fail silently.
            }
        }

        private void TrafficTimerTick(object state)
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"select BytesReceivedPersec, BytesSentPersec, Timestamp_Sys100NS from Win32_PerfRawData_Tcpip_NetworkInterface"))
                {
                    ulong bytesRxNewTotal = 0;
                    ulong bytesTxNewTotal = 0;
                    ulong newWmiTsSys100Ns = 0;
                    ManagementObjectCollection moc = searcher.Get();
                    foreach (ManagementObject adapterObject in moc)
                    {
                        bytesRxNewTotal += (ulong)adapterObject["BytesReceivedPersec"];
                        bytesTxNewTotal += (ulong)adapterObject["BytesSentPersec"];
                        newWmiTsSys100Ns = (ulong)adapterObject["Timestamp_Sys100NS"];
                    }

                    // If this is the first time we are running.
                    if ((bytesRxTotal == 0) && (bytesTxTotal == 0))
                    {
                        bytesRxTotal = bytesRxNewTotal;
                        bytesTxTotal = bytesTxNewTotal;
                    }

                    float timeDiff = (newWmiTsSys100Ns - WmiTsSys100Ns) / 10000000.0f;
                    float RxDiff = (bytesRxNewTotal - bytesRxTotal) / timeDiff;
                    float TxDiff = (bytesTxNewTotal - bytesTxTotal) / timeDiff;
                    bytesRxTotal = bytesRxNewTotal;
                    bytesTxTotal = bytesTxNewTotal;
                    WmiTsSys100Ns = newWmiTsSys100Ns;

                    float KBytesRxPerSec = RxDiff / 1024;
                    float KBytesTxPerSec = TxDiff / 1024;
                    float MBytesRxPerSec = KBytesRxPerSec / 1024;
                    float MBytesTxPerSec = KBytesTxPerSec / 1024;

                    if (MBytesRxPerSec > 1)
                        rxDisplay = string.Format(CultureInfo.CurrentCulture, "{0:f}MiB/s", MBytesRxPerSec);
                    else
                        rxDisplay = string.Format(CultureInfo.CurrentCulture, "{0:f}KiB/s", KBytesRxPerSec);

                    if (MBytesTxPerSec > 1)
                        txDisplay = string.Format(CultureInfo.CurrentCulture, "{0:f}MiB/s", MBytesTxPerSec);
                    else
                        txDisplay = string.Format(CultureInfo.CurrentCulture, "{0:f}KiB/s", KBytesTxPerSec);
                }
            }
            catch
            {
                // On some systems the WMI query fails. We disable traffic monitoring on those systems.
                mnuTrafficRate.Visible = false;
                toolStripMenuItem1.Visible = false;
                TrafficTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        private void StartUpdate(object sender, AnyEventArgs e)
        {
            Updater.StartUpdate();
        }

        void HotKeyWhitelistProcess_Pressed(object sender, HandledEventArgs e)
        {
            mnuWhitelistByProcess_Click(null, null);
        }

        void HotKeyWhitelistExecutable_Pressed(object sender, HandledEventArgs e)
        {
            mnuWhitelistByExecutable_Click(null, null);
        }

        void HotKeyWhitelistWindow_Pressed(object sender, HandledEventArgs e)
        {
            mnuWhitelistByWindow_Click(null, null);
        }

        private void mnuQuit_Click(object sender, EventArgs e)
        {
            ExitThread();
        }

        private void UpdateDisplay()
        {
            // TODO: Remove resource PKSoft.Resources.Messages.CurrentZone as it is not used any more.

            // Update UI based on current firewall mode
            string FirewallModeName = PKSoft.Resources.Messages.FirewallModeUnknown;
            switch (FirewallState.Mode)
            {
                case FirewallMode.Normal:
                    Tray.Icon = PKSoft.Resources.Icons.firewall;
                    mnuMode.Image = mnuModeNormal.Image;
                    FirewallModeName = PKSoft.Resources.Messages.FirewallModeNormal;
                    break;

                case FirewallMode.AllowOutgoing:
                    Tray.Icon = PKSoft.Resources.Icons.shield_red_small;
                    mnuMode.Image = mnuModeAllowOutgoing.Image;
                    FirewallModeName = PKSoft.Resources.Messages.FirewallModeAllowOut;
                    break;

                case FirewallMode.BlockAll:
                    Tray.Icon = PKSoft.Resources.Icons.shield_yellow_small;
                    mnuMode.Image = mnuModeBlockAll.Image;
                    FirewallModeName = PKSoft.Resources.Messages.FirewallModeBlockAll;
                    break;

                case FirewallMode.Disabled:
                    Tray.Icon = PKSoft.Resources.Icons.shield_grey_small;
                    mnuMode.Image = mnuModeDisabled.Image;
                    FirewallModeName = PKSoft.Resources.Messages.FirewallModeDisabled;
                    break;

                case FirewallMode.Learning:
                    Tray.Icon = PKSoft.Resources.Icons.shield_blue_small;
                    mnuMode.Image = mnuModeLearn.Image;
                    FirewallModeName = PKSoft.Resources.Messages.FirewallModeLearn;
                    break;

                case FirewallMode.Unknown:
                    Tray.Icon = PKSoft.Resources.Icons.shield_grey_small;
                    mnuMode.Image = PKSoft.Resources.Icons.shield_grey_small.ToBitmap();
                    FirewallModeName = PKSoft.Resources.Messages.FirewallModeUnknown;
                    break;
            }

            // TODO: Remove resource PKSoft.Resources.Messages.Zone as it is not used any more.
            Tray.Text = string.Format(CultureInfo.CurrentCulture, "TinyWall\r\n{0}: {1}",
                PKSoft.Resources.Messages.Mode, FirewallModeName);

            // Find out if we are locked and if we have a password
            this.Locked = FirewallState.Locked;

            // Do we have a passord at all?
            mnuLock.Visible = FirewallState.HasPassword;

            mnuAllowLocalSubnet.Checked = ActiveConfig.Service.AllowLocalSubnet;
            mnuEnableHostsBlocklist.Checked = ActiveConfig.Service.Blocklists.EnableBlocklists;
        }

        private void SetMode(FirewallMode mode)
        {
            TWControllerMessages opret = TWControllerMessages.RESPONSE_ERROR;
            Message req = new Message(TWControllerMessages.MODE_SWITCH, mode);
            Message resp = GlobalInstances.CommunicationMan.QueueMessage(req).GetResponse();

            string usermsg = string.Empty;
            switch (mode)
            {
                case FirewallMode.Normal:
                    usermsg = PKSoft.Resources.Messages.TheFirewallIsNowOperatingAsRecommended;
                    break;

                case FirewallMode.AllowOutgoing:
                    usermsg = PKSoft.Resources.Messages.TheFirewallIsNowAllowsOutgoingConnections;
                    break;

                case FirewallMode.BlockAll:
                    usermsg = PKSoft.Resources.Messages.TheFirewallIsNowBlockingAllInAndOut;
                    break;

                case FirewallMode.Disabled:
                    usermsg = PKSoft.Resources.Messages.TheFirewallIsNowDisabled;
                    break;

                case FirewallMode.Learning:
                    usermsg = PKSoft.Resources.Messages.TheFirewallIsNowLearning;
                    break;
            }

            switch (resp.Command)
            {
                case TWControllerMessages.RESPONSE_OK:
                    FirewallState.Mode = mode;
                    ShowBalloonTip(usermsg, ToolTipIcon.Info);
                    break;
                default:
                    DefaultPopups(opret);
                    break;
            }
        }

        private void mnuModeDisabled_Click(object sender, EventArgs e)
        {
            SetMode(FirewallMode.Disabled);
            UpdateDisplay();
        }

        private void mnuModeNormal_Click(object sender, EventArgs e)
        {
            SetMode(FirewallMode.Normal);
            UpdateDisplay();
        }

        private void mnuModeBlockAll_Click(object sender, EventArgs e)
        {
            SetMode(FirewallMode.BlockAll);
            UpdateDisplay();
        }

        private void mnuAllowOutgoing_Click(object sender, EventArgs e)
        {
            SetMode(FirewallMode.AllowOutgoing);
            UpdateDisplay();
        }

        // Returns true if the local copy of the settings have been updated.
        private bool LoadSettingsFromServer()
        {
            bool comError;
            return LoadSettingsFromServer(out comError, false);
        }

        // Returns true if the local copy of the settings have been updated.
        private bool LoadSettingsFromServer(out bool comError, bool force = false)
        {
            // Detect if server settings have changed in comparison to ours and download
            // settings only if we need them. Settings are "version numbered" using the "changeset"
            // property. We send our changeset number to the service and if it differs from his,
            // the service will send back the settings.

            bool SettingsUpdated = false;
            if (FirewallState == null)
                FirewallState = new ServiceState();

            int clientChangeset = (ActiveConfig.Service == null) ? -1 : ActiveConfig.Service.SequenceNumber;
            int serverChangeset = -2;
            Message req = new Message(TWControllerMessages.GET_SETTINGS, force ? int.MinValue : ActiveConfig.Service.SequenceNumber);
            Message resp = GlobalInstances.CommunicationMan.QueueMessage(req).GetResponse();
            comError = (resp.Command == TWControllerMessages.COM_ERROR);
            if (resp.Command == TWControllerMessages.RESPONSE_OK)
            {
                serverChangeset = (int)resp.Arguments[0];
                if (force || (serverChangeset != clientChangeset))
                {
                    ActiveConfig.Service = (ServiceSettings21)resp.Arguments[1];
                    FirewallState = (ServiceState)resp.Arguments[2];
                    SettingsUpdated = true;
                }
                else
                    SettingsUpdated = false;
            }
            else
            {
                ActiveConfig.Controller = new ControllerSettings();
                ActiveConfig.Service = new ServiceSettings21(true);
                FirewallState = new ServiceState();
                SettingsUpdated = true;
            }

            if (SettingsUpdated)
            {
                for (int i = 0; i < FirewallState.ClientNotifs.Count; ++i)
                {
                    switch (FirewallState.ClientNotifs[i])
                    {
                        case TWServiceMessages.DATABASE_UPDATED:
                            LoadDatabase();
                            break;
                    }
                }
                FirewallState.ClientNotifs.Clear();
                UpdateDisplay();
            }

            if (DateTime.Now - LastUpdateNotification > TimeSpan.FromHours(4))
            {
                ThreadPool.QueueUserWorkItem((WaitCallback)delegate(object state)
                {
                    VerifyUpdates();
                });
                LastUpdateNotification = DateTime.Now;
            }

            return SettingsUpdated;
        }

        private void TrayMenu_Opening(object sender, CancelEventArgs e)
        {
            e.Cancel = false;
            if (FirewallState.Mode == FirewallMode.Unknown)
            {
                if (!TinyWallDoctor.IsServiceRunning())
                {
                    ShowBalloonTip(PKSoft.Resources.Messages.TheTinyWallServiceIsUnavailable, ToolTipIcon.Error, 10000);
                    e.Cancel = true;
                }
            }

            mnuTrafficRate.Text = string.Format(CultureInfo.CurrentCulture, "{0}: {1}   {2}: {3}", PKSoft.Resources.Messages.TrafficIn, rxDisplay, PKSoft.Resources.Messages.TrafficOut, txDisplay);
        }

        private void mnuWhitelistByExecutable_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            FirewallException ex = new FirewallException(ofd.FileName, null);
            ex.ServiceName = string.Empty;

            Application app;
            AppExceptionAssoc appFile;
            ex.TryRecognizeApp(true, out app, out appFile);
            if (ActiveConfig.Controller.AskForExceptionDetails)
            {
                using (ApplicationExceptionForm f = new ApplicationExceptionForm(ex))
                {
                    if (f.ShowDialog() == DialogResult.Cancel)
                        return;

                    ex = f.ExceptionSettings;
                }
            }

            AddNewException(ex, appFile);
        }

        private void mnuWhitelistByProcess_Click(object sender, EventArgs e)
        {
            FirewallException ex = ProcessesForm.ChooseProcess();
            if (ex == null) return;

            Application app;
            AppExceptionAssoc appFile;
            ex.TryRecognizeApp(true, out app, out appFile);
            if (ActiveConfig.Controller.AskForExceptionDetails)
            {
                using (ApplicationExceptionForm f = new ApplicationExceptionForm(ex))
                {
                    if (f.ShowDialog() == DialogResult.Cancel)
                        return;

                    ex = f.ExceptionSettings;
                }
            }

            AddNewException(ex, appFile);
        }

        internal TWControllerMessages ApplyFirewallSettings(ServiceSettings21 srvConfig, bool showUI = true)
        {
            Message req = new Message(TWControllerMessages.PUT_SETTINGS, srvConfig);
            Message resp = GlobalInstances.CommunicationMan.QueueMessage(req).GetResponse();

            switch (resp.Command)
            {
                case TWControllerMessages.RESPONSE_OK:
                    if (showUI)
                        ShowBalloonTip(PKSoft.Resources.Messages.TheFirewallSettingsHaveBeenUpdated, ToolTipIcon.Info);
                    ActiveConfig.Service = srvConfig;
                    ActiveConfig.Service.SequenceNumber = (int)resp.Arguments[0];
                    break;
                case TWControllerMessages.RESPONSE_ERROR:
                    ActiveConfig.Service = (ServiceSettings21)resp.Arguments[0];
                    FirewallState = (ServiceState)resp.Arguments[1];

                    // We tell the user to re-do his changes to the settings to prevent overwriting the wrong configuration.
                    if (showUI)
                        ShowBalloonTip(PKSoft.Resources.Messages.SettingHaveChangedRetry, ToolTipIcon.Warning);
                    break;
                default:
                    if (showUI)
                        DefaultPopups(resp.Command);
                    LoadSettingsFromServer();
                    break;
            }

            return resp.Command;
        }

        private void DefaultPopups(TWControllerMessages op)
        {
            switch (op)
            {
                case TWControllerMessages.RESPONSE_OK:
                    ShowBalloonTip(PKSoft.Resources.Messages.Success, ToolTipIcon.Info);
                    break;
                case TWControllerMessages.RESPONSE_WARNING:
                    ShowBalloonTip(PKSoft.Resources.Messages.OtherSettingsPreventEffect, ToolTipIcon.Warning);
                    break;
                case TWControllerMessages.RESPONSE_ERROR:
                    ShowBalloonTip(PKSoft.Resources.Messages.OperationFailed, ToolTipIcon.Error);
                    break;
                case TWControllerMessages.RESPONSE_LOCKED:
                    ShowBalloonTip(PKSoft.Resources.Messages.TinyWallIsCurrentlyLocked, ToolTipIcon.Warning);
                    Locked = true;
                    break;
                case TWControllerMessages.COM_ERROR:
                default:
                    ShowBalloonTip(PKSoft.Resources.Messages.CommunicationWithTheServiceError, ToolTipIcon.Error);
                    break;
            }
        }

        private void mnuManage_Click(object sender, EventArgs e)
        {
            if (Locked)
            {
                DefaultPopups(TWControllerMessages.RESPONSE_LOCKED);
                return;
            }

            // If the settings form is already visible, do not load it but bring it to the foreground
            if (this.ShownSettings != null)
            {
                this.ShownSettings.Activate();
                this.ShownSettings.BringToFront();
                return;
            }

            try
            {
                LoadSettingsFromServer();

                using (this.ShownSettings = new SettingsForm(ActiveConfig.ToContainer()))
                {
                    SettingsForm sf = this.ShownSettings;

                    if (sf.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        // Handle password change request
                        string passwd = sf.NewPassword;
                        if (passwd != null)
                        {
                            // Set the password. If the operation is successfull, do not report anything as we will be setting 
                            // the other settings too and we want to avoid multiple popups.
                            Message req = new Message(TWControllerMessages.SET_PASSPHRASE, Hasher.HashString(passwd));
                            Message resp = GlobalInstances.CommunicationMan.QueueMessage(req).GetResponse();
                            if (resp.Command != TWControllerMessages.RESPONSE_OK)  // Only display a popup for setting the password if it did not succeed
                            {
                                DefaultPopups(resp.Command);
                                return;
                            }
                            else
                            {
                                FirewallState.HasPassword = !string.IsNullOrEmpty(passwd);
                            }
                        }

                        // Save settings
                        ActiveConfig.FromContainer(sf.TmpConfig);
                        ActiveConfig.Controller.Save();
                        ApplyFirewallSettings(ActiveConfig.Service);
                    }
                }
            }
            finally
            {
                this.ShownSettings = null;
                ApplyControllerSettings();
                UpdateDisplay();
            }
        }

        private void mnuWhitelistByWindow_Click(object sender, EventArgs e)
        {
            if (MouseInterceptor == null)
            {
                MouseInterceptor = new MouseInterceptor();
                MouseInterceptor.MouseLButtonDown += new PKSoft.MouseInterceptor.MouseHookLButtonDown(MouseInterceptor_MouseLButtonDown);
                ShowBalloonTip(PKSoft.Resources.Messages.ClickOnAWindowWhitelisting, ToolTipIcon.Info);
            }
            else
            {
                MouseInterceptor.Dispose();
                MouseInterceptor = null;
                ShowBalloonTip(PKSoft.Resources.Messages.WhitelistingCancelled, ToolTipIcon.Info);
            }
        }

        internal void MouseInterceptor_MouseLButtonDown(int x, int y)
        {
            // So, this looks crazy, doesn't it?
            // Call a method in a parallel thread just so that it can be called
            // on this same thread again?
            //
            // The point is, the body will execute on this same thread *after* this procedure
            // has terminated. We want this procedure to terminate before
            // calling MouseInterceptor.Dispose() or else it will lock up our UI thread for a 
            // couple of seconds. It will lock up because we are currently running in a hook procedure,
            // and MouseInterceptor.Dispose() unhooks us while we are running.
            // This apparently brings Windows temporarily to its knees. Anyway, starting
            // another thread that will invoke the body on our own thread again makes sure that the hook
            // has terminated by the time we unhook it, resolving all our problems.

            ThreadPool.QueueUserWorkItem((WaitCallback)delegate(object state)
            {
                Utils.Invoke(SyncCtx, (SendOrPostCallback)delegate(object o)
                {
                    MouseInterceptor.Dispose();
                    MouseInterceptor = null;

                    string AppPath = Utils.GetExecutableUnderCursor(x, y);
                    if (string.IsNullOrEmpty(AppPath))
                    {
                        ShowBalloonTip(PKSoft.Resources.Messages.CannotGetExecutablePathWhitelisting, ToolTipIcon.Error);
                        return;
                    }

                    FirewallException ex = new FirewallException(AppPath, null);
                    Application app;
                    AppExceptionAssoc appFile;
                    ex.TryRecognizeApp(true, out app, out appFile);
                    if (ActiveConfig.Controller.AskForExceptionDetails)
                    {
                        using (ApplicationExceptionForm f = new ApplicationExceptionForm(ex))
                        {
                            if (f.ShowDialog() == DialogResult.Cancel)
                                return;

                            ex = f.ExceptionSettings;
                        }
                    }

                    AddNewException(ex, appFile);
                });
            });
        }

        private void EditRecentException(object sender, AnyEventArgs e)
        {
            GenericTuple<FirewallException, AppExceptionAssoc> tuple = e.Arg as GenericTuple<FirewallException, AppExceptionAssoc>;
            FirewallException ex = tuple.obj1;
            AppExceptionAssoc exFile = tuple.obj2;
            using (ApplicationExceptionForm f = new ApplicationExceptionForm(ex))
            {
                if (f.ShowDialog() == DialogResult.Cancel)
                    return;

                ex = f.ExceptionSettings;
            }

            AddNewException(ex, exFile);
        }

        private void AddNewException(FirewallException ex, AppExceptionAssoc exFile)
        {
            List<FirewallException> exceptions = FirewallException.CheckForAppDependencies(ex, true, true, true);
            if (exceptions.Count == 0)
                return;

            LoadSettingsFromServer();
            ActiveConfig.Service.AppExceptions.AddRange(exceptions);
            ActiveConfig.Service.Normalize();

            TWControllerMessages resp = ApplyFirewallSettings(ActiveConfig.Service, false);
            switch (resp)
            {
                case TWControllerMessages.RESPONSE_OK:
                    GenericTuple<FirewallException, AppExceptionAssoc> tuple = new GenericTuple<FirewallException, AppExceptionAssoc>(ex, exFile);
                    if ((exFile != null) && (!exFile.IsSigned || exFile.IsSignatureValid))
                        ShowBalloonTip(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.FirewallRulesForRecognizedChanged, ex.ExecutableName), ToolTipIcon.Info, 5000, EditRecentException, Utils.DeepClone(tuple));
                    else
                        ShowBalloonTip(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.FirewallRulesForUnrecognizedChanged, ex.ExecutableName), ToolTipIcon.Info, 5000, EditRecentException, Utils.DeepClone(tuple));

                    break;
                case TWControllerMessages.RESPONSE_ERROR:
                    // We tell the user to re-do his changes to the settings to prevent overwriting the wrong configuration.
                    ShowBalloonTip(PKSoft.Resources.Messages.SettingHaveChangedRetry, ToolTipIcon.Warning);
                    break;
                default:
                    DefaultPopups(resp);
                    LoadSettingsFromServer();
                    break;
            }
        }

        private void mnuLock_Click(object sender, EventArgs e)
        {
            if (Locked)
            {
                using (PasswordForm pf = new PasswordForm())
                {
                    pf.BringToFront();
                    pf.Activate();
                    if (pf.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        Message req = new Message(TWControllerMessages.UNLOCK, pf.PassHash);
                        Message resp = GlobalInstances.CommunicationMan.QueueMessage(req).GetResponse();
                        switch (resp.Command)
                        {
                            case TWControllerMessages.RESPONSE_OK:
                                this.Locked = false;
                                FirewallState.Locked = false;
                                ShowBalloonTip(PKSoft.Resources.Messages.TinyWallHasBeenUnlocked, ToolTipIcon.Info);
                                break;
                            case TWControllerMessages.RESPONSE_ERROR:
                                ShowBalloonTip(PKSoft.Resources.Messages.UnlockFailed, ToolTipIcon.Error);
                                break;
                            default:
                                DefaultPopups(resp.Command);
                                break;
                        }
                    }
                }
            }
            else
            {
                if (GlobalInstances.CommunicationMan.QueueMessageSimple(TWControllerMessages.LOCK).Command == TWControllerMessages.RESPONSE_OK)
                {
                    this.Locked = true;
                    FirewallState.Locked = true;
                }
            }

            UpdateDisplay();
        }

        private void mnuAllowLocalSubnet_Click(object sender, EventArgs e)
        {
            // Copy, so that settings are not changed if they cannot be saved
            ServiceSettings21 confCopy = Utils.DeepClone(ActiveConfig.Service);
            confCopy.AllowLocalSubnet = !mnuAllowLocalSubnet.Checked;
            ApplyFirewallSettings(confCopy);

            mnuAllowLocalSubnet.Checked = ActiveConfig.Service.AllowLocalSubnet;
        }

        private void mnuEnableHostsBlocklist_Click(object sender, EventArgs e)
        {
            // Copy, so that settings are not changed if they cannot be saved
            ServiceSettings21 confCopy = Utils.DeepClone(ActiveConfig.Service);
            confCopy.Blocklists.EnableBlocklists = !mnuEnableHostsBlocklist.Checked;
            ApplyFirewallSettings(confCopy);

            mnuEnableHostsBlocklist.Checked = ActiveConfig.Service.Blocklists.EnableBlocklists;
        }

        private void ShowBalloonTip(string msg, ToolTipIcon icon, int period_ms = 5000, EventHandler<AnyEventArgs> balloonClicked = null, object handlerArg = null)
        {
            BalloonClickedCallback = balloonClicked;
            BalloonClickedCallbackArgument = handlerArg;
            Tray.ShowBalloonTip(period_ms, ServiceSettings21.APP_NAME, msg, icon);
        }

        private void SetHotkey(System.ComponentModel.ComponentResourceManager resman, ref Hotkey hk, HandledEventHandler hkCallback, Keys keyCode, ToolStripMenuItem menu, string mnuName)
        {
            if (ActiveConfig.Controller.EnableGlobalHotkeys)
            {   // enable hotkey
                if (hk == null)
                {
                    hk = new Hotkey(keyCode, true, true, false, false);
                    hk.Pressed += hkCallback;
                    hk.Register();
                    resman.ApplyResources(menu, mnuName);
                }
            }
            else
            {   // disable hotkey
                if (hk != null)
                {
                    hk.Dispose();
                    hk = null;
                }
                menu.ShortcutKeyDisplayString = string.Empty;
            }
        }

        private void ApplyControllerSettings()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TinyWallController));
            SetHotkey(resources, ref HotKeyWhitelistWindow, new HandledEventHandler(HotKeyWhitelistWindow_Pressed), Keys.W, mnuWhitelistByWindow, "mnuWhitelistByWindow");
            SetHotkey(resources, ref HotKeyWhitelistExecutable, new HandledEventHandler(HotKeyWhitelistExecutable_Pressed), Keys.E, mnuWhitelistByExecutable, "mnuWhitelistByExecutable");
            SetHotkey(resources, ref HotKeyWhitelistProcess, new HandledEventHandler(HotKeyWhitelistProcess_Pressed), Keys.P, mnuWhitelistByProcess, "mnuWhitelistByProcess");
        }

        private void mnuElevate_Click(object sender, EventArgs e)
        {
            try
            {
                Utils.StartProcess(Utils.ExecutablePath, "/desktop", true);
                System.Windows.Forms.Application.Exit();
            }
            catch (Win32Exception)
            {
                ShowBalloonTip(PKSoft.Resources.Messages.CouldNotElevatePrivileges, ToolTipIcon.Error);
            }
        }

        private void mnuConnections_Click(object sender, EventArgs e)
        {
            using (ConnectionsForm cf = new ConnectionsForm(this))
            {
                cf.ShowDialog();
            }
        }

        private void Tray_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                NativeMethods.DoMouseRightClick();
            }
        }

        private void Tray_BalloonTipClicked(object sender, EventArgs e)
        {
            if (BalloonClickedCallback != null)
            {
                BalloonClickedCallback(Tray, new AnyEventArgs(BalloonClickedCallbackArgument));
            }
        }

        private void LoadDatabase()
        {
            try
            {
                GlobalInstances.ProfileMan = ProfileManager.Load(ProfileManager.DBPath);
            }
            catch
            {
                GlobalInstances.ProfileMan = new ProfileManager();
                ThreadPool.QueueUserWorkItem((WaitCallback)delegate(object state)
                {
                    Utils.Invoke(SyncCtx, (SendOrPostCallback)delegate(object o)
                    {
                        ShowBalloonTip(PKSoft.Resources.Messages.DatabaseIsMissingOrCorrupt, ToolTipIcon.Warning);
                    });
                });

                throw;
            }
        }

        private void AutoWhitelist()
        {
            ApplicationCollection allApps = Utils.DeepClone(GlobalInstances.ProfileMan.KnownApplications);
            for (int i = 0; i < allApps.Count; ++i)
            {
                Application app = allApps[i];

                // If we've found at least one file, add the app to the list
                if (!app.Special && app.ResolveFilePaths())
                {
                    foreach (AppExceptionAssoc template in app.FileTemplates)
                    {
                        foreach (string execPath in template.ExecutableRealizations)
                        {
                            ActiveConfig.Service.AppExceptions.Add(template.CreateException(execPath));
                        }
                    }
                }
            }
            ActiveConfig.Service.Normalize();
            ApplyFirewallSettings(ActiveConfig.Service);
        }

        private void mnuModeLearn_Click(object sender, EventArgs e)
        {
            string firstLine, contentLines;
            Utils.SplitFirstLine(PKSoft.Resources.Messages.YouAreAboutToEnterLearningMode, out firstLine, out contentLines);

            TaskDialog dialog = new TaskDialog();
            dialog.CustomMainIcon = PKSoft.Resources.Icons.firewall;
            dialog.WindowTitle = PKSoft.Resources.Messages.TinyWall;
            dialog.MainInstruction = firstLine;
            dialog.Content = contentLines;
            dialog.AllowDialogCancellation = false;
            dialog.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;

            if (dialog.Show() != (int)DialogResult.Yes)
                return;

            SetMode(FirewallMode.Learning);
            UpdateDisplay();
        }

        private void InitController()
        {

            // We will load our database parallel to other things to improve startup performance
            using (ThreadBarrier barrier = new ThreadBarrier(2))
            {
                ThreadPool.QueueUserWorkItem((WaitCallback)delegate(object state)
                {
                    try
                    {
                        LoadDatabase();
                    }
                    catch { }
                    finally
                    {
                        barrier.Wait();
                    }
                });

                // --------------- CODE BETWEEN HERE MUST NOT USE DATABASE, SINCE IT IS BEING LOADED PARALLEL ---------------
                // BEGIN
                TrafficTimer = new System.Threading.Timer(TrafficTimerTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(TRAFFIC_TIMER_INTERVAL));
                mnuElevate.Visible = !Utils.RunningAsAdmin();
                mnuModeDisabled.Image = Resources.Icons.shield_grey_small.ToBitmap();
                mnuModeAllowOutgoing.Image = Resources.Icons.shield_red_small.ToBitmap();
                mnuModeBlockAll.Image = Resources.Icons.shield_yellow_small.ToBitmap();
                mnuModeNormal.Image = Resources.Icons.shield_green_small.ToBitmap();
                mnuModeLearn.Image = Resources.Icons.shield_blue_small.ToBitmap();
                ApplyControllerSettings();

                GlobalInstances.CommunicationMan = new PipeCom("TinyWallController");

                barrier.Wait();
                // END
                // --------------- CODE BETWEEN HERE MUST NOT USE DATABASE, SINCE IT IS BEING LOADED PARALLEL ---------------
                // --- THREAD BARRIER ---
            }

            bool comError;
            LoadSettingsFromServer(out comError, true);
#if !DEBUG
            if (comError)
            {
                if (TinyWallDoctor.EnsureServiceInstalledAndRunning())
                {
                    LoadSettingsFromServer(out comError, true);
                    UpdateDisplay();
                }
                else
                {
                    MessageBox.Show(PKSoft.Resources.Messages.TheTinyWallServiceIsUnavailable, PKSoft.Resources.Messages.TinyWall, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
#endif
            if (StartupOpts.autowhitelist)
            {
                AutoWhitelist();
            }

            if (StartupOpts.updatenow)
            {
                StartUpdate(null, null);
            }
        }

        public bool PreFilterMessage(ref System.Windows.Forms.Message m)
        {
            SyncCtx = SynchronizationContext.Current;
            return false;
        }
    }

    internal class AnyEventArgs : EventArgs
    {
        private object _arg;

        public AnyEventArgs(object arg = null)
        {
            _arg = arg;
        }
        public object Arg
        {
            get { return _arg; }
        }
    }
}
