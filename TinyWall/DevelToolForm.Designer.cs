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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DevelToolForm));
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
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.SuspendLayout();
            // 
            // ofd
            // 
            this.ofd.FileName = "openFileDialog1";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // txtAssocOutputPath
            // 
            resources.ApplyResources(this.txtAssocOutputPath, "txtAssocOutputPath");
            this.txtAssocOutputPath.Name = "txtAssocOutputPath";
            // 
            // btnAssocOutputBrowse
            // 
            resources.ApplyResources(this.btnAssocOutputBrowse, "btnAssocOutputBrowse");
            this.btnAssocOutputBrowse.Name = "btnAssocOutputBrowse";
            this.btnAssocOutputBrowse.UseVisualStyleBackColor = true;
            this.btnAssocOutputBrowse.Click += new System.EventHandler(this.btnAssocOutputBrowse_Click);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // txtDBFolderPath
            // 
            resources.ApplyResources(this.txtDBFolderPath, "txtDBFolderPath");
            this.txtDBFolderPath.Name = "txtDBFolderPath";
            // 
            // btnCollectionsCreate
            // 
            resources.ApplyResources(this.btnCollectionsCreate, "btnCollectionsCreate");
            this.btnCollectionsCreate.Name = "btnCollectionsCreate";
            this.btnCollectionsCreate.UseVisualStyleBackColor = true;
            this.btnCollectionsCreate.Click += new System.EventHandler(this.btnCollectionsCreate_Click);
            // 
            // btnProfileFolderBrowse
            // 
            resources.ApplyResources(this.btnProfileFolderBrowse, "btnProfileFolderBrowse");
            this.btnProfileFolderBrowse.Name = "btnProfileFolderBrowse";
            this.btnProfileFolderBrowse.UseVisualStyleBackColor = true;
            this.btnProfileFolderBrowse.Click += new System.EventHandler(this.btnProfileFolderBrowse_Click);
            // 
            // btnExit
            // 
            resources.ApplyResources(this.btnExit, "btnExit");
            this.btnExit.Name = "btnExit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // txtStrongName
            // 
            resources.ApplyResources(this.txtStrongName, "txtStrongName");
            this.txtStrongName.Name = "txtStrongName";
            // 
            // btnStrongNameBrowse
            // 
            resources.ApplyResources(this.btnStrongNameBrowse, "btnStrongNameBrowse");
            this.btnStrongNameBrowse.Name = "btnStrongNameBrowse";
            this.btnStrongNameBrowse.UseVisualStyleBackColor = true;
            this.btnStrongNameBrowse.Click += new System.EventHandler(this.btnStrongNameBrowse_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Controls.Add(this.tabPage3);
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.btnAssocCreate);
            this.tabPage1.Controls.Add(this.btnAssocBrowse);
            this.tabPage1.Controls.Add(this.txtAssocExePath);
            this.tabPage1.Controls.Add(this.txtAssocResult);
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // btnAssocCreate
            // 
            resources.ApplyResources(this.btnAssocCreate, "btnAssocCreate");
            this.btnAssocCreate.Name = "btnAssocCreate";
            this.btnAssocCreate.UseVisualStyleBackColor = true;
            this.btnAssocCreate.Click += new System.EventHandler(this.btnAssocCreate_Click);
            // 
            // btnAssocBrowse
            // 
            resources.ApplyResources(this.btnAssocBrowse, "btnAssocBrowse");
            this.btnAssocBrowse.Name = "btnAssocBrowse";
            this.btnAssocBrowse.UseVisualStyleBackColor = true;
            this.btnAssocBrowse.Click += new System.EventHandler(this.btnAssocBrowse_Click);
            // 
            // txtAssocExePath
            // 
            resources.ApplyResources(this.txtAssocExePath, "txtAssocExePath");
            this.txtAssocExePath.Name = "txtAssocExePath";
            // 
            // txtAssocResult
            // 
            resources.ApplyResources(this.txtAssocResult, "txtAssocResult");
            this.txtAssocResult.Name = "txtAssocResult";
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
            resources.ApplyResources(this.tabPage2, "tabPage2");
            this.tabPage2.Name = "tabPage2";
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
            resources.ApplyResources(this.tabPage4, "tabPage4");
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // btnUpdateInstallerBrowse
            // 
            resources.ApplyResources(this.btnUpdateInstallerBrowse, "btnUpdateInstallerBrowse");
            this.btnUpdateInstallerBrowse.Name = "btnUpdateInstallerBrowse";
            this.btnUpdateInstallerBrowse.UseVisualStyleBackColor = true;
            this.btnUpdateInstallerBrowse.Click += new System.EventHandler(this.btnUpdateInstallerBrowse_Click);
            // 
            // txtUpdateURL
            // 
            resources.ApplyResources(this.txtUpdateURL, "txtUpdateURL");
            this.txtUpdateURL.Name = "txtUpdateURL";
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // btnUpdateOutputBrowse
            // 
            resources.ApplyResources(this.btnUpdateOutputBrowse, "btnUpdateOutputBrowse");
            this.btnUpdateOutputBrowse.Name = "btnUpdateOutputBrowse";
            this.btnUpdateOutputBrowse.UseVisualStyleBackColor = true;
            this.btnUpdateOutputBrowse.Click += new System.EventHandler(this.btnUpdateOutputBrowse_Click);
            // 
            // btnUpdateHostsBorwse
            // 
            resources.ApplyResources(this.btnUpdateHostsBorwse, "btnUpdateHostsBorwse");
            this.btnUpdateHostsBorwse.Name = "btnUpdateHostsBorwse";
            this.btnUpdateHostsBorwse.UseVisualStyleBackColor = true;
            this.btnUpdateHostsBorwse.Click += new System.EventHandler(this.button1_Click);
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // txtUpdateOutput
            // 
            resources.ApplyResources(this.txtUpdateOutput, "txtUpdateOutput");
            this.txtUpdateOutput.Name = "txtUpdateOutput";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // txtUpdateHosts
            // 
            resources.ApplyResources(this.txtUpdateHosts, "txtUpdateHosts");
            this.txtUpdateHosts.Name = "txtUpdateHosts";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // txtUpdateTWInstaller
            // 
            resources.ApplyResources(this.txtUpdateTWInstaller, "txtUpdateTWInstaller");
            this.txtUpdateTWInstaller.Name = "txtUpdateTWInstaller";
            // 
            // txtUpdateDatabase
            // 
            resources.ApplyResources(this.txtUpdateDatabase, "txtUpdateDatabase");
            this.txtUpdateDatabase.Name = "txtUpdateDatabase";
            // 
            // btnUpdateDatabaseBrowse
            // 
            resources.ApplyResources(this.btnUpdateDatabaseBrowse, "btnUpdateDatabaseBrowse");
            this.btnUpdateDatabaseBrowse.Name = "btnUpdateDatabaseBrowse";
            this.btnUpdateDatabaseBrowse.UseVisualStyleBackColor = true;
            this.btnUpdateDatabaseBrowse.Click += new System.EventHandler(this.btnUpdateDatabaseBrowse_Click);
            // 
            // btnUpdateCreate
            // 
            resources.ApplyResources(this.btnUpdateCreate, "btnUpdateCreate");
            this.btnUpdateCreate.Name = "btnUpdateCreate";
            this.btnUpdateCreate.UseVisualStyleBackColor = true;
            this.btnUpdateCreate.Click += new System.EventHandler(this.btnUpdateCreate_Click);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.txtStrongName);
            this.tabPage3.Controls.Add(this.btnStrongNameBrowse);
            resources.ApplyResources(this.tabPage3, "tabPage3");
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // DevelToolForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.btnExit);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "DevelToolForm";
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