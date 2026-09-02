using DarkModeForms;
using System;
using System.Windows.Forms;

namespace pylorak.TinyWall
{
    internal partial class PathFilterForm : Form
    {
        private readonly DarkModeCS? DarkMode;

        internal string? ResultFilter { get; private set; }

        internal PathFilterForm(string executablePath, string? currentFilter)
        {
            InitializeComponent();
            Utils.SetRightToLeft(this);
            if (Utils.IsDarkModeActive(ActiveConfig.Controller))
                this.DarkMode = new(this, false) { ColorMode = DarkModeCS.DisplayMode.DarkMode };

            txtOriginalPath.Text = executablePath;
            txtPattern.Text = string.IsNullOrWhiteSpace(currentFilter) ? executablePath : currentFilter;
            txtPattern.SelectAll();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            string pattern = txtPattern.Text.Trim();
            if (WildcardPathMatcher.IsValidFilter(pattern, txtOriginalPath.Text))
            {
                ResultFilter = pattern;
                DialogResult = DialogResult.OK;
                return;
            }

            string message = pattern.IndexOfAny(new[] { '*', '?' }) < 0
                ? Resources.Messages.PathFilterMissingWildcard
                : !WildcardPathMatcher.HasProtectedLiteralPrefix(pattern)
                    ? labelSecurityBoundary.Text
                    : Resources.Messages.PathFilterInvalid;
            MessageBox.Show(
                this,
                message,
                Resources.Messages.PathFilterValidationTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            txtPattern.Focus();
            txtPattern.SelectAll();
        }

        private void btnClearFilter_Click(object sender, EventArgs e)
        {
            ResultFilter = null;
            DialogResult = DialogResult.OK;
        }

    }
}
