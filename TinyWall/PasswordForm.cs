using DarkModeForms;
using System;
using System.Windows.Forms;

namespace pylorak.TinyWall
{
    internal partial class PasswordForm : Form
    {
        internal string PassHash { get; private set; } = string.Empty;
        private readonly DarkModeCS? DarkMode;

        internal PasswordForm()
        {
            InitializeComponent();
            Utils.SetRightToLeft(this);
            try
            {
                if (Utils.IsDarkModeActive(ActiveConfig.Controller))
                    this.DarkMode = new(this) { ColorMode = DarkModeCS.DisplayMode.DarkMode };
            }
            catch {
                // PasswordForm can be shown during uninstall (if TinyWall is locked), and ActiveConfig.Controller will be null
                // and throw a NullReferenceExcpetion. We on purpose suppress all exceptions instead of doing a targeted null-check.
                // Being unable to activate dark mode isn't critical and really no errors that happen here should prevent the
                // form from working due to the possible installer context, so this is more robust.
            }
            this.btnOK.Image = GlobalInstances.ApplyBtnIcon;
            this.btnCancel.Image = GlobalInstances.CancelBtnIcon;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            PassHash = Hasher.HashString(txtPassphrase.Text);
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
