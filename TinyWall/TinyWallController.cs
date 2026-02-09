using Microsoft.Samples;
using pylorak.Utilities;
using pylorak.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace pylorak.TinyWall
{
    internal sealed class TinyWallController : ApplicationContext
    {
        #region Form-Designer generated code

        private IContainer components = new Container();

        [MemberNotNull(nameof(Tray),
            nameof(TrayMenu),
            nameof(toolStripMenuItem1),
            nameof(toolStripMenuItem2),
            nameof(mnuQuit),
            nameof(mnuMode),
            nameof(mnuModeNormal),
            nameof(mnuModeBlockAll),
            nameof(mnuModeDisabled),
            nameof(mnuManage),
            nameof(toolStripMenuItem5),
            nameof(mnuWhitelistByExecutable),
            nameof(mnuWhitelistByProcess),
            nameof(mnuWhitelistByWindow),
            nameof(mnuLock),
            nameof(mnuElevate),
            nameof(mnuConnections),
            nameof(mnuModeAllowOutgoing),
            nameof(ofd),
            nameof(toolStripMenuItem3),
            nameof(mnuAllowLocalSubnet),
            nameof(mnuEnableHostsBlocklist),
            nameof(mnuTrafficRate),
            nameof(mnuModeLearn)
        )]
        private void InitializeComponent()
        {
            components = new Container();
            ComponentResourceManager resources = new ComponentResourceManager(typeof(TinyWallController));
            Tray = new NotifyIcon(components);
            TrayMenu = new ContextMenuStrip(components);
            mnuTrafficRate = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripSeparator();
            mnuMode = new ToolStripMenuItem();
            mnuModeNormal = new ToolStripMenuItem();
            mnuModeBlockAll = new ToolStripMenuItem();
            mnuModeAllowOutgoing = new ToolStripMenuItem();
            mnuModeDisabled = new ToolStripMenuItem();
            mnuModeLearn = new ToolStripMenuItem();
            mnuManage = new ToolStripMenuItem();
            mnuConnections = new ToolStripMenuItem();
            mnuLock = new ToolStripMenuItem();
            mnuElevate = new ToolStripMenuItem();
            toolStripMenuItem2 = new ToolStripSeparator();
            mnuAllowLocalSubnet = new ToolStripMenuItem();
            mnuEnableHostsBlocklist = new ToolStripMenuItem();
            toolStripMenuItem3 = new ToolStripSeparator();
            mnuWhitelistByExecutable = new ToolStripMenuItem();
            mnuWhitelistByProcess = new ToolStripMenuItem();
            mnuWhitelistByWindow = new ToolStripMenuItem();
            toolStripMenuItem5 = new ToolStripSeparator();
            mnuQuit = new ToolStripMenuItem();
            ofd = new OpenFileDialog();
            TrayMenu.SuspendLayout();
            //
            // Tray
            //
            resources.ApplyResources(Tray, "Tray");
            Tray.Icon = Resources.Icons.firewall;
            Tray.Visible = false;
            Tray.BalloonTipClicked += Tray_BalloonTipClicked;
            Tray.MouseClick += Tray_MouseClick;
            //
            // TrayMenu
            //
            TrayMenu.Items.AddRange(new ToolStripItem[] {
            mnuTrafficRate,
            toolStripMenuItem1,
            mnuMode,
            mnuManage,
            mnuConnections,
            mnuLock,
            mnuElevate,
            toolStripMenuItem2,
            mnuAllowLocalSubnet,
            mnuEnableHostsBlocklist,
            toolStripMenuItem3,
            mnuWhitelistByExecutable,
            mnuWhitelistByProcess,
            mnuWhitelistByWindow,
            toolStripMenuItem5,
            mnuQuit});
            TrayMenu.Name = "TrayMenu";
            resources.ApplyResources(TrayMenu, "TrayMenu");
            TrayMenu.Opening += TrayMenu_Opening;
            //
            // mnuTrafficRate
            //
            mnuTrafficRate.AccessibleRole = AccessibleRole.StaticText;
            mnuTrafficRate.Image = Resources.Icons.info;
            mnuTrafficRate.Name = "mnuTrafficRate";
            resources.ApplyResources(mnuTrafficRate, "mnuTrafficRate");
            //
            // toolStripMenuItem1
            //
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            resources.ApplyResources(toolStripMenuItem1, "toolStripMenuItem1");
            //
            // mnuMode
            //
            mnuMode.AccessibleRole = AccessibleRole.ButtonMenu;
            mnuMode.DropDownItems.AddRange(new ToolStripItem[] {
            mnuModeNormal,
            mnuModeBlockAll,
            mnuModeAllowOutgoing,
            mnuModeDisabled,
            mnuModeLearn});
            mnuMode.Name = "mnuMode";
            resources.ApplyResources(mnuMode, "mnuMode");
            //
            // mnuModeNormal
            //
            mnuModeNormal.Name = "mnuModeNormal";
            resources.ApplyResources(mnuModeNormal, "mnuModeNormal");
            mnuModeNormal.Click += mnuModeNormal_Click;
            //
            // mnuModeBlockAll
            //
            mnuModeBlockAll.Name = "mnuModeBlockAll";
            resources.ApplyResources(mnuModeBlockAll, "mnuModeBlockAll");
            mnuModeBlockAll.Click += mnuModeBlockAll_Click;
            //
            // mnuModeAllowOutgoing
            //
            mnuModeAllowOutgoing.Name = "mnuModeAllowOutgoing";
            resources.ApplyResources(mnuModeAllowOutgoing, "mnuModeAllowOutgoing");
            mnuModeAllowOutgoing.Click += mnuAllowOutgoing_Click;
            //
            // mnuModeDisabled
            //
            mnuModeDisabled.Name = "mnuModeDisabled";
            resources.ApplyResources(mnuModeDisabled, "mnuModeDisabled");
            mnuModeDisabled.Click += mnuModeDisabled_Click;
            //
            // mnuModeLearn
            //
            mnuModeLearn.Name = "mnuModeLearn";
            resources.ApplyResources(mnuModeLearn, "mnuModeLearn");
            mnuModeLearn.Click += mnuModeLearn_Click;
            //
            // mnuManage
            //
            mnuManage.Image = Resources.Icons.manage;
            mnuManage.Name = "mnuManage";
            resources.ApplyResources(mnuManage, "mnuManage");
            mnuManage.Click += mnuManage_Click;
            //
            // mnuConnections
            //
            mnuConnections.Image = Resources.Icons.connections;
            mnuConnections.Name = "mnuConnections";
            resources.ApplyResources(mnuConnections, "mnuConnections");
            mnuConnections.Click += mnuConnections_Click;
            //
            // mnuLock
            //
            mnuLock.Image = Resources.Icons.lock_small;
            mnuLock.Name = "mnuLock";
            resources.ApplyResources(mnuLock, "mnuLock");
            mnuLock.Click += mnuLock_Click;
            //
            // mnuElevate
            //
            mnuElevate.Image = Resources.Icons.w7uacshield;
            mnuElevate.Name = "mnuElevate";
            resources.ApplyResources(mnuElevate, "mnuElevate");
            mnuElevate.Click += mnuElevate_Click;
            //
            // toolStripMenuItem2
            //
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            resources.ApplyResources(toolStripMenuItem2, "toolStripMenuItem2");
            //
            // mnuAllowLocalSubnet
            //
            mnuAllowLocalSubnet.Name = "mnuAllowLocalSubnet";
            resources.ApplyResources(mnuAllowLocalSubnet, "mnuAllowLocalSubnet");
            mnuAllowLocalSubnet.Click += mnuAllowLocalSubnet_Click;
            //
            // mnuEnableHostsBlocklist
            //
            mnuEnableHostsBlocklist.Name = "mnuEnableHostsBlocklist";
            resources.ApplyResources(mnuEnableHostsBlocklist, "mnuEnableHostsBlocklist");
            mnuEnableHostsBlocklist.Click += mnuEnableHostsBlocklist_Click;
            //
            // toolStripMenuItem3
            //
            toolStripMenuItem3.Name = "toolStripMenuItem3";
            resources.ApplyResources(toolStripMenuItem3, "toolStripMenuItem3");
            //
            // mnuWhitelistByExecutable
            //
            mnuWhitelistByExecutable.Image = Resources.Icons.executable;
            mnuWhitelistByExecutable.Name = "mnuWhitelistByExecutable";
            resources.ApplyResources(mnuWhitelistByExecutable, "mnuWhitelistByExecutable");
            mnuWhitelistByExecutable.Click += mnuWhitelistByExecutable_Click;
            //
            // mnuWhitelistByProcess
            //
            mnuWhitelistByProcess.Image = Resources.Icons.process;
            mnuWhitelistByProcess.Name = "mnuWhitelistByProcess";
            resources.ApplyResources(mnuWhitelistByProcess, "mnuWhitelistByProcess");
            mnuWhitelistByProcess.Click += mnuWhitelistByProcess_Click;
            //
            // mnuWhitelistByWindow
            //
            mnuWhitelistByWindow.Image = Resources.Icons.window;
            mnuWhitelistByWindow.Name = "mnuWhitelistByWindow";
            resources.ApplyResources(mnuWhitelistByWindow, "mnuWhitelistByWindow");
            mnuWhitelistByWindow.Click += mnuWhitelistByWindow_Click;
            //
            // toolStripMenuItem5
            //
            toolStripMenuItem5.Name = "toolStripMenuItem5";
            resources.ApplyResources(toolStripMenuItem5, "toolStripMenuItem5");
            //
            // mnuQuit
            //
            mnuQuit.Image = Resources.Icons.exit;
            mnuQuit.Name = "mnuQuit";
            resources.ApplyResources(mnuQuit, "mnuQuit");
            mnuQuit.Click += mnuQuit_Click;
            //
            // ofd
            //
            resources.ApplyResources(ofd, "ofd");
            TrayMenu.ResumeLayout(false);
        }

        private NotifyIcon Tray;
        private ContextMenuStrip TrayMenu;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripSeparator toolStripMenuItem2;
        private ToolStripMenuItem mnuQuit;
        private ToolStripMenuItem mnuMode;
        private ToolStripMenuItem mnuModeNormal;
        private ToolStripMenuItem mnuModeBlockAll;
        private ToolStripMenuItem mnuModeDisabled;
        private ToolStripMenuItem mnuManage;
        private ToolStripSeparator toolStripMenuItem5;
        private ToolStripMenuItem mnuWhitelistByExecutable;
        private ToolStripMenuItem mnuWhitelistByProcess;
        private ToolStripMenuItem mnuWhitelistByWindow;
        private ToolStripMenuItem mnuLock;
        private ToolStripMenuItem mnuElevate;
        private ToolStripMenuItem mnuConnections;
        private ToolStripMenuItem mnuModeAllowOutgoing;
        private OpenFileDialog ofd;
        private ToolStripSeparator toolStripMenuItem3;
        private ToolStripMenuItem mnuAllowLocalSubnet;
        private ToolStripMenuItem mnuEnableHostsBlocklist;
        private ToolStripMenuItem mnuTrafficRate;
        private ToolStripMenuItem mnuModeLearn;

        #endregion

        private readonly MouseInterceptor _mouseInterceptor = new();
        private readonly System.Threading.Timer _updateTimer;
        private readonly System.Windows.Forms.Timer _serviceTimer;
        private readonly DateTime _appStarted = DateTime.Now;
        private readonly List<Form> _activeForms = new();
        private ServerState _firewallState = new();

        // Traffic rate monitoring
        private readonly System.Threading.Timer _trafficTimer;
        private readonly TrafficRateMonitor? _trafficMonitor = new();
        private bool _trafficRateVisible = true;
        private bool _trayMenuShowing;

        private EventHandler<AnyEventArgs>? _balloonClickedCallback;
        private object? _balloonClickedCallbackArgument;
        [AllowNull]
        private SynchronizationContext? _syncCtx;

        private Hotkey? _hotKeyWhitelistExecutable;
        private Hotkey? _hotKeyWhitelistProcess;
        private Hotkey? _hotKeyWhitelistWindow;

        private readonly CmdLineArgs _startupOpts;

        private bool _mLocked;

        private bool Locked
        {
            get => _mLocked;
            set
            {
                _mLocked = value;
                _firewallState.Locked = value;
                if (_mLocked)
                {
                    mnuLock.Text = Resources.Messages.Unlock;
                    mnuLock.Visible = false;
                }
                else
                {
                    mnuLock.Text = Resources.Messages.Lock;
                    mnuLock.Visible = _firewallState.HasPassword;
                }
            }
        }

        public TinyWallController(CmdLineArgs opts)
        {
            _startupOpts = opts;

            ActiveConfig.Controller = ControllerSettings.Load();
            try
            {
                if (!ActiveConfig.Controller.Language.Equals("auto", StringComparison.InvariantCultureIgnoreCase))
                {
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo(ActiveConfig.Controller.Language);
                    Application.CurrentCulture = Thread.CurrentThread.CurrentUICulture;
                }
                else
                {
                    Thread.CurrentThread.CurrentUICulture = Program.DefaultOsCulture!;
                    Application.CurrentCulture = Program.DefaultOsCulture;
                }
            }
            catch
            {
                // ignored
            }

            InitializeComponent();
            Utils.SetRightToLeft(TrayMenu);
            _mouseInterceptor.MouseLButtonDown += MouseInterceptor_MouseLButtonDown;
            _trafficTimer = new System.Threading.Timer(TrafficTimerTick, null, Timeout.Infinite, Timeout.Infinite);
            _updateTimer = new System.Threading.Timer(UpdateTimerTick, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(240));
            _serviceTimer = new System.Windows.Forms.Timer(components);

            Application.Idle += Application_Idle;
            using var p = Process.GetCurrentProcess();
            ProcessManager.WakeMessageQueues(p);
        }

        private void Application_Idle(object sender, EventArgs e)
        {
            if (_syncCtx != null) return;

            _syncCtx = SynchronizationContext.Current;
            Application.Idle -= Application_Idle;
            InitController();
        }

        private void TrayMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            TrayMenuShowing = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                // Manually added
                _hotKeyWhitelistExecutable?.Dispose();
                _hotKeyWhitelistProcess?.Dispose();
                _hotKeyWhitelistWindow?.Dispose();
                _mouseInterceptor.Dispose();

                using (WaitHandle wh = new AutoResetEvent(false))
                {
                    _updateTimer.Dispose(wh);
                    wh.WaitOne();
                }

                using (WaitHandle wh = new AutoResetEvent(false))
                {
                    _trafficTimer.Dispose(wh);
                    wh.WaitOne();
                }
                _trafficMonitor?.Dispose();

                components.Dispose();
                PathMapper.Instance.Dispose();
            }

            base.Dispose(disposing);
        }

        private void VerifyUpdates()
        {
            try
            {
                var descriptor = _firewallState.Update;

                if (descriptor is null) return;

                var mainAppModule = UpdateChecker.GetMainAppModule(descriptor)!;

                if (mainAppModule.ComponentVersion == null || new Version(mainAppModule.ComponentVersion) <=
                    new Version(Application.ProductVersion)) return;

                if (_syncCtx != null)
                    Utils.Invoke(_syncCtx, delegate
                    {
                        var prompt = string.Format(CultureInfo.CurrentCulture,
                            Resources.Messages.UpdateAvailableBubble, mainAppModule.ComponentVersion);
                        ShowBalloonTip(prompt, ToolTipIcon.Info, 5000, StartUpdate, mainAppModule.UpdateUrl);
                    });
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
            if (ActiveConfig.Service.AutoUpdateCheck)
            {
                ThreadPool.QueueUserWorkItem((WaitCallback)delegate
                {
                    VerifyUpdates();
                });
            }
        }

        private void TrafficTimerTick(object? _)
        {
            if (!Monitor.TryEnter(_trafficTimer))
                return;

            try
            {
                _trafficMonitor.Update();
                UpdateTrafficRateText(_trafficMonitor.BytesReceivedPerSec, _trafficMonitor.BytesSentPerSec);
                TrafficRateVisible = true;
            }
            catch
            {
                TrafficRateVisible = false;
            }
            finally
            {
                Monitor.Exit(_trafficTimer);
            }
        }

        void UpdateTrafficRateText(long rxRate, long txRate)
        {
            if (!TrayMenuShowing || !TrafficRateVisible) return;

            var kBytesRxPerSec = (float)rxRate / 1024;
            var kBytesTxPerSec = (float)txRate / 1024;
            var mBytesRxPerSec = kBytesRxPerSec / 1024;
            var mBytesTxPerSec = kBytesTxPerSec / 1024;

            var rxDisplay = (mBytesRxPerSec > 1)
                ? string.Format(CultureInfo.CurrentCulture, "{0:f} MiB/s", mBytesRxPerSec)
                : string.Format(CultureInfo.CurrentCulture, "{0:f} KiB/s", kBytesRxPerSec);

            var txDisplay = (mBytesTxPerSec > 1)
                ? string.Format(CultureInfo.CurrentCulture, "{0:f} MiB/s", mBytesTxPerSec)
                : string.Format(CultureInfo.CurrentCulture, "{0:f} KiB/s", kBytesTxPerSec);

            var trafficRateText = string.Format(CultureInfo.CurrentCulture, "{0}: {1}    {2}: {3}", Resources.Messages.TrafficIn, rxDisplay, Resources.Messages.TrafficOut, txDisplay);

            Utils.Invoke(TrayMenu, delegate
            {
                mnuTrafficRate.Text = trafficRateText;
            });
        }

        private bool TrayMenuShowing
        {
            get => _trayMenuShowing;
            set
            {
                _trayMenuShowing = value;

                // Update more often while visible
                if ((_trafficMonitor != null) && _trayMenuShowing)
                {
                    TrafficTimerTick(null);
                    _trafficTimer.Change(2000, 2000);
                }
                else
                    _trafficTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        private bool TrafficRateVisible
        {
            get => _trafficRateVisible;
            set
            {
                if (value == _trafficRateVisible) return;

                _trafficRateVisible = value;
                Utils.Invoke(TrayMenu, delegate
                {
                    mnuTrafficRate.Visible = _trafficRateVisible;
                    toolStripMenuItem1.Visible = _trafficRateVisible;
                });
            }
        }

        private void StartUpdate(object sender, AnyEventArgs e)
        {
            Updater.StartUpdate();
        }

        private void HotKeyWhitelistProcess_Pressed(object sender, HandledEventArgs e)
        {
            mnuWhitelistByProcess_Click(this, EventArgs.Empty);
        }

        private void HotKeyWhitelistExecutable_Pressed(object sender, HandledEventArgs e)
        {
            mnuWhitelistByExecutable_Click(this, EventArgs.Empty);
        }

        private void HotKeyWhitelistWindow_Pressed(object sender, HandledEventArgs e)
        {
            mnuWhitelistByWindow_Click(this, EventArgs.Empty);
        }

        private void mnuQuit_Click(object sender, EventArgs e)
        {
            Tray.Visible = false;
            ExitThread();
        }

        private void UpdateDisplay()
        {
            // Update UI based on current firewall mode
            var firewallModeName = Resources.Messages.FirewallModeUnknown;

            switch (_firewallState.Mode)
            {
                case FirewallMode.Normal:
                    Tray.Icon = Resources.Icons.firewall;
                    mnuMode.Image = mnuModeNormal.Image;
                    firewallModeName = Resources.Messages.FirewallModeNormal;
                    break;

                case FirewallMode.AllowOutgoing:
                    Tray.Icon = Resources.Icons.shield_red_small;
                    mnuMode.Image = mnuModeAllowOutgoing.Image;
                    firewallModeName = Resources.Messages.FirewallModeAllowOut;
                    break;

                case FirewallMode.BlockAll:
                    Tray.Icon = Resources.Icons.shield_yellow_small;
                    mnuMode.Image = mnuModeBlockAll.Image;
                    firewallModeName = Resources.Messages.FirewallModeBlockAll;
                    break;

                case FirewallMode.Disabled:
                    Tray.Icon = Resources.Icons.shield_grey_small;
                    mnuMode.Image = mnuModeDisabled.Image;
                    firewallModeName = Resources.Messages.FirewallModeDisabled;
                    break;

                case FirewallMode.Learning:
                    Tray.Icon = Resources.Icons.shield_blue_small;
                    mnuMode.Image = mnuModeLearn.Image;
                    firewallModeName = Resources.Messages.FirewallModeLearn;
                    break;

                case FirewallMode.Unknown:
                    Tray.Icon = Resources.Icons.shield_grey_small;
                    mnuMode.Image = Resources.Icons.shield_grey_small.ToBitmap();
                    firewallModeName = Resources.Messages.FirewallModeUnknown;
                    break;
                default:
                    //throw new ArgumentOutOfRangeException();
                    break;
            }

            Tray.Text = string.Format(CultureInfo.CurrentCulture, @"TinyWall
{0}: {1}",
                Resources.Messages.Mode, firewallModeName);

            // Find out if we are locked and if we have a password
            Locked = _firewallState.Locked;

            mnuAllowLocalSubnet.Checked = ActiveConfig.Service.ActiveProfile.AllowLocalSubnet;
            mnuEnableHostsBlocklist.Checked = ActiveConfig.Service.Blocklists.EnableBlocklists;
        }

        private void SetMode(FirewallMode mode)
        {
            var resp = GlobalInstances.Controller!.SwitchFirewallMode(mode);
            HandleSetModeResponse(resp, mode);
        }

        /// <summary>
        /// Switches the firewall mode asynchronously without blocking the UI thread.
        /// </summary>
        private async Task SetModeAsync(FirewallMode mode)
        {
            var resp = await GlobalInstances.Controller!.SwitchFirewallModeAsync(mode);
            HandleSetModeResponse(resp, mode);
        }

        private void HandleSetModeResponse(MessageType resp, FirewallMode mode)
        {
            var userMessage = mode switch
            {
                FirewallMode.Normal => Resources.Messages.TheFirewallIsNowOperatingAsRecommended,
                FirewallMode.AllowOutgoing => Resources.Messages.TheFirewallIsNowAllowsOutgoingConnections,
                FirewallMode.BlockAll => Resources.Messages.TheFirewallIsNowBlockingAllInAndOut,
                FirewallMode.Disabled => Resources.Messages.TheFirewallIsNowDisabled,
                FirewallMode.Learning => Resources.Messages.TheFirewallIsNowLearning,
                _ => string.Empty
            };

            switch (resp)
            {
                case MessageType.MODE_SWITCH:
                    _firewallState.Mode = mode;
                    ShowBalloonTip(userMessage, ToolTipIcon.Info);
                    break;
                case MessageType.INVALID_COMMAND:
                case MessageType.RESPONSE_ERROR:
                case MessageType.RESPONSE_LOCKED:
                case MessageType.COM_ERROR:
                case MessageType.GET_SETTINGS:
                case MessageType.GET_PROCESS_PATH:
                case MessageType.READ_FW_LOG:
                case MessageType.IS_LOCKED:
                case MessageType.UNLOCK:
                case MessageType.REINIT:
                case MessageType.PUT_SETTINGS:
                case MessageType.LOCK:
                case MessageType.SET_PASSPHRASE:
                case MessageType.STOP_SERVICE:
                case MessageType.MINUTE_TIMER:
                case MessageType.REENUMERATE_ADDRESSES:
                case MessageType.DATABASE_UPDATED:
                case MessageType.ADD_TEMPORARY_EXCEPTION:
                case MessageType.RELOAD_WFP_FILTERS:
                case MessageType.DISPLAY_POWER_EVENT:
                    break;
                default:
                    DefaultPopups(resp);
                    break;
            }
        }

        private async void mnuModeDisabled_Click(object sender, EventArgs e)
        {
            if (!await EnsureUnlockedServerAsync())
                return;

            await SetModeAsync(FirewallMode.Disabled);
            UpdateDisplay();
        }

        private async void mnuModeNormal_Click(object sender, EventArgs e)
        {
            if (!await EnsureUnlockedServerAsync())
                return;

            await SetModeAsync(FirewallMode.Normal);
            UpdateDisplay();
        }

        private async void mnuModeBlockAll_Click(object sender, EventArgs e)
        {
            if (!await EnsureUnlockedServerAsync())
                return;

            await SetModeAsync(FirewallMode.BlockAll);
            UpdateDisplay();
        }

        private async void mnuAllowOutgoing_Click(object sender, EventArgs e)
        {
            if (!await EnsureUnlockedServerAsync())
                return;

            await SetModeAsync(FirewallMode.AllowOutgoing);
            UpdateDisplay();
        }

        // Returns true if the local copy of the settings have been updated.
        private bool LoadSettingsFromServer()
        {
            return LoadSettingsFromServer(out var comError, false);
        }

        // Returns true if the local copy of the settings have been updated.
        private bool LoadSettingsFromServer(out bool comError, bool force = false)
        {
            Guid inChangeset = force ? Guid.Empty : GlobalInstances.ClientChangeset;
            Guid outChangeset = inChangeset;
            MessageType ret = GlobalInstances.Controller!.GetServerConfig(out ServerConfiguration? config, out ServerState? state, ref outChangeset);

            comError = (MessageType.COM_ERROR == ret);
            bool updated = (inChangeset != outChangeset);

            if (MessageType.GET_SETTINGS == ret)
            {
                // Update our config based on what we received
                GlobalInstances.ClientChangeset = outChangeset;
                if (config is not null)
                    ActiveConfig.Service = config;
                if (state is not null)
                    _firewallState = state;
            }
            else
            {
                ActiveConfig.Controller = new ControllerSettings();
                ActiveConfig.Service = new ServerConfiguration
                {
                    ActiveProfileName = Resources.Messages.Default
                };
            }

            // See if there is a new notification for the client
            foreach (var t in _firewallState.ClientNotifs)
            {
                switch (t)
                {
                    case MessageType.DATABASE_UPDATED:
                        LoadDatabase();
                        break;
                    case MessageType.INVALID_COMMAND:
                    case MessageType.RESPONSE_ERROR:
                    case MessageType.RESPONSE_LOCKED:
                    case MessageType.COM_ERROR:
                    case MessageType.GET_SETTINGS:
                    case MessageType.GET_PROCESS_PATH:
                    case MessageType.READ_FW_LOG:
                    case MessageType.IS_LOCKED:
                    case MessageType.UNLOCK:
                    case MessageType.MODE_SWITCH:
                    case MessageType.REINIT:
                    case MessageType.PUT_SETTINGS:
                    case MessageType.LOCK:
                    case MessageType.SET_PASSPHRASE:
                    case MessageType.STOP_SERVICE:
                    case MessageType.MINUTE_TIMER:
                    case MessageType.REENUMERATE_ADDRESSES:
                    case MessageType.ADD_TEMPORARY_EXCEPTION:
                    case MessageType.RELOAD_WFP_FILTERS:
                    case MessageType.DISPLAY_POWER_EVENT:
                        break;
                    default:
                        //throw new ArgumentOutOfRangeException();
                        break;
                }
            }

            _firewallState.ClientNotifs.Clear();

            if (updated)
                UpdateDisplay();

            return updated;
        }

        /// <summary>
        /// Loads settings from the server asynchronously without blocking the UI thread.
        /// </summary>
        private async Task<bool> LoadSettingsFromServerAsync(bool force = false)
        {
            Guid inChangeset = force ? Guid.Empty : GlobalInstances.ClientChangeset;
            var (ret, config, state, outChangeset) = await GlobalInstances.Controller!.GetServerConfigAsync(inChangeset);

            bool updated = (inChangeset != outChangeset);

            if (MessageType.GET_SETTINGS == ret)
            {
                // Update our config based on what we received
                GlobalInstances.ClientChangeset = outChangeset;
                if (config is not null)
                    ActiveConfig.Service = config;
                if (state is not null)
                    _firewallState = state;
            }
            else if (MessageType.COM_ERROR != ret)
            {
                ActiveConfig.Controller = new ControllerSettings();
                ActiveConfig.Service = new ServerConfiguration
                {
                    ActiveProfileName = Resources.Messages.Default
                };
            }

            // See if there is a new notification for the client
            foreach (var t in _firewallState.ClientNotifs)
            {
                switch (t)
                {
                    case MessageType.DATABASE_UPDATED:
                        await LoadDatabaseAsync();
                        break;
                }
            }

            _firewallState.ClientNotifs.Clear();

            if (updated)
                UpdateDisplay();

            return updated;
        }

        private void TrayMenu_Opening(object sender, CancelEventArgs e)
        {
            e.Cancel = false;

            if (_firewallState.Mode == FirewallMode.Unknown)
            {
                if (!TinyWallDoctor.IsServiceRunning(Utils.LOG_ID_GUI, false))
                {
                    ShowBalloonTip(Resources.Messages.TheTinyWallServiceIsUnavailable, ToolTipIcon.Error, 10000);
                    e.Cancel = true;
                }
            }

            TrayMenuShowing = true;

            Locked = GlobalInstances.Controller!.IsServerLocked;
            UpdateDisplay();
        }

        private void mnuWhitelistByExecutable_Click(object sender, EventArgs e)
        {
            if (FlashIfOpen(typeof(SettingsForm)))
                return;

            if (!EnsureUnlockedServer())
                return;

            using var dummy = new Form();
            try
            {
                _activeForms.Add(dummy);
                if (ofd.ShowDialog() != DialogResult.OK)
                    return;
            }
            finally
            {
                _activeForms.Remove(dummy);
            }

            var subj = new ExecutableSubject(PathMapper.Instance.ConvertPathIgnoreErrors(ofd.FileName, PathFormat.Win32));
            AddExceptions(GlobalInstances.AppDatabase!.GetExceptionsForApp(subj, true, out _));
        }

        public void WhitelistProcesses(List<ProcessInfo> list)
        {
            var exceptions = new List<FirewallExceptionV3>();

            foreach (var sel in list)
            {
                if (string.IsNullOrEmpty(sel.Path))
                    continue;

                var subjects = new List<ExceptionSubject>();
                if (sel.Package.HasValue)
                    subjects.Add(new AppContainerSubject(sel.Package.Value));
                else if (sel.Services.Count > 0)
                {
                    subjects.AddRange(sel.Services.Select(srv => new ServiceSubject(sel.Path, srv)).Cast<ExceptionSubject>());
                }
                else
                    subjects.Add(new ExecutableSubject(sel.Path));

                foreach (var subj in from subj in subjects let found = exceptions.Any(ex => ex.Subject.Equals(subj)) where !found select subj)
                {
                    // Try to recognize app based on this file
                    exceptions.AddRange(GlobalInstances.AppDatabase!.GetExceptionsForApp(subj, true, out _));
                }
            }

            AddExceptions(exceptions);
        }

        private void mnuWhitelistByProcess_Click(object sender, EventArgs e)
        {
            if (FlashIfOpen(typeof(SettingsForm)))
                return;

            if (!EnsureUnlockedServer())
                return;

            var selection = new List<ProcessInfo>();

            using (var pf = new ProcessesForm(true))
            {
                try
                {
                    _activeForms.Add(pf);

                    if (pf.ShowDialog(null) == DialogResult.Cancel)
                        return;

                    selection.AddRange(pf.Selection);
                }
                finally
                {
                    _activeForms.Remove(pf);
                }
            }
            WhitelistProcesses(selection);
        }

        internal TwMessage ApplyFirewallSettings(ServerConfiguration srvConfig, bool showUi = true)
        {
            if (!EnsureUnlockedServer(showUi))
                return TwMessageLocked.Instance;

            var resp = GlobalInstances.Controller!.SetServerConfig(srvConfig, GlobalInstances.ClientChangeset);

            switch (resp.Type)
            {
                case MessageType.PUT_SETTINGS:
                    var respArgs = (TwMessagePutSettings)resp;
                    if (respArgs.State is not null)
                        _firewallState = respArgs.State;
                    ActiveConfig.Service = respArgs.Config;
                    GlobalInstances.ClientChangeset = respArgs.Changeset;
                    if (showUi)
                    {
                        if (respArgs.Warning)
                            ShowBalloonTip(Resources.Messages.SettingHaveChangedRetry, ToolTipIcon.Warning);
                        else
                            ShowBalloonTip(Resources.Messages.TheFirewallSettingsHaveBeenUpdated, ToolTipIcon.Info);
                    }
                    break;
                case MessageType.RESPONSE_ERROR:
                    if (showUi)
                        ShowBalloonTip(Resources.Messages.CouldNotApplySettingsInternalError, ToolTipIcon.Warning);
                    break;
                case MessageType.INVALID_COMMAND:
                case MessageType.RESPONSE_LOCKED:
                case MessageType.COM_ERROR:
                case MessageType.GET_SETTINGS:
                case MessageType.GET_PROCESS_PATH:
                case MessageType.READ_FW_LOG:
                case MessageType.IS_LOCKED:
                case MessageType.UNLOCK:
                case MessageType.MODE_SWITCH:
                case MessageType.REINIT:
                case MessageType.LOCK:
                case MessageType.SET_PASSPHRASE:
                case MessageType.STOP_SERVICE:
                case MessageType.MINUTE_TIMER:
                case MessageType.REENUMERATE_ADDRESSES:
                case MessageType.DATABASE_UPDATED:
                case MessageType.ADD_TEMPORARY_EXCEPTION:
                case MessageType.RELOAD_WFP_FILTERS:
                case MessageType.DISPLAY_POWER_EVENT:
                    break;
                default:
                    if (showUi)
                        DefaultPopups(resp.Type);
                    LoadSettingsFromServer();
                    break;
            }

            return resp;
        }

        internal async Task<TwMessage> ApplyFirewallSettingsAsync(ServerConfiguration srvConfig, bool showUi = true)
        {
            if (!await EnsureUnlockedServerAsync(showUi))
                return TwMessageLocked.Instance;

            var resp = await GlobalInstances.Controller!.SetServerConfigAsync(srvConfig, GlobalInstances.ClientChangeset);

            switch (resp.Type)
            {
                case MessageType.PUT_SETTINGS:
                    var respArgs = (TwMessagePutSettings)resp;
                    if (respArgs.State is not null)
                        _firewallState = respArgs.State;
                    ActiveConfig.Service = respArgs.Config;
                    GlobalInstances.ClientChangeset = respArgs.Changeset;
                    if (showUi)
                    {
                        if (respArgs.Warning)
                            ShowBalloonTip(Resources.Messages.SettingHaveChangedRetry, ToolTipIcon.Warning);
                        else
                            ShowBalloonTip(Resources.Messages.TheFirewallSettingsHaveBeenUpdated, ToolTipIcon.Info);
                    }
                    break;
                case MessageType.RESPONSE_ERROR:
                    if (showUi)
                        ShowBalloonTip(Resources.Messages.CouldNotApplySettingsInternalError, ToolTipIcon.Warning);
                    break;
                default:
                    if (showUi)
                        DefaultPopups(resp.Type);
                    await LoadSettingsFromServerAsync();
                    break;
            }

            return resp;
        }

        private void DefaultPopups(MessageType op)
        {
            switch (op)
            {
                case MessageType.INVALID_COMMAND:
                case MessageType.GET_SETTINGS:
                case MessageType.GET_PROCESS_PATH:
                case MessageType.READ_FW_LOG:
                case MessageType.IS_LOCKED:
                case MessageType.UNLOCK:
                case MessageType.MODE_SWITCH:
                case MessageType.REINIT:
                case MessageType.PUT_SETTINGS:
                case MessageType.LOCK:
                case MessageType.SET_PASSPHRASE:
                case MessageType.STOP_SERVICE:
                case MessageType.MINUTE_TIMER:
                case MessageType.REENUMERATE_ADDRESSES:
                case MessageType.DATABASE_UPDATED:
                case MessageType.ADD_TEMPORARY_EXCEPTION:
                case MessageType.RELOAD_WFP_FILTERS:
                case MessageType.DISPLAY_POWER_EVENT:
                    break;
                default:
                    ShowBalloonTip(Resources.Messages.Success, ToolTipIcon.Info);
                    break;
                case MessageType.RESPONSE_ERROR:
                    ShowBalloonTip(Resources.Messages.OperationFailed, ToolTipIcon.Error);
                    break;
                case MessageType.RESPONSE_LOCKED:
                    ShowBalloonTip(Resources.Messages.TinyWallIsCurrentlyLocked, ToolTipIcon.Warning);
                    break;
                case MessageType.COM_ERROR:
                    ShowBalloonTip(Resources.Messages.CommunicationWithTheServiceError, ToolTipIcon.Error);
                    break;
            }
        }

        public bool FlashIfOpen(Type formType)
        {
            foreach (var openForm in _activeForms.Where(openForm => openForm.GetType() == formType))
            {
                openForm.Activate();
                openForm.BringToFront();
                WindowFlasher.Flash(openForm.Handle, 2);
                return true;
            }

            return false;
        }
        public bool FlashIfOpen(Form frm)
        {
            return FlashIfOpen(frm.GetType());
        }

        private async void mnuManage_Click(object sender, EventArgs e)
        {
            if (!await EnsureUnlockedServerAsync())
                return;

            // The settings form should not be used with other windows at the same time
            if (_activeForms.Count != 0)
            {
                FlashIfOpen(_activeForms[0]);
                return;
            }

            await LoadSettingsFromServerAsync();

            using var sf = new SettingsForm(Utils.DeepClone(ActiveConfig.Service), Utils.DeepClone(ActiveConfig.Controller));
            _activeForms.Add(sf);
            try
            {
                if (sf.ShowDialog() != DialogResult.OK) return;

                var oldLang = ActiveConfig.Controller.Language;

                // Save settings
                ActiveConfig.Controller = sf.TmpConfig.Controller;
                ActiveConfig.Controller.Save();
                await ApplyFirewallSettingsAsync(sf.TmpConfig.Service);

                // Handle password change request
                string? newPassword = sf.NewPassword;
                if (newPassword is not null)
                {
                    // If the new password is empty, we do not hash it because an empty password
                    // is a special value signalizing the non-existence of a password.
                    MessageType resp = await GlobalInstances.Controller!.SetPassphraseAsync(string.IsNullOrEmpty(newPassword) ? string.Empty : Hasher.HashString(newPassword));
                    if (resp != MessageType.SET_PASSPHRASE)
                    {
                        // Only display a popup for setting the password if it did not succeed
                        DefaultPopups(resp);
                        return;
                    }
                    else
                    {
                        // If the operation is successful, do not report anything as we will be setting
                        // the other settings too and we want to avoid multiple popups.
                        _firewallState.HasPassword = !string.IsNullOrEmpty(newPassword);
                    }
                }

                if (oldLang == ActiveConfig.Controller.Language) return;

                Program.RestartOnQuit = true;
                ExitThread();
            }
            finally
            {
                _activeForms.Remove(sf);
                ApplyControllerSettings();
                UpdateDisplay();
            }
        }

        private void mnuWhitelistByWindow_Click(object sender, EventArgs e)
        {
            if (!EnsureUnlockedServer())
                return;

            if (!_mouseInterceptor.IsStarted)
            {
                _mouseInterceptor.Start();
                ShowBalloonTip(Resources.Messages.ClickOnAWindowWhitelisting, ToolTipIcon.Info);
            }
            else
            {
                _mouseInterceptor.Stop();
                ShowBalloonTip(Resources.Messages.WhitelistingCancelled, ToolTipIcon.Info);
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

            ThreadPool.QueueUserWorkItem((WaitCallback)delegate
            {
                if (_syncCtx != null)
                    Utils.Invoke(_syncCtx, delegate
                    {
                        _mouseInterceptor.Stop();

                        uint pid = Utils.GetPidUnderCursor(x, y);
                        string exePath = Utils.GetPathOfProcessUseTwService(pid, GlobalInstances.Controller!);
                        var packageList = new UwpPackageList();
                        var appContainer = packageList.FindPackageForProcess(pid);

                        ExceptionSubject subj;
                        if (appContainer.HasValue)
                        {
                            subj = new AppContainerSubject(appContainer.Value);
                        }
                        else if (string.IsNullOrEmpty(exePath))
                        {
                            ShowBalloonTip(Resources.Messages.CannotGetExecutablePathWhitelisting, ToolTipIcon.Error);
                            return;
                        }
                        else
                        {
                            subj = new ExecutableSubject(exePath);
                        }

                        AddExceptions(GlobalInstances.AppDatabase!.GetExceptionsForApp(subj, true, out _));
                    });
            });
        }

        // Called when a user double-clicks on a popup to edit the most recent exception
        private void EditRecentException(object sender, AnyEventArgs e)
        {
            using var f = new ApplicationExceptionForm((FirewallExceptionV3)e.Arg!);
            if (f.ShowDialog() == DialogResult.Cancel)
                return;

            // Add exceptions, along with other files that belong to this app
            AddExceptions(f.ExceptionSettings, false);
        }

        internal void AddExceptions(List<FirewallExceptionV3> list, bool showEditUi = true)
        {
            if (list.Count == 0)
                // Nothing to do
                return;

            LoadSettingsFromServer();

            bool single = (list.Count == 1);

            if (single && ActiveConfig.Controller.AskForExceptionDetails && showEditUi)
            {
                using var f = new ApplicationExceptionForm(list[0]);
                if (f.ShowDialog() == DialogResult.Cancel)
                    return;

                list.Clear();
                list.AddRange(f.ExceptionSettings);
                single = (list.Count == 1);
            }

            var confCopy = Utils.DeepClone(ActiveConfig.Service);
            confCopy.ActiveProfile.AddExceptions(list);

            if (!single)
            {
                ApplyFirewallSettings(confCopy, true);
                return;
            }

            var resp = ApplyFirewallSettings(confCopy, false);
            switch (resp.Type)
            {
                case MessageType.PUT_SETTINGS:
                    var respArgs = (TwMessagePutSettings)resp;
                    if (respArgs.Warning)
                    {
                        // We tell the user to re-do his changes to the settings to prevent overwriting the wrong configuration.
                        ShowBalloonTip(Resources.Messages.SettingHaveChangedRetry, ToolTipIcon.Warning);
                    }
                    else
                    {
                        var signedAndValid = false;
                        if (list[0].Subject is ExecutableSubject exesub)
                            signedAndValid = exesub is { IsSigned: true, CertValid: true };

                        ShowBalloonTip(
                            signedAndValid
                                ? string.Format(CultureInfo.CurrentCulture,
                                    Resources.Messages.FirewallRulesForRecognizedChanged, list[0].Subject.ToString())
                                : string.Format(CultureInfo.CurrentCulture,
                                    Resources.Messages.FirewallRulesForUnrecognizedChanged, list[0].Subject.ToString()),
                            ToolTipIcon.Info, 5000, EditRecentException, Utils.DeepClone(list[0]));
                    }
                    break;
                case MessageType.RESPONSE_ERROR:
                    ShowBalloonTip(string.Format(CultureInfo.CurrentCulture, Resources.Messages.CouldNotWhitelistProcess, list[0].Subject.ToString()), ToolTipIcon.Warning);
                    break;
                case MessageType.INVALID_COMMAND:
                case MessageType.RESPONSE_LOCKED:
                case MessageType.COM_ERROR:
                case MessageType.GET_SETTINGS:
                case MessageType.GET_PROCESS_PATH:
                case MessageType.READ_FW_LOG:
                case MessageType.IS_LOCKED:
                case MessageType.UNLOCK:
                case MessageType.MODE_SWITCH:
                case MessageType.REINIT:
                case MessageType.LOCK:
                case MessageType.SET_PASSPHRASE:
                case MessageType.STOP_SERVICE:
                case MessageType.MINUTE_TIMER:
                case MessageType.REENUMERATE_ADDRESSES:
                case MessageType.DATABASE_UPDATED:
                case MessageType.ADD_TEMPORARY_EXCEPTION:
                case MessageType.RELOAD_WFP_FILTERS:
                case MessageType.DISPLAY_POWER_EVENT:
                    break;
                default:
                    DefaultPopups(resp.Type);
                    LoadSettingsFromServer();
                    break;
            }
        }

        internal bool EnsureUnlockedServer(bool showUi = true)
        {
            Locked = GlobalInstances.Controller!.IsServerLocked;
            if (!Locked)
                return true;

            using var pf = new PasswordForm();
            pf.BringToFront();
            pf.Activate();
            if (pf.ShowDialog() != DialogResult.OK) return false;

            MessageType resp = GlobalInstances.Controller.TryUnlockServer(pf.PassHash);
            switch (resp)
            {
                case MessageType.UNLOCK:
                    Locked = false;
                    return true;
                case MessageType.RESPONSE_ERROR:
                    if (showUi)
                        ShowBalloonTip(Resources.Messages.UnlockFailed, ToolTipIcon.Error);
                    break;
                case MessageType.INVALID_COMMAND:
                case MessageType.RESPONSE_LOCKED:
                case MessageType.COM_ERROR:
                case MessageType.GET_SETTINGS:
                case MessageType.GET_PROCESS_PATH:
                case MessageType.READ_FW_LOG:
                case MessageType.IS_LOCKED:
                case MessageType.MODE_SWITCH:
                case MessageType.REINIT:
                case MessageType.PUT_SETTINGS:
                case MessageType.LOCK:
                case MessageType.SET_PASSPHRASE:
                case MessageType.STOP_SERVICE:
                case MessageType.MINUTE_TIMER:
                case MessageType.REENUMERATE_ADDRESSES:
                case MessageType.DATABASE_UPDATED:
                case MessageType.ADD_TEMPORARY_EXCEPTION:
                case MessageType.RELOAD_WFP_FILTERS:
                case MessageType.DISPLAY_POWER_EVENT:
                    break;
                default:
                    if (showUi)
                        DefaultPopups(resp);
                    break;
            }

            return false;
        }

        /// <summary>
        /// Checks if the server is locked and attempts to unlock it asynchronously.
        /// Does not block the UI thread during the lock check or unlock attempt.
        /// </summary>
        internal async Task<bool> EnsureUnlockedServerAsync(bool showUi = true)
        {
            Locked = await GlobalInstances.Controller!.IsServerLockedAsync();
            if (!Locked)
                return true;

            using var pf = new PasswordForm();
            pf.BringToFront();
            pf.Activate();
            if (pf.ShowDialog() != DialogResult.OK) return false;

            MessageType resp = await GlobalInstances.Controller.TryUnlockServerAsync(pf.PassHash);
            switch (resp)
            {
                case MessageType.UNLOCK:
                    Locked = false;
                    return true;
                case MessageType.RESPONSE_ERROR:
                    if (showUi)
                        ShowBalloonTip(Resources.Messages.UnlockFailed, ToolTipIcon.Error);
                    break;
                case MessageType.INVALID_COMMAND:
                case MessageType.RESPONSE_LOCKED:
                case MessageType.COM_ERROR:
                case MessageType.GET_SETTINGS:
                case MessageType.GET_PROCESS_PATH:
                case MessageType.READ_FW_LOG:
                case MessageType.IS_LOCKED:
                case MessageType.MODE_SWITCH:
                case MessageType.REINIT:
                case MessageType.PUT_SETTINGS:
                case MessageType.LOCK:
                case MessageType.SET_PASSPHRASE:
                case MessageType.STOP_SERVICE:
                case MessageType.MINUTE_TIMER:
                case MessageType.REENUMERATE_ADDRESSES:
                case MessageType.DATABASE_UPDATED:
                case MessageType.ADD_TEMPORARY_EXCEPTION:
                case MessageType.RELOAD_WFP_FILTERS:
                case MessageType.DISPLAY_POWER_EVENT:
                    break;
                default:
                    if (showUi)
                        DefaultPopups(resp);
                    break;
            }

            return false;
        }

        private void mnuLock_Click(object sender, EventArgs e)
        {
            MessageType lockResp = GlobalInstances.Controller!.LockServer();

            if ((lockResp == MessageType.LOCK) || (lockResp == MessageType.RESPONSE_LOCKED))
            {
                Locked = true;
            }

            UpdateDisplay();
        }

        private void mnuAllowLocalSubnet_Click(object sender, EventArgs e)
        {
            if (!EnsureUnlockedServer())
                return;

            LoadSettingsFromServer();

            // Copy, so that settings are not changed if they cannot be saved
            ServerConfiguration confCopy = Utils.DeepClone(ActiveConfig.Service);
            confCopy.ActiveProfile.AllowLocalSubnet = !mnuAllowLocalSubnet.Checked;
            ApplyFirewallSettings(confCopy);

            mnuAllowLocalSubnet.Checked = ActiveConfig.Service.ActiveProfile.AllowLocalSubnet;
        }

        private void mnuEnableHostsBlocklist_Click(object sender, EventArgs e)
        {
            if (!EnsureUnlockedServer())
                return;

            LoadSettingsFromServer();

            // Copy, so that settings are not changed if they cannot be saved
            ServerConfiguration confCopy = Utils.DeepClone(ActiveConfig.Service);
            confCopy.Blocklists.EnableBlocklists = !mnuEnableHostsBlocklist.Checked;
            ApplyFirewallSettings(confCopy);

            mnuEnableHostsBlocklist.Checked = ActiveConfig.Service.Blocklists.EnableBlocklists;
        }

        private void ShowBalloonTip(string msg, ToolTipIcon icon, int periodMs = 5000, EventHandler<AnyEventArgs>? balloonClicked = null, object? handlerArg = null)
        {
            _balloonClickedCallback = balloonClicked;
            _balloonClickedCallbackArgument = handlerArg;
            Tray.ShowBalloonTip(periodMs, "TinyWall", msg, icon);
            Thread.Sleep(500);
        }

        private static void SetHotkey(ComponentResourceManager resourceManager, ref Hotkey? hk, HandledEventHandler hkCallback, Keys keyCode, ToolStripMenuItem menu, string mnuName)
        {
            if (ActiveConfig.Controller.EnableGlobalHotkeys)
            {
                // enable hotkey
                if (hk != null) return;

                hk = new Hotkey(keyCode, true, true, false, false);
                hk.Pressed += hkCallback;
                hk.Register();
                resourceManager.ApplyResources(menu, mnuName);
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
            var resources = new ComponentResourceManager(typeof(TinyWallController));
            SetHotkey(resources, ref _hotKeyWhitelistWindow, HotKeyWhitelistWindow_Pressed, Keys.W, mnuWhitelistByWindow, "mnuWhitelistByWindow");
            SetHotkey(resources, ref _hotKeyWhitelistExecutable, HotKeyWhitelistExecutable_Pressed, Keys.E, mnuWhitelistByExecutable, "mnuWhitelistByExecutable");
            SetHotkey(resources, ref _hotKeyWhitelistProcess, HotKeyWhitelistProcess_Pressed, Keys.P, mnuWhitelistByProcess, "mnuWhitelistByProcess");
        }

        private void mnuElevate_Click(object sender, EventArgs e)
        {
            try
            {
                Utils.StartProcess(Utils.ExecutablePath, string.Empty, true);
                Application.Exit();
            }
            catch
            {
                ShowBalloonTip(Resources.Messages.CouldNotElevatePrivileges, ToolTipIcon.Error);
            }
        }

        private void mnuConnections_Click(object sender, EventArgs e)
        {
            if (FlashIfOpen(typeof(SettingsForm)))
                return;

            if (FlashIfOpen(typeof(ConnectionsForm)))
                return;

            using var cf = new ConnectionsForm(this);
            try
            {
                _activeForms.Add(cf);
                cf.ShowDialog();
            }
            finally
            {
                _activeForms.Remove(cf);
            }
        }

        private void Tray_MouseClick(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    Utils.SafeNativeMethods.DoMouseRightClick();
                    break;
                case MouseButtons.Middle:
                    mnuConnections_Click(sender, e);
                    break;
                case MouseButtons.None:
                case MouseButtons.Right:
                case MouseButtons.XButton1:
                case MouseButtons.XButton2:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Tray_BalloonTipClicked(object sender, EventArgs e)
        {
            _balloonClickedCallback?.Invoke(Tray, new AnyEventArgs(_balloonClickedCallbackArgument));
        }

        private void LoadDatabase()
        {
            try
            {
                GlobalInstances.AppDatabase = DatabaseClasses.AppDatabase.Load();
            }
            catch
            {
                GlobalInstances.AppDatabase = new DatabaseClasses.AppDatabase();
                ThreadPool.QueueUserWorkItem((WaitCallback)delegate
                {
                    if (_syncCtx != null)
                        Utils.Invoke(_syncCtx, delegate
                            {
                                ShowBalloonTip(Resources.Messages.DatabaseIsMissingOrCorrupt, ToolTipIcon.Warning);
                            });
                });

                throw;
            }
        }

        private async Task LoadDatabaseAsync()
        {
            try
            {
                GlobalInstances.AppDatabase = await Task.Run(() => DatabaseClasses.AppDatabase.Load());
            }
            catch
            {
                GlobalInstances.AppDatabase = new DatabaseClasses.AppDatabase();
                if (_syncCtx != null)
                {
                    Utils.Invoke(_syncCtx, delegate
                    {
                        ShowBalloonTip(Resources.Messages.DatabaseIsMissingOrCorrupt, ToolTipIcon.Warning);
                    });
                }
                throw;
            }
        }

        private void AutoWhitelist()
        {
            // Copy, so that settings are not changed if they cannot be saved
            ServerConfiguration confCopy = Utils.DeepClone(ActiveConfig.Service);
            confCopy.ActiveProfile.AddExceptions(GlobalInstances.AppDatabase!.FastSearchMachineForKnownApps());
            ApplyFirewallSettings(confCopy);
        }

        private void mnuModeLearn_Click(object sender, EventArgs e)
        {
            if (!EnsureUnlockedServer())
                return;

            Utils.SplitFirstLine(Resources.Messages.YouAreAboutToEnterLearningMode, out var firstLine, out var contentLines);

            var dialog = new TaskDialog
            {
                CustomMainIcon = Resources.Icons.firewall,
                WindowTitle = Resources.Messages.TinyWall,
                MainInstruction = firstLine,
                Content = contentLines,
                AllowDialogCancellation = false,
                CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No
            };

            if (dialog.Show() != (int)DialogResult.Yes)
                return;

            SetMode(FirewallMode.Learning);
            UpdateDisplay();
        }

        private void InitController()
        {
            mnuTrafficRate.Text = string.Format(CultureInfo.CurrentCulture, @"{0}: {1}   {2}: {3}", Resources.Messages.TrafficIn, "...", Resources.Messages.TrafficOut, "...");

            // We will load our database parallel to other things to improve startup performance
            using (var barrier = new ThreadBarrier(2))
            {
                ThreadPool.QueueUserWorkItem((WaitCallback)delegate
                {
                    try
                    {
                        LoadDatabase();
                    }
                    catch
                    {
                        // ignored
                    }
                    finally
                    {
                        barrier.Wait();
                    }
                });

                // --------------- CODE BETWEEN HERE MUST NOT USE DATABASE, SINCE IT IS BEING LOADED PARALLEL ---------------
                // BEGIN
                TrayMenu.Closed += TrayMenu_Closed;
                Tray.ContextMenuStrip = TrayMenu;
                mnuElevate.Visible = !Utils.RunningAsAdmin();
                mnuModeDisabled.Image = Resources.Icons.shield_grey_small.ToBitmap();
                mnuModeAllowOutgoing.Image = Resources.Icons.shield_red_small.ToBitmap();
                mnuModeBlockAll.Image = Resources.Icons.shield_yellow_small.ToBitmap();
                mnuModeNormal.Image = Resources.Icons.shield_green_small.ToBitmap();
                mnuModeLearn.Image = Resources.Icons.shield_blue_small.ToBitmap();
                TrayMenuShowing = false;

                ApplyControllerSettings();
                GlobalInstances.InitClient();

                barrier.Wait();
                // END
                // --------------- CODE BETWEEN HERE MUST NOT USE DATABASE, SINCE IT IS BEING LOADED PARALLEL ---------------
                // --- THREAD BARRIER ---
            }

            LoadSettingsFromServer(out bool comError, true);
#if !DEBUG
            if (comError)
            {
                if (TinyWallDoctor.EnsureServiceInstalledAndRunning(Utils.LOG_ID_GUI, false))
                    LoadSettingsFromServer(out comError, true);
                else
                    MessageBox.Show(Resources.Messages.TheTinyWallServiceIsUnavailable, Resources.Messages.TinyWall, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
#endif

            if ((_firewallState.Mode != FirewallMode.Unknown) || (!_startupOpts.startup))
            {
                Tray.Visible = true;

                if (_startupOpts.autowhitelist)
                {
                    AutoWhitelist();
                }

                if (_startupOpts.updatenow)
                {
                    StartUpdate(this, AnyEventArgs.Empty);
                }
            }
            else
            {
                // Keep on trying to reach the service
                _serviceTimer.Tick += ServiceTimer_Tick;
                _serviceTimer.Interval = 2000;
                _serviceTimer.Enabled = true;
            }
        }

        private void ServiceTimer_Tick(object sender, EventArgs e)
        {
            LoadSettingsFromServer(out var comError, true);

            var maxTimeElapsed = (DateTime.Now - _appStarted) > TimeSpan.FromSeconds(90);

            if (comError && !maxTimeElapsed) return;

            _serviceTimer.Enabled = false;
            Tray.Visible = true;
        }
    }

    internal class AnyEventArgs : EventArgs
    {
        public new static AnyEventArgs Empty { get; } = new();

        public AnyEventArgs(object? arg = null)
        {
            Arg = arg;
        }

        public object? Arg { get; }
    }
}
