using System.Threading;

namespace PKSoft
{
    partial class MainForm
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();

                // Manually added
                HotKeyWhitelistExecutable.Dispose();
                HotKeyWhitelistProcess.Dispose();
                HotKeyWhitelistWindow.Dispose();
                if (MouseInterceptor != null)
                    MouseInterceptor.Dispose();

                using (WaitHandle wh = new AutoResetEvent(false))
                {
                    TrafficTimer.Dispose(wh);
                    wh.WaitOne();
                }
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.Tray = new System.Windows.Forms.NotifyIcon(this.components);
            this.TrayMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuCurrentPolicy = new System.Windows.Forms.ToolStripMenuItem();
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
            this.SuspendLayout();
            // 
            // Tray
            // 
            resources.ApplyResources(this.Tray, "Tray");
            this.Tray.BalloonTipClicked += new System.EventHandler(this.Tray_BalloonTipClicked);
            this.Tray.MouseClick += new System.Windows.Forms.MouseEventHandler(this.Tray_MouseClick);
            // 
            // TrayMenu
            // 
            this.TrayMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuCurrentPolicy,
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
            // mnuCurrentPolicy
            // 
            this.mnuCurrentPolicy.AccessibleRole = System.Windows.Forms.AccessibleRole.StaticText;
            this.mnuCurrentPolicy.Image = global::PKSoft.Resources.Icons.info;
            this.mnuCurrentPolicy.Name = "mnuCurrentPolicy";
            resources.ApplyResources(this.mnuCurrentPolicy, "mnuCurrentPolicy");
            // 
            // mnuTrafficRate
            // 
            this.mnuTrafficRate.AccessibleRole = System.Windows.Forms.AccessibleRole.StaticText;
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
            // 
            // MainForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ControlBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.ShowInTaskbar = false;
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.TrayMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon Tray;
        private System.Windows.Forms.ContextMenuStrip TrayMenu;
        private System.Windows.Forms.ToolStripMenuItem mnuCurrentPolicy;
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
    }
}

