using DarkModeForms;
using System;
using System.IO;
using System.Windows.Forms;

namespace pylorak.TinyWall
{
    internal partial class WildcardPatternForm : Form
    {
        private readonly DarkModeCS? DarkMode;

        internal string? ResultPattern { get; private set; }

        internal WildcardPatternForm(string executablePath, string? currentPattern)
        {
            InitializeComponent();
            Utils.SetRightToLeft(this);
            if (Utils.IsDarkModeActive(ActiveConfig.Controller))
                this.DarkMode = new(this, false) { ColorMode = DarkModeCS.DisplayMode.DarkMode };

            txtOriginalPath.Text = executablePath;
            txtPattern.Text = string.IsNullOrWhiteSpace(currentPattern) ? executablePath : currentPattern;
            txtPattern.SelectAll();
        }

        private void DisplayValidationMsgBox(WildcardValidation result)
        {
#pragma warning disable CS8524 // The switch expression does not handle unnamed enum values.
            string message = result switch
            {
                WildcardValidation.ErrorDisallowedFolder => Resources.Messages.WildcardPatternDisallowedFolder,
                WildcardValidation.ErrorWildcardedFilename => Resources.Messages.WildcardPatternMustUseExactFilename,
                WildcardValidation.ErrorEmptyParameter
                or WildcardValidation.ErrorMissingWildcards => Resources.Messages.WildcardPatternMissingWildcard,
                WildcardValidation.ErrorFileSignatureFail => Resources.Messages.WildcardPatternSignatureRequired,
                WildcardValidation.ErrorGeneric => Resources.Messages.WildcardPatternValidationFailed,
                WildcardValidation.ErrorPathNotMatched => Resources.Messages.WildcardPatternMustMatchOriginalPath,
                WildcardValidation.ErrorHasRelativeComponents
                or WildcardValidation.ErrorInvalidChars
                or WildcardValidation.ErrorNotFullyQualified => Resources.Messages.WildcardPatternMustBeAbsolutePath,
                WildcardValidation.Success => "This exists for exhaustiveness. This is not a message you should see XD",
            };
#pragma warning restore CS8524 // The switch expression does not handle unnamed enum values.

            MessageBox.Show(
                this,
                message,
                Resources.Messages.WildcardPatternValidationTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            txtPattern.Focus();
            txtPattern.SelectAll();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            bool? sigVerifyCache = null;
            var pattern = txtPattern.Text.Trim().Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            var validationResult = WildcardPathMatcher.IsPatternSyntaxValid(pattern);
            if (WildcardValidation.Success != validationResult)
            {
                DisplayValidationMsgBox(validationResult);
                return;
            }

            validationResult = WildcardPathMatcher.MatchPatternToPath(pattern, txtOriginalPath.Text, ref sigVerifyCache);
            if (WildcardValidation.Success != validationResult)
            {
                DisplayValidationMsgBox(validationResult);
                return;
            }

            ResultPattern = pattern;
            DialogResult = DialogResult.OK;
        }

        private void btnClearPattern_Click(object sender, EventArgs e)
        {
            ResultPattern = null;
            DialogResult = DialogResult.OK;
        }

    }
}
