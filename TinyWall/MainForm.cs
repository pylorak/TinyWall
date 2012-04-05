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
    internal partial class MainForm : Form
    {
        private MouseInterceptor MouseInterceptor;
        private SettingsForm ShownSettings;
        private ServiceState FirewallState;
        private DateTime LastUpdateNotification = DateTime.MinValue;
        private uint WM_NOTIFY_BY_SERVICE;

        // Traffic rate monitoring
        private System.Threading.Timer TrafficTimer = null;
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
                    mnuLock.Text = PKSoft.Resources.Messages.Unlock;
                }
                else
                {
                    mnuLock.Text = PKSoft.Resources.Messages.Lock;
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
                        Utils.Invoke(this, (MethodInvoker)delegate()
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
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"select BytesReceivedPersec, BytesSentPersec from Win32_PerfRawData_Tcpip_NetworkInterface"))
                {
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
                TrafficTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }
        
        private void StartUpdate(object sender, AnyEventArgs e)
        {
            Updater.StartUpdate(this);
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
            // Update string showing current network profile
            mnuCurrentPolicy.Text = string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.CurrentZone, SettingsManager.CurrentZone.ZoneName);

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

            Tray.Text = string.Format(CultureInfo.CurrentCulture, "TinyWall\r\n{0}: {1}\r\n{2}: {3}",
                PKSoft.Resources.Messages.Zone, SettingsManager.CurrentZone.ZoneName,
                PKSoft.Resources.Messages.Mode, FirewallModeName);

            // Find out if we are locked and if we have a password
            this.Locked = FirewallState.Locked;

            // Do we have a passord at all?
            mnuLock.Visible = FirewallState.HasPassword;

            mnuAllowLocalSubnet.Checked = SettingsManager.CurrentZone.AllowLocalSubnet;
            mnuEnableHostsBlocklist.Checked = SettingsManager.GlobalConfig.Blocklists.EnableBlocklists;
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

            Message req = new Message(TWControllerMessages.GET_SETTINGS, force ? int.MinValue : FirewallState.SettingsChangeset, this.Handle );
            Message resp = GlobalInstances.CommunicationMan.QueueMessage(req).GetResponse();
            comError = (resp.Command == TWControllerMessages.COM_ERROR);
            if (resp.Command == TWControllerMessages.RESPONSE_OK)
            {
                int ServerChangeSet = (int)resp.Arguments[0];
                if (force || (ServerChangeSet != FirewallState.SettingsChangeset))
                {
                    SettingsManager.GlobalConfig = (MachineSettings)resp.Arguments[1];
                    SettingsManager.CurrentZone = (ZoneSettings)resp.Arguments[2];
                    FirewallState = (ServiceState)resp.Arguments[3];
                    SettingsUpdated = true;
                }
                else
                    SettingsUpdated = false;
            }
            else
            {
                SettingsManager.GlobalConfig = new MachineSettings();
                SettingsManager.CurrentZone = new ZoneSettings(true);
                FirewallState = new ServiceState();
                SettingsUpdated = true;
            }

            if (SettingsUpdated)
            {
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
            if (ofd.ShowDialog(this) != System.Windows.Forms.DialogResult.OK)
                return;

            FirewallException ex = new FirewallException(ofd.FileName, null);
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
            FirewallException ex = ProcessesForm.ChooseProcess(this);
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
        
        internal TWControllerMessages ApplyFirewallSettings(MachineSettings machine, ZoneSettings zone, bool showUI = true)
        {
            Message resp;
            if (LoadSettingsFromServer())
            {
                // From LoadSettingsFromServer we cannot tell if there was a communication error or if no settings were reloaded.
                // We ping to determine if there was a communication error.
                resp = GlobalInstances.CommunicationMan.QueueMessageSimple(TWControllerMessages.PING);
                if (resp.Command != TWControllerMessages.RESPONSE_OK)
                {
                    DefaultPopups(resp.Command);
                    return resp.Command;
                }
                    
                // We tell the user to re-do his changes to the settings to prevent overwriting the wrong configuration.
                ShowBalloonTip(PKSoft.Resources.Messages.SettingHaveChangedRetry, ToolTipIcon.Warning);

                return TWControllerMessages.RESPONSE_ERROR;
            }

            Message req = new Message(TWControllerMessages.PUT_SETTINGS, machine, zone);
            resp = GlobalInstances.CommunicationMan.QueueMessage(req).GetResponse();

            if (showUI)
            {
                switch (resp.Command)
                {
                    case TWControllerMessages.RESPONSE_OK:
                        ShowBalloonTip(PKSoft.Resources.Messages.TheFirewallSettingsHaveBeenUpdated, ToolTipIcon.Info);
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
                        SettingsManager.ControllerConfig = sf.TmpControllerConfig;
                        SettingsManager.ControllerConfig.Save();
                        ApplyFirewallSettings(sf.TmpMachineConfig, sf.TmpZoneConfig);
                    }
                }
            }
            finally
            {
                this.ShownSettings = null;
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
                Utils.Invoke(this, (MethodInvoker)delegate()
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
            FirewallException ex = e.Arg as FirewallException;
            using (ApplicationExceptionForm f = new ApplicationExceptionForm(ex))
            {
                if (f.ShowDialog(this) == DialogResult.Cancel)
                    return;

                ex = f.ExceptionSettings;
            }

            AddNewException(ex);
        }

        private void AddNewException(FirewallException ex)
        {
            List<FirewallException> exceptions = FirewallException.CheckForAppDependencies(ex, true, true, this);
            if (exceptions.Count == 0)
                return;

            SettingsManager.CurrentZone.AppExceptions.AddRange(exceptions);
            SettingsManager.CurrentZone.Normalize();

            TWControllerMessages resp = ApplyFirewallSettings(null, SettingsManager.CurrentZone, false);
            switch (resp)
            {
                case TWControllerMessages.RESPONSE_OK:
                    if (ex.Recognized.HasValue && ex.Recognized.Value)
                        ShowBalloonTip(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.FirewallRulesForRecognizedChanged, ex.ExecutableName), ToolTipIcon.Info, 5000, EditRecentException, Utils.DeepClone(ex));
                    else
                        ShowBalloonTip(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.FirewallRulesForUnrecognizedChanged, ex.ExecutableName), ToolTipIcon.Info, 5000, EditRecentException, Utils.DeepClone(ex));

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
            SettingsManager.GlobalConfig.Blocklists.EnableBlocklists = mnuEnableHostsBlocklist.Checked;
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

            TrafficTimer = new System.Threading.Timer(TrafficTimerTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(TRAFFIC_TIMER_INTERVAL));

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
                mnuElevate.Visible = !Utils.RunningAsAdmin();
                mnuModeDisabled.Image = Resources.Icons.shield_grey_small.ToBitmap();
                mnuModeAllowOutgoing.Image = Resources.Icons.shield_red_small.ToBitmap();
                mnuModeBlockAll.Image = Resources.Icons.shield_yellow_small.ToBitmap();
                mnuModeNormal.Image = Resources.Icons.shield_green_small.ToBitmap();
                mnuModeLearn.Image = Resources.Icons.shield_blue_small.ToBitmap();

                HotKeyWhitelistWindow = new Hotkey(Keys.W, true, true, false, false);
                HotKeyWhitelistWindow.Pressed += new HandledEventHandler(HotKeyWhitelistWindow_Pressed);
                HotKeyWhitelistWindow.Register(this);

                HotKeyWhitelistExecutable = new Hotkey(Keys.E, true, true, false, false);
                HotKeyWhitelistExecutable.Pressed += new HandledEventHandler(HotKeyWhitelistExecutable_Pressed);
                HotKeyWhitelistExecutable.Register(this);

                HotKeyWhitelistProcess = new Hotkey(Keys.P, true, true, false, false);
                HotKeyWhitelistProcess.Pressed += new HandledEventHandler(HotKeyWhitelistProcess_Pressed);
                HotKeyWhitelistProcess.Register(this);

                GlobalInstances.CommunicationMan = new PipeCom("TinyWallController");
                SettingsManager.ControllerConfig = ControllerSettings.Load();

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

            // Enable opening the tray menu
            Tray.ContextMenuStrip = TrayMenu;

            if (StartupOpts.autowhitelist)
            {
                AutoWhitelist();
            }

            if (StartupOpts.updatenow)
            {
                StartUpdate(null, null);
            }

            Utils.MinimizeMemory();
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
                    Utils.Invoke(this, (MethodInvoker)delegate()
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
                            SettingsManager.CurrentZone.AppExceptions.Add(template.CreateException(execPath));
                        }
                    }
                }
            }
            SettingsManager.CurrentZone.Normalize();
            ApplyFirewallSettings(null, SettingsManager.CurrentZone);
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (WM_NOTIFY_BY_SERVICE == 0)
                WM_NOTIFY_BY_SERVICE = NativeMethods.RegisterWindowMessage("WM_NOTIFY_BY_SERVICE");

            //if the window message id equals the QueryCancelAutoPlay message id
            if ((uint)m.Msg == WM_NOTIFY_BY_SERVICE)
            {
                switch (m.WParam.ToInt32())
                {
                    case (int)TWServiceMessages.DATABASE_UPDATED:
                        try
                        {
                            LoadDatabase();
                        }
                        catch { }
                        break;
                    case (int)TWServiceMessages.SETTINGS_CHANGED:
                        LoadSettingsFromServer();
                        break;
                }
                m.Result = (IntPtr)1;
            }

            //calling the base first is important, otherwise the values you set later will be lost
            base.WndProc(ref m);
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

            if (dialog.Show(this)  != (int)DialogResult.Yes)
                return;

            SetMode(FirewallMode.Learning);
            UpdateDisplay();
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
