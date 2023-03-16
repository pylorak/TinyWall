using pylorak.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;

namespace pylorak.TinyWall
{
    internal partial class ApplicationExceptionForm : Form
    {
        private List<FirewallExceptionV3> _tmpExceptionSettings = new();

        internal List<FirewallExceptionV3> ExceptionSettings => _tmpExceptionSettings;

        internal ApplicationExceptionForm(FirewallExceptionV3 fwex)
        {
            InitializeComponent();
            Utils.SetRightToLeft(this);

            try
            {
                Type type = transparentLabel1.GetType();
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                MethodInfo method = type.GetMethod("SetStyle", flags);

                if (method != null)
                {
                    object[] param = { ControlStyles.SupportsTransparentBackColor, true };
                    method.Invoke(transparentLabel1, param);
                }
            }
            catch
            {
                // Don't do anything, we are running in a trusted context.
            }

            this.Icon = Resources.Icons.firewall;
            this.btnOK.Image = GlobalInstances.ApplyBtnIcon;
            this.btnCancel.Image = GlobalInstances.CancelBtnIcon;

            this._tmpExceptionSettings.Add(fwex);

            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Width = this.Width;
            panel2.Location = new System.Drawing.Point(0, panel1.Height);
            panel2.Width = this.Width;

            cmbTimer.SuspendLayout();
            var timerTexts = new Dictionary<AppExceptionTimer, KeyValuePair<string, AppExceptionTimer>>
            {
                {
                    AppExceptionTimer.Permanent,
                    new KeyValuePair<string, AppExceptionTimer>(Resources.Messages.Permanent, AppExceptionTimer.Permanent)
                },
                {
                    AppExceptionTimer.Until_Reboot,
                    new KeyValuePair<string, AppExceptionTimer>(Resources.Messages.UntilReboot, AppExceptionTimer.Until_Reboot)
                },
                {
                    AppExceptionTimer.For_5_Minutes,
                    new KeyValuePair<string, AppExceptionTimer>(string.Format(CultureInfo.CurrentCulture, Resources.Messages.XMinutes, 5), AppExceptionTimer.For_5_Minutes)
                },
                {
                    AppExceptionTimer.For_30_Minutes,
                    new KeyValuePair<string, AppExceptionTimer>(string.Format(CultureInfo.CurrentCulture, Resources.Messages.XMinutes, 30), AppExceptionTimer.For_30_Minutes)
                },
                {
                    AppExceptionTimer.For_1_Hour,
                    new KeyValuePair<string, AppExceptionTimer>(string.Format(CultureInfo.CurrentCulture, Resources.Messages.XHour, 1), AppExceptionTimer.For_1_Hour)
                },
                {
                    AppExceptionTimer.For_4_Hours,
                    new KeyValuePair<string, AppExceptionTimer>(string.Format(CultureInfo.CurrentCulture, Resources.Messages.XHours, 4), AppExceptionTimer.For_4_Hours)
                },
                {
                    AppExceptionTimer.For_9_Hours,
                    new KeyValuePair<string, AppExceptionTimer>(string.Format(CultureInfo.CurrentCulture, Resources.Messages.XHours, 9), AppExceptionTimer.For_9_Hours)
                },
                {
                    AppExceptionTimer.For_24_Hours,
                    new KeyValuePair<string, AppExceptionTimer>(string.Format(CultureInfo.CurrentCulture, Resources.Messages.XHours, 24), AppExceptionTimer.For_24_Hours)
                }
            };

            foreach (AppExceptionTimer timerVal in Enum.GetValues(typeof(AppExceptionTimer)))
            {
                if (timerVal != AppExceptionTimer.Invalid)
                    cmbTimer.Items.Add(timerTexts[timerVal]);
            }
            cmbTimer.DisplayMember = "Key";
            cmbTimer.ValueMember = "Value";
            cmbTimer.ResumeLayout(true);
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
                if (((KeyValuePair<string, AppExceptionTimer>)cmbTimer.Items[i]).Value == _tmpExceptionSettings[0].Timer)
                {
                    cmbTimer.SelectedIndex = i;
                    break;
                }
            }

            var exeSubj = _tmpExceptionSettings[0].Subject as ExecutableSubject;
            var srvSubj = _tmpExceptionSettings[0].Subject as ServiceSubject;
            var uwpSubj = _tmpExceptionSettings[0].Subject as AppContainerSubject;

            // Update top colored banner
            bool hasSignature = false;
            bool validSignature = false;
            if (exeSubj != null)
            {
                hasSignature = exeSubj.IsSigned;
                validSignature = exeSubj.CertValid;
            }
            else if (uwpSubj != null)
            {
                UwpPackage.Package? package = UwpPackage.FindPackageDetails(uwpSubj.Sid);
                if (package.HasValue && (package.Value.Tampered != UwpPackage.TamperedState.Unknown))
                {
                    hasSignature = true;
                    validSignature = (package.Value.Tampered == UwpPackage.TamperedState.No);
                }
            }

            if (hasSignature && validSignature)
            {
                // Recognized app
                panel1.BackgroundImage = Resources.Icons.green_banner;
                transparentLabel1.Text = string.Format(CultureInfo.InvariantCulture, Resources.Messages.RecognizedApplication, _tmpExceptionSettings[0].Subject.ToString());
            }
            else if (hasSignature && !validSignature)
            {
                // Recognized, but compromised app
                panel1.BackgroundImage = Resources.Icons.red_banner;
                transparentLabel1.Text = string.Format(CultureInfo.InvariantCulture, Resources.Messages.CompromisedApplication, _tmpExceptionSettings[0].Subject.ToString());
            }
            else
            {
                // Unknown app
                panel1.BackgroundImage = Resources.Icons.blue_banner;
                transparentLabel1.Text = Resources.Messages.UnknownApplication;
            }

            Utils.CenterControlInParent(transparentLabel1);

            // Update subject fields
            switch (_tmpExceptionSettings[0].Subject.SubjectType)
            {
                case SubjectType.Global:
                    listBoxAppPath.Items.Add(Resources.Messages.AllApplications);
                    txtSrvName.Text = Resources.Messages.SubjectTypeGlobal;
                    break;
                case SubjectType.Executable:
                    listBoxAppPath.Items.Add(exeSubj!.ExecutablePath);
                    txtSrvName.Text = Resources.Messages.SubjectTypeExecutable;
                    break;
                case SubjectType.Service:
                    listBoxAppPath.Items.Add($@"{srvSubj!.ServiceName} ({srvSubj.ExecutablePath})");
                    txtSrvName.Text = Resources.Messages.SubjectTypeService;
                    break;
                case SubjectType.AppContainer:
                    listBoxAppPath.Items.Add(uwpSubj!.DisplayName);
                    txtSrvName.Text = Resources.Messages.SubjectTypeUwpApp;
                    break;
                case SubjectType.Invalid:
                default:
                    throw new NotImplementedException();
            }

            // Update rule/policy fields

            chkInheritToChildren.Checked = _tmpExceptionSettings[0].ChildProcessesInherit;

            switch (_tmpExceptionSettings[0].Policy.PolicyType)
            {
                case PolicyType.HardBlock:
                    radBlock.Checked = true;
                    radRestriction_CheckedChanged(this, EventArgs.Empty);
                    break;
                case PolicyType.RuleList:
                    radBlock.Enabled = false;
                    radUnrestricted.Enabled = false;
                    radTcpUdpUnrestricted.Enabled = false;
                    radTcpUdpOut.Enabled = false;
                    radOnlySpecifiedPorts.Enabled = false;
                    chkRestrictToLocalNetwork.Enabled = false;
                    chkRestrictToLocalNetwork.Checked = false;
                    break;
                case PolicyType.TcpUdpOnly:
                    TcpUdpPolicy pol = (TcpUdpPolicy)_tmpExceptionSettings[0].Policy;
                    if (
                        string.Equals(pol.AllowedLocalTcpListenerPorts, "*")
                        && string.Equals(pol.AllowedLocalUdpListenerPorts, "*")
                        && string.Equals(pol.AllowedRemoteTcpConnectPorts, "*")
                        && string.Equals(pol.AllowedRemoteUdpConnectPorts, "*")
                    )
                    {
                        radTcpUdpUnrestricted.Checked = true;
                    }
                    else if (
                        string.Equals(pol.AllowedRemoteTcpConnectPorts, "*")
                        && string.Equals(pol.AllowedRemoteUdpConnectPorts, "*")
                        )
                    {
                        radTcpUdpOut.Checked = true;
                    }
                    else
                    {
                        radOnlySpecifiedPorts.Checked = true;
                    }

                    radRestriction_CheckedChanged(this, EventArgs.Empty);
                    chkRestrictToLocalNetwork.Checked = pol.LocalNetworkOnly;
                    txtOutboundPortTCP.Text = (pol.AllowedRemoteTcpConnectPorts is null) ? string.Empty : pol.AllowedRemoteTcpConnectPorts.Replace(",", ", ");
                    txtOutboundPortUDP.Text = (pol.AllowedRemoteUdpConnectPorts is null) ? string.Empty : pol.AllowedRemoteUdpConnectPorts.Replace(",", ", ");
                    txtListenPortTCP.Text = (pol.AllowedLocalTcpListenerPorts is null) ? string.Empty : pol.AllowedLocalTcpListenerPorts.Replace(",", ", ");
                    txtListenPortUDP.Text = (pol.AllowedLocalUdpListenerPorts is null) ? string.Empty : pol.AllowedLocalUdpListenerPorts.Replace(",", ", ");
                    break;
                case PolicyType.Unrestricted:
                    UnrestrictedPolicy upol = (UnrestrictedPolicy)_tmpExceptionSettings[0].Policy;
                    radUnrestricted.Checked = true;
                    radRestriction_CheckedChanged(this, EventArgs.Empty);
                    chkRestrictToLocalNetwork.Checked = upol.LocalNetworkOnly;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static string CleanupPortsList(string str)
        {
            string res = str;
            res = res.Replace(" ", string.Empty);
            res = res.Replace(';', ',');

            // Remove empty elements
            while (res.Contains(",,"))
                res = res.Replace(",,", ",");

            // Terminate early if nothing left
            if (string.IsNullOrEmpty(res))
                return string.Empty;

            // Check validity
            string[] elems = res.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            res = string.Empty;
            foreach (var e in elems)
            {
                bool isRange = (-1 != e.IndexOf('-'));
                if (isRange)
                {
                    string[] minmax = e.Split('-');
                    ushort x = ushort.Parse(minmax[0], CultureInfo.InvariantCulture);
                    ushort y = ushort.Parse(minmax[1], CultureInfo.InvariantCulture);
                    ushort min = Math.Min(x, y);
                    ushort max = Math.Max(x, y);
                    res = $"{res},{min:D}-{max:D}";
                }
                else
                {
                    if (e.Equals("*"))
                        // If we have a wildcard, all other list elements are redundant
                        return "*";

                    ushort x = ushort.Parse(e, CultureInfo.InvariantCulture);
                    res = $"{res},{x:D}";
                }
            }

            // Now we have a ',' at the very start. Remove it.
            res = res.Remove(0, 1);

            return res;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            _tmpExceptionSettings[0].ChildProcessesInherit = chkInheritToChildren.Checked;

            if (radBlock.Checked)
            {
                _tmpExceptionSettings[0].Policy = HardBlockPolicy.Instance;
            }
            else if (radOnlySpecifiedPorts.Checked || radTcpUdpOut.Checked || radTcpUdpUnrestricted.Checked)
            {
                var pol = new TcpUdpPolicy();

                try
                {
                    pol.LocalNetworkOnly = chkRestrictToLocalNetwork.Checked;
                    pol.AllowedRemoteTcpConnectPorts = CleanupPortsList(txtOutboundPortTCP.Text);
                    pol.AllowedRemoteUdpConnectPorts = CleanupPortsList(txtOutboundPortUDP.Text);
                    pol.AllowedLocalTcpListenerPorts = CleanupPortsList(txtListenPortTCP.Text);
                    pol.AllowedLocalUdpListenerPorts = CleanupPortsList(txtListenPortUDP.Text);
                    _tmpExceptionSettings[0].Policy = pol;
                }
                catch
                {
                    Utils.ShowMessageBox(
                        Resources.Messages.PortListInvalid,
                        Resources.Messages.TinyWall,
                        Microsoft.Samples.TaskDialogCommonButtons.Ok,
                        Microsoft.Samples.TaskDialogIcon.Warning,
                        this);

                    return;
                }
            }
            else if (radUnrestricted.Checked)
            {
                var pol = new UnrestrictedPolicy();
                pol.LocalNetworkOnly = chkRestrictToLocalNetwork.Checked;
                _tmpExceptionSettings[0].Policy = pol;
            }

            this._tmpExceptionSettings[0].CreationDate = DateTime.Now;

            this.DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void btnProcess_Click(object sender, EventArgs e)
        {
            var procList = new List<ProcessInfo>();
            using (var pf = new ProcessesForm(false))
            {
                if (pf.ShowDialog(this) == DialogResult.Cancel)
                    return;

                procList.AddRange(pf.Selection);
            }
            if (procList.Count == 0) return;

            ExceptionSubject subject;
            if (procList[0].Package.HasValue)
                subject = new AppContainerSubject(procList[0].Package!.Value);
            else
                subject = new ExecutableSubject(procList[0].Path);

            ReinitFormFromSubject(subject);
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog(this) != DialogResult.OK)
                return;

            ReinitFormFromSubject(new ExecutableSubject(PathMapper.Instance.ConvertPathIgnoreErrors(ofd.FileName, PathFormat.Win32)));
        }

        private void btnChooseService_Click(object sender, EventArgs e)
        {
            ServiceSubject? subject = ServicesForm.ChooseService(this);
            if (subject == null) return;

            ReinitFormFromSubject(subject);
        }

        private void btnSelectUwpApp_Click(object sender, EventArgs e)
        {
            var packageList = UwpPackagesForm.ChoosePackage(this, false);
            if (packageList.Count == 0) return;

            ReinitFormFromSubject(new AppContainerSubject(packageList[0]));
        }

        private void ReinitFormFromSubject(ExceptionSubject subject)
        {
            List<FirewallExceptionV3> exceptions = GlobalInstances.AppDatabase.GetExceptionsForApp(subject, true, out _);

            if (exceptions.Count == 0)
                return;

            _tmpExceptionSettings = exceptions;

            UpdateUI();

            if (_tmpExceptionSettings.Count > 1)
                // Multiple known files, just accept them as is
                this.DialogResult = DialogResult.OK;
        }

        private void listBoxAppPath_SizeChanged(object sender, EventArgs e)
        {

        }

        private void txtSrvName_TextChanged(object sender, EventArgs e)
        {
        }

        private void cmbTimer_SelectedIndexChanged(object sender, EventArgs e)
        {
            _tmpExceptionSettings[0].Timer = ((KeyValuePair<string, AppExceptionTimer>)cmbTimer.SelectedItem).Value;
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
                chkRestrictToLocalNetwork.Enabled = false;
                chkRestrictToLocalNetwork.Checked = false;
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
                chkRestrictToLocalNetwork.Enabled = true;
            }
            else if (radTcpUdpOut.Checked)
            {
                panel3.Enabled = true;
                txtListenPortTCP.Text = string.Empty;
                txtListenPortUDP.Text = string.Empty;
                txtOutboundPortTCP.Text = @"*";
                txtOutboundPortUDP.Text = @"*";
                txtOutboundPortTCP.Enabled = false;
                txtOutboundPortUDP.Enabled = false;
                label7.Enabled = false;
                label8.Enabled = false;
                chkRestrictToLocalNetwork.Enabled = true;
            }
            else if (radTcpUdpUnrestricted.Checked)
            {
                panel3.Enabled = false;
                txtListenPortTCP.Text = @"*";
                txtListenPortUDP.Text = @"*";
                txtOutboundPortTCP.Text = @"*";
                txtOutboundPortUDP.Text = @"*";
                chkRestrictToLocalNetwork.Enabled = true;
            }
            else if (radUnrestricted.Checked)
            {
                panel3.Enabled = false;
                txtListenPortTCP.Text = @"*";
                txtListenPortUDP.Text = @"*";
                txtOutboundPortTCP.Text = @"*";
                txtOutboundPortUDP.Text = @"*";
                chkRestrictToLocalNetwork.Enabled = true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
