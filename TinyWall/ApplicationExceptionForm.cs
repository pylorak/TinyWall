using pylorak.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace pylorak.TinyWall
{
    internal partial class ApplicationExceptionForm : Form
    {
        private static readonly char[] PortListSeparators = { ',' };

        internal List<FirewallExceptionV3> ExceptionSettings { get; } = new();

        internal ApplicationExceptionForm(FirewallExceptionV3 firewallExceptionV3)
        {
            ApplicationExceptionFormInitialise(firewallExceptionV3);
        }

        internal ApplicationExceptionForm()
        {
            ApplicationExceptionFormInitialise(null);
        }

        private void ApplicationExceptionFormInitialise(FirewallExceptionV3? firewallExceptionV3)
        {
            InitializeComponent();
            Utils.SetRightToLeft(this);

            try
            {
                var type = transparentLabel1.GetType();
                const BindingFlags FLAGS = BindingFlags.NonPublic | BindingFlags.Instance;
                MethodInfo? method = type.GetMethod("SetStyle", FLAGS);

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

            Icon = Resources.Icons.firewall;
            btnOK.Image = GlobalInstances.ApplyBtnIcon;
            btnCancel.Image = GlobalInstances.CancelBtnIcon;

            if (firewallExceptionV3 is not null)
            {
                ExceptionSettings.Add(firewallExceptionV3);
                UpdateUi();
            }

            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Width = Width;
            panel2.Location = new System.Drawing.Point(0, panel1.Height);
            panel2.Width = Width;

            cmbTimer.SuspendLayout();
            var timerTexts = new Dictionary<AppExceptionTimer, KeyValuePair<string, AppExceptionTimer>>
            {
                {
                    AppExceptionTimer.Permanent,
                    new KeyValuePair<string, AppExceptionTimer>(Resources.Messages.Permanent, AppExceptionTimer.Permanent)
                },
                {
                    AppExceptionTimer.UNTIL_REBOOT,
                    new KeyValuePair<string, AppExceptionTimer>(Resources.Messages.UntilReboot, AppExceptionTimer.UNTIL_REBOOT)
                },
                {
                    AppExceptionTimer.FOR_5_MINUTES,
                    new KeyValuePair<string, AppExceptionTimer>(
                        string.Format(CultureInfo.CurrentCulture, Resources.Messages.XMinutes, 5),
                        AppExceptionTimer.FOR_5_MINUTES)
                },
                {
                    AppExceptionTimer.FOR_30_MINUTES,
                    new KeyValuePair<string, AppExceptionTimer>(
                        string.Format(CultureInfo.CurrentCulture, Resources.Messages.XMinutes, 30),
                        AppExceptionTimer.FOR_30_MINUTES)
                },
                {
                    AppExceptionTimer.FOR_1_HOUR,
                    new KeyValuePair<string, AppExceptionTimer>(
                        string.Format(CultureInfo.CurrentCulture, Resources.Messages.XHour, 1), AppExceptionTimer.FOR_1_HOUR)
                },
                {
                    AppExceptionTimer.FOR_4_HOURS,
                    new KeyValuePair<string, AppExceptionTimer>(
                        string.Format(CultureInfo.CurrentCulture, Resources.Messages.XHours, 4), AppExceptionTimer.FOR_4_HOURS)
                },
                {
                    AppExceptionTimer.FOR_9_HOURS,
                    new KeyValuePair<string, AppExceptionTimer>(
                        string.Format(CultureInfo.CurrentCulture, Resources.Messages.XHours, 9), AppExceptionTimer.FOR_9_HOURS)
                },
                {
                    AppExceptionTimer.FOR_24_HOURS,
                    new KeyValuePair<string, AppExceptionTimer>(
                        string.Format(CultureInfo.CurrentCulture, Resources.Messages.XHours, 24),
                        AppExceptionTimer.FOR_24_HOURS)
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
        }

        private void UpdateUi()
        {
            var index = ExceptionSettings.Count - 1;

            // Display timer
            for (var i = 0; i < cmbTimer.Items.Count; ++i)
            {
                if (((KeyValuePair<string, AppExceptionTimer>)cmbTimer.Items[i]).Value != ExceptionSettings[index].Timer) continue;

                cmbTimer.SelectedIndex = i;
                break;
            }

            var exeSubj = ExceptionSettings[index].Subject as ExecutableSubject;
            var srvSubj = ExceptionSettings[index].Subject as ServiceSubject;
            var uwpSubj = ExceptionSettings[index].Subject as AppContainerSubject;

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
                var packageList = new UwpPackageList();
                var package = packageList.FindPackage(uwpSubj.Sid);

                if (package.HasValue && (package.Value.Tampered != UwpPackageList.TamperedState.Unknown))
                {
                    hasSignature = true;
                    validSignature = (package.Value.Tampered == UwpPackageList.TamperedState.No);
                }
            }

            switch (hasSignature)
            {
                case true when validSignature:
                    // Recognised app
                    panel1.BackgroundImage = Resources.Icons.green_banner;
                    transparentLabel1.Text = string.Format(CultureInfo.InvariantCulture, Resources.Messages.RecognizedApplication, ExceptionSettings[index].Subject.ToString());
                    break;
                case true when !validSignature:
                    // Recognised, but compromised app
                    panel1.BackgroundImage = Resources.Icons.red_banner;
                    transparentLabel1.Text = string.Format(CultureInfo.InvariantCulture, Resources.Messages.CompromisedApplication, ExceptionSettings[index].Subject.ToString());
                    break;
                default:
                    // Unknown app
                    panel1.BackgroundImage = Resources.Icons.blue_banner;
                    transparentLabel1.Text = Resources.Messages.UnknownApplication;
                    break;
            }

            Utils.CentreControlInParent(transparentLabel1);

            // Update subject fields
            switch (ExceptionSettings[index].Subject.SubjectType)
            {
                case SubjectType.Global:
                    //txtSrvName.Text = Resources.Messages.SubjectTypeGlobal;
                    break;
                case SubjectType.Executable:
                    listViewAppPath.Items.Add(new ListViewItem()
                    {
                        Text = exeSubj!.ExecutablePath,
                        SubItems = { Resources.Messages.SubjectTypeExecutable }
                    });
                    break;
                case SubjectType.Service:
                    listViewAppPath.Items.Add(new ListViewItem()
                    {
                        Text = $@"{srvSubj!.ServiceName} ({srvSubj.ExecutablePath})",
                        SubItems = { Resources.Messages.SubjectTypeService }
                    });
                    break;
                case SubjectType.AppContainer:
                    listViewAppPath.Items.Add(new ListViewItem()
                    {
                        Text = uwpSubj!.DisplayName,
                        SubItems = { Resources.Messages.SubjectTypeUwpApp }
                    });
                    break;
                case SubjectType.Invalid:
                default:
                    throw new NotImplementedException();
            }

            // Update rule/policy fields

            chkInheritToChildren.Checked = ExceptionSettings[index].ChildProcessesInherit;

            switch (ExceptionSettings[index].Policy.PolicyType)
            {
                case PolicyType.HardBlock:
                    radBlock.Checked = true;
                    RadRestriction_CheckedChanged(this, EventArgs.Empty);
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
                    TcpUdpPolicy pol = (TcpUdpPolicy)ExceptionSettings[index].Policy;
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

                    RadRestriction_CheckedChanged(this, EventArgs.Empty);
                    chkRestrictToLocalNetwork.Checked = pol.LocalNetworkOnly;
                    txtOutboundPortTCP.Text = (pol.AllowedRemoteTcpConnectPorts is null) ? string.Empty : pol.AllowedRemoteTcpConnectPorts.Replace(",", ", ");
                    txtOutboundPortUDP.Text = (pol.AllowedRemoteUdpConnectPorts is null) ? string.Empty : pol.AllowedRemoteUdpConnectPorts.Replace(",", ", ");
                    txtListenPortTCP.Text = (pol.AllowedLocalTcpListenerPorts is null) ? string.Empty : pol.AllowedLocalTcpListenerPorts.Replace(",", ", ");
                    txtListenPortUDP.Text = (pol.AllowedLocalUdpListenerPorts is null) ? string.Empty : pol.AllowedLocalUdpListenerPorts.Replace(",", ", ");
                    break;
                case PolicyType.Unrestricted:
                    UnrestrictedPolicy upol = (UnrestrictedPolicy)ExceptionSettings[index].Policy;
                    radUnrestricted.Checked = true;
                    RadRestriction_CheckedChanged(this, EventArgs.Empty);
                    chkRestrictToLocalNetwork.Checked = upol.LocalNetworkOnly;
                    break;
                case PolicyType.Invalid:
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
            if (string.IsNullOrWhiteSpace(res))
                return string.Empty;

            // Check validity
            string[] elems = res.Split(PortListSeparators, StringSplitOptions.RemoveEmptyEntries);

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

        private async void BtnOK_Click(object sender, EventArgs e)
        {
            Parallel.For(0, ExceptionSettings.Count - 1, (i, _) =>
            {
                ExceptionSettings[i].ChildProcessesInherit = chkInheritToChildren.Checked;
            });

            if (radBlock.Checked)
            {
                Parallel.For(0, ExceptionSettings.Count - 1, (i, _) =>
                {
                    ExceptionSettings[i].Policy = HardBlockPolicy.Instance;
                });
            }
            else if (radOnlySpecifiedPorts.Checked || radTcpUdpOut.Checked || radTcpUdpUnrestricted.Checked)
            {
                var pol = new TcpUdpPolicy();

                try
                {
                    pol.LocalNetworkOnly = chkRestrictToLocalNetwork.Checked;
                    pol.AllowedRemoteTcpConnectPorts = await Task.Run(() => CleanupPortsList(txtOutboundPortTCP.Text));
                    pol.AllowedRemoteUdpConnectPorts = await Task.Run(() => CleanupPortsList(txtOutboundPortUDP.Text));
                    pol.AllowedLocalTcpListenerPorts = await Task.Run(() => CleanupPortsList(txtListenPortTCP.Text));
                    pol.AllowedLocalUdpListenerPorts = await Task.Run(() => CleanupPortsList(txtListenPortUDP.Text));

                    Parallel.For(0, ExceptionSettings.Count - 1, (i, _) =>
                    {
                        ExceptionSettings[i].Policy = pol;
                    });
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
                var pol = new UnrestrictedPolicy
                {
                    LocalNetworkOnly = chkRestrictToLocalNetwork.Checked
                };

                Parallel.For(0, ExceptionSettings.Count - 1, (i, _) =>
                {
                    ExceptionSettings[i].Policy = pol;
                });

            }

            var dateTimeNow = DateTime.Now;
            Parallel.For(0, ExceptionSettings.Count - 1, (i, _) =>
            {
                ExceptionSettings[i].CreationDate = dateTimeNow;
            });

            DialogResult = DialogResult.OK;
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void BtnProcess_Click(object sender, EventArgs e)
        {
            var procList = new List<ProcessInfo>();

            using (var pf = new ProcessesForm(false))
            {
                if (pf.ShowDialog(this) == DialogResult.Cancel) return;

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

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            ReinitFormFromSubject(new ExecutableSubject(PathMapper.Instance.ConvertPathIgnoreErrors(ofd.FileName, PathFormat.Win32)));
        }

        private void BtnChooseService_Click(object sender, EventArgs e)
        {
            ServiceSubject? subject = ServicesForm.ChooseService(this);

            if (subject == null) return;

            ReinitFormFromSubject(subject);
        }

        private void BtnSelectUwpApp_Click(object sender, EventArgs e)
        {
            var packageList = UwpPackagesForm.ChoosePackage(this, false);

            if (packageList.Count == 0) return;

            ReinitFormFromSubject(new AppContainerSubject(packageList[0]));
        }

        private void ReinitFormFromSubject(ExceptionSubject subject)
        {
            List<FirewallExceptionV3> exceptions = GlobalInstances.AppDatabase!.GetExceptionsForApp(subject, true, out _);

            if (exceptions.Count == 0 || ExceptionSettings.Exists(e => e.Subject.Equals(exceptions[0].Subject))) return;

            ExceptionSettings.AddRange(exceptions);

            UpdateUi();
        }

        private void CmbTimer_SelectedIndexChanged(object sender, EventArgs e)
        {
            ExceptionSettings[0].Timer = ((KeyValuePair<string, AppExceptionTimer>)cmbTimer.SelectedItem).Value;
        }

        private void RadRestriction_CheckedChanged(object sender, EventArgs e)
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
                OutTCPLabel.Enabled = true;
                OutUDPLabel.Enabled = true;
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
                OutTCPLabel.Enabled = false;
                OutUDPLabel.Enabled = false;
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

        private void BtnRemoveSoftware_Click(object sender, EventArgs e)
        {
            if (listViewAppPath.Items.Count <= 0 || listViewAppPath.SelectedItems.Count <= 0)
            {
                Utils.ShowMessageBox(
                    Resources.Messages.RemoveSoftwareDialogueText,
                    Resources.Messages.RemoveSoftwareDialogueCaption,
                    Microsoft.Samples.TaskDialogCommonButtons.Ok,
                    Microsoft.Samples.TaskDialogIcon.Warning,
                    this);

                return;
            }

            FirewallExceptionV3 firewallExceptionV3 = ExceptionSettings.Find(f => listViewAppPath.SelectedItems[0].Text.Contains(f.ToString()));

            if (firewallExceptionV3 is null) return;

            ExceptionSettings.Remove(firewallExceptionV3);
            listViewAppPath.SelectedItems[0].Remove();
        }
    }
}
