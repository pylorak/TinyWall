using DarkModeForms;
using System;
using System.IO;
using System.Windows.Forms;

namespace pylorak.TinyWall
{
    internal partial class WildcardPatternForm : Form
    {
        private readonly DarkModeCS? DarkMode;

        private bool? SignatureCheckCache;

        internal string? ResultPattern { get; private set; }

        internal WildcardPatternForm(string executablePath, string? currentPattern)
        {
            InitializeComponent();
            Utils.SetRightToLeft(this);
            if (Utils.IsDarkModeActive(ActiveConfig.Controller))
                this.DarkMode = new(this, false) { ColorMode = DarkModeCS.DisplayMode.DarkMode };

            this.Icon = Resources.Icons.firewall;
            this.btnOK.Image = GlobalInstances.ApplyBtnIcon;
            this.btnCancel.Image = GlobalInstances.CancelBtnIcon;

            txtOriginalPath.Text = executablePath;
            txtPattern.Text = currentPattern ?? executablePath;
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
                WildcardValidation.Success => throw new InvalidOperationException("Unreachable condition, listed for exhaustiveness.")
            };
#pragma warning restore CS8524 // The switch expression does not handle unnamed enum values.

            MessageBox.Show(
                this,
                message,
                Resources.Messages.TinyWall,
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation);
        }

        private bool ValidateInputs(string pattern)
        {
            var validationResult = WildcardPathMatcher.IsPatternSyntaxValid(pattern);
            if (WildcardValidation.Success != validationResult)
            {
                DisplayValidationMsgBox(validationResult);
                return false;
            }

            validationResult = WildcardPathMatcher.CheckPatternWithFile(pattern, txtOriginalPath.Text, ref SignatureCheckCache);
            if (WildcardValidation.Success != validationResult)
            {
                DisplayValidationMsgBox(validationResult);
                return false;
            }

            return true;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Enabled = false;

            var pattern = txtPattern.Text.Trim().Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (!ValidateInputs(pattern))
            {
                this.Enabled = true;
                txtPattern.Focus();
                return;
            }

            ResultPattern = pattern;
            DialogResult = DialogResult.OK;
        }

        private void WildcardPatternForm_Shown(object sender, EventArgs e)
        {
            txtPattern.SelectAll();
            txtPattern.Focus();
        }
    }
}
