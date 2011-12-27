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
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(431, 469);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 33);
            this.btnCancel.TabIndex = 10;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOK
            // 
            this.btnOK.Image = global::PKSoft.Icons.accept;
            this.btnOK.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnOK.Location = new System.Drawing.Point(350, 469);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 33);
            this.btnOK.TabIndex = 9;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.SystemColors.Window;
            this.label1.Location = new System.Drawing.Point(33, 60);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(87, 13);
            this.label1.TabIndex = 14;
            this.label1.Text = "Executable path:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.SystemColors.Window;
            this.label2.Location = new System.Drawing.Point(45, 87);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 13);
            this.label2.TabIndex = 15;
            this.label2.Text = "Service name:";
            // 
            // txtSrvName
            // 
            this.txtSrvName.BackColor = System.Drawing.SystemColors.Window;
            this.txtSrvName.Location = new System.Drawing.Point(126, 84);
            this.txtSrvName.Name = "txtSrvName";
            this.txtSrvName.ReadOnly = true;
            this.txtSrvName.Size = new System.Drawing.Size(214, 20);
            this.txtSrvName.TabIndex = 16;
            this.txtSrvName.TextChanged += new System.EventHandler(this.txtSrvName_TextChanged);
            // 
            // txtAppPath
            // 
            this.txtAppPath.BackColor = System.Drawing.SystemColors.Window;
            this.txtAppPath.Location = new System.Drawing.Point(126, 57);
            this.txtAppPath.Name = "txtAppPath";
            this.txtAppPath.ReadOnly = true;
            this.txtAppPath.Size = new System.Drawing.Size(214, 20);
            this.txtAppPath.TabIndex = 17;
            this.txtAppPath.TextChanged += new System.EventHandler(this.txtAppPath_TextChanged);
            // 
            // btnBrowse
            // 
            this.btnBrowse.BackColor = System.Drawing.Color.AliceBlue;
            this.btnBrowse.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowse.Location = new System.Drawing.Point(364, 55);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(142, 23);
            this.btnBrowse.TabIndex = 18;
            this.btnBrowse.Text = "Browse for a file...";
            this.btnBrowse.UseVisualStyleBackColor = false;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // btnProcess
            // 
            this.btnProcess.BackColor = System.Drawing.Color.AliceBlue;
            this.btnProcess.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnProcess.Location = new System.Drawing.Point(364, 28);
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.Size = new System.Drawing.Size(142, 23);
            this.btnProcess.TabIndex = 19;
            this.btnProcess.Text = "Select a process...";
            this.btnProcess.UseVisualStyleBackColor = false;
            this.btnProcess.Click += new System.EventHandler(this.btnProcess_Click);
            // 
            // btnChooseService
            // 
            this.btnChooseService.BackColor = System.Drawing.Color.AliceBlue;
            this.btnChooseService.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnChooseService.Location = new System.Drawing.Point(364, 82);
            this.btnChooseService.Name = "btnChooseService";
            this.btnChooseService.Size = new System.Drawing.Size(142, 23);
            this.btnChooseService.TabIndex = 20;
            this.btnChooseService.Text = "Choose a service...";
            this.btnChooseService.UseVisualStyleBackColor = false;
            this.btnChooseService.Click += new System.EventHandler(this.btnChooseService_Click);
            // 
            // listEnabledProfiles
            // 
            this.listEnabledProfiles.FormattingEnabled = true;
            this.listEnabledProfiles.Location = new System.Drawing.Point(31, 211);
            this.listEnabledProfiles.Name = "listEnabledProfiles";
            this.listEnabledProfiles.Size = new System.Drawing.Size(197, 238);
            this.listEnabledProfiles.Sorted = true;
            this.listEnabledProfiles.TabIndex = 21;
            this.listEnabledProfiles.SelectedIndexChanged += new System.EventHandler(this.listEnabledProfiles_SelectedIndexChanged);
            this.listEnabledProfiles.DoubleClick += new System.EventHandler(this.listEnabledProfiles_DoubleClick);
            // 
            // listAllProfiles
            // 
            this.listAllProfiles.FormattingEnabled = true;
            this.listAllProfiles.Location = new System.Drawing.Point(309, 211);
            this.listAllProfiles.Name = "listAllProfiles";
            this.listAllProfiles.Size = new System.Drawing.Size(197, 238);
            this.listAllProfiles.Sorted = true;
            this.listAllProfiles.TabIndex = 22;
            this.listAllProfiles.SelectedIndexChanged += new System.EventHandler(this.listAllProfiles_SelectedIndexChanged);
            this.listAllProfiles.DoubleClick += new System.EventHandler(this.listAllProfiles_DoubleClick);
            // 
            // btnAddProfile
            // 
            this.btnAddProfile.Enabled = false;
            this.btnAddProfile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddProfile.Location = new System.Drawing.Point(234, 240);
            this.btnAddProfile.Name = "btnAddProfile";
            this.btnAddProfile.Size = new System.Drawing.Size(69, 31);
            this.btnAddProfile.TabIndex = 23;
            this.btnAddProfile.Text = "<<";
            this.btnAddProfile.UseVisualStyleBackColor = true;
            this.btnAddProfile.Click += new System.EventHandler(this.btnAddProfile_Click);
            // 
            // btnRemoveProfile
            // 
            this.btnRemoveProfile.Enabled = false;
            this.btnRemoveProfile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRemoveProfile.Location = new System.Drawing.Point(234, 277);
            this.btnRemoveProfile.Name = "btnRemoveProfile";
            this.btnRemoveProfile.Size = new System.Drawing.Size(69, 31);
            this.btnRemoveProfile.TabIndex = 24;
            this.btnRemoveProfile.Text = ">>";
            this.btnRemoveProfile.UseVisualStyleBackColor = true;
            this.btnRemoveProfile.Click += new System.EventHandler(this.btnRemoveProfile_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(28, 195);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(82, 13);
            this.label3.TabIndex = 25;
            this.label3.Text = "Enabled profiles";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(306, 195);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(86, 13);
            this.label4.TabIndex = 26;
            this.label4.Text = "Available profiles";
            // 
            // ofd
            // 
            this.ofd.Filter = "*.exe|*.exe|*.*|*.*";
            this.ofd.Title = "Select application executable";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.BackColor = System.Drawing.SystemColors.Window;
            this.label6.Location = new System.Drawing.Point(28, 33);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(92, 13);
            this.label6.TabIndex = 28;
            this.label6.Text = "Exception lifetime:";
            // 
            // cmbTimer
            // 
            this.cmbTimer.BackColor = System.Drawing.SystemColors.Window;
            this.cmbTimer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTimer.FormattingEnabled = true;
            this.cmbTimer.Location = new System.Drawing.Point(126, 30);
            this.cmbTimer.Name = "cmbTimer";
            this.cmbTimer.Size = new System.Drawing.Size(144, 21);
            this.cmbTimer.TabIndex = 29;
            this.cmbTimer.SelectedIndexChanged += new System.EventHandler(this.cmbTimer_SelectedIndexChanged);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackgroundImage = global::PKSoft.Icons.green_banner;
            this.panel1.Controls.Add(this.transparentLabel1);
            this.panel1.Location = new System.Drawing.Point(0, -1);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(535, 48);
            this.panel1.TabIndex = 30;
            // 
            // transparentLabel1
            // 
            this.transparentLabel1.AutoSize = true;
            this.transparentLabel1.BackColor = System.Drawing.Color.Transparent;
            this.transparentLabel1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.transparentLabel1.ForeColor = System.Drawing.Color.White;
            this.transparentLabel1.Location = new System.Drawing.Point(162, 12);
            this.transparentLabel1.Name = "transparentLabel1";
            this.transparentLabel1.Size = new System.Drawing.Size(207, 24);
            this.transparentLabel1.TabIndex = 0;
            this.transparentLabel1.Text = "Recognized application";
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
            this.panel2.Location = new System.Drawing.Point(0, 47);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(535, 133);
            this.panel2.TabIndex = 31;
            // 
            // btnAdvSettings
            // 
            this.btnAdvSettings.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnAdvSettings.Image = global::PKSoft.Icons.manage;
            this.btnAdvSettings.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnAdvSettings.Location = new System.Drawing.Point(31, 469);
            this.btnAdvSettings.Name = "btnAdvSettings";
            this.btnAdvSettings.Size = new System.Drawing.Size(105, 33);
            this.btnAdvSettings.TabIndex = 32;
            this.btnAdvSettings.Text = "Advanced";
            this.btnAdvSettings.UseVisualStyleBackColor = true;
            this.btnAdvSettings.Click += new System.EventHandler(this.btnAdvSettings_Click);
            // 
            // ApplicationExceptionForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(534, 517);
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
            this.Text = "Add/Modify Firewall Exception - TinyWall";
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