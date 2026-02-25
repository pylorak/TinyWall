namespace pylorak.TinyWall
{
    partial class ConnectionsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConnectionsForm));
            this.btnClose = new System.Windows.Forms.Button();
            this.list = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader10 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader9 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuUnblock = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuCloseProcess = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuSearch = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuVirusTotal = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuProcessLibrary = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFileNameOnTheWeb = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuRemoteAddressOnTheWeb = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuCopyRemoteAddress = new System.Windows.Forms.ToolStripMenuItem();
            this.IconList = new System.Windows.Forms.ImageList(this.components);
            this.btnRefresh = new System.Windows.Forms.Button();
            this.chkShowListen = new System.Windows.Forms.CheckBox();
            this.chkShowActive = new System.Windows.Forms.CheckBox();
            this.chkShowBlocked = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.LblSearch = new System.Windows.Forms.Label();
            this.btnSearch = new System.Windows.Forms.Button();
            this.BtnClear = new System.Windows.Forms.Button();
            this.lblPleaseWait = new System.Windows.Forms.Label();
            this.contextMenuStrip1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnClose
            // 
            resources.ApplyResources(this.btnClose, "btnClose");
            this.btnClose.Name = "btnClose";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.BtnClose_Click);
            // 
            // list
            // 
            resources.ApplyResources(this.list, "list");
            this.list.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader10,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6,
            this.columnHeader7,
            this.columnHeader8,
            this.columnHeader9});
            this.list.ContextMenuStrip = this.contextMenuStrip1;
            this.list.FullRowSelect = true;
            this.list.GridLines = true;
            this.list.HideSelection = false;
            this.list.Name = "list";
            this.list.ShowItemToolTips = true;
            this.list.SmallImageList = this.IconList;
            this.list.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.list.UseCompatibleStateImageBehavior = false;
            this.list.View = System.Windows.Forms.View.Details;
            this.list.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.List_ColumnClick);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Tag = "colProcess";
            resources.ApplyResources(this.columnHeader1, "columnHeader1");
            // 
            // columnHeader10
            // 
            this.columnHeader10.Tag = "colService";
            resources.ApplyResources(this.columnHeader10, "columnHeader10");
            // 
            // columnHeader2
            // 
            this.columnHeader2.Tag = "colProtocol";
            resources.ApplyResources(this.columnHeader2, "columnHeader2");
            // 
            // columnHeader3
            // 
            this.columnHeader3.Tag = "colSrcPort";
            resources.ApplyResources(this.columnHeader3, "columnHeader3");
            // 
            // columnHeader4
            // 
            this.columnHeader4.Tag = "colLocalAddress";
            resources.ApplyResources(this.columnHeader4, "columnHeader4");
            // 
            // columnHeader5
            // 
            this.columnHeader5.Tag = "colDstPort";
            resources.ApplyResources(this.columnHeader5, "columnHeader5");
            // 
            // columnHeader6
            // 
            this.columnHeader6.Tag = "colRemoteAddress";
            resources.ApplyResources(this.columnHeader6, "columnHeader6");
            // 
            // columnHeader7
            // 
            this.columnHeader7.Tag = "colState";
            resources.ApplyResources(this.columnHeader7, "columnHeader7");
            // 
            // columnHeader8
            // 
            this.columnHeader8.Tag = "colDirection";
            resources.ApplyResources(this.columnHeader8, "columnHeader8");
            // 
            // columnHeader9
            // 
            this.columnHeader9.Tag = "colTimestamp";
            resources.ApplyResources(this.columnHeader9, "columnHeader9");
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuUnblock,
            this.mnuCloseProcess,
            this.mnuSearch,
            this.mnuCopyRemoteAddress});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            resources.ApplyResources(this.contextMenuStrip1, "contextMenuStrip1");
            this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.ContextMenuStrip1_Opening);
            // 
            // mnuUnblock
            // 
            this.mnuUnblock.Image = global::pylorak.TinyWall.Resources.Icons.executable;
            this.mnuUnblock.Name = "mnuUnblock";
            resources.ApplyResources(this.mnuUnblock, "mnuUnblock");
            this.mnuUnblock.Click += new System.EventHandler(this.MnuUnblock_Click);
            // 
            // mnuCloseProcess
            // 
            this.mnuCloseProcess.Image = global::pylorak.TinyWall.Resources.Icons.exit;
            this.mnuCloseProcess.Name = "mnuCloseProcess";
            resources.ApplyResources(this.mnuCloseProcess, "mnuCloseProcess");
            this.mnuCloseProcess.Click += new System.EventHandler(this.MnuCloseProcess_Click);
            // 
            // mnuSearch
            // 
            this.mnuSearch.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuVirusTotal,
            this.mnuProcessLibrary,
            this.mnuFileNameOnTheWeb,
            this.mnuRemoteAddressOnTheWeb});
            this.mnuSearch.Image = global::pylorak.TinyWall.Resources.Icons.search;
            this.mnuSearch.Name = "mnuSearch";
            resources.ApplyResources(this.mnuSearch, "mnuSearch");
            // 
            // mnuVirusTotal
            // 
            this.mnuVirusTotal.Name = "mnuVirusTotal";
            resources.ApplyResources(this.mnuVirusTotal, "mnuVirusTotal");
            this.mnuVirusTotal.Click += new System.EventHandler(this.MnuVirusTotal_Click);
            // 
            // mnuProcessLibrary
            // 
            this.mnuProcessLibrary.Name = "mnuProcessLibrary";
            resources.ApplyResources(this.mnuProcessLibrary, "mnuProcessLibrary");
            this.mnuProcessLibrary.Click += new System.EventHandler(this.MnuProcessLibrary_Click);
            // 
            // mnuFileNameOnTheWeb
            // 
            this.mnuFileNameOnTheWeb.Name = "mnuFileNameOnTheWeb";
            resources.ApplyResources(this.mnuFileNameOnTheWeb, "mnuFileNameOnTheWeb");
            this.mnuFileNameOnTheWeb.Click += new System.EventHandler(this.MnuFileNameOnTheWeb_Click);
            // 
            // mnuRemoteAddressOnTheWeb
            // 
            this.mnuRemoteAddressOnTheWeb.Name = "mnuRemoteAddressOnTheWeb";
            resources.ApplyResources(this.mnuRemoteAddressOnTheWeb, "mnuRemoteAddressOnTheWeb");
            this.mnuRemoteAddressOnTheWeb.Click += new System.EventHandler(this.MnuRemoteAddressOnTheWeb_Click);
            // 
            // mnuCopyRemoteAddress
            // 
            this.mnuCopyRemoteAddress.Name = "mnuCopyRemoteAddress";
            resources.ApplyResources(this.mnuCopyRemoteAddress, "mnuCopyRemoteAddress");
            this.mnuCopyRemoteAddress.Click += new System.EventHandler(this.MnuCopyRemoteAddress_Click);
            // 
            // IconList
            // 
            this.IconList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            resources.ApplyResources(this.IconList, "IconList");
            this.IconList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // btnRefresh
            // 
            resources.ApplyResources(this.btnRefresh, "btnRefresh");
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.BtnRefresh_Click);
            // 
            // chkShowListen
            // 
            resources.ApplyResources(this.chkShowListen, "chkShowListen");
            this.chkShowListen.Name = "chkShowListen";
            this.chkShowListen.UseVisualStyleBackColor = true;
            this.chkShowListen.CheckedChanged += new System.EventHandler(this.ChkShowListen_CheckedChanged);
            // 
            // chkShowActive
            // 
            resources.ApplyResources(this.chkShowActive, "chkShowActive");
            this.chkShowActive.Checked = true;
            this.chkShowActive.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowActive.Name = "chkShowActive";
            this.chkShowActive.UseVisualStyleBackColor = true;
            this.chkShowActive.CheckedChanged += new System.EventHandler(this.ChkShowActive_CheckedChanged);
            // 
            // chkShowBlocked
            // 
            resources.ApplyResources(this.chkShowBlocked, "chkShowBlocked");
            this.chkShowBlocked.Name = "chkShowBlocked";
            this.chkShowBlocked.UseVisualStyleBackColor = true;
            this.chkShowBlocked.CheckedChanged += new System.EventHandler(this.ChkShowBlocked_CheckedChanged);
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.chkShowListen, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.chkShowActive, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.chkShowBlocked, 0, 2);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // txtSearch
            // 
            resources.ApplyResources(this.txtSearch, "txtSearch");
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtSearch_KeyDown);
            // 
            // LblSearch
            // 
            resources.ApplyResources(this.LblSearch, "LblSearch");
            this.LblSearch.Name = "LblSearch";
            // 
            // btnSearch
            // 
            resources.ApplyResources(this.btnSearch, "btnSearch");
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.UseVisualStyleBackColor = true;
            this.btnSearch.Click += new System.EventHandler(this.BtnSearch_Click);
            // 
            // BtnClear
            // 
            resources.ApplyResources(this.BtnClear, "BtnClear");
            this.BtnClear.Name = "BtnClear";
            this.BtnClear.UseVisualStyleBackColor = true;
            this.BtnClear.Click += new System.EventHandler(this.BtnClear_Click);
            // 
            // lblPleaseWait
            // 
            resources.ApplyResources(this.lblPleaseWait, "lblPleaseWait");
            this.lblPleaseWait.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPleaseWait.Name = "lblPleaseWait";
            this.lblPleaseWait.UseCompatibleTextRendering = true;
            this.lblPleaseWait.UseWaitCursor = true;
            // 
            // ConnectionsForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblPleaseWait);
            this.Controls.Add(this.BtnClear);
            this.Controls.Add(this.btnSearch);
            this.Controls.Add(this.LblSearch);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.list);
            this.Controls.Add(this.btnClose);
            this.KeyPreview = true;
            this.Name = "ConnectionsForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ConnectionsForm_FormClosing);
            this.Load += new System.EventHandler(this.ConnectionsForm_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ConnectionsForm_KeyDown);
            this.contextMenuStrip1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.ListView list;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.ImageList IconList;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.CheckBox chkShowListen;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem mnuCloseProcess;
        private System.Windows.Forms.CheckBox chkShowActive;
        private System.Windows.Forms.CheckBox chkShowBlocked;
        private System.Windows.Forms.ToolStripMenuItem mnuUnblock;
        private System.Windows.Forms.ToolStripMenuItem mnuCopyRemoteAddress;
        private System.Windows.Forms.ToolStripMenuItem mnuSearch;
        private System.Windows.Forms.ToolStripMenuItem mnuVirusTotal;
        private System.Windows.Forms.ToolStripMenuItem mnuProcessLibrary;
        private System.Windows.Forms.ToolStripMenuItem mnuFileNameOnTheWeb;
        private System.Windows.Forms.ToolStripMenuItem mnuRemoteAddressOnTheWeb;
        private System.Windows.Forms.ColumnHeader columnHeader8;
        private System.Windows.Forms.ColumnHeader columnHeader9;
        private System.Windows.Forms.ColumnHeader columnHeader10;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label LblSearch;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Button BtnClear;
        private System.Windows.Forms.Label lblPleaseWait;
    }
}