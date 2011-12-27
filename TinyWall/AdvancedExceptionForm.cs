using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using PKSoft.WindowsFirewall;

namespace PKSoft
{
    public partial class AdvancedExceptionForm : Form
    {
        internal AppExceptionSettings TmpAppException;

        public AdvancedExceptionForm(AppExceptionSettings app)
        {
            InitializeComponent();
            this.Icon = Icons.firewall;
            TmpAppException = app;
        }

        private void UpdateUI()
        {
            txtOutboundPortTCP.Text = TmpAppException.OpenPortOutboundRemoteTCP.Replace(",", ", ");
            txtOutboundPortUDP.Text = TmpAppException.OpenPortOutboundRemoteUDP.Replace(",", ", ");
            txtListenPortTCP.Text = TmpAppException.OpenPortListenLocalTCP.Replace(",", ", ");
            txtListenPortUDP.Text = TmpAppException.OpenPortListenLocalUDP.Replace(",", ", ");
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Format and check user input
            try
            {
                TmpAppException.OpenPortOutboundRemoteTCP = CleanupPortsList(txtOutboundPortTCP.Text);
                TmpAppException.OpenPortOutboundRemoteUDP = CleanupPortsList(txtOutboundPortUDP.Text);
                TmpAppException.OpenPortListenLocalTCP = CleanupPortsList(txtListenPortTCP.Text);
                TmpAppException.OpenPortListenLocalUDP = CleanupPortsList(txtListenPortUDP.Text);
            }
            catch
            {
                MessageBox.Show(this, "Format of port list is invalid.", "Invalid input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private static string CleanupPortsList(string str)
        {
            string res = str;
            res = res.Replace(" ", string.Empty);
            res = res.Replace(';', ',');

            // Check validity
            Rule r = new Rule("", "", ProfileType.Private, RuleDirection.In, PacketAction.Allow, Protocol.TCP);
            r.LocalPorts = res;

            return res;
        }

        private void AdvancedExceptionForm_Shown(object sender, EventArgs e)
        {
            UpdateUI();
        }
    }
}
