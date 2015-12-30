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
            this.ofd = new System.Windows.Forms.OpenFileDialog();
            this.label6 = new System.Windows.Forms.Label();
            this.cmbTimer = new System.Windows.Forms.ComboBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.transparentLabel1 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.chkRestrictToLocalNetwork = new System.Windows.Forms.CheckBox();
            this.radBlock = new System.Windows.Forms.RadioButton();
            this.radTcpUdpOut = new System.Windows.Forms.RadioButton();
            this.radTcpUdpUnrestricted = new System.Windows.Forms.RadioButton();
            this.radUnrestricted = new System.Windows.Forms.RadioButton();
            this.label5 = new System.Windows.Forms.Label();
            this.txtListenPortUDP = new System.Windows.Forms.TextBox();
            this.txtListenPortTCP = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtOutboundPortUDP = new System.Windows.Forms.TextBox();
            this.txtOutboundPortTCP = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.radOnlySpecifiedPorts = new System.Windows.Forms.RadioButton();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Image = global::PKSoft.Resources.Icons.cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOK
            // 
            resources.ApplyResources(this.btnOK, "btnOK");
            this.btnOK.Image = global::PKSoft.Resources.Icons.accept;
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
            resources.ApplyResources(this.txtSrvName, "txtSrvName");
            this.txtSrvName.BackColor = System.Drawing.SystemColors.Window;
            this.txtSrvName.Name = "txtSrvName";
            this.txtSrvName.ReadOnly = true;
            this.txtSrvName.TextChanged += new System.EventHandler(this.txtSrvName_TextChanged);
            // 
            // txtAppPath
            // 
            resources.ApplyResources(this.txtAppPath, "txtAppPath");
            this.txtAppPath.BackColor = System.Drawing.SystemColors.Window;
            this.txtAppPath.Name = "txtAppPath";
            this.txtAppPath.ReadOnly = true;
            this.txtAppPath.TextChanged += new System.EventHandler(this.txtAppPath_TextChanged);
            // 
            // btnBrowse
            // 
            resources.ApplyResources(this.btnBrowse, "btnBrowse");
            this.btnBrowse.BackColor = System.Drawing.Color.AliceBlue;
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.UseVisualStyleBackColor = false;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // btnProcess
            // 
            resources.ApplyResources(this.btnProcess, "btnProcess");
            this.btnProcess.BackColor = System.Drawing.Color.AliceBlue;
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.UseVisualStyleBackColor = false;
            this.btnProcess.Click += new System.EventHandler(this.btnProcess_Click);
            // 
            // btnChooseService
            // 
            resources.ApplyResources(this.btnChooseService, "btnChooseService");
            this.btnChooseService.BackColor = System.Drawing.Color.AliceBlue;
            this.btnChooseService.Name = "btnChooseService";
            this.btnChooseService.UseVisualStyleBackColor = false;
            this.btnChooseService.Click += new System.EventHandler(this.btnChooseService_Click);
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
            resources.ApplyResources(this.cmbTimer, "cmbTimer");
            this.cmbTimer.BackColor = System.Drawing.SystemColors.Window;
            this.cmbTimer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTimer.FormattingEnabled = true;
            this.cmbTimer.Name = "cmbTimer";
            this.cmbTimer.SelectedIndexChanged += new System.EventHandler(this.cmbTimer_SelectedIndexChanged);
            // 
            // panel1
            // 
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.BackgroundImage = global::PKSoft.Resources.Icons.green_banner;
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
            resources.ApplyResources(this.panel2, "panel2");
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
            this.panel2.Name = "panel2";
            // 
            // chkRestrictToLocalNetwork
            // 
            resources.ApplyResources(this.chkRestrictToLocalNetwork, "chkRestrictToLocalNetwork");
            this.chkRestrictToLocalNetwork.Name = "chkRestrictToLocalNetwork";
            this.chkRestrictToLocalNetwork.UseVisualStyleBackColor = true;
            // 
            // radBlock
            // 
            resources.ApplyResources(this.radBlock, "radBlock");
            this.radBlock.Name = "radBlock";
            this.radBlock.TabStop = true;
            this.radBlock.UseVisualStyleBackColor = true;
            this.radBlock.CheckedChanged += new System.EventHandler(this.radRestriction_CheckedChanged);
            // 
            // radTcpUdpOut
            // 
            resources.ApplyResources(this.radTcpUdpOut, "radTcpUdpOut");
            this.radTcpUdpOut.Name = "radTcpUdpOut";
            this.radTcpUdpOut.TabStop = true;
            this.radTcpUdpOut.UseVisualStyleBackColor = true;
            this.radTcpUdpOut.CheckedChanged += new System.EventHandler(this.radRestriction_CheckedChanged);
            // 
            // radTcpUdpUnrestricted
            // 
            resources.ApplyResources(this.radTcpUdpUnrestricted, "radTcpUdpUnrestricted");
            this.radTcpUdpUnrestricted.Checked = true;
            this.radTcpUdpUnrestricted.Name = "radTcpUdpUnrestricted";
            this.radTcpUdpUnrestricted.TabStop = true;
            this.radTcpUdpUnrestricted.UseVisualStyleBackColor = true;
            this.radTcpUdpUnrestricted.CheckedChanged += new System.EventHandler(this.radRestriction_CheckedChanged);
            // 
            // radUnrestricted
            // 
            resources.ApplyResources(this.radUnrestricted, "radUnrestricted");
            this.radUnrestricted.Name = "radUnrestricted";
            this.radUnrestricted.TabStop = true;
            this.radUnrestricted.UseVisualStyleBackColor = true;
            this.radUnrestricted.CheckedChanged += new System.EventHandler(this.radRestriction_CheckedChanged);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // txtListenPortUDP
            // 
            resources.ApplyResources(this.txtListenPortUDP, "txtListenPortUDP");
            this.txtListenPortUDP.Name = "txtListenPortUDP";
            // 
            // txtListenPortTCP
            // 
            resources.ApplyResources(this.txtListenPortTCP, "txtListenPortTCP");
            this.txtListenPortTCP.Name = "txtListenPortTCP";
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
            // txtOutboundPortUDP
            // 
            resources.ApplyResources(this.txtOutboundPortUDP, "txtOutboundPortUDP");
            this.txtOutboundPortUDP.Name = "txtOutboundPortUDP";
            // 
            // txtOutboundPortTCP
            // 
            resources.ApplyResources(this.txtOutboundPortTCP, "txtOutboundPortTCP");
            this.txtOutboundPortTCP.Name = "txtOutboundPortTCP";
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label9.Name = "label9";
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label10.Name = "label10";
            // 
            // radOnlySpecifiedPorts
            // 
            resources.ApplyResources(this.radOnlySpecifiedPorts, "radOnlySpecifiedPorts");
            this.radOnlySpecifiedPorts.Name = "radOnlySpecifiedPorts";
            this.radOnlySpecifiedPorts.TabStop = true;
            this.radOnlySpecifiedPorts.UseVisualStyleBackColor = true;
            this.radOnlySpecifiedPorts.CheckedChanged += new System.EventHandler(this.radRestriction_CheckedChanged);
            // 
            // panel3
            // 
            resources.ApplyResources(this.panel3, "panel3");
            this.panel3.Controls.Add(this.label5);
            this.panel3.Controls.Add(this.label10);
            this.panel3.Controls.Add(this.label7);
            this.panel3.Controls.Add(this.txtListenPortUDP);
            this.panel3.Controls.Add(this.txtOutboundPortTCP);
            this.panel3.Controls.Add(this.txtOutboundPortUDP);
            this.panel3.Controls.Add(this.label8);
            this.panel3.Controls.Add(this.label4);
            this.panel3.Controls.Add(this.label3);
            this.panel3.Controls.Add(this.txtListenPortTCP);
            this.panel3.Controls.Add(this.label9);
            this.panel3.Name = "panel3";
            // 
            // ApplicationExceptionForm
            // 
            this.AcceptButton = this.btnOK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ControlBox = false;
            this.Controls.Add(this.radOnlySpecifiedPorts);
            this.Controls.Add(this.radUnrestricted);
            this.Controls.Add(this.radTcpUdpUnrestricted);
            this.Controls.Add(this.radTcpUdpOut);
            this.Controls.Add(this.radBlock);
            this.Controls.Add(this.chkRestrictToLocalNetwork);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel3);
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
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
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
        private System.Windows.Forms.OpenFileDialog ofd;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cmbTimer;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label transparentLabel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.CheckBox chkRestrictToLocalNetwork;
        private System.Windows.Forms.RadioButton radBlock;
        private System.Windows.Forms.RadioButton radTcpUdpOut;
        private System.Windows.Forms.RadioButton radTcpUdpUnrestricted;
        private System.Windows.Forms.RadioButton radUnrestricted;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtListenPortUDP;
        private System.Windows.Forms.TextBox txtListenPortTCP;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtOutboundPortUDP;
        private System.Windows.Forms.TextBox txtOutboundPortTCP;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.RadioButton radOnlySpecifiedPorts;
        private System.Windows.Forms.Panel panel3;
    }
}