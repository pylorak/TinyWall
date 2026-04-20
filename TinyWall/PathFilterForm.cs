using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace pylorak.TinyWall
{
    public partial class PathFilterForm : Form
    {
        public string? ResultFilter { get; private set; }
        public PathFilterForm(string ExecutablePath, string CustomPathFilter)
        {
            InitializeComponent();
            this.txtFullPath.Text = ExecutablePath;
            if (string.IsNullOrEmpty(CustomPathFilter))
            {
                this.txtFilter.Text = ExecutablePath;
            }
            else
            {
                this.txtFilter.Text = CustomPathFilter;
            }
        }

        private void btnSet_Click(object sender, EventArgs e)
        {
            string rawInput = txtFilter.Text?.Trim() ?? string.Empty;

            // 1. New Validation: Must contain at least one wildcard character
            bool hasWildcard = rawInput.Contains('*') || rawInput.Contains('?');

            // 2. Convert to Regex for path matching
            string regexPattern = Utils.WildcardToRegex(rawInput);

            // 3. Perform all checks
            if (!string.IsNullOrEmpty(rawInput) &&
                hasWildcard &&
                Regex.IsMatch(this.txtFullPath.Text, regexPattern, RegexOptions.IgnoreCase))
            {
                ResultFilter = rawInput;
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                // Determine which error message to show
                string errorKey = !hasWildcard ? "MsgMissingWildcardSymbol" : "MsgInvalidWildcard";

                var res = new System.ComponentModel.ComponentResourceManager(typeof(PathFilterForm));
                string message = res.GetString(errorKey) ?? "Invalid input.";
                string title = res.GetString("TitleValidationError") ?? "Error";

                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFilter.Focus();
            }
        }
    }
}
