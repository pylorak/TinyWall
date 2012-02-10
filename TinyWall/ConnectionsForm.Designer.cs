namespace PKSoft
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
            this.btnClose = new System.Windows.Forms.Button();
            this.list = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuCloseProcess = new System.Windows.Forms.ToolStripMenuItem();
            this.IconList = new System.Windows.Forms.ImageList(this.components);
            this.btnRefresh = new System.Windows.Forms.Button();
            this.chkShowListen = new System.Windows.Forms.CheckBox();
            this.chkShowActive = new System.Windows.Forms.CheckBox();
            this.chkShowBlocked = new System.Windows.Forms.CheckBox();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.Location = new System.Drawing.Point(724, 303);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 33);
            this.btnClose.TabIndex = 0;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // list
            // 
            this.list.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.list.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6,
            this.columnHeader7});
            this.list.ContextMenuStrip = this.contextMenuStrip1;
            this.list.FullRowSelect = true;
            this.list.GridLines = true;
            this.list.Location = new System.Drawing.Point(12, 12);
            this.list.Name = "list";
            this.list.ShowItemToolTips = true;
            this.list.Size = new System.Drawing.Size(787, 285);
            this.list.SmallImageList = this.IconList;
            this.list.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.list.TabIndex = 2;
            this.list.UseCompatibleStateImageBehavior = false;
            this.list.View = System.Windows.Forms.View.Details;
            this.list.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.list_ColumnClick);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Process (id)";
            this.columnHeader1.Width = 123;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Protocol";
            this.columnHeader2.Width = 75;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Local port";
            this.columnHeader3.Width = 68;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Local address";
            this.columnHeader4.Width = 160;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Remote port";
            this.columnHeader5.Width = 74;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Remote address";
            this.columnHeader6.Width = 160;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "State";
            this.columnHeader7.Width = 89;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuCloseProcess});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(147, 26);
            this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            // 
            // mnuCloseProcess
            // 
            this.mnuCloseProcess.Image = global::PKSoft.Icons.exit;
            this.mnuCloseProcess.Name = "mnuCloseProcess";
            this.mnuCloseProcess.Size = new System.Drawing.Size(146, 22);
            this.mnuCloseProcess.Text = "Close process";
            this.mnuCloseProcess.Click += new System.EventHandler(this.mnuCloseProcess_Click);
            // 
            // IconList
            // 
            this.IconList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.IconList.ImageSize = new System.Drawing.Size(16, 16);
            this.IconList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRefresh.Location = new System.Drawing.Point(643, 303);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 33);
            this.btnRefresh.TabIndex = 3;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // chkShowListen
            // 
            this.chkShowListen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkShowListen.AutoSize = true;
            this.chkShowListen.Location = new System.Drawing.Point(164, 312);
            this.chkShowListen.Name = "chkShowListen";
            this.chkShowListen.Size = new System.Drawing.Size(106, 17);
            this.chkShowListen.TabIndex = 4;
            this.chkShowListen.Text = "Show open ports";
            this.chkShowListen.UseVisualStyleBackColor = true;
            this.chkShowListen.CheckedChanged += new System.EventHandler(this.chkShowListen_CheckedChanged);
            // 
            // chkShowActive
            // 
            this.chkShowActive.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkShowActive.AutoSize = true;
            this.chkShowActive.Checked = true;
            this.chkShowActive.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowActive.Location = new System.Drawing.Point(12, 312);
            this.chkShowActive.Name = "chkShowActive";
            this.chkShowActive.Size = new System.Drawing.Size(146, 17);
            this.chkShowActive.TabIndex = 5;
            this.chkShowActive.Text = "Show active connections";
            this.chkShowActive.UseVisualStyleBackColor = true;
            this.chkShowActive.CheckedChanged += new System.EventHandler(this.chkShowActive_CheckedChanged);
            // 
            // chkShowBlocked
            // 
            this.chkShowBlocked.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkShowBlocked.AutoSize = true;
            this.chkShowBlocked.Location = new System.Drawing.Point(276, 312);
            this.chkShowBlocked.Name = "chkShowBlocked";
            this.chkShowBlocked.Size = new System.Drawing.Size(189, 17);
            this.chkShowBlocked.TabIndex = 6;
            this.chkShowBlocked.Text = "Show blocked apps (in last 2 mins)";
            this.chkShowBlocked.UseVisualStyleBackColor = true;
            this.chkShowBlocked.CheckedChanged += new System.EventHandler(this.chkShowBlocked_CheckedChanged);
            // 
            // ConnectionsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(811, 348);
            this.Controls.Add(this.chkShowBlocked);
            this.Controls.Add(this.chkShowActive);
            this.Controls.Add(this.chkShowListen);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.list);
            this.Controls.Add(this.btnClose);
            this.Name = "ConnectionsForm";
            this.Text = "Connections - TinyWall";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ConnectionsForm_FormClosing);
            this.Load += new System.EventHandler(this.ConnectionsForm_Load);
            this.contextMenuStrip1.ResumeLayout(false);
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
    }
}