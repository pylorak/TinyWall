using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using PKSoft.WindowsFirewall;

namespace PKSoft
{
    internal partial class ApplicationExceptionForm : Form
    {
        private FirewallException TmpExceptionSettings;
        private string AppName = string.Empty;

        internal FirewallException ExceptionSettings
        {
            get { return TmpExceptionSettings; }
        }

        internal ApplicationExceptionForm(FirewallException AppEx)
        {
            InitializeComponent();
            this.Icon = Resources.Icons.firewall;

            this.TmpExceptionSettings = AppEx;

            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Width = this.Width;
            panel2.Location = new System.Drawing.Point(0, panel1.Height);
            panel2.Width = this.Width;

            cmbTimer.SuspendLayout();
            Dictionary<AppExceptionTimer, KeyValuePair<string, AppExceptionTimer>> timerTexts = new Dictionary<AppExceptionTimer, KeyValuePair<string, AppExceptionTimer>>();
            timerTexts.Add(AppExceptionTimer.Permanent,
                new KeyValuePair<string, AppExceptionTimer>(PKSoft.Resources.Messages.Permanent, AppExceptionTimer.Permanent)
                );
            timerTexts.Add(AppExceptionTimer.Until_Reboot,
                new KeyValuePair<string, AppExceptionTimer>(PKSoft.Resources.Messages.UntilReboot, AppExceptionTimer.Until_Reboot)
                );
            timerTexts.Add(AppExceptionTimer.For_5_Minutes,
                new KeyValuePair<string, AppExceptionTimer>(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.XMinutes, 5), AppExceptionTimer.For_5_Minutes)
                );
            timerTexts.Add(AppExceptionTimer.For_30_Minutes,
                new KeyValuePair<string, AppExceptionTimer>(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.XMinutes, 30), AppExceptionTimer.For_30_Minutes)
                );
            timerTexts.Add(AppExceptionTimer.For_1_Hour, 
                new KeyValuePair<string, AppExceptionTimer>(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.XHour, 1), AppExceptionTimer.For_1_Hour)
                );
            timerTexts.Add(AppExceptionTimer.For_4_Hours,
                new KeyValuePair<string, AppExceptionTimer>(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.XHours, 4), AppExceptionTimer.For_4_Hours)
                );
            timerTexts.Add(AppExceptionTimer.For_9_Hours,
                new KeyValuePair<string, AppExceptionTimer>(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.XHours, 9), AppExceptionTimer.For_9_Hours)
                );
            timerTexts.Add(AppExceptionTimer.For_24_Hours,
                new KeyValuePair<string, AppExceptionTimer>(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.XHours, 24), AppExceptionTimer.For_24_Hours)
                );

            foreach (AppExceptionTimer timerVal in Enum.GetValues(typeof(AppExceptionTimer)))
            {
                if (timerVal != AppExceptionTimer.Invalid)
                    cmbTimer.Items.Add(timerTexts[timerVal]);
            }
            cmbTimer.DisplayMember = "Key";
            cmbTimer.ValueMember = "Value";
            cmbTimer.ResumeLayout(true);

            if (!TmpExceptionSettings.Recognized.HasValue)
                AppName = TmpExceptionSettings.TryRecognizeApp(true);
        }

        private void ApplicationExceptionForm_Load(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            // Display timer
            for (int i = 0; i < cmbTimer.Items.Count; ++i)
            {
                if (((KeyValuePair<string, AppExceptionTimer>)cmbTimer.Items[i]).Value == TmpExceptionSettings.Timer)
                {
                    cmbTimer.SelectedIndex = i;
                    break;
                }
            }

            if (TmpExceptionSettings.Recognized.Value)
            {
                // Recognized app
                panel1.BackgroundImage = Resources.Icons.green_banner;
                transparentLabel1.Text = string.Format(CultureInfo.InvariantCulture, PKSoft.Resources.Messages.RecognizedApplication, AppName);
            }
            else
            {
                // Unknown app
                panel1.BackgroundImage = Resources.Icons.blue_banner;
                transparentLabel1.Text = PKSoft.Resources.Messages.UnknownApplication;
            }

            Utils.CenterControlInParent(transparentLabel1);
            txtAppPath.Text = TmpExceptionSettings.ExecutablePath;
            txtSrvName.Text = TmpExceptionSettings.ServiceName;
            chkRestrictToLocalNetwork.Checked = TmpExceptionSettings.LocalNetworkOnly;

            // Select the right radio button
            if (TmpExceptionSettings.AlwaysBlockTraffic)
            {
                radBlock.Checked = true;
            }
            else if (TmpExceptionSettings.UnrestricedTraffic)
            {
                radUnrestricted.Checked = true;
            }
            else if (
                string.Equals(TmpExceptionSettings.OpenPortListenLocalTCP, "*")
                && string.Equals(TmpExceptionSettings.OpenPortListenLocalUDP, "*")
                && string.Equals(TmpExceptionSettings.OpenPortOutboundRemoteTCP, "*")
                && string.Equals(TmpExceptionSettings.OpenPortOutboundRemoteUDP, "*")
                )
            {
                radTcpUdpUnrestricted.Checked = true;
            }
            else if (
                string.Equals(TmpExceptionSettings.OpenPortOutboundRemoteTCP, "*")
                && string.Equals(TmpExceptionSettings.OpenPortOutboundRemoteUDP, "*")
                )
            {
                radTcpUdpOut.Checked = true;
            }
            else
            {
                radOnlySpecifiedPorts.Checked = true;
            }
            radRestriction_CheckedChanged(null, null);

            // Display ports list
            txtOutboundPortTCP.Text = TmpExceptionSettings.OpenPortOutboundRemoteTCP.Replace(",", ", ");
            txtOutboundPortUDP.Text = TmpExceptionSettings.OpenPortOutboundRemoteUDP.Replace(",", ", ");
            txtListenPortTCP.Text = TmpExceptionSettings.OpenPortListenLocalTCP.Replace(",", ", ");
            txtListenPortUDP.Text = TmpExceptionSettings.OpenPortListenLocalUDP.Replace(",", ", ");

            UpdateOKButtonEnabled();
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
        
        private void UpdateOKButtonEnabled()
        {
            btnOK.Enabled = System.IO.File.Exists(TmpExceptionSettings.ExecutablePath);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Validate port lists
            try
            {
                TmpExceptionSettings.OpenPortOutboundRemoteTCP = CleanupPortsList(txtOutboundPortTCP.Text);
                TmpExceptionSettings.OpenPortOutboundRemoteUDP = CleanupPortsList(txtOutboundPortUDP.Text);
                TmpExceptionSettings.OpenPortListenLocalTCP = CleanupPortsList(txtListenPortTCP.Text);
                TmpExceptionSettings.OpenPortListenLocalUDP = CleanupPortsList(txtListenPortUDP.Text);
            }
            catch
            {
                Utils.ShowMessageBox(this,
                    PKSoft.Resources.Messages.PortListInvalid,
                    PKSoft.Resources.Messages.TinyWall,
                    Microsoft.Samples.TaskDialogCommonButtons.Ok,
                    Microsoft.Samples.TaskDialogIcon.Warning);

                return;
            }

            this.TmpExceptionSettings.LocalNetworkOnly = chkRestrictToLocalNetwork.Checked;
            this.TmpExceptionSettings.CreationDate = DateTime.Now;
            this.TmpExceptionSettings.AlwaysBlockTraffic = radBlock.Checked;
            this.TmpExceptionSettings.UnrestricedTraffic = radUnrestricted.Checked;
            

            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void btnProcess_Click(object sender, EventArgs e)
        {
            FirewallException proc = ProcessesForm.ChooseProcess(this);
            if (proc == null) return;

            TmpExceptionSettings = proc;
            AppName = TmpExceptionSettings.TryRecognizeApp(true);
            UpdateUI();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog(this) != System.Windows.Forms.DialogResult.OK)
                return;

            TmpExceptionSettings.ExecutablePath = ofd.FileName;
            TmpExceptionSettings.ServiceName = string.Empty;
            AppName = TmpExceptionSettings.TryRecognizeApp(true);
            UpdateUI();
        }

        private void btnChooseService_Click(object sender, EventArgs e)
        {
            FirewallException serv = ServicesForm.ChooseService(this);
            if (serv == null) return;

            TmpExceptionSettings = serv;
            AppName = TmpExceptionSettings.TryRecognizeApp(true);
            UpdateUI(); 
        }

        private void txtAppPath_TextChanged(object sender, EventArgs e)
        {
            UpdateOKButtonEnabled();
        }

        private void txtSrvName_TextChanged(object sender, EventArgs e)
        {
            UpdateOKButtonEnabled();
        }

        private void cmbTimer_SelectedIndexChanged(object sender, EventArgs e)
        {
            TmpExceptionSettings.Timer = ((KeyValuePair<string, AppExceptionTimer>)cmbTimer.SelectedItem).Value;
        }

        private void radRestriction_CheckedChanged(object sender, EventArgs e)
        {
            if (radBlock.Checked)
            {
                panel3.Enabled = false;
                txtListenPortTCP.Text = string.Empty;
                txtListenPortUDP.Text = string.Empty;
                txtOutboundPortTCP.Text = string.Empty;
                txtOutboundPortUDP.Text = string.Empty;
            }
            else if (radOnlySpecifiedPorts.Checked)
            {
                panel3.Enabled = true;
                txtListenPortTCP.Text = string.Empty;
                txtListenPortUDP.Text = string.Empty;
                txtOutboundPortTCP.Text = string.Empty;
                txtOutboundPortUDP.Text = string.Empty;
                txtOutboundPortTCP.Enabled = true;
                txtOutboundPortUDP.Enabled = true;
                label7.Enabled = true;
                label8.Enabled = true;
            }
            else if (radTcpUdpOut.Checked)
            {
                panel3.Enabled = true;
                txtListenPortTCP.Text = string.Empty;
                txtListenPortUDP.Text = string.Empty;
                txtOutboundPortTCP.Text = "*";
                txtOutboundPortUDP.Text = "*";
                txtOutboundPortTCP.Enabled = false;
                txtOutboundPortUDP.Enabled = false;
                label7.Enabled = false;
                label8.Enabled = false;
            }
            else if (radTcpUdpUnrestricted.Checked)
            {
                panel3.Enabled = false;
                txtListenPortTCP.Text = "*";
                txtListenPortUDP.Text = "*";
                txtOutboundPortTCP.Text = "*";
                txtOutboundPortUDP.Text = "*";
            }
            else if (radUnrestricted.Checked)
            {
                panel3.Enabled = false;
                txtListenPortTCP.Text = "*";
                txtListenPortUDP.Text = "*";
                txtOutboundPortTCP.Text = "*";
                txtOutboundPortUDP.Text = "*";
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
