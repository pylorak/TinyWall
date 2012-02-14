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
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.txtAssocResult = new System.Windows.Forms.TextBox();
            this.txtAssocExePath = new System.Windows.Forms.TextBox();
            this.btnAssocBrowse = new System.Windows.Forms.Button();
            this.btnAssocCreate = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtUpdateTWInstaller = new System.Windows.Forms.TextBox();
            this.txtUpdateDatabase = new System.Windows.Forms.TextBox();
            this.btnUpdateDatabaseBrowse = new System.Windows.Forms.Button();
            this.btnUpdateCreate = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.txtUpdateHosts = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtUpdateOutput = new System.Windows.Forms.TextBox();
            this.btnUpdateHostsBorwse = new System.Windows.Forms.Button();
            this.btnUpdateOutputBrowse = new System.Windows.Forms.Button();
            this.txtUpdateURL = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.btnUpdateInstallerBrowse = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.SuspendLayout();
            // 
            // ofd
            // 
            this.ofd.FileName = "openFileDialog1";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 56);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(71, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Output folder:";
            // 
            // txtAssocOutputPath
            // 
            this.txtAssocOutputPath.Location = new System.Drawing.Point(11, 72);
            this.txtAssocOutputPath.Name = "txtAssocOutputPath";
            this.txtAssocOutputPath.Size = new System.Drawing.Size(423, 20);
            this.txtAssocOutputPath.TabIndex = 8;
            this.txtAssocOutputPath.Text = "D:\\Projects\\TinyWall\\TinyWall\\bin\\Debug";
            // 
            // btnAssocOutputBrowse
            // 
            this.btnAssocOutputBrowse.Location = new System.Drawing.Point(440, 70);
            this.btnAssocOutputBrowse.Name = "btnAssocOutputBrowse";
            this.btnAssocOutputBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnAssocOutputBrowse.TabIndex = 7;
            this.btnAssocOutputBrowse.Text = "Browse...";
            this.btnAssocOutputBrowse.UseVisualStyleBackColor = true;
            this.btnAssocOutputBrowse.Click += new System.EventHandler(this.btnAssocOutputBrowse_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 17);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(111, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Database base folder:";
            // 
            // txtDBFolderPath
            // 
            this.txtDBFolderPath.Location = new System.Drawing.Point(11, 33);
            this.txtDBFolderPath.Name = "txtDBFolderPath";
            this.txtDBFolderPath.Size = new System.Drawing.Size(423, 20);
            this.txtDBFolderPath.TabIndex = 3;
            this.txtDBFolderPath.Text = "D:\\Projects\\TinyWall\\TinyWall\\Database";
            // 
            // btnCollectionsCreate
            // 
            this.btnCollectionsCreate.Location = new System.Drawing.Point(521, 33);
            this.btnCollectionsCreate.Name = "btnCollectionsCreate";
            this.btnCollectionsCreate.Size = new System.Drawing.Size(109, 59);
            this.btnCollectionsCreate.TabIndex = 2;
            this.btnCollectionsCreate.Text = "Create";
            this.btnCollectionsCreate.UseVisualStyleBackColor = true;
            this.btnCollectionsCreate.Click += new System.EventHandler(this.btnCollectionsCreate_Click);
            // 
            // btnProfileFolderBrowse
            // 
            this.btnProfileFolderBrowse.Location = new System.Drawing.Point(440, 31);
            this.btnProfileFolderBrowse.Name = "btnProfileFolderBrowse";
            this.btnProfileFolderBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnProfileFolderBrowse.TabIndex = 0;
            this.btnProfileFolderBrowse.Text = "Browse...";
            this.btnProfileFolderBrowse.UseVisualStyleBackColor = true;
            this.btnProfileFolderBrowse.Click += new System.EventHandler(this.btnProfileFolderBrowse_Click);
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(709, 367);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(75, 23);
            this.btnExit.TabIndex = 2;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // txtStrongName
            // 
            this.txtStrongName.Location = new System.Drawing.Point(97, 25);
            this.txtStrongName.Name = "txtStrongName";
            this.txtStrongName.Size = new System.Drawing.Size(651, 20);
            this.txtStrongName.TabIndex = 1;
            // 
            // btnStrongNameBrowse
            // 
            this.btnStrongNameBrowse.Location = new System.Drawing.Point(16, 23);
            this.btnStrongNameBrowse.Name = "btnStrongNameBrowse";
            this.btnStrongNameBrowse.Size = new System.Drawing.Size(75, 23);
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
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(772, 349);
            this.tabControl1.TabIndex = 4;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.txtStrongName);
            this.tabPage3.Controls.Add(this.btnStrongNameBrowse);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(764, 323);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Strong name";
            this.tabPage3.UseVisualStyleBackColor = true;
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
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Size = new System.Drawing.Size(764, 323);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Update creator";
            this.tabPage4.UseVisualStyleBackColor = true;
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
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(764, 323);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Database creator";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.btnAssocCreate);
            this.tabPage1.Controls.Add(this.btnAssocBrowse);
            this.tabPage1.Controls.Add(this.txtAssocExePath);
            this.tabPage1.Controls.Add(this.txtAssocResult);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(764, 323);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Profile creator";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // txtAssocResult
            // 
            this.txtAssocResult.Location = new System.Drawing.Point(11, 55);
            this.txtAssocResult.Multiline = true;
            this.txtAssocResult.Name = "txtAssocResult";
            this.txtAssocResult.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtAssocResult.Size = new System.Drawing.Size(747, 251);
            this.txtAssocResult.TabIndex = 2;
            this.txtAssocResult.WordWrap = false;
            // 
            // txtAssocExePath
            // 
            this.txtAssocExePath.Location = new System.Drawing.Point(11, 28);
            this.txtAssocExePath.Name = "txtAssocExePath";
            this.txtAssocExePath.Size = new System.Drawing.Size(423, 20);
            this.txtAssocExePath.TabIndex = 1;
            // 
            // btnAssocBrowse
            // 
            this.btnAssocBrowse.Location = new System.Drawing.Point(440, 26);
            this.btnAssocBrowse.Name = "btnAssocBrowse";
            this.btnAssocBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnAssocBrowse.TabIndex = 0;
            this.btnAssocBrowse.Text = "Browse...";
            this.btnAssocBrowse.UseVisualStyleBackColor = true;
            this.btnAssocBrowse.Click += new System.EventHandler(this.btnAssocBrowse_Click);
            // 
            // btnAssocCreate
            // 
            this.btnAssocCreate.Location = new System.Drawing.Point(521, 26);
            this.btnAssocCreate.Name = "btnAssocCreate";
            this.btnAssocCreate.Size = new System.Drawing.Size(75, 23);
            this.btnAssocCreate.TabIndex = 4;
            this.btnAssocCreate.Text = "Create";
            this.btnAssocCreate.UseVisualStyleBackColor = true;
            this.btnAssocCreate.Click += new System.EventHandler(this.btnAssocCreate_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Executable:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(14, 102);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(81, 13);
            this.label3.TabIndex = 16;
            this.label3.Text = "Database XML:";
            // 
            // txtUpdateTWInstaller
            // 
            this.txtUpdateTWInstaller.Location = new System.Drawing.Point(17, 79);
            this.txtUpdateTWInstaller.Name = "txtUpdateTWInstaller";
            this.txtUpdateTWInstaller.Size = new System.Drawing.Size(423, 20);
            this.txtUpdateTWInstaller.TabIndex = 12;
            // 
            // txtUpdateDatabase
            // 
            this.txtUpdateDatabase.Location = new System.Drawing.Point(17, 118);
            this.txtUpdateDatabase.Name = "txtUpdateDatabase";
            this.txtUpdateDatabase.Size = new System.Drawing.Size(423, 20);
            this.txtUpdateDatabase.TabIndex = 15;
            // 
            // btnUpdateDatabaseBrowse
            // 
            this.btnUpdateDatabaseBrowse.Location = new System.Drawing.Point(446, 116);
            this.btnUpdateDatabaseBrowse.Name = "btnUpdateDatabaseBrowse";
            this.btnUpdateDatabaseBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnUpdateDatabaseBrowse.TabIndex = 14;
            this.btnUpdateDatabaseBrowse.Text = "Browse...";
            this.btnUpdateDatabaseBrowse.UseVisualStyleBackColor = true;
            this.btnUpdateDatabaseBrowse.Click += new System.EventHandler(this.btnUpdateDatabaseBrowse_Click);
            // 
            // btnUpdateCreate
            // 
            this.btnUpdateCreate.Location = new System.Drawing.Point(536, 107);
            this.btnUpdateCreate.Name = "btnUpdateCreate";
            this.btnUpdateCreate.Size = new System.Drawing.Size(109, 59);
            this.btnUpdateCreate.TabIndex = 11;
            this.btnUpdateCreate.Text = "Create";
            this.btnUpdateCreate.UseVisualStyleBackColor = true;
            this.btnUpdateCreate.Click += new System.EventHandler(this.btnUpdateCreate_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(14, 63);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(89, 13);
            this.label5.TabIndex = 13;
            this.label5.Text = "TinyWall installer:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(14, 146);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(53, 13);
            this.label6.TabIndex = 18;
            this.label6.Text = "Hosts file:";
            // 
            // txtUpdateHosts
            // 
            this.txtUpdateHosts.Location = new System.Drawing.Point(17, 162);
            this.txtUpdateHosts.Name = "txtUpdateHosts";
            this.txtUpdateHosts.Size = new System.Drawing.Size(423, 20);
            this.txtUpdateHosts.TabIndex = 17;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(14, 190);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(71, 13);
            this.label7.TabIndex = 20;
            this.label7.Text = "Output folder:";
            // 
            // txtUpdateOutput
            // 
            this.txtUpdateOutput.Location = new System.Drawing.Point(17, 206);
            this.txtUpdateOutput.Name = "txtUpdateOutput";
            this.txtUpdateOutput.Size = new System.Drawing.Size(423, 20);
            this.txtUpdateOutput.TabIndex = 19;
            // 
            // btnUpdateHostsBorwse
            // 
            this.btnUpdateHostsBorwse.Location = new System.Drawing.Point(446, 162);
            this.btnUpdateHostsBorwse.Name = "btnUpdateHostsBorwse";
            this.btnUpdateHostsBorwse.Size = new System.Drawing.Size(75, 23);
            this.btnUpdateHostsBorwse.TabIndex = 21;
            this.btnUpdateHostsBorwse.Text = "Browse...";
            this.btnUpdateHostsBorwse.UseVisualStyleBackColor = true;
            this.btnUpdateHostsBorwse.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnUpdateOutputBrowse
            // 
            this.btnUpdateOutputBrowse.Location = new System.Drawing.Point(446, 203);
            this.btnUpdateOutputBrowse.Name = "btnUpdateOutputBrowse";
            this.btnUpdateOutputBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnUpdateOutputBrowse.TabIndex = 22;
            this.btnUpdateOutputBrowse.Text = "Browse...";
            this.btnUpdateOutputBrowse.UseVisualStyleBackColor = true;
            this.btnUpdateOutputBrowse.Click += new System.EventHandler(this.btnUpdateOutputBrowse_Click);
            // 
            // txtUpdateURL
            // 
            this.txtUpdateURL.Location = new System.Drawing.Point(17, 34);
            this.txtUpdateURL.Name = "txtUpdateURL";
            this.txtUpdateURL.Size = new System.Drawing.Size(423, 20);
            this.txtUpdateURL.TabIndex = 23;
            this.txtUpdateURL.Text = "http://tinywall.pados.hu/updates/UpdVer2/";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(14, 18);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(96, 13);
            this.label8.TabIndex = 24;
            this.label8.Text = "Update base URL:";
            // 
            // btnUpdateInstallerBrowse
            // 
            this.btnUpdateInstallerBrowse.Location = new System.Drawing.Point(446, 79);
            this.btnUpdateInstallerBrowse.Name = "btnUpdateInstallerBrowse";
            this.btnUpdateInstallerBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnUpdateInstallerBrowse.TabIndex = 25;
            this.btnUpdateInstallerBrowse.Text = "Browse...";
            this.btnUpdateInstallerBrowse.UseVisualStyleBackColor = true;
            this.btnUpdateInstallerBrowse.Click += new System.EventHandler(this.btnUpdateInstallerBrowse_Click);
            // 
            // DevelToolForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(796, 398);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.btnExit);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "DevelToolForm";
            this.Text = "TinyWall Development Helper Tool";
            this.Load += new System.EventHandler(this.DevelToolForm_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.tabPage4.ResumeLayout(false);
            this.tabPage4.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
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
    }
}