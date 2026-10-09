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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WildcardPatternForm));
            this.tableForm = new System.Windows.Forms.TableLayoutPanel();
            this.lblOriginalPath = new System.Windows.Forms.Label();
            this.txtOriginalPath = new System.Windows.Forms.TextBox();
            this.lblPattern = new System.Windows.Forms.Label();
            this.txtPattern = new System.Windows.Forms.TextBox();
            this.tableButtons = new System.Windows.Forms.TableLayoutPanel();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnClearPattern = new System.Windows.Forms.Button();
            this.lblHints = new System.Windows.Forms.Label();
            this.tableForm.SuspendLayout();
            this.tableButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableForm
            // 
            resources.ApplyResources(this.tableForm, "tableForm");
            this.tableForm.Controls.Add(this.lblOriginalPath, 0, 0);
            this.tableForm.Controls.Add(this.txtOriginalPath, 1, 0);
            this.tableForm.Controls.Add(this.lblPattern, 0, 1);
            this.tableForm.Controls.Add(this.txtPattern, 1, 1);
            this.tableForm.Controls.Add(this.tableButtons, 0, 6);
            this.tableForm.Controls.Add(this.lblHints, 0, 3);
            this.tableForm.Name = "tableForm";
            // 
            // lblOriginalPath
            // 
            resources.ApplyResources(this.lblOriginalPath, "lblOriginalPath");
            this.lblOriginalPath.Name = "lblOriginalPath";
            // 
            // txtOriginalPath
            // 
            this.txtOriginalPath.BackColor = System.Drawing.SystemColors.Window;
            resources.ApplyResources(this.txtOriginalPath, "txtOriginalPath");
            this.txtOriginalPath.Name = "txtOriginalPath";
            this.txtOriginalPath.ReadOnly = true;
            this.txtOriginalPath.TabStop = false;
            // 
            // lblPattern
            // 
            resources.ApplyResources(this.lblPattern, "lblPattern");
            this.lblPattern.Name = "lblPattern";
            // 
            // txtPattern
            // 
            this.txtPattern.BackColor = System.Drawing.SystemColors.Window;
            resources.ApplyResources(this.txtPattern, "txtPattern");
            this.txtPattern.Name = "txtPattern";
            // 
            // tableButtons
            // 
            resources.ApplyResources(this.tableButtons, "tableButtons");
            this.tableForm.SetColumnSpan(this.tableButtons, 2);
            this.tableButtons.Controls.Add(this.btnOK, 2, 0);
            this.tableButtons.Controls.Add(this.btnCancel, 3, 0);
            this.tableButtons.Controls.Add(this.btnClearPattern, 0, 0);
            this.tableButtons.Name = "tableButtons";
            // 
            // btnOK
            // 
            resources.ApplyResources(this.btnOK, "btnOK");
            this.btnOK.Name = "btnOK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
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
            this.btnClearPattern.UseVisualStyleBackColor = true;
            this.btnClearPattern.Click += new System.EventHandler(this.btnClearPattern_Click);
            // 
            // lblHints
            // 
            resources.ApplyResources(this.lblHints, "lblHints");
            this.tableForm.SetColumnSpan(this.lblHints, 2);
            this.lblHints.Name = "lblHints";
            // 
            // WildcardPatternForm
            // 
            this.AcceptButton = this.btnOK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ControlBox = false;
            this.Controls.Add(this.tableForm);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "WildcardPatternForm";
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Shown += new System.EventHandler(this.WildcardPatternForm_Shown);
            this.tableForm.ResumeLayout(false);
            this.tableForm.PerformLayout();
            this.tableButtons.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.TableLayoutPanel tableForm;
        private System.Windows.Forms.Label lblOriginalPath;
        private System.Windows.Forms.TextBox txtOriginalPath;
        private System.Windows.Forms.Label lblPattern;
        private System.Windows.Forms.Label lblHints;
        private System.Windows.Forms.TextBox txtPattern;
        private System.Windows.Forms.TableLayoutPanel tableButtons;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnClearPattern;
    }
}
