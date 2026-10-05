namespace pylorak.TinyWall
{
    partial class PathFilterForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PathFilterForm));
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.labelOriginalPath = new System.Windows.Forms.Label();
            this.txtOriginalPath = new System.Windows.Forms.TextBox();
            this.labelPattern = new System.Windows.Forms.Label();
            this.txtPattern = new System.Windows.Forms.TextBox();
            this.labelSecurityBoundary = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnClearFilter = new System.Windows.Forms.Button();
            this.btnApply = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.tableLayoutPanel.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // tableLayoutPanel
            //
            resources.ApplyResources(this.tableLayoutPanel, "tableLayoutPanel");
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.Controls.Add(this.labelOriginalPath, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.txtOriginalPath, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.labelPattern, 0, 1);
            this.tableLayoutPanel.Controls.Add(this.txtPattern, 1, 1);
            this.tableLayoutPanel.Controls.Add(this.labelSecurityBoundary, 0, 2);
            this.tableLayoutPanel.Controls.Add(this.buttonPanel, 0, 3);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            //
            // labelOriginalPath
            //
            resources.ApplyResources(this.labelOriginalPath, "labelOriginalPath");
            this.labelOriginalPath.Name = "labelOriginalPath";
            //
            // txtOriginalPath
            //
            resources.ApplyResources(this.txtOriginalPath, "txtOriginalPath");
            this.txtOriginalPath.BackColor = System.Drawing.SystemColors.Window;
            this.txtOriginalPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtOriginalPath.Name = "txtOriginalPath";
            this.txtOriginalPath.ReadOnly = true;
            //
            // labelPattern
            //
            resources.ApplyResources(this.labelPattern, "labelPattern");
            this.labelPattern.Name = "labelPattern";
            //
            // txtPattern
            //
            resources.ApplyResources(this.txtPattern, "txtPattern");
            this.txtPattern.BackColor = System.Drawing.SystemColors.Window;
            this.txtPattern.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPattern.Name = "txtPattern";
            this.toolTip.SetToolTip(this.txtPattern, resources.GetString("txtPattern.ToolTip"));
            //
            // labelSecurityBoundary
            //
            resources.ApplyResources(this.labelSecurityBoundary, "labelSecurityBoundary");
            this.tableLayoutPanel.SetColumnSpan(this.labelSecurityBoundary, 2);
            this.labelSecurityBoundary.ForeColor = System.Drawing.Color.DarkOrange;
            this.labelSecurityBoundary.Name = "labelSecurityBoundary";
            //
            // buttonPanel
            //
            resources.ApplyResources(this.buttonPanel, "buttonPanel");
            this.tableLayoutPanel.SetColumnSpan(this.buttonPanel, 2);
            this.buttonPanel.Controls.Add(this.btnCancel);
            this.buttonPanel.Controls.Add(this.btnClearFilter);
            this.buttonPanel.Controls.Add(this.btnApply);
            this.buttonPanel.Name = "buttonPanel";
            //
            // btnCancel
            //
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            //
            // btnClearFilter
            //
            resources.ApplyResources(this.btnClearFilter, "btnClearFilter");
            this.btnClearFilter.Name = "btnClearFilter";
            this.btnClearFilter.UseVisualStyleBackColor = true;
            this.btnClearFilter.Click += new System.EventHandler(this.btnClearFilter_Click);
            this.toolTip.SetToolTip(this.btnClearFilter, resources.GetString("btnClearFilter.ToolTip"));
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
            // PathFilterForm
            //
            resources.ApplyResources(this, "$this");
            this.AcceptButton = this.btnApply;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.tableLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PathFilterForm";
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
        private System.Windows.Forms.Label labelOriginalPath;
        private System.Windows.Forms.TextBox txtOriginalPath;
        private System.Windows.Forms.Label labelPattern;
        private System.Windows.Forms.TextBox txtPattern;
        private System.Windows.Forms.Label labelSecurityBoundary;
        private System.Windows.Forms.FlowLayoutPanel buttonPanel;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnClearFilter;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
