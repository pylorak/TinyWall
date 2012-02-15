namespace PKSoft
{
    partial class ApplicationExceptionForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ApplicationExceptionForm));
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtSrvName = new System.Windows.Forms.TextBox();
            this.txtAppPath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.btnProcess = new System.Windows.Forms.Button();
            this.btnChooseService = new System.Windows.Forms.Button();
            this.listEnabledProfiles = new System.Windows.Forms.ListBox();
            this.listAllProfiles = new System.Windows.Forms.ListBox();
            this.btnAddProfile = new System.Windows.Forms.Button();
            this.btnRemoveProfile = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.ofd = new System.Windows.Forms.OpenFileDialog();
            this.label6 = new System.Windows.Forms.Label();
            this.cmbTimer = new System.Windows.Forms.ComboBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.transparentLabel1 = new PKSoft.TransparentLabel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnAdvSettings = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Image = global::PKSoft.Icons.cancel;
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOK
            // 
            this.btnOK.Image = global::PKSoft.Icons.accept;
            resources.ApplyResources(this.btnOK, "btnOK");
            this.btnOK.Name = "btnOK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.BackColor = System.Drawing.SystemColors.Window;
            this.label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.BackColor = System.Drawing.SystemColors.Window;
            this.label2.Name = "label2";
            // 
            // txtSrvName
            // 
            this.txtSrvName.BackColor = System.Drawing.SystemColors.Window;
            resources.ApplyResources(this.txtSrvName, "txtSrvName");
            this.txtSrvName.Name = "txtSrvName";
            this.txtSrvName.ReadOnly = true;
            this.txtSrvName.TextChanged += new System.EventHandler(this.txtSrvName_TextChanged);
            // 
            // txtAppPath
            // 
            this.txtAppPath.BackColor = System.Drawing.SystemColors.Window;
            resources.ApplyResources(this.txtAppPath, "txtAppPath");
            this.txtAppPath.Name = "txtAppPath";
            this.txtAppPath.ReadOnly = true;
            this.txtAppPath.TextChanged += new System.EventHandler(this.txtAppPath_TextChanged);
            // 
            // btnBrowse
            // 
            this.btnBrowse.BackColor = System.Drawing.Color.AliceBlue;
            resources.ApplyResources(this.btnBrowse, "btnBrowse");
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.UseVisualStyleBackColor = false;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // btnProcess
            // 
            this.btnProcess.BackColor = System.Drawing.Color.AliceBlue;
            resources.ApplyResources(this.btnProcess, "btnProcess");
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.UseVisualStyleBackColor = false;
            this.btnProcess.Click += new System.EventHandler(this.btnProcess_Click);
            // 
            // btnChooseService
            // 
            this.btnChooseService.BackColor = System.Drawing.Color.AliceBlue;
            resources.ApplyResources(this.btnChooseService, "btnChooseService");
            this.btnChooseService.Name = "btnChooseService";
            this.btnChooseService.UseVisualStyleBackColor = false;
            this.btnChooseService.Click += new System.EventHandler(this.btnChooseService_Click);
            // 
            // listEnabledProfiles
            // 
            this.listEnabledProfiles.FormattingEnabled = true;
            resources.ApplyResources(this.listEnabledProfiles, "listEnabledProfiles");
            this.listEnabledProfiles.Name = "listEnabledProfiles";
            this.listEnabledProfiles.Sorted = true;
            this.listEnabledProfiles.SelectedIndexChanged += new System.EventHandler(this.listEnabledProfiles_SelectedIndexChanged);
            this.listEnabledProfiles.DoubleClick += new System.EventHandler(this.listEnabledProfiles_DoubleClick);
            // 
            // listAllProfiles
            // 
            this.listAllProfiles.FormattingEnabled = true;
            resources.ApplyResources(this.listAllProfiles, "listAllProfiles");
            this.listAllProfiles.Name = "listAllProfiles";
            this.listAllProfiles.Sorted = true;
            this.listAllProfiles.SelectedIndexChanged += new System.EventHandler(this.listAllProfiles_SelectedIndexChanged);
            this.listAllProfiles.DoubleClick += new System.EventHandler(this.listAllProfiles_DoubleClick);
            // 
            // btnAddProfile
            // 
            resources.ApplyResources(this.btnAddProfile, "btnAddProfile");
            this.btnAddProfile.Name = "btnAddProfile";
            this.btnAddProfile.UseVisualStyleBackColor = true;
            this.btnAddProfile.Click += new System.EventHandler(this.btnAddProfile_Click);
            // 
            // btnRemoveProfile
            // 
            resources.ApplyResources(this.btnRemoveProfile, "btnRemoveProfile");
            this.btnRemoveProfile.Name = "btnRemoveProfile";
            this.btnRemoveProfile.UseVisualStyleBackColor = true;
            this.btnRemoveProfile.Click += new System.EventHandler(this.btnRemoveProfile_Click);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // ofd
            // 
            resources.ApplyResources(this.ofd, "ofd");
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.BackColor = System.Drawing.SystemColors.Window;
            this.label6.Name = "label6";
            // 
            // cmbTimer
            // 
            this.cmbTimer.BackColor = System.Drawing.SystemColors.Window;
            this.cmbTimer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTimer.FormattingEnabled = true;
            resources.ApplyResources(this.cmbTimer, "cmbTimer");
            this.cmbTimer.Name = "cmbTimer";
            this.cmbTimer.SelectedIndexChanged += new System.EventHandler(this.cmbTimer_SelectedIndexChanged);
            // 
            // panel1
            // 
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.BackgroundImage = global::PKSoft.Icons.green_banner;
            this.panel1.Controls.Add(this.transparentLabel1);
            this.panel1.Name = "panel1";
            // 
            // transparentLabel1
            // 
            resources.ApplyResources(this.transparentLabel1, "transparentLabel1");
            this.transparentLabel1.BackColor = System.Drawing.Color.Transparent;
            this.transparentLabel1.ForeColor = System.Drawing.Color.White;
            this.transparentLabel1.Name = "transparentLabel1";
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.Window;
            this.panel2.Controls.Add(this.cmbTimer);
            this.panel2.Controls.Add(this.label6);
            this.panel2.Controls.Add(this.label1);
            this.panel2.Controls.Add(this.label2);
            this.panel2.Controls.Add(this.txtSrvName);
            this.panel2.Controls.Add(this.txtAppPath);
            this.panel2.Controls.Add(this.btnBrowse);
            this.panel2.Controls.Add(this.btnProcess);
            this.panel2.Controls.Add(this.btnChooseService);
            resources.ApplyResources(this.panel2, "panel2");
            this.panel2.Name = "panel2";
            // 
            // btnAdvSettings
            // 
            this.btnAdvSettings.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnAdvSettings.Image = global::PKSoft.Icons.manage;
            resources.ApplyResources(this.btnAdvSettings, "btnAdvSettings");
            this.btnAdvSettings.Name = "btnAdvSettings";
            this.btnAdvSettings.UseVisualStyleBackColor = true;
            this.btnAdvSettings.Click += new System.EventHandler(this.btnAdvSettings_Click);
            // 
            // ApplicationExceptionForm
            // 
            this.AcceptButton = this.btnOK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ControlBox = false;
            this.Controls.Add(this.btnAdvSettings);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnRemoveProfile);
            this.Controls.Add(this.btnAddProfile);
            this.Controls.Add(this.listAllProfiles);
            this.Controls.Add(this.listEnabledProfiles);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panel2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "ApplicationExceptionForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Load += new System.EventHandler(this.ApplicationExceptionForm_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtSrvName;
        private System.Windows.Forms.TextBox txtAppPath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Button btnProcess;
        private System.Windows.Forms.Button btnChooseService;
        private System.Windows.Forms.ListBox listEnabledProfiles;
        private System.Windows.Forms.ListBox listAllProfiles;
        private System.Windows.Forms.Button btnAddProfile;
        private System.Windows.Forms.Button btnRemoveProfile;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.OpenFileDialog ofd;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cmbTimer;
        private System.Windows.Forms.Panel panel1;
        private TransparentLabel transparentLabel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnAdvSettings;
    }
}