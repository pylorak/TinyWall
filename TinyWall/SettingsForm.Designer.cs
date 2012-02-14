namespace PKSoft
{
    partial class SettingsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.btnAppRemoveAll = new System.Windows.Forms.Button();
            this.btnAppAutoDetect = new System.Windows.Forms.Button();
            this.btnSubmitAssoc = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.btnAppRemove = new System.Windows.Forms.Button();
            this.btnAppModify = new System.Windows.Forms.Button();
            this.btnAppAdd = new System.Windows.Forms.Button();
            this.listApplications = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.IconList = new System.Windows.Forms.ImageList(this.components);
            this.label4 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.label5 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.listOptionalGlobalProfiles = new System.Windows.Forms.CheckedListBox();
            this.listRecommendedGlobalProfiles = new System.Windows.Forms.CheckedListBox();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.chkLockHostsFile = new System.Windows.Forms.CheckBox();
            this.chkHostsBlocklist = new System.Windows.Forms.CheckBox();
            this.chkAutoUpdateCheck = new System.Windows.Forms.CheckBox();
            this.chkBlockMalwarePorts = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.chkChangePassword = new System.Windows.Forms.CheckBox();
            this.txtPasswordAgain = new System.Windows.Forms.TextBox();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.chkAskForExceptionDetails = new System.Windows.Forms.CheckBox();
            this.chkEnableDefaultWindowsRules = new System.Windows.Forms.CheckBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.btnImport = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnDonate = new System.Windows.Forms.PictureBox();
            this.lblLinkLicense = new System.Windows.Forms.LinkLabel();
            this.label10 = new System.Windows.Forms.Label();
            this.lblAboutHomepageLink = new System.Windows.Forms.LinkLabel();
            this.label6 = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnWeb = new System.Windows.Forms.Button();
            this.btnUninstall = new System.Windows.Forms.Button();
            this.sfd = new System.Windows.Forms.SaveFileDialog();
            this.ofd = new System.Windows.Forms.OpenFileDialog();
            this.tabPage3.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.btnDonate)).BeginInit();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Image = global::PKSoft.Icons.cancel;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(661, 393);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 33);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Image = global::PKSoft.Icons.accept;
            this.btnOK.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnOK.Location = new System.Drawing.Point(580, 393);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 33);
            this.btnOK.TabIndex = 7;
            this.btnOK.Text = "Apply";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.btnAppRemoveAll);
            this.tabPage3.Controls.Add(this.btnAppAutoDetect);
            this.tabPage3.Controls.Add(this.btnSubmitAssoc);
            this.tabPage3.Controls.Add(this.label3);
            this.tabPage3.Controls.Add(this.btnAppRemove);
            this.tabPage3.Controls.Add(this.btnAppModify);
            this.tabPage3.Controls.Add(this.btnAppAdd);
            this.tabPage3.Controls.Add(this.listApplications);
            this.tabPage3.Controls.Add(this.label4);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(716, 349);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Application Exceptions";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // btnAppRemoveAll
            // 
            this.btnAppRemoveAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAppRemoveAll.Image = global::PKSoft.Icons.remove;
            this.btnAppRemoveAll.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnAppRemoveAll.Location = new System.Drawing.Point(566, 223);
            this.btnAppRemoveAll.Name = "btnAppRemoveAll";
            this.btnAppRemoveAll.Size = new System.Drawing.Size(127, 36);
            this.btnAppRemoveAll.TabIndex = 19;
            this.btnAppRemoveAll.Text = "Remove all";
            this.btnAppRemoveAll.UseVisualStyleBackColor = true;
            this.btnAppRemoveAll.Click += new System.EventHandler(this.btnAppRemoveAll_Click);
            // 
            // btnAppAutoDetect
            // 
            this.btnAppAutoDetect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAppAutoDetect.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAppAutoDetect.Image = global::PKSoft.Icons.uninstall;
            this.btnAppAutoDetect.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnAppAutoDetect.Location = new System.Drawing.Point(566, 30);
            this.btnAppAutoDetect.Name = "btnAppAutoDetect";
            this.btnAppAutoDetect.Size = new System.Drawing.Size(127, 36);
            this.btnAppAutoDetect.TabIndex = 18;
            this.btnAppAutoDetect.Text = "Detect";
            this.btnAppAutoDetect.UseVisualStyleBackColor = true;
            this.btnAppAutoDetect.Click += new System.EventHandler(this.btnAppAutoDetect_Click);
            // 
            // btnSubmitAssoc
            // 
            this.btnSubmitAssoc.Enabled = false;
            this.btnSubmitAssoc.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSubmitAssoc.Image = global::PKSoft.Icons.submit;
            this.btnSubmitAssoc.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSubmitAssoc.Location = new System.Drawing.Point(566, 290);
            this.btnSubmitAssoc.Name = "btnSubmitAssoc";
            this.btnSubmitAssoc.Size = new System.Drawing.Size(127, 36);
            this.btnSubmitAssoc.TabIndex = 17;
            this.btnSubmitAssoc.Text = "Submit";
            this.btnSubmitAssoc.UseVisualStyleBackColor = true;
            this.btnSubmitAssoc.Click += new System.EventHandler(this.btnSubmitAssoc_Click);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(23, 30);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(518, 48);
            this.label3.TabIndex = 16;
            this.label3.Text = resources.GetString("label3.Text");
            // 
            // btnAppRemove
            // 
            this.btnAppRemove.Enabled = false;
            this.btnAppRemove.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAppRemove.Image = global::PKSoft.Icons.remove;
            this.btnAppRemove.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnAppRemove.Location = new System.Drawing.Point(566, 181);
            this.btnAppRemove.Name = "btnAppRemove";
            this.btnAppRemove.Size = new System.Drawing.Size(127, 36);
            this.btnAppRemove.TabIndex = 14;
            this.btnAppRemove.Text = "Remove";
            this.btnAppRemove.UseVisualStyleBackColor = true;
            this.btnAppRemove.Click += new System.EventHandler(this.btnAppRemove_Click);
            // 
            // btnAppModify
            // 
            this.btnAppModify.Enabled = false;
            this.btnAppModify.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAppModify.Image = global::PKSoft.Icons.modify;
            this.btnAppModify.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnAppModify.Location = new System.Drawing.Point(566, 139);
            this.btnAppModify.Name = "btnAppModify";
            this.btnAppModify.Size = new System.Drawing.Size(127, 36);
            this.btnAppModify.TabIndex = 13;
            this.btnAppModify.Text = "Modify";
            this.btnAppModify.UseVisualStyleBackColor = true;
            this.btnAppModify.Click += new System.EventHandler(this.btnAppModify_Click);
            // 
            // btnAppAdd
            // 
            this.btnAppAdd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAppAdd.Image = global::PKSoft.Icons.add;
            this.btnAppAdd.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnAppAdd.Location = new System.Drawing.Point(566, 97);
            this.btnAppAdd.Name = "btnAppAdd";
            this.btnAppAdd.Size = new System.Drawing.Size(127, 36);
            this.btnAppAdd.TabIndex = 11;
            this.btnAppAdd.Text = "Add application";
            this.btnAppAdd.UseVisualStyleBackColor = true;
            this.btnAppAdd.Click += new System.EventHandler(this.btnAppAdd_Click);
            // 
            // listApplications
            // 
            this.listApplications.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
            this.listApplications.FullRowSelect = true;
            this.listApplications.GridLines = true;
            this.listApplications.Location = new System.Drawing.Point(26, 97);
            this.listApplications.Name = "listApplications";
            this.listApplications.Size = new System.Drawing.Size(515, 229);
            this.listApplications.SmallImageList = this.IconList;
            this.listApplications.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.listApplications.TabIndex = 10;
            this.listApplications.UseCompatibleStateImageBehavior = false;
            this.listApplications.View = System.Windows.Forms.View.Details;
            this.listApplications.SelectedIndexChanged += new System.EventHandler(this.listApplications_SelectedIndexChanged);
            this.listApplications.DoubleClick += new System.EventHandler(this.listApplications_DoubleClick);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Executable";
            this.columnHeader1.Width = 99;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Path";
            this.columnHeader2.Width = 302;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Profiles";
            this.columnHeader3.Width = 104;
            // 
            // IconList
            // 
            this.IconList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.IconList.ImageSize = new System.Drawing.Size(16, 16);
            this.IconList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(23, 78);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(105, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Enabled applications";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.label5);
            this.tabPage2.Controls.Add(this.label2);
            this.tabPage2.Controls.Add(this.label1);
            this.tabPage2.Controls.Add(this.listOptionalGlobalProfiles);
            this.tabPage2.Controls.Add(this.listRecommendedGlobalProfiles);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(716, 349);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Special Exceptions";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(26, 30);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(670, 40);
            this.label5.TabIndex = 6;
            this.label5.Text = "Select special tasks or applications that you\'d like to enable on this machine. N" +
    "ote that disabling recommended exceptions can seriously limit the usability and " +
    "security of your system.";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(369, 72);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Optional";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(106, 72);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(79, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Recommended";
            // 
            // listOptionalGlobalProfiles
            // 
            this.listOptionalGlobalProfiles.FormattingEnabled = true;
            this.listOptionalGlobalProfiles.Location = new System.Drawing.Point(372, 91);
            this.listOptionalGlobalProfiles.Name = "listOptionalGlobalProfiles";
            this.listOptionalGlobalProfiles.Size = new System.Drawing.Size(238, 229);
            this.listOptionalGlobalProfiles.Sorted = true;
            this.listOptionalGlobalProfiles.TabIndex = 3;
            this.listOptionalGlobalProfiles.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.listOptionalGlobalProfiles_ItemCheck);
            // 
            // listRecommendedGlobalProfiles
            // 
            this.listRecommendedGlobalProfiles.FormattingEnabled = true;
            this.listRecommendedGlobalProfiles.Location = new System.Drawing.Point(109, 91);
            this.listRecommendedGlobalProfiles.Name = "listRecommendedGlobalProfiles";
            this.listRecommendedGlobalProfiles.Size = new System.Drawing.Size(238, 229);
            this.listRecommendedGlobalProfiles.Sorted = true;
            this.listRecommendedGlobalProfiles.TabIndex = 2;
            this.listRecommendedGlobalProfiles.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.listRecommendedGlobalProfiles_ItemCheck);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.chkLockHostsFile);
            this.tabPage1.Controls.Add(this.chkHostsBlocklist);
            this.tabPage1.Controls.Add(this.chkAutoUpdateCheck);
            this.tabPage1.Controls.Add(this.chkBlockMalwarePorts);
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Controls.Add(this.chkAskForExceptionDetails);
            this.tabPage1.Controls.Add(this.chkEnableDefaultWindowsRules);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(716, 349);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "General";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // chkLockHostsFile
            // 
            this.chkLockHostsFile.AutoSize = true;
            this.chkLockHostsFile.Location = new System.Drawing.Point(376, 48);
            this.chkLockHostsFile.Name = "chkLockHostsFile";
            this.chkLockHostsFile.Size = new System.Drawing.Size(183, 17);
            this.chkLockHostsFile.TabIndex = 10;
            this.chkLockHostsFile.Text = "Prevent modifications to hosts file";
            this.chkLockHostsFile.UseVisualStyleBackColor = true;
            // 
            // chkHostsBlocklist
            // 
            this.chkHostsBlocklist.AutoSize = true;
            this.chkHostsBlocklist.Location = new System.Drawing.Point(376, 71);
            this.chkHostsBlocklist.Name = "chkHostsBlocklist";
            this.chkHostsBlocklist.Size = new System.Drawing.Size(240, 17);
            this.chkHostsBlocklist.TabIndex = 9;
            this.chkHostsBlocklist.Text = "Block malware and ad servers using hosts file";
            this.chkHostsBlocklist.UseVisualStyleBackColor = true;
            this.chkHostsBlocklist.CheckedChanged += new System.EventHandler(this.chkHostsBlocklist_CheckedChanged);
            // 
            // chkAutoUpdateCheck
            // 
            this.chkAutoUpdateCheck.AutoSize = true;
            this.chkAutoUpdateCheck.Location = new System.Drawing.Point(104, 48);
            this.chkAutoUpdateCheck.Name = "chkAutoUpdateCheck";
            this.chkAutoUpdateCheck.Size = new System.Drawing.Size(177, 17);
            this.chkAutoUpdateCheck.TabIndex = 8;
            this.chkAutoUpdateCheck.Text = "Automatically check for updates";
            this.chkAutoUpdateCheck.UseVisualStyleBackColor = true;
            // 
            // chkBlockMalwarePorts
            // 
            this.chkBlockMalwarePorts.AutoSize = true;
            this.chkBlockMalwarePorts.Location = new System.Drawing.Point(104, 94);
            this.chkBlockMalwarePorts.Name = "chkBlockMalwarePorts";
            this.chkBlockMalwarePorts.Size = new System.Drawing.Size(164, 17);
            this.chkBlockMalwarePorts.TabIndex = 7;
            this.chkBlockMalwarePorts.Text = "Block common malware ports";
            this.chkBlockMalwarePorts.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.chkChangePassword);
            this.groupBox1.Controls.Add(this.txtPasswordAgain);
            this.groupBox1.Controls.Add(this.txtPassword);
            this.groupBox1.Location = new System.Drawing.Point(104, 154);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(508, 125);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Password protection";
            // 
            // label9
            // 
            this.label9.Location = new System.Drawing.Point(249, 60);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(253, 31);
            this.label9.TabIndex = 5;
            this.label9.Text = "To remove an enabled password protection, check \"Change password\" and leave the t" +
    "ext fields empty.";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(48, 83);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(44, 13);
            this.label8.TabIndex = 4;
            this.label8.Text = "Retype:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(36, 60);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(56, 13);
            this.label7.TabIndex = 3;
            this.label7.Text = "Password:";
            // 
            // chkChangePassword
            // 
            this.chkChangePassword.AutoSize = true;
            this.chkChangePassword.Location = new System.Drawing.Point(16, 30);
            this.chkChangePassword.Name = "chkChangePassword";
            this.chkChangePassword.Size = new System.Drawing.Size(111, 17);
            this.chkChangePassword.TabIndex = 2;
            this.chkChangePassword.Text = "Change password";
            this.chkChangePassword.UseVisualStyleBackColor = true;
            this.chkChangePassword.CheckedChanged += new System.EventHandler(this.chkEnablePassword_CheckedChanged);
            // 
            // txtPasswordAgain
            // 
            this.txtPasswordAgain.Enabled = false;
            this.txtPasswordAgain.Location = new System.Drawing.Point(98, 80);
            this.txtPasswordAgain.MaxLength = 16;
            this.txtPasswordAgain.Name = "txtPasswordAgain";
            this.txtPasswordAgain.PasswordChar = '*';
            this.txtPasswordAgain.Size = new System.Drawing.Size(138, 20);
            this.txtPasswordAgain.TabIndex = 1;
            this.txtPasswordAgain.UseSystemPasswordChar = true;
            // 
            // txtPassword
            // 
            this.txtPassword.Enabled = false;
            this.txtPassword.Location = new System.Drawing.Point(98, 57);
            this.txtPassword.MaxLength = 16;
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(138, 20);
            this.txtPassword.TabIndex = 0;
            this.txtPassword.UseSystemPasswordChar = true;
            // 
            // chkAskForExceptionDetails
            // 
            this.chkAskForExceptionDetails.AutoSize = true;
            this.chkAskForExceptionDetails.Location = new System.Drawing.Point(104, 117);
            this.chkAskForExceptionDetails.Name = "chkAskForExceptionDetails";
            this.chkAskForExceptionDetails.Size = new System.Drawing.Size(156, 17);
            this.chkAskForExceptionDetails.TabIndex = 4;
            this.chkAskForExceptionDetails.Text = "Prompt for exception details";
            this.chkAskForExceptionDetails.UseVisualStyleBackColor = true;
            // 
            // chkEnableDefaultWindowsRules
            // 
            this.chkEnableDefaultWindowsRules.AutoSize = true;
            this.chkEnableDefaultWindowsRules.Location = new System.Drawing.Point(104, 71);
            this.chkEnableDefaultWindowsRules.Name = "chkEnableDefaultWindowsRules";
            this.chkEnableDefaultWindowsRules.Size = new System.Drawing.Size(166, 17);
            this.chkEnableDefaultWindowsRules.TabIndex = 3;
            this.chkEnableDefaultWindowsRules.Text = "Enable default Windows rules";
            this.chkEnableDefaultWindowsRules.UseVisualStyleBackColor = true;
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(724, 375);
            this.tabControl1.TabIndex = 6;
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.btnImport);
            this.tabPage4.Controls.Add(this.btnExport);
            this.tabPage4.Controls.Add(this.groupBox2);
            this.tabPage4.Controls.Add(this.btnUpdate);
            this.tabPage4.Controls.Add(this.btnWeb);
            this.tabPage4.Controls.Add(this.btnUninstall);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(716, 349);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Maintenance";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // btnImport
            // 
            this.btnImport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnImport.Image = global::PKSoft.Icons.import;
            this.btnImport.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnImport.Location = new System.Drawing.Point(104, 47);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(127, 36);
            this.btnImport.TabIndex = 9;
            this.btnImport.Text = "Import";
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click_1);
            // 
            // btnExport
            // 
            this.btnExport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExport.Image = global::PKSoft.Icons.export;
            this.btnExport.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnExport.Location = new System.Drawing.Point(104, 89);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(127, 36);
            this.btnExport.TabIndex = 8;
            this.btnExport.Text = "Export";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click_1);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btnDonate);
            this.groupBox2.Controls.Add(this.lblLinkLicense);
            this.groupBox2.Controls.Add(this.label10);
            this.groupBox2.Controls.Add(this.lblAboutHomepageLink);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.lblVersion);
            this.groupBox2.Controls.Add(this.label12);
            this.groupBox2.Location = new System.Drawing.Point(104, 154);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(508, 125);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "About";
            // 
            // btnDonate
            // 
            this.btnDonate.BackColor = System.Drawing.Color.Transparent;
            this.btnDonate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDonate.Image = global::PKSoft.Icons.donate;
            this.btnDonate.Location = new System.Drawing.Point(375, 79);
            this.btnDonate.Name = "btnDonate";
            this.btnDonate.Size = new System.Drawing.Size(92, 26);
            this.btnDonate.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.btnDonate.TabIndex = 8;
            this.btnDonate.TabStop = false;
            this.btnDonate.Click += new System.EventHandler(this.btnDonate_Click);
            this.btnDonate.MouseEnter += new System.EventHandler(this.btnDonate_MouseEnter);
            this.btnDonate.MouseLeave += new System.EventHandler(this.btnDonate_MouseLeave);
            // 
            // lblLinkLicense
            // 
            this.lblLinkLicense.AutoSize = true;
            this.lblLinkLicense.Location = new System.Drawing.Point(18, 92);
            this.lblLinkLicense.Name = "lblLinkLicense";
            this.lblLinkLicense.Size = new System.Drawing.Size(44, 13);
            this.lblLinkLicense.TabIndex = 9;
            this.lblLinkLicense.TabStop = true;
            this.lblLinkLicense.Text = "License";
            this.lblLinkLicense.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblLinkLicense_LinkClicked);
            // 
            // label10
            // 
            this.label10.Location = new System.Drawing.Point(188, 32);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(279, 37);
            this.label10.TabIndex = 8;
            this.label10.Text = "If you like and use this free program, please consider donating to cover hosting," +
    " hardware and software costs.";
            // 
            // lblAboutHomepageLink
            // 
            this.lblAboutHomepageLink.AutoSize = true;
            this.lblAboutHomepageLink.Location = new System.Drawing.Point(18, 77);
            this.lblAboutHomepageLink.Name = "lblAboutHomepageLink";
            this.lblAboutHomepageLink.Size = new System.Drawing.Size(119, 13);
            this.lblAboutHomepageLink.TabIndex = 7;
            this.lblAboutHomepageLink.TabStop = true;
            this.lblAboutHomepageLink.Text = "http://tinywall.pados.hu";
            this.lblAboutHomepageLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblAboutHomepageLink_LinkClicked);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(18, 62);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(93, 13);
            this.label6.TabIndex = 6;
            this.label6.Text = "All rights reserved.";
            // 
            // lblVersion
            // 
            this.lblVersion.AutoSize = true;
            this.lblVersion.Location = new System.Drawing.Point(18, 32);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(51, 13);
            this.lblVersion.TabIndex = 1;
            this.lblVersion.Text = "TinyWall ";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(18, 47);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(155, 13);
            this.label12.TabIndex = 5;
            this.label12.Text = "Copyright © 2011 Károly Pados";
            // 
            // btnUpdate
            // 
            this.btnUpdate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUpdate.Image = global::PKSoft.Icons.update;
            this.btnUpdate.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnUpdate.Location = new System.Drawing.Point(274, 47);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(127, 36);
            this.btnUpdate.TabIndex = 1;
            this.btnUpdate.Text = "Update";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // btnWeb
            // 
            this.btnWeb.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnWeb.Image = global::PKSoft.Icons.web;
            this.btnWeb.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnWeb.Location = new System.Drawing.Point(444, 47);
            this.btnWeb.Name = "btnWeb";
            this.btnWeb.Size = new System.Drawing.Size(127, 36);
            this.btnWeb.TabIndex = 0;
            this.btnWeb.Text = "Visit webpage";
            this.btnWeb.UseVisualStyleBackColor = true;
            this.btnWeb.Click += new System.EventHandler(this.btnWeb_Click);
            // 
            // btnUninstall
            // 
            this.btnUninstall.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUninstall.Image = global::PKSoft.Icons.uninstall;
            this.btnUninstall.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnUninstall.Location = new System.Drawing.Point(274, 89);
            this.btnUninstall.Name = "btnUninstall";
            this.btnUninstall.Size = new System.Drawing.Size(127, 36);
            this.btnUninstall.TabIndex = 2;
            this.btnUninstall.Text = "Uninstall";
            this.btnUninstall.UseVisualStyleBackColor = true;
            this.btnUninstall.Click += new System.EventHandler(this.btnUninstall_Click);
            // 
            // sfd
            // 
            this.sfd.DefaultExt = "xml";
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(748, 438);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.Text = "TinyWall Firewall Settings";
            this.Shown += new System.EventHandler(this.SettingsForm_Shown);
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage4.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.btnDonate)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnAppRemove;
        private System.Windows.Forms.Button btnAppModify;
        private System.Windows.Forms.Button btnAppAdd;
        private System.Windows.Forms.ListView listApplications;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckedListBox listOptionalGlobalProfiles;
        private System.Windows.Forms.CheckedListBox listRecommendedGlobalProfiles;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.CheckBox chkEnableDefaultWindowsRules;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.CheckBox chkAskForExceptionDetails;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox chkChangePassword;
        private System.Windows.Forms.TextBox txtPasswordAgain;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button btnSubmitAssoc;
        private System.Windows.Forms.SaveFileDialog sfd;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.Button btnUninstall;
        private System.Windows.Forms.Button btnWeb;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.CheckBox chkBlockMalwarePorts;
        private System.Windows.Forms.Button btnAppAutoDetect;
        private System.Windows.Forms.ImageList IconList;
        private System.Windows.Forms.CheckBox chkAutoUpdateCheck;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.LinkLabel lblAboutHomepageLink;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.LinkLabel lblLinkLicense;
        private System.Windows.Forms.PictureBox btnDonate;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.OpenFileDialog ofd;
        private System.Windows.Forms.CheckBox chkLockHostsFile;
        private System.Windows.Forms.CheckBox chkHostsBlocklist;
        private System.Windows.Forms.Button btnAppRemoveAll;
    }
}