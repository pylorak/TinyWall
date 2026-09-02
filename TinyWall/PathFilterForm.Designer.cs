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
            this.tableLayoutPanel.ColumnCount = 2;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.Controls.Add(this.labelOriginalPath, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.txtOriginalPath, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.labelPattern, 0, 1);
            this.tableLayoutPanel.Controls.Add(this.txtPattern, 1, 1);
            this.tableLayoutPanel.Controls.Add(this.labelSecurityBoundary, 0, 2);
            this.tableLayoutPanel.Controls.Add(this.buttonPanel, 0, 3);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(12, 12);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 4;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.Size = new System.Drawing.Size(676, 186);
            this.tableLayoutPanel.TabIndex = 0;
            //
            // labelOriginalPath
            //
            resources.ApplyResources(this.labelOriginalPath, "labelOriginalPath");
            this.labelOriginalPath.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelOriginalPath.AutoSize = true;
            this.labelOriginalPath.Location = new System.Drawing.Point(3, 7);
            this.labelOriginalPath.Name = "labelOriginalPath";
            this.labelOriginalPath.TabIndex = 0;
            //
            // txtOriginalPath
            //
            this.txtOriginalPath.BackColor = System.Drawing.SystemColors.Window;
            this.txtOriginalPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtOriginalPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtOriginalPath.Location = new System.Drawing.Point(9, 3);
            this.txtOriginalPath.Name = "txtOriginalPath";
            this.txtOriginalPath.ReadOnly = true;
            this.txtOriginalPath.Size = new System.Drawing.Size(664, 23);
            this.txtOriginalPath.TabIndex = 1;
            //
            // labelPattern
            //
            resources.ApplyResources(this.labelPattern, "labelPattern");
            this.labelPattern.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelPattern.AutoSize = true;
            this.labelPattern.Location = new System.Drawing.Point(3, 36);
            this.labelPattern.Name = "labelPattern";
            this.labelPattern.TabIndex = 2;
            //
            // txtPattern
            //
            resources.ApplyResources(this.txtPattern, "txtPattern");
            this.txtPattern.BackColor = System.Drawing.SystemColors.Window;
            this.txtPattern.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPattern.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPattern.Location = new System.Drawing.Point(9, 32);
            this.txtPattern.Name = "txtPattern";
            this.txtPattern.Size = new System.Drawing.Size(664, 23);
            this.txtPattern.TabIndex = 3;
            this.toolTip.SetToolTip(this.txtPattern, resources.GetString("txtPattern.ToolTip"));
            //
            // labelSecurityBoundary
            //
            resources.ApplyResources(this.labelSecurityBoundary, "labelSecurityBoundary");
            this.labelSecurityBoundary.AutoSize = true;
            this.tableLayoutPanel.SetColumnSpan(this.labelSecurityBoundary, 2);
            this.labelSecurityBoundary.ForeColor = System.Drawing.Color.DarkRed;
            this.labelSecurityBoundary.Location = new System.Drawing.Point(3, 58);
            this.labelSecurityBoundary.MaximumSize = new System.Drawing.Size(660, 0);
            this.labelSecurityBoundary.Name = "labelSecurityBoundary";
            this.labelSecurityBoundary.TabIndex = 4;
            //
            // buttonPanel
            //
            this.tableLayoutPanel.SetColumnSpan(this.buttonPanel, 2);
            this.buttonPanel.Controls.Add(this.btnCancel);
            this.buttonPanel.Controls.Add(this.btnClearFilter);
            this.buttonPanel.Controls.Add(this.btnApply);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonPanel.Location = new System.Drawing.Point(3, 143);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new System.Drawing.Size(670, 40);
            this.buttonPanel.TabIndex = 5;
            //
            // btnCancel
            //
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.AutoSize = true;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnCancel.Location = new System.Drawing.Point(592, 3);
            this.btnCancel.MinimumSize = new System.Drawing.Size(75, 29);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 29);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.UseVisualStyleBackColor = true;
            //
            // btnClearFilter
            //
            resources.ApplyResources(this.btnClearFilter, "btnClearFilter");
            this.btnClearFilter.AutoSize = true;
            this.btnClearFilter.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnClearFilter.Location = new System.Drawing.Point(511, 3);
            this.btnClearFilter.MinimumSize = new System.Drawing.Size(75, 29);
            this.btnClearFilter.Name = "btnClearFilter";
            this.btnClearFilter.Size = new System.Drawing.Size(75, 29);
            this.btnClearFilter.TabIndex = 1;
            this.btnClearFilter.UseVisualStyleBackColor = true;
            this.btnClearFilter.Click += new System.EventHandler(this.btnClearFilter_Click);
            this.toolTip.SetToolTip(this.btnClearFilter, resources.GetString("btnClearFilter.ToolTip"));
            //
            // btnApply
            //
            resources.ApplyResources(this.btnApply, "btnApply");
            this.btnApply.AutoSize = true;
            this.btnApply.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnApply.Location = new System.Drawing.Point(430, 3);
            this.btnApply.MinimumSize = new System.Drawing.Size(75, 29);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(75, 29);
            this.btnApply.TabIndex = 0;
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
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.tableLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PathFilterForm";
            this.Padding = new System.Windows.Forms.Padding(12);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
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
