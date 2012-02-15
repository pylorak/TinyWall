using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;

namespace PKSoft
{
    internal partial class MainForm : Form
    {
        private System.Threading.Timer UpdateTimer;
        private MouseInterceptor MouseInterceptor;
        private SettingsForm ShownSettings;
        private FirewallMode FwMode;

        // Traffic rate monitoring
        private System.Threading.Timer TrafficTimer = null;
        private ManagementObjectSearcher searcher = null;
        private const int TRAFFIC_TIMER_INTERVAL = 3;
        private ulong bytesRxTotal = 0;
        private ulong bytesTxTotal = 0;
        private string rxDisplay = string.Empty;
        private string txDisplay = string.Empty;

        private EventHandler<AnyEventArgs> BalloonClickedCallback;
        private object BalloonClickedCallbackArgument;

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
                    mnuLock.Text = "Unlock";
                }
                else
                {
                    mnuLock.Text = "Lock";
                }
            }
        }

        internal MainForm(CmdLineArgs opts)
        {
            this.StartupOpts = opts;

            InitializeComponent();
            this.Icon = Resources.Icons.firewall;
            this.Tray.Icon = Resources.Icons.firewall;
        }

        private void ApplyControllerSettings()
        {
            if ((SettingsManager.GlobalConfig.AutoUpdateCheck) && (UpdateTimer == null))
            {
                UpdateTimer = new System.Threading.Timer(UpdateTimerTick, null, TimeSpan.FromMinutes(2), TimeSpan.FromHours(2));
            }
            if ((!SettingsManager.GlobalConfig.AutoUpdateCheck) && (UpdateTimer != null))
            {
                if (UpdateTimer != null)
                {
                    using (WaitHandle wh = new AutoResetEvent(false))
                    {
                        UpdateTimer.Dispose(wh);
                        wh.WaitOne();
                    }
                    UpdateTimer = null;
                }
            }
        }

        private void UpdateTimerTick(object state)
        {
            try
            {
                Message resp = GlobalInstances.CommunicationMan.QueueMessageSimple(TinyWallCommands.GET_UPDATE_DESCRIPTOR);
                UpdateDescriptor descriptor = (UpdateDescriptor)resp.Arguments[0];
                if (descriptor != null)
                {
                    UpdateModule MainAppModule = UpdateChecker.GetMainAppModule(descriptor);
                    if (new Version(MainAppModule.ComponentVersion) > new Version(System.Windows.Forms.Application.ProductVersion))
                    {
                        Utils.Invoke(this, (MethodInvoker)delegate()
                        {
                            string prompt = "A newer version " + MainAppModule.ComponentVersion + " of TinyWall is available. Click this bubble to start the update process.";
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
            // Use WMI technology to retrieve the interface details
            if (searcher == null)
                searcher = new ManagementObjectSearcher("select BytesReceivedPersec, BytesSentPersec from Win32_PerfRawData_Tcpip_NetworkInterface");

            ulong bytesRxNewTotal = 0;
            ulong bytesTxNewTotal = 0;
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject adapterObject in moc)
            {
                bytesRxNewTotal += (ulong)adapterObject["BytesReceivedPersec"];
                bytesTxNewTotal += (ulong)adapterObject["BytesSentPersec"];
            }

            // If this is the first time we are running.
            if ((bytesRxTotal == 0) && (bytesTxTotal == 0))
            {
                bytesRxTotal = bytesRxNewTotal;
                bytesTxTotal = bytesTxNewTotal;
            }

            float RxDiff = (bytesRxNewTotal - bytesRxTotal) / (float)TRAFFIC_TIMER_INTERVAL;
            float TxDiff = (bytesTxNewTotal - bytesTxTotal) / (float)TRAFFIC_TIMER_INTERVAL;
            bytesRxTotal = bytesRxNewTotal;
            bytesTxTotal = bytesTxNewTotal;

            float KBytesRxPerSec = RxDiff / 1024;
            float KBytesTxPerSec = TxDiff / 1024;
            float MBytesRxPerSec = KBytesRxPerSec / 1024;
            float MBytesTxPerSec = KBytesTxPerSec / 1024;

            if (MBytesRxPerSec > 1)
                rxDisplay = string.Format("{0:f}MB/s", MBytesRxPerSec);
            else
                rxDisplay = string.Format("{0:f}KB/s", KBytesRxPerSec);

            if (MBytesTxPerSec > 1)
                txDisplay = string.Format("{0:f}MB/s", MBytesTxPerSec);
            else
                txDisplay = string.Format("{0:f}KB/s", KBytesTxPerSec);
        }
        
        private void StartUpdate(object sender, AnyEventArgs e)
        {
            UpdateForm.StartUpdate(this);
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
            System.Windows.Forms.Application.Exit();
        }

        private void UpdateDisplay()
        {
            Message resp;

            // Update string showing current network profile
            mnuCurrentPolicy.Text = "You are in: " + SettingsManager.CurrentZone.ZoneName + " zone";

            // Find out current firewall mode
            resp = GlobalInstances.CommunicationMan.QueueMessageSimple(TinyWallCommands.GET_MODE);
            FwMode = (resp.Command == TinyWallCommands.RESPONSE_OK) ? (FirewallMode)resp.Arguments[0] : FirewallMode.Unknown;

            // Update UI based on current firewall mode
            switch (FwMode)
            {
                case FirewallMode.Normal:
                    Tray.Icon = PKSoft.Resources.Icons.firewall;
                    mnuMode.Image = mnuModeNormal.Image;
                    break;

                case FirewallMode.AllowOutgoing:
                    Tray.Icon = PKSoft.Resources.Icons.shield_red_small;
                    mnuMode.Image = mnuModeAllowOutgoing.Image;
                    break;

                case FirewallMode.BlockAll:
                    Tray.Icon = PKSoft.Resources.Icons.shield_yellow_small;
                    mnuMode.Image = mnuModeBlockAll.Image;
                    break;

                case FirewallMode.Disabled:
                    Tray.Icon = PKSoft.Resources.Icons.shield_grey_small;
                    mnuMode.Image = mnuModeDisabled.Image;
                    break;
                case FirewallMode.Unknown:
                    Tray.Icon = PKSoft.Resources.Icons.shield_grey_small;
                    mnuMode.Image = PKSoft.Resources.Icons.shield_grey_small.ToBitmap();
                    break;
            }
            Tray.Text = "TinyWall" + Environment.NewLine +
                SettingsManager.CurrentZone.ZoneName + " zone" + Environment.NewLine +
                FwMode.ToString() + " mode";

            // Find out if we are locked and if we have a password
            resp = GlobalInstances.CommunicationMan.QueueMessageSimple(TinyWallCommands.GET_LOCK_STATE);
            if (resp.Command == TinyWallCommands.RESPONSE_OK)
            {
                // Are we locked?
                this.Locked = (int)resp.Arguments[1] == 1;

                // Do we have a passord at all?
                mnuLock.Visible = (int)resp.Arguments[0] == 1;
            }

            mnuAllowLocalSubnet.Checked = SettingsManager.CurrentZone.AllowLocalSubnet;
            mnuEnableHostsBlocklist.Checked = SettingsManager.GlobalConfig.HostsBlocklist;
        }

        private void SetMode(FirewallMode mode)
        {
            TinyWallCommands opret = TinyWallCommands.RESPONSE_ERROR;
            Message req = new Message(TinyWallCommands.MODE_SWITCH, mode);
            Message resp = GlobalInstances.CommunicationMan.QueueMessage(req).GetResponse();

            string usermsg = string.Empty;
            switch (mode)
            {
                case FirewallMode.Normal:
                    usermsg = "The firewall is now operating in the recommended mode.";
                    break;

                case FirewallMode.AllowOutgoing:
                    usermsg = "The firewall is now set to allow all outgoing connections.";
                    break;

                case FirewallMode.BlockAll:
                    usermsg = "The firewall is now blocking all inbound and outbound traffic.";
                    break;

                case FirewallMode.Disabled:
                    usermsg = "The firewall is now disabled.";
                    break;
            }

            switch (resp.Command)
            {
                case TinyWallCommands.RESPONSE_OK:
                    ShowBalloonTip(usermsg, ToolTipIcon.Info);
                    break;
                default:
                    DefaultPopups(opret);
                    break;
            }

            if (mode != FirewallMode.Disabled)
            {
                SettingsManager.GlobalConfig.StartupMode = mode;
                ApplyFirewallSettings(SettingsManager.GlobalConfig, null, false);
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
        private bool LoadSettingsFromServer(bool force = false)
        {
            // Detect of server settings have changed in comparison to ours and download
            // settings only if we need them. Settings are "version numbered" using the "changeset"
            // property. We send our changeset number to the service and if it differs from his,
            // the service will send back the settings.

            bool SettingsUpdated = false;

            Message req = new Message(TinyWallCommands.GET_SETTINGS, force ? int.MinValue :  SettingsManager.Changeset );
            Message resp = GlobalInstances.CommunicationMan.QueueMessage(req).GetResponse();
            if (resp.Command == TinyWallCommands.RESPONSE_OK)
            {
                int ServerChangeSet = (int)resp.Arguments[0];
                if (force || (ServerChangeSet != SettingsManager.Changeset))
                {
                    SettingsManager.Changeset = ServerChangeSet;
                    SettingsManager.GlobalConfig = (MachineSettings)resp.Arguments[1];
                    SettingsManager.CurrentZone = (ZoneSettings)resp.Arguments[2];
                    SettingsUpdated = true;
                }
                else
                    SettingsUpdated = false;
            }
            else
            {
                SettingsManager.GlobalConfig = new MachineSettings();
                SettingsManager.CurrentZone = new ZoneSettings();
                SettingsUpdated = true;
            }

            if (SettingsUpdated)
                UpdateDisplay();

            return SettingsUpdated;
        }

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            if (Locked)
            {
                DefaultPopups(TinyWallCommands.RESPONSE_LOCKED);
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

                using (this.ShownSettings = new SettingsForm(
                    SettingsManager.ControllerConfig.Clone() as ControllerSettings,
                    SettingsManager.GlobalConfig.Clone() as MachineSettings,
                    SettingsManager.CurrentZone.Clone() as ZoneSettings))
                {
                    SettingsForm sf = this.ShownSettings;

                    if (sf.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    {
                        // Handle password change request
                        string passwd = sf.NewPassword;
                        if (passwd != null)
                        {
                            // Set the password. If the operation is successfull, do not report anything as we will be setting 
                            // the other settings too and we want to avoid multiple popups.
                            Message req = new Message(TinyWallCommands.SET_PASSPHRASE, Hasher.HashString(passwd));
                            Message resp = GlobalInstances.CommunicationMan.QueueMessage(req).GetResponse();
                            if (resp.Command != TinyWallCommands.RESPONSE_OK)  // Only display a popup for setting the password if it did not succeed
                            {
                                DefaultPopups(resp.Command);
                                return;
                            }
                        }

                        // Save settings
                        SettingsManager.ControllerConfig = sf.TmpControllerConfig;
                        SettingsManager.ControllerConfig.Save();
                        ApplyFirewallSettings(sf.TmpMachineConfig, sf.TmpZoneConfig);
                        ApplyControllerSettings();
                    }
                }
            }
            finally
            {
                this.ShownSettings = null;
            }
        }

        private void TrayMenu_Opening(object sender, CancelEventArgs e)
        {
            LoadSettingsFromServer();

            if (FwMode == FirewallMode.Unknown)
            {
                try
                {
                    // Check if the service is running
                    using (ServiceController scm = new ServiceController(TinyWallService.SERVICE_NAME))
                    {
                        if (scm.Status == ServiceControllerStatus.Stopped)
                        {
                            ShowBalloonTip("TinyWall Service is stopped. Please ensure that both TinyWall Service and the Windows Firewall service are started, then retry.", ToolTipIcon.Error, 10000);
                            e.Cancel = true;
                        }
                    }
                }
                catch   // If thrown, it means the TinyWall service did not even exist
                {
                    ShowBalloonTip("TinyWall Service is not installed. Please re-run the TinyWall installer.", ToolTipIcon.Error, 10000);
                    e.Cancel = true;
                }
            }

           mnuTrafficRate.Text = "Down: " + rxDisplay + "   " + "Up: " + txDisplay;
        }

        private void mnuWhitelistByExecutable_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog(this) != System.Windows.Forms.DialogResult.OK)
                return;

            AppExceptionSettings ex = new AppExceptionSettings(ofd.FileName);
            ex.ServiceName = string.Empty;

            ex.TryRecognizeApp(true);
            if (SettingsManager.ControllerConfig.AskForExceptionDetails)
            {
                using (ApplicationExceptionForm f = new ApplicationExceptionForm(ex))
                {
                    if (f.ShowDialog(this) == DialogResult.Cancel)
                        return;

                    ex = f.ExceptionSettings;
                }
            }

            AddNewException(ex); 
        }

        private void mnuWhitelistByProcess_Click(object sender, EventArgs e)
        {
            AppExceptionSettings ex = ProcessesForm.ChooseProcess(this);
            if (ex == null) return;

            ex.TryRecognizeApp(true);
            if (SettingsManager.ControllerConfig.AskForExceptionDetails)
            {
                using (ApplicationExceptionForm f = new ApplicationExceptionForm(ex))
                {
                    if (f.ShowDialog(this) == DialogResult.Cancel)
                        return;

                    ex = f.ExceptionSettings;
                }
            }

            AddNewException(ex); 
        }
        
        internal TinyWallCommands ApplyFirewallSettings(MachineSettings machine, ZoneSettings zone, bool showUI = true)
        {
            Message resp;
            if (LoadSettingsFromServer())
            {
                // From LoadSettingsFromServer we cannot tell if there was a communication error or if no settings were reloaded.
                // We ping to determine if there was a communication error.
                resp = GlobalInstances.CommunicationMan.QueueMessageSimple(TinyWallCommands.PING);
                if (resp.Command != TinyWallCommands.RESPONSE_OK)
                {
                    DefaultPopups(resp.Command);
                    return resp.Command;
                }
                    
                // We tell the user to re-do his changes to the settings to prevent overwriting the wrong configuration.
                ShowBalloonTip("The current network profile has changed while you modified the preferences. Please make your changes again.", ToolTipIcon.Warning);

                return TinyWallCommands.RESPONSE_ERROR;
            }

            Message req = new Message(TinyWallCommands.PUT_SETTINGS, machine, zone);
            resp = GlobalInstances.CommunicationMan.QueueMessage(req).GetResponse();

            if (showUI)
            {
                switch (resp.Command)
                {
                    case TinyWallCommands.RESPONSE_OK:
                        ShowBalloonTip("The firewall settings have been successfully updated.", ToolTipIcon.Info);
                        if (machine != null) 
                            SettingsManager.GlobalConfig = machine;
                        if (zone != null)
                            SettingsManager.CurrentZone = zone;
                        break;
                    default:
                        DefaultPopups(resp.Command);
                        LoadSettingsFromServer();
                        break;
                }
            }

            return resp.Command;
        }

        private void DefaultPopups(TinyWallCommands op)
        {
            switch (op)
            {
                case TinyWallCommands.RESPONSE_OK:
                    ShowBalloonTip("Success.", ToolTipIcon.Info);
                    break;
                case TinyWallCommands.RESPONSE_WARNING:
                    ShowBalloonTip("The operation succeeded, but other settings prevent it from taking full effect.", ToolTipIcon.Warning);
                    break;
                case TinyWallCommands.RESPONSE_ERROR:
                    ShowBalloonTip("Operation failed.", ToolTipIcon.Error);
                    break;
                case TinyWallCommands.RESPONSE_LOCKED:
                    ShowBalloonTip("The requested operation did not succeed, because TinyWall is currently locked.", ToolTipIcon.Warning);
                    Locked = true;
                    break;
                case TinyWallCommands.COM_ERROR:
                default:
                    ShowBalloonTip("Communication with TinyWall Service encountered an error.", ToolTipIcon.Error);
                    break;
                    //throw new InvalidOperationException("Invalid program flow. Received " + op.ToString() + ". Please contact the application's author.");
            }
        }

        private void mnuManage_Click(object sender, EventArgs e)
        {
            mnuSettings_Click(sender, e);
        }

        private void mnuWhitelistByWindow_Click(object sender, EventArgs e)
        {
            if (MouseInterceptor == null)
            {
                MouseInterceptor = new MouseInterceptor();
                MouseInterceptor.MouseLButtonDown += new PKSoft.MouseInterceptor.MouseHookLButtonDown(MouseInterceptor_MouseLButtonDown);
                ShowBalloonTip("Click on the inner area of any open window to select its application for whitelisting.", ToolTipIcon.Info);
            }
            else
            {
                MouseInterceptor.Dispose();
                MouseInterceptor = null;
                ShowBalloonTip("Whitelisting cancelled.", ToolTipIcon.Info);
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
                Utils.Invoke(this, (MethodInvoker)delegate()
                {
                    MouseInterceptor.Dispose();
                    MouseInterceptor = null;

                    string AppPath = null;
                    try
                    {
                        AppPath = Utils.GetExecutableUnderCursor(x, y);
                    }
                    catch (Win32Exception)
                    {
                        ShowBalloonTip("Cannot get executable path, process is probably running with elevated privileges. Elevate TinyWall and try again.", ToolTipIcon.Error);
                        return;
                    }

                    AppExceptionSettings ex = new AppExceptionSettings(AppPath);
                    ex.TryRecognizeApp(true);
                    if (SettingsManager.ControllerConfig.AskForExceptionDetails)
                    {
                        using (ApplicationExceptionForm f = new ApplicationExceptionForm(ex))
                        {
                            if (f.ShowDialog(this) == DialogResult.Cancel)
                                return;

                            ex = f.ExceptionSettings;
                        }
                    }

                    AddNewException(ex);
                });
            });
        }

        private void EditRecentException(object sender, AnyEventArgs e)
        {
            AppExceptionSettings ex = e.Arg as AppExceptionSettings;
            using (ApplicationExceptionForm f = new ApplicationExceptionForm(ex))
            {
                if (f.ShowDialog(this) == DialogResult.Cancel)
                    return;

                ex = f.ExceptionSettings;
            }

            AddNewException(ex);
        }

        private void AddNewException(AppExceptionSettings ex)
        {
            List<AppExceptionSettings> exceptions = AppExceptionSettings.CheckForAppDependencies(this, ex);
            for (int i = 0; i < exceptions.Count; ++i)
                SettingsManager.CurrentZone.AppExceptions = Utils.ArrayAddItem(SettingsManager.CurrentZone.AppExceptions, exceptions[i]);
            SettingsManager.CurrentZone.Normalize();

            TinyWallCommands resp = ApplyFirewallSettings(null, SettingsManager.CurrentZone, false);
            switch (resp)
            {
                case TinyWallCommands.RESPONSE_OK:
                    if (ex.Recognized.HasValue && ex.Recognized.Value)
                        ShowBalloonTip("Firewall rules for recognized " + ex.ExecutableName + " have been changed. Click this popup to edit the exception.", ToolTipIcon.Info, 5000, EditRecentException, Utils.DeepClone(ex));
                    else
                        ShowBalloonTip("Firewall rules for unrecognized " + ex.ExecutableName + " have been changed. Click this popup to edit the exception.", ToolTipIcon.Info, 5000, EditRecentException, Utils.DeepClone(ex));

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
                    if (pf.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    {
                        Message req = new Message(TinyWallCommands.UNLOCK, pf.PassHash);
                        Message resp = GlobalInstances.CommunicationMan.QueueMessage(req).GetResponse();
                        switch (resp.Command)
                        {
                            case TinyWallCommands.RESPONSE_OK:
                                ShowBalloonTip("TinyWall has been unlocked. You may now issue commands that modify the configuration.", ToolTipIcon.Info);
                                break;
                            case TinyWallCommands.RESPONSE_ERROR:
                                ShowBalloonTip("Unlock failed. Try again with another passphrase.", ToolTipIcon.Error);
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
                GlobalInstances.CommunicationMan.QueueMessageSimple(TinyWallCommands.LOCK);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MouseInterceptor != null)
                MouseInterceptor.Dispose();
        }

        private void mnuAllowLocalSubnet_Click(object sender, EventArgs e)
        {
            mnuAllowLocalSubnet.Checked = !mnuAllowLocalSubnet.Checked;

            // Copy, so that settings are not changed if they cannot be saved
            ZoneSettings zoneCopy = SettingsManager.CurrentZone.Clone() as ZoneSettings;
            zoneCopy.AllowLocalSubnet = mnuAllowLocalSubnet.Checked;
            ApplyFirewallSettings(null, zoneCopy);
        }

        private void mnuEnableHostsBlocklist_Click(object sender, EventArgs e)
        {
            mnuEnableHostsBlocklist.Checked = !mnuEnableHostsBlocklist.Checked;
            SettingsManager.GlobalConfig.HostsBlocklist = mnuEnableHostsBlocklist.Checked;
            if (SettingsManager.GlobalConfig.HostsBlocklist)
                SettingsManager.GlobalConfig.LockHostsFile = true;

            ApplyFirewallSettings(SettingsManager.GlobalConfig, null);
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == WindowState)
                Hide();
            else
                WindowState = FormWindowState.Minimized;
        }

        private void ShowBalloonTip(string msg, ToolTipIcon icon, int period_ms = 5000, EventHandler<AnyEventArgs> balloonClicked = null, object handlerArg = null)
        {
            BalloonClickedCallback = balloonClicked;
            BalloonClickedCallbackArgument = handlerArg;
            Tray.ShowBalloonTip(period_ms, SettingsManager.APP_NAME, msg, icon);
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            Hide();

            // We will load our database parallel to other things to improve startup performance
            ThreadBarrier barrier = new ThreadBarrier(2);
            ThreadPool.QueueUserWorkItem((WaitCallback)delegate(object state)
            {
                try
                {
                    GlobalInstances.ProfileMan = ProfileManager.Load(ProfileManager.DBPath);
                }
                catch
                {
                    GlobalInstances.ProfileMan = new ProfileManager();
                    Utils.Invoke(this, (MethodInvoker)delegate()
                    {
                        ShowBalloonTip("Database is missing or corrupt.", ToolTipIcon.Warning);
                    });
                }
                finally
                {
                    barrier.Wait();
                }
            });
            
            // --------------- CODE BETWEEN HERE MUST NOT USE DATABASE, SINCE IT IS BEING LOADED PARALLEL ---------------
            // BEGIN
            mnuElevate.Visible = !Utils.RunningAsAdmin();
            mnuModeDisabled.Image = Resources.Icons.shield_grey_small.ToBitmap();
            mnuModeAllowOutgoing.Image = Resources.Icons.shield_red_small.ToBitmap();
            mnuModeBlockAll.Image = Resources.Icons.shield_yellow_small.ToBitmap();
            mnuModeNormal.Image = Resources.Icons.shield_green_small.ToBitmap();

            HotKeyWhitelistWindow = new Hotkey(Keys.W, true, true, false, false);
            HotKeyWhitelistWindow.Pressed += new HandledEventHandler(HotKeyWhitelistWindow_Pressed);
            HotKeyWhitelistWindow.Register(this);

            HotKeyWhitelistExecutable = new Hotkey(Keys.E, true, true, false, false);
            HotKeyWhitelistExecutable.Pressed += new HandledEventHandler(HotKeyWhitelistExecutable_Pressed);
            HotKeyWhitelistExecutable.Register(this);

            HotKeyWhitelistProcess = new Hotkey(Keys.P, true, true, false, false);
            HotKeyWhitelistProcess.Pressed += new HandledEventHandler(HotKeyWhitelistProcess_Pressed);
            HotKeyWhitelistProcess.Register(this);

            TrafficTimer = new System.Threading.Timer(TrafficTimerTick, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(TRAFFIC_TIMER_INTERVAL));
            SettingsManager.ControllerConfig = ControllerSettings.Load();
            ApplyControllerSettings();

            GlobalInstances.CommunicationMan = new PipeCom("TinyWallController");
            LoadSettingsFromServer(true);

            barrier.Wait();
            // END
            // --------------- CODE BETWEEN HERE MUST NOT USE DATABASE, SINCE IT IS BEING LOADED PARALLEL ---------------

            // --- THREAD BARRIER ---

            if (StartupOpts.autowhitelist)
            {
                ApplicationCollection allApps = Utils.DeepClone(GlobalInstances.ProfileMan.KnownApplications);
                for (int i = 0; i < allApps.Count; ++i)
                {
                    Application app = allApps[i];

                    // If we've found at least one file, add the app to the list
                    if (!app.Special && app.ResolveFilePaths())
                    {
                        foreach (ProfileAssoc appFile in app.FileRealizations)
                        {
                            SettingsManager.CurrentZone.AppExceptions = Utils.ArrayAddItem(SettingsManager.CurrentZone.AppExceptions, appFile.ToExceptionSetting());
                        }
                    }
                }
                SettingsManager.CurrentZone.Normalize();
                ApplyFirewallSettings(null, SettingsManager.CurrentZone);
            }

            if (StartupOpts.updatenow)
            {
                StartUpdate(null, null);
            }
        }

        private void mnuElevate_Click(object sender, EventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo(Utils.ExecutablePath, "/desktop");
            psi.UseShellExecute = true;
            psi.Verb = "runas";

            try
            {
                Process.Start(psi);
                System.Windows.Forms.Application.Exit();
            }
            catch (Win32Exception)
            {
                ShowBalloonTip("Could not elevate privileges.", ToolTipIcon.Error);
            }
        }

        private void mnuConnections_Click(object sender, EventArgs e)
        {
            using (ConnectionsForm cf = new ConnectionsForm(this))
            {
                cf.ShowDialog(this);
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
