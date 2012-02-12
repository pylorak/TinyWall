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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnAssocCreate = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.txtAssocResult = new System.Windows.Forms.TextBox();
            this.txtAssocExePath = new System.Windows.Forms.TextBox();
            this.btnAssocBrowse = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtAssocOutputPath = new System.Windows.Forms.TextBox();
            this.btnAssocOutputBrowse = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txtDBFolderPath = new System.Windows.Forms.TextBox();
            this.btnCollectionsCreate = new System.Windows.Forms.Button();
            this.btnProfileFolderBrowse = new System.Windows.Forms.Button();
            this.fbd = new System.Windows.Forms.FolderBrowserDialog();
            this.btnExit = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.txtStrongName = new System.Windows.Forms.TextBox();
            this.btnStrongNameBrowse = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // ofd
            // 
            this.ofd.FileName = "openFileDialog1";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnAssocCreate);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.txtAssocResult);
            this.groupBox1.Controls.Add(this.txtAssocExePath);
            this.groupBox1.Controls.Add(this.btnAssocBrowse);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(772, 300);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Create Profile Association";
            // 
            // btnAssocCreate
            // 
            this.btnAssocCreate.Location = new System.Drawing.Point(525, 42);
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
            this.label1.Location = new System.Drawing.Point(12, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Executable:";
            // 
            // txtAssocResult
            // 
            this.txtAssocResult.Location = new System.Drawing.Point(15, 70);
            this.txtAssocResult.Multiline = true;
            this.txtAssocResult.Name = "txtAssocResult";
            this.txtAssocResult.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtAssocResult.Size = new System.Drawing.Size(751, 224);
            this.txtAssocResult.TabIndex = 2;
            this.txtAssocResult.WordWrap = false;
            // 
            // txtAssocExePath
            // 
            this.txtAssocExePath.Location = new System.Drawing.Point(15, 44);
            this.txtAssocExePath.Name = "txtAssocExePath";
            this.txtAssocExePath.Size = new System.Drawing.Size(423, 20);
            this.txtAssocExePath.TabIndex = 1;
            // 
            // btnAssocBrowse
            // 
            this.btnAssocBrowse.Location = new System.Drawing.Point(444, 42);
            this.btnAssocBrowse.Name = "btnAssocBrowse";
            this.btnAssocBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnAssocBrowse.TabIndex = 0;
            this.btnAssocBrowse.Text = "Browse...";
            this.btnAssocBrowse.UseVisualStyleBackColor = true;
            this.btnAssocBrowse.Click += new System.EventHandler(this.btnAssocBrowse_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.txtAssocOutputPath);
            this.groupBox2.Controls.Add(this.btnAssocOutputBrowse);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.txtDBFolderPath);
            this.groupBox2.Controls.Add(this.btnCollectionsCreate);
            this.groupBox2.Controls.Add(this.btnProfileFolderBrowse);
            this.groupBox2.Location = new System.Drawing.Point(12, 318);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(772, 131);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Collection Creator";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 71);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(71, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Output folder:";
            // 
            // txtAssocOutputPath
            // 
            this.txtAssocOutputPath.Location = new System.Drawing.Point(15, 87);
            this.txtAssocOutputPath.Name = "txtAssocOutputPath";
            this.txtAssocOutputPath.Size = new System.Drawing.Size(423, 20);
            this.txtAssocOutputPath.TabIndex = 8;
            this.txtAssocOutputPath.Text = "D:\\Projects\\TinyWall\\TinyWall\\bin\\Debug";
            // 
            // btnAssocOutputBrowse
            // 
            this.btnAssocOutputBrowse.Location = new System.Drawing.Point(444, 85);
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
            this.label2.Location = new System.Drawing.Point(12, 32);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(111, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Database base folder:";
            // 
            // txtDBFolderPath
            // 
            this.txtDBFolderPath.Location = new System.Drawing.Point(15, 48);
            this.txtDBFolderPath.Name = "txtDBFolderPath";
            this.txtDBFolderPath.Size = new System.Drawing.Size(423, 20);
            this.txtDBFolderPath.TabIndex = 3;
            this.txtDBFolderPath.Text = "D:\\Projects\\TinyWall\\TinyWall\\Database";
            // 
            // btnCollectionsCreate
            // 
            this.btnCollectionsCreate.Location = new System.Drawing.Point(525, 48);
            this.btnCollectionsCreate.Name = "btnCollectionsCreate";
            this.btnCollectionsCreate.Size = new System.Drawing.Size(109, 59);
            this.btnCollectionsCreate.TabIndex = 2;
            this.btnCollectionsCreate.Text = "Create";
            this.btnCollectionsCreate.UseVisualStyleBackColor = true;
            this.btnCollectionsCreate.Click += new System.EventHandler(this.btnCollectionsCreate_Click);
            // 
            // btnProfileFolderBrowse
            // 
            this.btnProfileFolderBrowse.Location = new System.Drawing.Point(444, 46);
            this.btnProfileFolderBrowse.Name = "btnProfileFolderBrowse";
            this.btnProfileFolderBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnProfileFolderBrowse.TabIndex = 0;
            this.btnProfileFolderBrowse.Text = "Browse...";
            this.btnProfileFolderBrowse.UseVisualStyleBackColor = true;
            this.btnProfileFolderBrowse.Click += new System.EventHandler(this.btnProfileFolderBrowse_Click);
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(709, 538);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(75, 23);
            this.btnExit.TabIndex = 2;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.txtStrongName);
            this.groupBox3.Controls.Add(this.btnStrongNameBrowse);
            this.groupBox3.Location = new System.Drawing.Point(12, 455);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(772, 66);
            this.groupBox3.TabIndex = 3;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Strong Name";
            // 
            // txtStrongName
            // 
            this.txtStrongName.Location = new System.Drawing.Point(96, 30);
            this.txtStrongName.Name = "txtStrongName";
            this.txtStrongName.Size = new System.Drawing.Size(670, 20);
            this.txtStrongName.TabIndex = 1;
            // 
            // btnStrongNameBrowse
            // 
            this.btnStrongNameBrowse.Location = new System.Drawing.Point(15, 28);
            this.btnStrongNameBrowse.Name = "btnStrongNameBrowse";
            this.btnStrongNameBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnStrongNameBrowse.TabIndex = 0;
            this.btnStrongNameBrowse.Text = "Browse...";
            this.btnStrongNameBrowse.UseVisualStyleBackColor = true;
            this.btnStrongNameBrowse.Click += new System.EventHandler(this.btnStrongNameBrowse_Click);
            // 
            // DevelToolForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(796, 570);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "DevelToolForm";
            this.Text = "TinyWall Development Helper Tool";
            this.Load += new System.EventHandler(this.DevelToolForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog ofd;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnAssocCreate;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtAssocResult;
        private System.Windows.Forms.TextBox txtAssocExePath;
        private System.Windows.Forms.Button btnAssocBrowse;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtDBFolderPath;
        private System.Windows.Forms.Button btnCollectionsCreate;
        private System.Windows.Forms.Button btnProfileFolderBrowse;
        private System.Windows.Forms.FolderBrowserDialog fbd;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox txtStrongName;
        private System.Windows.Forms.Button btnStrongNameBrowse;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtAssocOutputPath;
        private System.Windows.Forms.Button btnAssocOutputBrowse;
    }
}