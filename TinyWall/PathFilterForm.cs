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
            bool? sigVerifyCache = null;
            if (WildcardPathMatcher.IsValidFilter(pattern, txtOriginalPath.Text, ref sigVerifyCache))
            {
                ResultFilter = pattern;
                DialogResult = DialogResult.OK;
                return;
            }

            // WildcardPathMatcher.IsValidFilter() expands env.vars internally, so when we call
            // Matches() ourself, we must expand manually to keep match results are identical.
            pattern = Environment.ExpandEnvironmentVariables(pattern);

            string message;
            if (pattern.IndexOfAny(new[] { '*', '?' }) < 0)
            {
                message = Resources.Messages.PathFilterMissingWildcard;
            }
            else if (!WildcardPathMatcher.Matches(pattern, txtOriginalPath.Text))
            {
                message = Resources.Messages.PathFilterInvalid;
            }
            else
            {
                message = labelSecurityBoundary.Text;
            }

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
