namespace PKSoft
{
    partial class DevelToolForm
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
            this.ofd = new System.Windows.Forms.OpenFileDialog();
            this.label4 = new System.Windows.Forms.Label();
            this.txtAssocOutputPath = new System.Windows.Forms.TextBox();
            this.btnAssocOutputBrowse = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txtDBFolderPath = new System.Windows.Forms.TextBox();
            this.btnCollectionsCreate = new System.Windows.Forms.Button();
            this.btnProfileFolderBrowse = new System.Windows.Forms.Button();
            this.fbd = new System.Windows.Forms.FolderBrowserDialog();
            this.btnExit = new System.Windows.Forms.Button();
            this.txtStrongName = new System.Windows.Forms.TextBox();
            this.btnStrongNameBrowse = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label1 = new System.Windows.Forms.Label();
            this.btnAssocCreate = new System.Windows.Forms.Button();
            this.btnAssocBrowse = new System.Windows.Forms.Button();
            this.txtAssocExePath = new System.Windows.Forms.TextBox();
            this.txtAssocResult = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.btnUpdateInstallerBrowse = new System.Windows.Forms.Button();
            this.txtUpdateURL = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.btnUpdateOutputBrowse = new System.Windows.Forms.Button();
            this.btnUpdateHostsBorwse = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.txtUpdateOutput = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtUpdateHosts = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtUpdateTWInstaller = new System.Windows.Forms.TextBox();
            this.txtUpdateDatabase = new System.Windows.Forms.TextBox();
            this.btnUpdateDatabaseBrowse = new System.Windows.Forms.Button();
            this.btnUpdateCreate = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.listPrimaries = new System.Windows.Forms.ListBox();
            this.txtOutputPath = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.btnAddPrimaries = new System.Windows.Forms.Button();
            this.btnResxClear = new System.Windows.Forms.Button();
            this.btnOptimize = new System.Windows.Forms.Button();
            this.listSatellites = new System.Windows.Forms.ListBox();
            this.tabPage6 = new System.Windows.Forms.TabPage();
            this.txtTimestampingServ = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.btnSigntoolBrowse = new System.Windows.Forms.Button();
            this.txtSigntool = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.btnBatchSign = new System.Windows.Forms.Button();
            this.btnSignDir = new System.Windows.Forms.Button();
            this.txtSignDir = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.txtCertPass = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.btnCertBrowse = new System.Windows.Forms.Button();
            this.txtCert = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.tabPage5.SuspendLayout();
            this.tabPage6.SuspendLayout();
            this.SuspendLayout();
            // 
            // ofd
            // 
            this.ofd.FileName = "openFileDialog1";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(11, 69);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(95, 17);
            this.label4.TabIndex = 9;
            this.label4.Text = "Output folder:";
            // 
            // txtAssocOutputPath
            // 
            this.txtAssocOutputPath.Location = new System.Drawing.Point(15, 89);
            this.txtAssocOutputPath.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtAssocOutputPath.Name = "txtAssocOutputPath";
            this.txtAssocOutputPath.Size = new System.Drawing.Size(563, 22);
            this.txtAssocOutputPath.TabIndex = 8;
            this.txtAssocOutputPath.Text = "D:\\archive\\d0\\projects\\TinyWall\\TinyWall\\bin";
            // 
            // btnAssocOutputBrowse
            // 
            this.btnAssocOutputBrowse.Location = new System.Drawing.Point(587, 86);
            this.btnAssocOutputBrowse.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnAssocOutputBrowse.Name = "btnAssocOutputBrowse";
            this.btnAssocOutputBrowse.Size = new System.Drawing.Size(100, 28);
            this.btnAssocOutputBrowse.TabIndex = 7;
            this.btnAssocOutputBrowse.Text = "Browse...";
            this.btnAssocOutputBrowse.UseVisualStyleBackColor = true;
            this.btnAssocOutputBrowse.Click += new System.EventHandler(this.btnAssocOutputBrowse_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 21);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(148, 17);
            this.label2.TabIndex = 5;
            this.label2.Text = "Database base folder:";
            // 
            // txtDBFolderPath
            // 
            this.txtDBFolderPath.Location = new System.Drawing.Point(15, 41);
            this.txtDBFolderPath.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtDBFolderPath.Name = "txtDBFolderPath";
            this.txtDBFolderPath.Size = new System.Drawing.Size(563, 22);
            this.txtDBFolderPath.TabIndex = 3;
            this.txtDBFolderPath.Text = "D:\\archive\\d0\\projects\\TinyWall\\TinyWall\\Database";
            // 
            // btnCollectionsCreate
            // 
            this.btnCollectionsCreate.Location = new System.Drawing.Point(695, 41);
            this.btnCollectionsCreate.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnCollectionsCreate.Name = "btnCollectionsCreate";
            this.btnCollectionsCreate.Size = new System.Drawing.Size(145, 73);
            this.btnCollectionsCreate.TabIndex = 2;
            this.btnCollectionsCreate.Text = "Create";
            this.btnCollectionsCreate.UseVisualStyleBackColor = true;
            this.btnCollectionsCreate.Click += new System.EventHandler(this.btnCollectionsCreate_Click);
            // 
            // btnProfileFolderBrowse
            // 
            this.btnProfileFolderBrowse.Location = new System.Drawing.Point(587, 38);
            this.btnProfileFolderBrowse.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnProfileFolderBrowse.Name = "btnProfileFolderBrowse";
            this.btnProfileFolderBrowse.Size = new System.Drawing.Size(100, 28);
            this.btnProfileFolderBrowse.TabIndex = 0;
            this.btnProfileFolderBrowse.Text = "Browse...";
            this.btnProfileFolderBrowse.UseVisualStyleBackColor = true;
            this.btnProfileFolderBrowse.Click += new System.EventHandler(this.btnProfileFolderBrowse_Click);
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(945, 452);
            this.btnExit.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(100, 28);
            this.btnExit.TabIndex = 2;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // txtStrongName
            // 
            this.txtStrongName.Location = new System.Drawing.Point(129, 31);
            this.txtStrongName.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtStrongName.Name = "txtStrongName";
            this.txtStrongName.Size = new System.Drawing.Size(867, 22);
            this.txtStrongName.TabIndex = 1;
            // 
            // btnStrongNameBrowse
            // 
            this.btnStrongNameBrowse.Location = new System.Drawing.Point(21, 28);
            this.btnStrongNameBrowse.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnStrongNameBrowse.Name = "btnStrongNameBrowse";
            this.btnStrongNameBrowse.Size = new System.Drawing.Size(100, 28);
            this.btnStrongNameBrowse.TabIndex = 0;
            this.btnStrongNameBrowse.Text = "Browse...";
            this.btnStrongNameBrowse.UseVisualStyleBackColor = true;
            this.btnStrongNameBrowse.Click += new System.EventHandler(this.btnStrongNameBrowse_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage5);
            this.tabControl1.Controls.Add(this.tabPage6);
            this.tabControl1.Location = new System.Drawing.Point(16, 15);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1029, 430);
            this.tabControl1.TabIndex = 4;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.btnAssocCreate);
            this.tabPage1.Controls.Add(this.btnAssocBrowse);
            this.tabPage1.Controls.Add(this.txtAssocExePath);
            this.tabPage1.Controls.Add(this.txtAssocResult);
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage1.Size = new System.Drawing.Size(1021, 401);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Profile creator";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 15);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(81, 17);
            this.label1.TabIndex = 3;
            this.label1.Text = "Executable:";
            // 
            // btnAssocCreate
            // 
            this.btnAssocCreate.Location = new System.Drawing.Point(695, 32);
            this.btnAssocCreate.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnAssocCreate.Name = "btnAssocCreate";
            this.btnAssocCreate.Size = new System.Drawing.Size(100, 28);
            this.btnAssocCreate.TabIndex = 4;
            this.btnAssocCreate.Text = "Create";
            this.btnAssocCreate.UseVisualStyleBackColor = true;
            this.btnAssocCreate.Click += new System.EventHandler(this.btnAssocCreate_Click);
            // 
            // btnAssocBrowse
            // 
            this.btnAssocBrowse.Location = new System.Drawing.Point(587, 32);
            this.btnAssocBrowse.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnAssocBrowse.Name = "btnAssocBrowse";
            this.btnAssocBrowse.Size = new System.Drawing.Size(100, 28);
            this.btnAssocBrowse.TabIndex = 0;
            this.btnAssocBrowse.Text = "Browse...";
            this.btnAssocBrowse.UseVisualStyleBackColor = true;
            this.btnAssocBrowse.Click += new System.EventHandler(this.btnAssocBrowse_Click);
            // 
            // txtAssocExePath
            // 
            this.txtAssocExePath.Location = new System.Drawing.Point(15, 34);
            this.txtAssocExePath.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtAssocExePath.Name = "txtAssocExePath";
            this.txtAssocExePath.Size = new System.Drawing.Size(563, 22);
            this.txtAssocExePath.TabIndex = 1;
            // 
            // txtAssocResult
            // 
            this.txtAssocResult.Location = new System.Drawing.Point(15, 68);
            this.txtAssocResult.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtAssocResult.Multiline = true;
            this.txtAssocResult.Name = "txtAssocResult";
            this.txtAssocResult.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtAssocResult.Size = new System.Drawing.Size(995, 308);
            this.txtAssocResult.TabIndex = 2;
            this.txtAssocResult.WordWrap = false;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.label4);
            this.tabPage2.Controls.Add(this.txtDBFolderPath);
            this.tabPage2.Controls.Add(this.txtAssocOutputPath);
            this.tabPage2.Controls.Add(this.btnProfileFolderBrowse);
            this.tabPage2.Controls.Add(this.btnAssocOutputBrowse);
            this.tabPage2.Controls.Add(this.btnCollectionsCreate);
            this.tabPage2.Controls.Add(this.label2);
            this.tabPage2.Location = new System.Drawing.Point(4, 25);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage2.Size = new System.Drawing.Size(1021, 401);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Database creator";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.btnUpdateInstallerBrowse);
            this.tabPage4.Controls.Add(this.txtUpdateURL);
            this.tabPage4.Controls.Add(this.label8);
            this.tabPage4.Controls.Add(this.btnUpdateOutputBrowse);
            this.tabPage4.Controls.Add(this.btnUpdateHostsBorwse);
            this.tabPage4.Controls.Add(this.label7);
            this.tabPage4.Controls.Add(this.txtUpdateOutput);
            this.tabPage4.Controls.Add(this.label6);
            this.tabPage4.Controls.Add(this.txtUpdateHosts);
            this.tabPage4.Controls.Add(this.label3);
            this.tabPage4.Controls.Add(this.txtUpdateTWInstaller);
            this.tabPage4.Controls.Add(this.txtUpdateDatabase);
            this.tabPage4.Controls.Add(this.btnUpdateDatabaseBrowse);
            this.tabPage4.Controls.Add(this.btnUpdateCreate);
            this.tabPage4.Controls.Add(this.label5);
            this.tabPage4.Location = new System.Drawing.Point(4, 25);
            this.tabPage4.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Size = new System.Drawing.Size(1021, 401);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Update creator";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // btnUpdateInstallerBrowse
            // 
            this.btnUpdateInstallerBrowse.Location = new System.Drawing.Point(595, 97);
            this.btnUpdateInstallerBrowse.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnUpdateInstallerBrowse.Name = "btnUpdateInstallerBrowse";
            this.btnUpdateInstallerBrowse.Size = new System.Drawing.Size(100, 28);
            this.btnUpdateInstallerBrowse.TabIndex = 25;
            this.btnUpdateInstallerBrowse.Text = "Browse...";
            this.btnUpdateInstallerBrowse.UseVisualStyleBackColor = true;
            this.btnUpdateInstallerBrowse.Click += new System.EventHandler(this.btnUpdateInstallerBrowse_Click);
            // 
            // txtUpdateURL
            // 
            this.txtUpdateURL.Location = new System.Drawing.Point(23, 42);
            this.txtUpdateURL.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtUpdateURL.Name = "txtUpdateURL";
            this.txtUpdateURL.Size = new System.Drawing.Size(563, 22);
            this.txtUpdateURL.TabIndex = 23;
            this.txtUpdateURL.Text = "http://tinywall.pados.hu/updates/UpdVer3/";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(19, 22);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(125, 17);
            this.label8.TabIndex = 24;
            this.label8.Text = "Update base URL:";
            // 
            // btnUpdateOutputBrowse
            // 
            this.btnUpdateOutputBrowse.Location = new System.Drawing.Point(595, 250);
            this.btnUpdateOutputBrowse.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnUpdateOutputBrowse.Name = "btnUpdateOutputBrowse";
            this.btnUpdateOutputBrowse.Size = new System.Drawing.Size(100, 28);
            this.btnUpdateOutputBrowse.TabIndex = 22;
            this.btnUpdateOutputBrowse.Text = "Browse...";
            this.btnUpdateOutputBrowse.UseVisualStyleBackColor = true;
            this.btnUpdateOutputBrowse.Click += new System.EventHandler(this.btnUpdateOutputBrowse_Click);
            // 
            // btnUpdateHostsBorwse
            // 
            this.btnUpdateHostsBorwse.Location = new System.Drawing.Point(595, 199);
            this.btnUpdateHostsBorwse.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnUpdateHostsBorwse.Name = "btnUpdateHostsBorwse";
            this.btnUpdateHostsBorwse.Size = new System.Drawing.Size(100, 28);
            this.btnUpdateHostsBorwse.TabIndex = 21;
            this.btnUpdateHostsBorwse.Text = "Browse...";
            this.btnUpdateHostsBorwse.UseVisualStyleBackColor = true;
            this.btnUpdateHostsBorwse.Click += new System.EventHandler(this.button1_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(19, 234);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(95, 17);
            this.label7.TabIndex = 20;
            this.label7.Text = "Output folder:";
            // 
            // txtUpdateOutput
            // 
            this.txtUpdateOutput.Location = new System.Drawing.Point(23, 254);
            this.txtUpdateOutput.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtUpdateOutput.Name = "txtUpdateOutput";
            this.txtUpdateOutput.Size = new System.Drawing.Size(563, 22);
            this.txtUpdateOutput.TabIndex = 19;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(19, 180);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(70, 17);
            this.label6.TabIndex = 18;
            this.label6.Text = "Hosts file:";
            // 
            // txtUpdateHosts
            // 
            this.txtUpdateHosts.Location = new System.Drawing.Point(23, 199);
            this.txtUpdateHosts.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtUpdateHosts.Name = "txtUpdateHosts";
            this.txtUpdateHosts.Size = new System.Drawing.Size(563, 22);
            this.txtUpdateHosts.TabIndex = 17;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(19, 126);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(105, 17);
            this.label3.TabIndex = 16;
            this.label3.Text = "Database XML:";
            // 
            // txtUpdateTWInstaller
            // 
            this.txtUpdateTWInstaller.Location = new System.Drawing.Point(23, 97);
            this.txtUpdateTWInstaller.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtUpdateTWInstaller.Name = "txtUpdateTWInstaller";
            this.txtUpdateTWInstaller.Size = new System.Drawing.Size(563, 22);
            this.txtUpdateTWInstaller.TabIndex = 12;
            // 
            // txtUpdateDatabase
            // 
            this.txtUpdateDatabase.Location = new System.Drawing.Point(23, 145);
            this.txtUpdateDatabase.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtUpdateDatabase.Name = "txtUpdateDatabase";
            this.txtUpdateDatabase.Size = new System.Drawing.Size(563, 22);
            this.txtUpdateDatabase.TabIndex = 15;
            // 
            // btnUpdateDatabaseBrowse
            // 
            this.btnUpdateDatabaseBrowse.Location = new System.Drawing.Point(595, 143);
            this.btnUpdateDatabaseBrowse.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnUpdateDatabaseBrowse.Name = "btnUpdateDatabaseBrowse";
            this.btnUpdateDatabaseBrowse.Size = new System.Drawing.Size(100, 28);
            this.btnUpdateDatabaseBrowse.TabIndex = 14;
            this.btnUpdateDatabaseBrowse.Text = "Browse...";
            this.btnUpdateDatabaseBrowse.UseVisualStyleBackColor = true;
            this.btnUpdateDatabaseBrowse.Click += new System.EventHandler(this.btnUpdateDatabaseBrowse_Click);
            // 
            // btnUpdateCreate
            // 
            this.btnUpdateCreate.Location = new System.Drawing.Point(715, 132);
            this.btnUpdateCreate.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnUpdateCreate.Name = "btnUpdateCreate";
            this.btnUpdateCreate.Size = new System.Drawing.Size(145, 73);
            this.btnUpdateCreate.TabIndex = 11;
            this.btnUpdateCreate.Text = "Create";
            this.btnUpdateCreate.UseVisualStyleBackColor = true;
            this.btnUpdateCreate.Click += new System.EventHandler(this.btnUpdateCreate_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(19, 78);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(119, 17);
            this.label5.TabIndex = 13;
            this.label5.Text = "TinyWall installer:";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.txtStrongName);
            this.tabPage3.Controls.Add(this.btnStrongNameBrowse);
            this.tabPage3.Location = new System.Drawing.Point(4, 25);
            this.tabPage3.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(1021, 401);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Strong name";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage5
            // 
            this.tabPage5.Controls.Add(this.listPrimaries);
            this.tabPage5.Controls.Add(this.txtOutputPath);
            this.tabPage5.Controls.Add(this.label9);
            this.tabPage5.Controls.Add(this.btnAddPrimaries);
            this.tabPage5.Controls.Add(this.btnResxClear);
            this.tabPage5.Controls.Add(this.btnOptimize);
            this.tabPage5.Controls.Add(this.listSatellites);
            this.tabPage5.Location = new System.Drawing.Point(4, 25);
            this.tabPage5.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage5.Size = new System.Drawing.Size(1021, 401);
            this.tabPage5.TabIndex = 4;
            this.tabPage5.Text = "ResX optimizer";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // listPrimaries
            // 
            this.listPrimaries.FormattingEnabled = true;
            this.listPrimaries.ItemHeight = 16;
            this.listPrimaries.Location = new System.Drawing.Point(8, 7);
            this.listPrimaries.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.listPrimaries.Name = "listPrimaries";
            this.listPrimaries.Size = new System.Drawing.Size(281, 324);
            this.listPrimaries.TabIndex = 8;
            this.listPrimaries.SelectedIndexChanged += new System.EventHandler(this.listPrimaries_SelectedIndexChanged);
            // 
            // txtOutputPath
            // 
            this.txtOutputPath.Location = new System.Drawing.Point(12, 356);
            this.txtOutputPath.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtOutputPath.Name = "txtOutputPath";
            this.txtOutputPath.Size = new System.Drawing.Size(464, 22);
            this.txtOutputPath.TabIndex = 12;
            this.txtOutputPath.Text = "D:\\archive\\d0\\projects\\TinyWall";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(8, 336);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(87, 17);
            this.label9.TabIndex = 13;
            this.label9.Text = "Output path:";
            // 
            // btnAddPrimaries
            // 
            this.btnAddPrimaries.Location = new System.Drawing.Point(299, 23);
            this.btnAddPrimaries.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnAddPrimaries.Name = "btnAddPrimaries";
            this.btnAddPrimaries.Size = new System.Drawing.Size(175, 37);
            this.btnAddPrimaries.TabIndex = 7;
            this.btnAddPrimaries.Text = "Add Primaries";
            this.btnAddPrimaries.UseVisualStyleBackColor = true;
            this.btnAddPrimaries.Click += new System.EventHandler(this.btnAddPrimaries_Click);
            // 
            // btnResxClear
            // 
            this.btnResxClear.Location = new System.Drawing.Point(299, 68);
            this.btnResxClear.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnResxClear.Name = "btnResxClear";
            this.btnResxClear.Size = new System.Drawing.Size(175, 37);
            this.btnResxClear.TabIndex = 11;
            this.btnResxClear.Text = "Clear";
            this.btnResxClear.UseVisualStyleBackColor = true;
            this.btnResxClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnOptimize
            // 
            this.btnOptimize.Location = new System.Drawing.Point(299, 142);
            this.btnOptimize.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnOptimize.Name = "btnOptimize";
            this.btnOptimize.Size = new System.Drawing.Size(175, 37);
            this.btnOptimize.TabIndex = 10;
            this.btnOptimize.Text = "Optimize!";
            this.btnOptimize.UseVisualStyleBackColor = true;
            this.btnOptimize.Click += new System.EventHandler(this.btnOptimize_Click);
            // 
            // listSatellites
            // 
            this.listSatellites.FormattingEnabled = true;
            this.listSatellites.ItemHeight = 16;
            this.listSatellites.Location = new System.Drawing.Point(481, 7);
            this.listSatellites.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.listSatellites.Name = "listSatellites";
            this.listSatellites.Size = new System.Drawing.Size(281, 324);
            this.listSatellites.TabIndex = 9;
            // 
            // tabPage6
            // 
            this.tabPage6.Controls.Add(this.txtTimestampingServ);
            this.tabPage6.Controls.Add(this.label14);
            this.tabPage6.Controls.Add(this.btnSigntoolBrowse);
            this.tabPage6.Controls.Add(this.txtSigntool);
            this.tabPage6.Controls.Add(this.label13);
            this.tabPage6.Controls.Add(this.btnBatchSign);
            this.tabPage6.Controls.Add(this.btnSignDir);
            this.tabPage6.Controls.Add(this.txtSignDir);
            this.tabPage6.Controls.Add(this.label12);
            this.tabPage6.Controls.Add(this.txtCertPass);
            this.tabPage6.Controls.Add(this.label11);
            this.tabPage6.Controls.Add(this.btnCertBrowse);
            this.tabPage6.Controls.Add(this.txtCert);
            this.tabPage6.Controls.Add(this.label10);
            this.tabPage6.Location = new System.Drawing.Point(4, 25);
            this.tabPage6.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage6.Name = "tabPage6";
            this.tabPage6.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tabPage6.Size = new System.Drawing.Size(1021, 401);
            this.tabPage6.TabIndex = 5;
            this.tabPage6.Text = "Batch signer";
            this.tabPage6.UseVisualStyleBackColor = true;
            // 
            // txtTimestampingServ
            // 
            this.txtTimestampingServ.Location = new System.Drawing.Point(12, 245);
            this.txtTimestampingServ.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtTimestampingServ.Name = "txtTimestampingServ";
            this.txtTimestampingServ.Size = new System.Drawing.Size(437, 22);
            this.txtTimestampingServ.TabIndex = 13;
            this.txtTimestampingServ.Text = "http://timestamp.comodoca.com/rfc3161";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(8, 225);
            this.label14.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(149, 17);
            this.label14.TabIndex = 12;
            this.label14.Text = "Timestamping service:";
            // 
            // btnSigntoolBrowse
            // 
            this.btnSigntoolBrowse.Location = new System.Drawing.Point(459, 194);
            this.btnSigntoolBrowse.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnSigntoolBrowse.Name = "btnSigntoolBrowse";
            this.btnSigntoolBrowse.Size = new System.Drawing.Size(100, 28);
            this.btnSigntoolBrowse.TabIndex = 11;
            this.btnSigntoolBrowse.Text = "Browse...";
            this.btnSigntoolBrowse.UseVisualStyleBackColor = true;
            this.btnSigntoolBrowse.Click += new System.EventHandler(this.btnSigntoolBrowse_Click);
            // 
            // txtSigntool
            // 
            this.txtSigntool.Location = new System.Drawing.Point(12, 197);
            this.txtSigntool.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtSigntool.Name = "txtSigntool";
            this.txtSigntool.Size = new System.Drawing.Size(437, 22);
            this.txtSigntool.TabIndex = 10;
            this.txtSigntool.Text = "C:\\Program Files (x86)\\Microsoft SDKs\\Windows\\v7.1A\\Bin\\Signtool.exe";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(8, 177);
            this.label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(116, 17);
            this.label13.TabIndex = 9;
            this.label13.Text = "Signtool location:";
            // 
            // btnBatchSign
            // 
            this.btnBatchSign.Location = new System.Drawing.Point(12, 289);
            this.btnBatchSign.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnBatchSign.Name = "btnBatchSign";
            this.btnBatchSign.Size = new System.Drawing.Size(145, 73);
            this.btnBatchSign.TabIndex = 8;
            this.btnBatchSign.Text = "Batch sign!";
            this.btnBatchSign.UseVisualStyleBackColor = true;
            this.btnBatchSign.Click += new System.EventHandler(this.btnBatchSign_Click);
            // 
            // btnSignDir
            // 
            this.btnSignDir.Location = new System.Drawing.Point(459, 146);
            this.btnSignDir.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnSignDir.Name = "btnSignDir";
            this.btnSignDir.Size = new System.Drawing.Size(100, 28);
            this.btnSignDir.TabIndex = 7;
            this.btnSignDir.Text = "Browse...";
            this.btnSignDir.UseVisualStyleBackColor = true;
            this.btnSignDir.Click += new System.EventHandler(this.btnSignDir_Click);
            // 
            // txtSignDir
            // 
            this.txtSignDir.Location = new System.Drawing.Point(12, 149);
            this.txtSignDir.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtSignDir.Name = "txtSignDir";
            this.txtSignDir.Size = new System.Drawing.Size(437, 22);
            this.txtSignDir.TabIndex = 6;
            this.txtSignDir.Text = "D:\\archive\\d0\\projects\\TinyWall\\MsiSetup\\";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(8, 129);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(159, 17);
            this.label12.TabIndex = 5;
            this.label12.Text = "Directory to sign files in:";
            // 
            // txtCertPass
            // 
            this.txtCertPass.Location = new System.Drawing.Point(12, 101);
            this.txtCertPass.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtCertPass.Name = "txtCertPass";
            this.txtCertPass.Size = new System.Drawing.Size(437, 22);
            this.txtCertPass.TabIndex = 4;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(8, 81);
            this.label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(139, 17);
            this.label11.TabIndex = 3;
            this.label11.Text = "Certificate password:";
            // 
            // btnCertBrowse
            // 
            this.btnCertBrowse.Location = new System.Drawing.Point(459, 50);
            this.btnCertBrowse.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnCertBrowse.Name = "btnCertBrowse";
            this.btnCertBrowse.Size = new System.Drawing.Size(100, 28);
            this.btnCertBrowse.TabIndex = 2;
            this.btnCertBrowse.Text = "Browse...";
            this.btnCertBrowse.UseVisualStyleBackColor = true;
            this.btnCertBrowse.Click += new System.EventHandler(this.btnCertBrowse_Click);
            // 
            // txtCert
            // 
            this.txtCert.Location = new System.Drawing.Point(12, 53);
            this.txtCert.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtCert.Name = "txtCert";
            this.txtCert.Size = new System.Drawing.Size(437, 22);
            this.txtCert.TabIndex = 1;
            this.txtCert.Text = "D:\\archive\\d0\\projects\\TinyWall\\Cert\\KarolyPadosCodeSign.pfx";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(8, 33);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(97, 17);
            this.label10.TabIndex = 0;
            this.label10.Text = "Certificate file:";
            // 
            // DevelToolForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1061, 490);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.btnExit);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.Name = "DevelToolForm";
            this.Text = "TinyWall Development Helper Tool";
            this.Load += new System.EventHandler(this.DevelToolForm_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage4.ResumeLayout(false);
            this.tabPage4.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.tabPage5.ResumeLayout(false);
            this.tabPage5.PerformLayout();
            this.tabPage6.ResumeLayout(false);
            this.tabPage6.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog ofd;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtDBFolderPath;
        private System.Windows.Forms.Button btnCollectionsCreate;
        private System.Windows.Forms.Button btnProfileFolderBrowse;
        private System.Windows.Forms.FolderBrowserDialog fbd;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.TextBox txtStrongName;
        private System.Windows.Forms.Button btnStrongNameBrowse;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtAssocOutputPath;
        private System.Windows.Forms.Button btnAssocOutputBrowse;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnAssocCreate;
        private System.Windows.Forms.Button btnAssocBrowse;
        private System.Windows.Forms.TextBox txtAssocExePath;
        private System.Windows.Forms.TextBox txtAssocResult;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.Button btnUpdateOutputBrowse;
        private System.Windows.Forms.Button btnUpdateHostsBorwse;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtUpdateOutput;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtUpdateHosts;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtUpdateTWInstaller;
        private System.Windows.Forms.TextBox txtUpdateDatabase;
        private System.Windows.Forms.Button btnUpdateDatabaseBrowse;
        private System.Windows.Forms.Button btnUpdateCreate;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtUpdateURL;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button btnUpdateInstallerBrowse;
        private System.Windows.Forms.TabPage tabPage5;
        private System.Windows.Forms.ListBox listPrimaries;
        private System.Windows.Forms.TextBox txtOutputPath;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button btnAddPrimaries;
        private System.Windows.Forms.Button btnResxClear;
        private System.Windows.Forms.Button btnOptimize;
        private System.Windows.Forms.ListBox listSatellites;
        private System.Windows.Forms.TabPage tabPage6;
        private System.Windows.Forms.Button btnBatchSign;
        private System.Windows.Forms.Button btnSignDir;
        private System.Windows.Forms.TextBox txtSignDir;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txtCertPass;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button btnCertBrowse;
        private System.Windows.Forms.TextBox txtCert;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button btnSigntoolBrowse;
        private System.Windows.Forms.TextBox txtSigntool;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtTimestampingServ;
        private System.Windows.Forms.Label label14;
    }
}