using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Samples;

using TinyWall.Interface;
using TinyWall.Interface.Internal;

using PKSoft.DatabaseClasses;

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
        private ServerState FirewallState;
        private DateTime LastUpdateNotification = DateTime.MinValue;

        // Traffic rate monitoring
        private System.Threading.Timer TrafficTimer = null;
        private System.Threading.Timer UpdateTimer = null;
        private const int TRAFFIC_TIMER_INTERVAL = 2;
        private ulong bytesRxTotal = 0;
        private ulong bytesTxTotal = 0;
        private ulong WmiTsSys100Ns = 0;
        private string rxDisplay = string.Empty;
        private string txDisplay = string.Empty;
        private readonly bool RunningOnWin8;

        private EventHandler<AnyEventArgs> BalloonClickedCallback;
        private object BalloonClickedCallbackArgument;
        private SynchronizationContext SyncCtx;

        private Hotkey HotKeyWhitelistExecutable;
        private Hotkey HotKeyWhitelistProcess;
        private Hotkey HotKeyWhitelistWindow;

        private readonly CmdLineArgs StartupOpts;

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
            Version OsVer = Environment.OSVersion.Version;
            Version Win8Version = new Version(6, 2, 0, 0);
            this.RunningOnWin8 = (OsVer >= Win8Version);
            this.StartupOpts = opts;

            ActiveConfig.Controller = ControllerSettings.Load();
            if (!ActiveConfig.Controller.Language.Equals("auto", StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo(ActiveConfig.Controller.Language);
                    System.Windows.Forms.Application.CurrentCulture = Thread.CurrentThread.CurrentUICulture;
                }
                catch { }
            }

            System.Windows.Forms.Application.AddMessageFilter(this);
            InitializeComponent();
            InitController();

            TrayMenu.Closed += TrayMenu_Closed;
            Tray.ContextMenuStrip = TrayMenu;
            Tray.Visible = true;
        }

        private void TrayMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            ShowTraffixRate = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                // Manually added
                HotKeyWhitelistExecutable?.Dispose();
                HotKeyWhitelistProcess?.Dispose();
                HotKeyWhitelistWindow?.Dispose();
                MouseInterceptor?.Dispose();

                using (WaitHandle wh = new AutoResetEvent(false))
                {
                    TrafficTimer.Dispose(wh);
                    wh.WaitOne();
                }

                using (WaitHandle wh = new AutoResetEvent(false))
                {
                    UpdateTimer.Dispose(wh);
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

        private void UpdateTimerTick(object state)
        {
            if (DateTime.Now - LastUpdateNotification > TimeSpan.FromHours(4))
            {
                ThreadPool.QueueUserWorkItem((WaitCallback)delegate (object dummy)
                {
                    VerifyUpdates();
                });
                LastUpdateNotification = DateTime.Now;
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

                    UpdateTrafficRateText();
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

        private bool _ShowTraffixRate = false;
        private bool ShowTraffixRate
        {
            get { return _ShowTraffixRate; }
            set
            {
                _ShowTraffixRate = value;

                // Update more often while visible
                if (value)
                    TrafficTimer.Change(0, 1000);
                else
                    TrafficTimer.Change(5000, 5000);
            }
        }

        private void UpdateTrafficRateText()
        {
            if (!ShowTraffixRate)
                return;

            string text = string.Format(CultureInfo.CurrentCulture, "{0}: {1}   {2}: {3}", PKSoft.Resources.Messages.TrafficIn, rxDisplay, PKSoft.Resources.Messages.TrafficOut, txDisplay);
            if (TrayMenu.InvokeRequired)
                TrayMenu.BeginInvoke((MethodInvoker)delegate
                {
                   mnuTrafficRate.Text = text;
                });
            else
                mnuTrafficRate.Text = text;
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
            // Update UI based on current firewall mode
            string FirewallModeName = PKSoft.Resources.Messages.FirewallModeUnknown;
            switch (FirewallState.Mode)
            {
                case TinyWall.Interface.FirewallMode.Normal:
                    Tray.Icon = PKSoft.Resources.Icons.firewall;
                    mnuMode.Image = mnuModeNormal.Image;
                    FirewallModeName = PKSoft.Resources.Messages.FirewallModeNormal;
                    break;

                case TinyWall.Interface.FirewallMode.AllowOutgoing:
                    Tray.Icon = PKSoft.Resources.Icons.shield_red_small;
                    mnuMode.Image = mnuModeAllowOutgoing.Image;
                    FirewallModeName = PKSoft.Resources.Messages.FirewallModeAllowOut;
                    break;

                case TinyWall.Interface.FirewallMode.BlockAll:
                    Tray.Icon = PKSoft.Resources.Icons.shield_yellow_small;
                    mnuMode.Image = mnuModeBlockAll.Image;
                    FirewallModeName = PKSoft.Resources.Messages.FirewallModeBlockAll;
                    break;

                case TinyWall.Interface.FirewallMode.Disabled:
                    Tray.Icon = PKSoft.Resources.Icons.shield_grey_small;
                    mnuMode.Image = mnuModeDisabled.Image;
                    FirewallModeName = PKSoft.Resources.Messages.FirewallModeDisabled;
                    break;

                case TinyWall.Interface.FirewallMode.Learning:
                    Tray.Icon = PKSoft.Resources.Icons.shield_blue_small;
                    mnuMode.Image = mnuModeLearn.Image;
                    FirewallModeName = PKSoft.Resources.Messages.FirewallModeLearn;
                    break;

                case TinyWall.Interface.FirewallMode.Unknown:
                    Tray.Icon = PKSoft.Resources.Icons.shield_grey_small;
                    mnuMode.Image = PKSoft.Resources.Icons.shield_grey_small.ToBitmap();
                    FirewallModeName = PKSoft.Resources.Messages.FirewallModeUnknown;
                    break;
            }

            Tray.Text = string.Format(CultureInfo.CurrentCulture, "TinyWall\r\n{0}: {1}",
                PKSoft.Resources.Messages.Mode, FirewallModeName);

            // Find out if we are locked and if we have a password
            this.Locked = FirewallState.Locked;

            // Do we have a password at all?
            mnuLock.Visible = FirewallState.HasPassword;

            mnuAllowLocalSubnet.Checked = ActiveConfig.Service.ActiveProfile.AllowLocalSubnet;
            mnuEnableHostsBlocklist.Checked = ActiveConfig.Service.Blocklists.EnableBlocklists;
        }

        private void SetMode(TinyWall.Interface.FirewallMode mode)
        {
            MessageType resp = GlobalInstances.Controller.SwitchFirewallMode(mode);

            string usermsg = string.Empty;
            switch (mode)
            {
                case TinyWall.Interface.FirewallMode.Normal:
                    usermsg = PKSoft.Resources.Messages.TheFirewallIsNowOperatingAsRecommended;
                    break;

                case TinyWall.Interface.FirewallMode.AllowOutgoing:
                    usermsg = PKSoft.Resources.Messages.TheFirewallIsNowAllowsOutgoingConnections;
                    break;

                case TinyWall.Interface.FirewallMode.BlockAll:
                    usermsg = PKSoft.Resources.Messages.TheFirewallIsNowBlockingAllInAndOut;
                    break;

                case TinyWall.Interface.FirewallMode.Disabled:
                    usermsg = PKSoft.Resources.Messages.TheFirewallIsNowDisabled;
                    break;

                case TinyWall.Interface.FirewallMode.Learning:
                    usermsg = PKSoft.Resources.Messages.TheFirewallIsNowLearning;
                    break;
            }

            switch (resp)
            {
                case MessageType.RESPONSE_OK:
                    FirewallState.Mode = mode;
                    ShowBalloonTip(usermsg, ToolTipIcon.Info);
                    break;
                default:
                    DefaultPopups(resp);
                    break;
            }
        }

        private void mnuModeDisabled_Click(object sender, EventArgs e)
        {
            SetMode(TinyWall.Interface.FirewallMode.Disabled);
            UpdateDisplay();
        }

        private void mnuModeNormal_Click(object sender, EventArgs e)
        {
            SetMode(TinyWall.Interface.FirewallMode.Normal);
            UpdateDisplay();
        }

        private void mnuModeBlockAll_Click(object sender, EventArgs e)
        {
            SetMode(TinyWall.Interface.FirewallMode.BlockAll);
            UpdateDisplay();
        }

        private void mnuAllowOutgoing_Click(object sender, EventArgs e)
        {
            SetMode(TinyWall.Interface.FirewallMode.AllowOutgoing);
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
            ServerConfiguration config;
            ServerState state = FirewallState;

            Guid inChangeset = force ? Guid.Empty : GlobalInstances.ClientChangeset;
            Guid outChangeset = inChangeset;
            MessageType ret = GlobalInstances.Controller.GetServerConfig(out config, out state, ref outChangeset);

            comError = (MessageType.COM_ERROR == ret);
            bool SettingsUpdated = force || (inChangeset != outChangeset);

            if (MessageType.RESPONSE_OK == ret)
            {
                // Update our config based on what we received
                if (SettingsUpdated)
                {
                    GlobalInstances.ClientChangeset = outChangeset;
                    ActiveConfig.Service = config;
                    FirewallState = state;
                }
            }
            else
            {
                ActiveConfig.Controller = new ControllerSettings();
                ActiveConfig.Service = new ServerConfiguration();
                ActiveConfig.Service.SetActiveProfile(PKSoft.Resources.Messages.Default);
                FirewallState = new ServerState();
                SettingsUpdated = true;
            }

            // See if there is a new notifaction for the client
            if (SettingsUpdated)
            {
                for (int i = 0; i < FirewallState.ClientNotifs.Count; ++i)
                {
                    switch (FirewallState.ClientNotifs[i])
                    {
                        case MessageType.DATABASE_UPDATED:
                            LoadDatabase();
                            break;
                    }
                }
                FirewallState.ClientNotifs.Clear();
                UpdateDisplay();
            }

            return SettingsUpdated;
        }

        private void TrayMenu_Opening(object sender, CancelEventArgs e)
        {
            e.Cancel = false;
            if (FirewallState.Mode == TinyWall.Interface.FirewallMode.Unknown)
            {
                if (!TinyWallDoctor.IsServiceRunning())
                {
                    ShowBalloonTip(PKSoft.Resources.Messages.TheTinyWallServiceIsUnavailable, ToolTipIcon.Error, 10000);
                    e.Cancel = true;
                }
            }

            ShowTraffixRate = true;

            bool locked = GlobalInstances.Controller.IsServerLocked;
            this.Locked = locked;
            FirewallState.Locked = locked;
            UpdateDisplay();
        }

        private void mnuWhitelistByExecutable_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            WhitelistSubject(new ExecutableSubject(ofd.FileName));
        }

        private void mnuWhitelistByProcess_Click(object sender, EventArgs e)
        {
            List<FirewallExceptionV3> exceptions = new List<FirewallExceptionV3>();
            List<string> pathList = ProcessesForm.ChooseProcess(null, true);

            foreach (string path in pathList)
            {
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

            AddExceptionList(exceptions);
        }

        internal MessageType ApplyFirewallSettings(ServerConfiguration srvConfig, bool showUI = true)
        {
            ServerState state;
            Guid localChangeset = GlobalInstances.ClientChangeset;
            MessageType resp = GlobalInstances.Controller.SetServerConfig(ref srvConfig, ref localChangeset, out state);

            switch (resp)
            {
                case MessageType.RESPONSE_OK:
                    FirewallState = state;
                    ActiveConfig.Service = srvConfig;
                    GlobalInstances.ClientChangeset = localChangeset;
                    if (showUI)
                        ShowBalloonTip(PKSoft.Resources.Messages.TheFirewallSettingsHaveBeenUpdated, ToolTipIcon.Info);
                    break;
                case MessageType.RESPONSE_WARNING:
                    FirewallState = state;

                    // We tell the user to re-do his changes to the settings to prevent overwriting the wrong configuration.
                    if (showUI)
                        ShowBalloonTip(PKSoft.Resources.Messages.SettingHaveChangedRetry, ToolTipIcon.Warning);
                    break;
                case MessageType.RESPONSE_ERROR:
                    if (showUI)
                        ShowBalloonTip(PKSoft.Resources.Messages.CouldNotApplySettingsInternalError, ToolTipIcon.Warning);
                    break;
                default:
                    if (showUI)
                        DefaultPopups(resp);
                    LoadSettingsFromServer();
                    break;
            }

            return resp;
        }

        private void DefaultPopups(MessageType op)
        {
            switch (op)
            {
                case MessageType.RESPONSE_OK:
                    ShowBalloonTip(PKSoft.Resources.Messages.Success, ToolTipIcon.Info);
                    break;
                case MessageType.RESPONSE_WARNING:
                    ShowBalloonTip(PKSoft.Resources.Messages.OtherSettingsPreventEffect, ToolTipIcon.Warning);
                    break;
                case MessageType.RESPONSE_ERROR:
                    ShowBalloonTip(PKSoft.Resources.Messages.OperationFailed, ToolTipIcon.Error);
                    break;
                case MessageType.RESPONSE_LOCKED:
                    ShowBalloonTip(PKSoft.Resources.Messages.TinyWallIsCurrentlyLocked, ToolTipIcon.Warning);
                    Locked = true;
                    break;
                case MessageType.COM_ERROR:
                default:
                    ShowBalloonTip(PKSoft.Resources.Messages.CommunicationWithTheServiceError, ToolTipIcon.Error);
                    break;
            }
        }

        private void mnuManage_Click(object sender, EventArgs e)
        {
            if (Locked)
            {
                DefaultPopups(MessageType.RESPONSE_LOCKED);
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

                using (this.ShownSettings = new SettingsForm(Utils.DeepClone(ActiveConfig.Service), Utils.DeepClone(ActiveConfig.Controller)))
                {
                    SettingsForm sf = this.ShownSettings;

                    if (sf.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        // Handle password change request
                        string passwd = sf.NewPassword;
                        if (passwd != null)
                        {
                            // If the new password is empty, we do not hash it because an empty password
                            // is a special value signalizing the non-existence of a password.
                            MessageType resp = GlobalInstances.Controller.SetPassphrase(string.IsNullOrEmpty(passwd) ? string.Empty : Hasher.HashString(passwd));
                            if (resp != MessageType.RESPONSE_OK)
                            {
                                // Only display a popup for setting the password if it did not succeed
                                DefaultPopups(resp);
                                return;
                            }
                            else
                            {
                                // If the operation is successfull, do not report anything as we will be setting
                                // the other settings too and we want to avoid multiple popups.
                                FirewallState.HasPassword = !string.IsNullOrEmpty(passwd);
                            }
                        }

                        // Save settings
                        ActiveConfig.Controller = sf.TmpConfig.Controller;
                        ActiveConfig.Controller.Save();
                        ApplyFirewallSettings(sf.TmpConfig.Service);
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
            bool success;
            bool foregroundIsMetro = RunningOnWin8 && Utils.IsMetroActive(out success);

            if (foregroundIsMetro)
            {
                try
                {
                    int pid = Utils.GetForegroundProcessPid();
                    string filePath = Utils.GetPathOfProcessUseTwService(pid, GlobalInstances.Controller);
                    if (string.IsNullOrEmpty(filePath))
                        throw new Exception();
                    WhitelistSubject(new ExecutableSubject(filePath));
                }
                catch
                {
                    ShowBalloonTip(PKSoft.Resources.Messages.CannotGetExecutablePathWhitelisting, ToolTipIcon.Error);
                }
            }
            else
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

                    string filePath = Utils.GetExecutableUnderCursor(x, y, GlobalInstances.Controller);
                    if (string.IsNullOrEmpty(filePath))
                    {
                        ShowBalloonTip(PKSoft.Resources.Messages.CannotGetExecutablePathWhitelisting, ToolTipIcon.Error);
                        return;
                    }

                    WhitelistSubject(new ExecutableSubject(filePath));
                });
            });
        }

        internal void WhitelistSubject(ExecutableSubject subject)
        {
            List<FirewallExceptionV3> exceptions = GlobalInstances.AppDatabase.GetExceptionsForApp(subject, true, out DatabaseClasses.Application dummyApp);
            if (exceptions.Count == 0)
                return;

            // Did we find any related files?
            if ((exceptions.Count == 1) && (ActiveConfig.Controller.AskForExceptionDetails))
            {
                using (ApplicationExceptionForm f = new ApplicationExceptionForm(exceptions[0]))
                {
                    bool success;
                    if (Utils.IsMetroActive(out success))
                        Utils.ShowToastNotif(Resources.Messages.ToastInputNeeded);

                    if (f.ShowDialog() == DialogResult.Cancel)
                        return;

                    exceptions = f.ExceptionSettings;
                }
            }

            // Add exceptions, along with other files that belong to this app
            AddExceptionList(exceptions);
        }

        internal void AddExceptionList(List<FirewallExceptionV3> list)
        {
            LoadSettingsFromServer();

            if (list.Count > 1)
            {
                ServerConfiguration confCopy = Utils.DeepClone(ActiveConfig.Service);
                confCopy.ActiveProfile.AppExceptions.AddRange(list);
                confCopy.ActiveProfile.Normalize();
                ApplyFirewallSettings(confCopy);
            }
            else if (list.Count == 1)
                AddNewException(list[0]);
        }

        // Called when a user double-clicks on a popup to edit the most recent exception
        private void EditRecentException(object sender, AnyEventArgs e)
        {
            List<FirewallExceptionV3> exceptions = null;

            using (ApplicationExceptionForm f = new ApplicationExceptionForm(e.Arg as FirewallExceptionV3))
            {
                if (f.ShowDialog() == DialogResult.Cancel)
                    return;

                exceptions = f.ExceptionSettings;
            }

            // Add exceptions, along with other files that belong to this app
            AddExceptionList(exceptions);
        }

        private void AddNewException(FirewallExceptionV3 fwex)
        {
            ServerConfiguration confCopy = Utils.DeepClone(ActiveConfig.Service);
            confCopy.ActiveProfile.AppExceptions.Add(fwex);
            confCopy.ActiveProfile.Normalize();

            MessageType resp = ApplyFirewallSettings(confCopy, false);

            bool success;
            bool metroActive = Utils.IsMetroActive(out success);
            if (!metroActive)
            {
                switch (resp)
                {
                    case MessageType.RESPONSE_OK:
                        bool signedAndValid = false;
                        ExecutableSubject exesub = fwex.Subject as ExecutableSubject;
                        if (null != exesub)
                            signedAndValid = exesub.IsSigned && exesub.CertValid;

                        if (signedAndValid)
                            ShowBalloonTip(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.FirewallRulesForRecognizedChanged, fwex.Subject.ToString()), ToolTipIcon.Info, 5000, EditRecentException, Utils.DeepClone(fwex));
                        else
                            ShowBalloonTip(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.FirewallRulesForUnrecognizedChanged, fwex.Subject.ToString()), ToolTipIcon.Info, 5000, EditRecentException, Utils.DeepClone(fwex));
                        break;
                    case MessageType.RESPONSE_WARNING:
                        // We tell the user to re-do his changes to the settings to prevent overwriting the wrong configuration.
                        ShowBalloonTip(PKSoft.Resources.Messages.SettingHaveChangedRetry, ToolTipIcon.Warning);
                        break;
                    case MessageType.RESPONSE_ERROR:
                        ShowBalloonTip(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.CouldNotWhitelistProcess, fwex.Subject.ToString()), ToolTipIcon.Warning);
                        break;
                    default:
                        DefaultPopups(resp);
                        LoadSettingsFromServer();
                        break;
                }
            }
            else
            {
                switch (resp)
                {
                    case MessageType.RESPONSE_OK:
                        string exeList = fwex.Subject.ToString();
                        Utils.ShowToastNotif(string.Format(Resources.Messages.ToastAppWhitelisted+"\n{0}", exeList));
                        break;
                    default:
                        Utils.ShowToastNotif(Resources.Messages.ToastWhitelistFailed);
                        break;
                }
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
                        MessageType resp = GlobalInstances.Controller.TryUnlockServer(pf.PassHash);
                        switch (resp)
                        {
                            case MessageType.RESPONSE_OK:
                                this.Locked = false;
                                FirewallState.Locked = false;
                                ShowBalloonTip(PKSoft.Resources.Messages.TinyWallHasBeenUnlocked, ToolTipIcon.Info);
                                break;
                            case MessageType.RESPONSE_ERROR:
                                ShowBalloonTip(PKSoft.Resources.Messages.UnlockFailed, ToolTipIcon.Error);
                                break;
                            default:
                                DefaultPopups(resp);
                                break;
                        }
                    }
                }
            }
            else
            {
                MessageType lockResp = GlobalInstances.Controller.LockServer();
                if ((lockResp == MessageType.RESPONSE_OK) || (lockResp== MessageType.RESPONSE_LOCKED))
                {
                    this.Locked = true;
                    FirewallState.Locked = true;
                }
            }

            UpdateDisplay();
        }

        private void mnuAllowLocalSubnet_Click(object sender, EventArgs e)
        {
            LoadSettingsFromServer();

            // Copy, so that settings are not changed if they cannot be saved
            ServerConfiguration confCopy = Utils.DeepClone(ActiveConfig.Service);
            confCopy.ActiveProfile.AllowLocalSubnet = !mnuAllowLocalSubnet.Checked;
            ApplyFirewallSettings(confCopy);

            mnuAllowLocalSubnet.Checked = ActiveConfig.Service.ActiveProfile.AllowLocalSubnet;
        }

        private void mnuEnableHostsBlocklist_Click(object sender, EventArgs e)
        {
            LoadSettingsFromServer();

            // Copy, so that settings are not changed if they cannot be saved
            ServerConfiguration confCopy = Utils.DeepClone(ActiveConfig.Service);
            confCopy.Blocklists.EnableBlocklists = !mnuEnableHostsBlocklist.Checked;
            ApplyFirewallSettings(confCopy);

            mnuEnableHostsBlocklist.Checked = ActiveConfig.Service.Blocklists.EnableBlocklists;
        }

        private void ShowBalloonTip(string msg, ToolTipIcon icon, int period_ms = 5000, EventHandler<AnyEventArgs> balloonClicked = null, object handlerArg = null)
        {
            BalloonClickedCallback = balloonClicked;
            BalloonClickedCallbackArgument = handlerArg;
            Tray.ShowBalloonTip(period_ms, "TinyWall", msg, icon);
            Thread.Sleep(500);
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
                hk?.Dispose();
                hk = null;
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
                Utils.StartProcess(TinyWall.Interface.Internal.Utils.ExecutablePath, "/desktop", true);
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
                Utils.SafeNativeMethods.DoMouseRightClick();
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
                GlobalInstances.AppDatabase = AppDatabase.Load(AppDatabase.DBPath);
            }
            catch
            {
                GlobalInstances.AppDatabase = new AppDatabase();
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
            // Copy, so that settings are not changed if they cannot be saved
            ServerConfiguration confCopy = Utils.DeepClone(ActiveConfig.Service);
            confCopy.ActiveProfile.AppExceptions.AddRange(GlobalInstances.AppDatabase.FastSearchMachineForKnownApps());
            confCopy.ActiveProfile.Normalize();
            ApplyFirewallSettings(confCopy);
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

            SetMode(TinyWall.Interface.FirewallMode.Learning);
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
                TrafficTimer = new System.Threading.Timer(TrafficTimerTick, null, 0, Timeout.Infinite);
                UpdateTimer = new System.Threading.Timer(UpdateTimerTick, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10));
                ShowTraffixRate = false;
                mnuElevate.Visible = !Utils.RunningAsAdmin();
                mnuModeDisabled.Image = Resources.Icons.shield_grey_small.ToBitmap();
                mnuModeAllowOutgoing.Image = Resources.Icons.shield_red_small.ToBitmap();
                mnuModeBlockAll.Image = Resources.Icons.shield_yellow_small.ToBitmap();
                mnuModeNormal.Image = Resources.Icons.shield_green_small.ToBitmap();
                mnuModeLearn.Image = Resources.Icons.shield_blue_small.ToBitmap();
                ApplyControllerSettings();

                GlobalInstances.Controller = new Controller("TinyWallController");

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
