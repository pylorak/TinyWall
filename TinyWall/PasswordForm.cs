using System;
using System.Windows.Forms;

namespace pylorak.TinyWall
{
    internal partial class PasswordForm : Form
    {
        private string? m_PassHash;

        internal string? PassHash
        {
            get { return m_PassHash; }
        }

        internal PasswordForm()
        {
            InitializeComponent();
            Utils.SetRightToLeft(this);
            this.btnOK.Image = GlobalInstances.ApplyBtnIcon;
            this.btnCancel.Image = GlobalInstances.CancelBtnIcon;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            m_PassHash = Hasher.HashString(txtPassphrase.Text);
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void PasswordForm_Shown(object sender, EventArgs e)
        {
            txtPassphrase.Focus();
        }
    }
}
