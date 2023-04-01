using pylorak.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace pylorak.TinyWall
{
    internal partial class ApplicationExceptionForm : Form
    {
        private readonly List<FirewallExceptionV3> _tmpExceptionSettings = new();

        internal List<FirewallExceptionV3> ExceptionSettings => _tmpExceptionSettings;

        internal ApplicationExceptionForm(FirewallExceptionV3 fwex)
        {
            ApplicationExceptionFormInitialise(fwex);
        }

        internal ApplicationExceptionForm()
        {
            ApplicationExceptionFormInitialise(null);
        }

        private void ApplicationExceptionFormInitialise(FirewallExceptionV3? fwex)
        {
            InitializeComponent();
            Utils.SetRightToLeft(this);

            try
            {
                var type = transparentLabel1.GetType();
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                MethodInfo? method = type.GetMethod("SetStyle", flags);

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

            if (fwex is not null)
            {
                _tmpExceptionSettings.Add(fwex);
                listViewAppPath.Items.Add(new ListViewItem()
                {
                    Text = fwex.Subject.ToString(),
                    SubItems = { fwex.Subject.ToString() }
                });

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
            //UpdateUI();

            //BUG: ??? - Have to add columns in ListView by using code below otherwise they don't appear when using the UI method
            listViewAppPath.Columns.AddRange(new ColumnHeader[]
            {
                new ColumnHeader() { Text = @"Application", Width = 800 },
                new ColumnHeader() { Text = @"Type", Width = 200 }
            });
        }

        private void UpdateUI()
        {
            var index = _tmpExceptionSettings.Count - 1;

            // Display timer
            for (var i = 0; i < cmbTimer.Items.Count; ++i)
            {
                if (((KeyValuePair<string, AppExceptionTimer>)cmbTimer.Items[i]).Value != _tmpExceptionSettings[index].Timer) continue;

                cmbTimer.SelectedIndex = i;
                break;
            }

            var exeSubj = _tmpExceptionSettings[index].Subject as ExecutableSubject;
            var srvSubj = _tmpExceptionSettings[index].Subject as ServiceSubject;
            var uwpSubj = _tmpExceptionSettings[index].Subject as AppContainerSubject;

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

            switch (hasSignature)
            {
                case true when validSignature:
                    // Recognised app
                    panel1.BackgroundImage = Resources.Icons.green_banner;
                    transparentLabel1.Text = string.Format(CultureInfo.InvariantCulture, Resources.Messages.RecognizedApplication, _tmpExceptionSettings[index].Subject.ToString());
                    break;
                case true when !validSignature:
                    // Recognised, but compromised app
                    panel1.BackgroundImage = Resources.Icons.red_banner;
                    transparentLabel1.Text = string.Format(CultureInfo.InvariantCulture, Resources.Messages.CompromisedApplication, _tmpExceptionSettings[index].Subject.ToString());
                    break;
                default:
                    // Unknown app
                    panel1.BackgroundImage = Resources.Icons.blue_banner;
                    transparentLabel1.Text = Resources.Messages.UnknownApplication;
                    break;
            }

            Utils.CentreControlInParent(transparentLabel1);

            // Update subject fields
            switch (_tmpExceptionSettings[index].Subject.SubjectType)
            {
                case SubjectType.Global:
                    //txtSrvName.Text = Resources.Messages.SubjectTypeGlobal;

                    listViewAppPath.Columns.AddRange(new ColumnHeader[]
                    {
                        new ColumnHeader() { Text = @"Application", Width = 100 },
                        new ColumnHeader() { Text = @"Type", Width = 100 }
                    });
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

            chkInheritToChildren.Checked = _tmpExceptionSettings[index].ChildProcessesInherit;

            switch (_tmpExceptionSettings[index].Policy.PolicyType)
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
                    TcpUdpPolicy pol = (TcpUdpPolicy)_tmpExceptionSettings[index].Policy;
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
                    UnrestrictedPolicy upol = (UnrestrictedPolicy)_tmpExceptionSettings[index].Policy;
                    radUnrestricted.Checked = true;
                    radRestriction_CheckedChanged(this, EventArgs.Empty);
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
            Parallel.For(0, _tmpExceptionSettings.Count - 1, (i, state) =>
            {
                _tmpExceptionSettings[i].ChildProcessesInherit = chkInheritToChildren.Checked;
            });

            //_tmpExceptionSettings[0].ChildProcessesInherit = chkInheritToChildren.Checked;

            if (radBlock.Checked)
            {
                //_tmpExceptionSettings[0].Policy = HardBlockPolicy.Instance;

                Parallel.For(0, _tmpExceptionSettings.Count - 1, (i, state) =>
                {
                    _tmpExceptionSettings[i].Policy = HardBlockPolicy.Instance;
                });
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
                    //_tmpExceptionSettings[0].Policy = pol;

                    Parallel.For(0, _tmpExceptionSettings.Count - 1, (i, state) =>
                    {
                        _tmpExceptionSettings[i].Policy = pol;
                    });
                }
                catch
                {
                    Utils.ShowMessageBox(
                        Resources.Messages.PortListInvalid,
                        Resources.Messages.TinyWall,
                        Microsoft.Samples.TaskDialogueCommonButtons.Ok,
                        Microsoft.Samples.TaskDialogueIcon.Warning,
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
                //_tmpExceptionSettings[0].Policy = pol;

                Parallel.For(0, _tmpExceptionSettings.Count - 1, (i, state) =>
                {
                    _tmpExceptionSettings[i].Policy = pol;
                });

            }

            //this._tmpExceptionSettings[0].CreationDate = DateTime.Now;
            var dateTimeNow = DateTime.Now;
            Parallel.For(0, _tmpExceptionSettings.Count - 1, (i, state) =>
            {
                _tmpExceptionSettings[i].CreationDate = dateTimeNow;
            });

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

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

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

            if (!packageList.Any()) return;

            ReinitFormFromSubject(new AppContainerSubject(packageList[0]));
        }

        private void ReinitFormFromSubject(ExceptionSubject subject)
        {
            List<FirewallExceptionV3> exceptions = GlobalInstances.AppDatabase.GetExceptionsForApp(subject, true, out _);

            if (!exceptions.Any() || _tmpExceptionSettings.Exists(e => e.Subject.Equals(exceptions[0].Subject))) return;

            _tmpExceptionSettings.AddRange(exceptions);

            //_tmpExceptionSettings = exceptions;

            UpdateUI();

            //if (_tmpExceptionSettings.Any())
            //    // Multiple known files, just accept them as is
            //    this.DialogResult = DialogResult.OK;
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
    }
}
