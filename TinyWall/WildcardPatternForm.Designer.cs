namespace pylorak.TinyWall
{
    partial class WildcardPatternForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WildcardPatternForm));
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblOriginalPath = new System.Windows.Forms.Label();
            this.txtOriginalPath = new System.Windows.Forms.TextBox();
            this.lblPattern = new System.Windows.Forms.Label();
            this.txtPattern = new System.Windows.Forms.TextBox();
            this.lblSecurityBoundary = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnClearPattern = new System.Windows.Forms.Button();
            this.btnApply = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.tableLayoutPanel.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            resources.ApplyResources(this.tableLayoutPanel, "tableLayoutPanel");
            this.tableLayoutPanel.Controls.Add(this.lblOriginalPath, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.txtOriginalPath, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.lblPattern, 0, 1);
            this.tableLayoutPanel.Controls.Add(this.txtPattern, 1, 1);
            this.tableLayoutPanel.Controls.Add(this.lblSecurityBoundary, 0, 2);
            this.tableLayoutPanel.Controls.Add(this.buttonPanel, 0, 3);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            // 
            // lblOriginalPath
            // 
            resources.ApplyResources(this.lblOriginalPath, "lblOriginalPath");
            this.lblOriginalPath.Name = "lblOriginalPath";
            // 
            // txtOriginalPath
            // 
            this.txtOriginalPath.BackColor = System.Drawing.SystemColors.Window;
            this.txtOriginalPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.txtOriginalPath, "txtOriginalPath");
            this.txtOriginalPath.Name = "txtOriginalPath";
            this.txtOriginalPath.ReadOnly = true;
            // 
            // lblPattern
            // 
            resources.ApplyResources(this.lblPattern, "lblPattern");
            this.lblPattern.Name = "lblPattern";
            // 
            // txtPattern
            // 
            this.txtPattern.BackColor = System.Drawing.SystemColors.Window;
            this.txtPattern.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.txtPattern, "txtPattern");
            this.txtPattern.Name = "txtPattern";
            this.toolTip.SetToolTip(this.txtPattern, resources.GetString("txtPattern.ToolTip"));
            // 
            // lblSecurityBoundary
            // 
            resources.ApplyResources(this.lblSecurityBoundary, "lblSecurityBoundary");
            this.tableLayoutPanel.SetColumnSpan(this.lblSecurityBoundary, 2);
            this.lblSecurityBoundary.ForeColor = System.Drawing.Color.DarkOrange;
            this.lblSecurityBoundary.Name = "lblSecurityBoundary";
            // 
            // buttonPanel
            // 
            this.tableLayoutPanel.SetColumnSpan(this.buttonPanel, 2);
            this.buttonPanel.Controls.Add(this.btnCancel);
            this.buttonPanel.Controls.Add(this.btnClearPattern);
            this.buttonPanel.Controls.Add(this.btnApply);
            resources.ApplyResources(this.buttonPanel, "buttonPanel");
            this.buttonPanel.Name = "buttonPanel";
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnClearPattern
            // 
            resources.ApplyResources(this.btnClearPattern, "btnClearPattern");
            this.btnClearPattern.Name = "btnClearPattern";
            this.toolTip.SetToolTip(this.btnClearPattern, resources.GetString("btnClearPattern.ToolTip"));
            this.btnClearPattern.UseVisualStyleBackColor = true;
            this.btnClearPattern.Click += new System.EventHandler(this.btnClearPattern_Click);
            // 
            // btnApply
            // 
            resources.ApplyResources(this.btnApply, "btnApply");
            this.btnApply.Name = "btnApply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // toolTip
            // 
            this.toolTip.AutoPopDelay = 10000;
            this.toolTip.InitialDelay = 300;
            this.toolTip.ReshowDelay = 100;
            this.toolTip.ShowAlways = true;
            // 
            // WildcardPatternForm
            // 
            this.AcceptButton = this.btnApply;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.tableLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "WildcardPatternForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            this.buttonPanel.ResumeLayout(false);
            this.buttonPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.Label lblOriginalPath;
        private System.Windows.Forms.TextBox txtOriginalPath;
        private System.Windows.Forms.Label lblPattern;
        private System.Windows.Forms.TextBox txtPattern;
        private System.Windows.Forms.Label lblSecurityBoundary;
        private System.Windows.Forms.FlowLayoutPanel buttonPanel;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnClearPattern;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
