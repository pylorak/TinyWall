using DarkModeForms;
using System;
using System.IO;
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

        private void DisplayValidationMsgBox(WildcardValidation result)
        {
            // TODO: Make the messages below localizable.
#pragma warning disable CS8524 // The switch expression does not handle unnamed enum values.
            string message = result switch
            {
                WildcardValidation.ErrorDisallowedFolder => "Wildcard filter crosses or points to disallowed folder.",
                WildcardValidation.ErrorEmptyParameter
                or WildcardValidation.ErrorMissingWildcards => Resources.Messages.PathFilterMissingWildcard,
                WildcardValidation.ErrorFileSignatureFail => "File signature required but missing.",
                WildcardValidation.ErrorGeneric => "Invalid wildcard filter specified.",
                WildcardValidation.ErrorPathNotMatched => Resources.Messages.PathFilterMustMatchOriginalPath,
                WildcardValidation.ErrorHasRelativeComponents
                or WildcardValidation.ErrorInvalidChars
                or WildcardValidation.ErrorNotFullyQualified => "Wildcard must specify a valid absolute file path.",
                WildcardValidation.Success => "This is not a message you should see XD",
            };
#pragma warning restore CS8524 // The switch expression does not handle unnamed enum values.

            MessageBox.Show(
                this,
                message,
                Resources.Messages.PathFilterValidationTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            txtPattern.Focus();
            txtPattern.SelectAll();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            bool? sigVerifyCache = null;
            var pattern = txtPattern.Text.Trim().Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            var validationResult = WildcardPathMatcher.IsFilterSyntaxValid(pattern);
            if (WildcardValidation.Success != validationResult)
            {
                DisplayValidationMsgBox(validationResult);
                return;
            }

            validationResult = WildcardPathMatcher.IsValidFilter(pattern, txtOriginalPath.Text, ref sigVerifyCache);
            if (WildcardValidation.Success != validationResult)
            {
                DisplayValidationMsgBox(validationResult);
                return;
            }

            ResultFilter = pattern;
            DialogResult = DialogResult.OK;
        }

        private void btnClearFilter_Click(object sender, EventArgs e)
        {
            ResultFilter = null;
            DialogResult = DialogResult.OK;
        }

    }
}
