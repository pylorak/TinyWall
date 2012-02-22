using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Windows.Forms;

namespace PKSoft
{
    internal partial class SettingsForm : Form
    {
        internal ZoneSettings TmpZoneConfig;
        internal MachineSettings TmpMachineConfig;
        internal ControllerSettings TmpControllerConfig;

        private List<ListViewItem> ExceptionItems = new List<ListViewItem>();
        private bool LoadingSettings;
        private string m_NewPassword;

        internal SettingsForm(ControllerSettings controller, MachineSettings machine, ZoneSettings zone)
        {
            InitializeComponent();
            this.Icon = Resources.Icons.firewall;

            TmpZoneConfig = zone;
            TmpMachineConfig = machine;
            TmpControllerConfig = controller;
            TmpZoneConfig.Normalize();
        }

        internal string NewPassword
        {
            get { return m_NewPassword; }
        }

        private void InitSettingsUI()
        {
            LoadingSettings = true;
            try
            {
                // General page
                chkAutoUpdateCheck.Checked = TmpMachineConfig.AutoUpdateCheck;
                chkEnableDefaultWindowsRules.Checked = TmpZoneConfig.EnableDefaultWindowsRules;
                chkAskForExceptionDetails.Checked = TmpControllerConfig.AskForExceptionDetails;

                // Fill Machine Settings tab
                chkBlockMalwarePorts.Checked = TmpZoneConfig.BlockMalwarePorts;
                chkLockHostsFile.Checked = TmpMachineConfig.LockHostsFile;
                chkHostsBlocklist.Checked = TmpMachineConfig.HostsBlocklist;

                // These will be reused multiple times
                ApplicationCollection allApps = GlobalInstances.ProfileMan.KnownApplications;

                // Fill lists of special exceptions
                listRecommendedGlobalProfiles.SuspendLayout();
                listOptionalGlobalProfiles.SuspendLayout();
                listRecommendedGlobalProfiles.Items.Clear();
                listOptionalGlobalProfiles.Items.Clear();
                foreach (Application app in allApps)
                {
                    if (app.Special && app.ResolveFilePaths())
                    {
                        if (app.Recommended)
                        {
                            int itemIdx = listRecommendedGlobalProfiles.Items.Add(app.Name);
                            listRecommendedGlobalProfiles.SetItemChecked(itemIdx, TmpZoneConfig.SpecialExceptions.Contains(app.Name));
                        }
                        else
                        {
                            int itemIdx = listOptionalGlobalProfiles.Items.Add(app.Name);
                            listOptionalGlobalProfiles.SetItemChecked(itemIdx, TmpZoneConfig.SpecialExceptions.Contains(app.Name));
                        }
                    }
                }
                listRecommendedGlobalProfiles.ResumeLayout(true);
                listOptionalGlobalProfiles.ResumeLayout(true);


                // Fill list of applications
                RebuildExceptionsList();
            }
            finally
            {
                LoadingSettings = false;
            }
        }

        private void RebuildExceptionsList()
        {
            ExceptionItems.Clear();
            for (int i = 0; i < TmpZoneConfig.AppExceptions.Length; ++i)
            {
                AppExceptionSettings ex = TmpZoneConfig.AppExceptions[i];
                ExceptionItems.Add(ListItemFromAppException(ex));
            }
            ApplyExceptionFilter();
        }

        private void ApplyExceptionFilter()
        {
            string filter = txtExceptionListFilter.Text.Trim().ToUpperInvariant();
            List<ListViewItem> icoll = new List<ListViewItem>();

            if (string.IsNullOrEmpty(filter))
            {
                // No filter, add everything
                for (int i = 0; i < ExceptionItems.Count; ++i)
                {
                    icoll.Add(ExceptionItems[i]);
                }
            }
            else
            {
                // Apply filter
                for (int i = 0; i < ExceptionItems.Count; ++i)
                {
                    string sub0 = ExceptionItems[i].SubItems[0].Text.ToUpperInvariant();
                    string sub1 = ExceptionItems[i].SubItems[1].Text.ToUpperInvariant();
                    string sub2 = ExceptionItems[i].SubItems[2].Text.ToUpperInvariant();
                    if (sub0.Contains(filter) || sub1.Contains(filter) || sub2.Contains(filter))
                        icoll.Add(ExceptionItems[i]);
                }
            }

            // Update visible list
            listApplications.SuspendLayout();
            listApplications.Items.Clear();
            listApplications.Items.AddRange(icoll.ToArray());
            listApplications.ResumeLayout();

            // Update buttons
            listApplications_SelectedIndexChanged(listApplications, null);
        }

        private ListViewItem ListItemFromAppException(AppExceptionSettings ex)
        {
            string name = string.IsNullOrEmpty(ex.ServiceName) ? ex.ExecutableName : "Srv: " + ex.ServiceName;

            ListViewItem li = new ListViewItem(name);
            li.SubItems.Add(ex.ExecutablePath);
            li.Tag = ex;

            if (File.Exists(ex.ExecutablePath))
            {
                if (!IconList.Images.ContainsKey(ex.ExecutablePath))
                    IconList.Images.Add(ex.ExecutablePath, Utils.GetIcon(ex.ExecutablePath, 16, 16));
                li.ImageKey = ex.ExecutablePath;
            }
            else
                li.ImageKey = "deleted";

            string AppProfiles = string.Empty;
            if (ex.Profiles.Length > 0)
            {
                // Add first profile
                if (GlobalInstances.ProfileMan.GetProfile(ex.Profiles[0]) != null)
                    AppProfiles = ex.Profiles[0];

                // Add rest of profiles
                for (int j = 1; j < ex.Profiles.Length; ++j)
                {
                    if (GlobalInstances.ProfileMan.GetProfile(ex.Profiles[j]) != null)
                        AppProfiles += ", " + ex.Profiles[j];
                }
            }

            li.SubItems.Add(AppProfiles);
            return li;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Check password input
            if (chkChangePassword.Checked)
            {
                if (txtPassword.Text != txtPasswordAgain.Text)
                {
                    MessageBox.Show(this, PKSoft.Resources.Messages.PasswordFieldsDoNotMatch, PKSoft.Resources.Messages.TinyWall, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }

            // Set password
            m_NewPassword = chkChangePassword.Checked ? txtPassword.Text : null;

            // Save settings
            TmpZoneConfig.BlockMalwarePorts = chkBlockMalwarePorts.Checked;
            TmpZoneConfig.EnableDefaultWindowsRules = chkEnableDefaultWindowsRules.Checked;
            TmpControllerConfig.AskForExceptionDetails = chkAskForExceptionDetails.Checked;
            TmpMachineConfig.AutoUpdateCheck = chkAutoUpdateCheck.Checked;
            TmpControllerConfig.ManageTabIndex = tabControl1.SelectedIndex;
            TmpMachineConfig.LockHostsFile = chkLockHostsFile.Checked;
            TmpMachineConfig.HostsBlocklist = chkHostsBlocklist.Checked;

            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void listRecommendedGlobalProfiles_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (LoadingSettings) return;

            CheckedListBox clb = sender as CheckedListBox;
            string itemText = clb.Items[e.Index].ToString();
            if (e.NewValue == CheckState.Checked)
            {
                TmpZoneConfig.SpecialExceptions = Utils.ArrayAddItem(TmpZoneConfig.SpecialExceptions, itemText);
            }
            else
            {
                TmpZoneConfig.SpecialExceptions = Utils.ArrayRemoveItem(TmpZoneConfig.SpecialExceptions, itemText);
            }
        }

        private void listOptionalGlobalProfiles_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // The code is exactly the same as for listRecommendedGlobalProfiles
            listRecommendedGlobalProfiles_ItemCheck(sender, e);
        }

        private void listApplications_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool IsItemSelected = listApplications.SelectedItems.Count != 0;
            btnAppModify.Enabled = IsItemSelected;
            btnAppRemove.Enabled = IsItemSelected;
            btnSubmitAssoc.Enabled = IsItemSelected;
        }

        private void btnAppRemove_Click(object sender, EventArgs e)
        {
            ListViewItem li = listApplications.SelectedItems[0];
            TmpZoneConfig.AppExceptions = Utils.ArrayRemoveItem(TmpZoneConfig.AppExceptions, (AppExceptionSettings)li.Tag);
            RebuildExceptionsList();
        }

        private void btnAppRemoveAll_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, PKSoft.Resources.Messages.AreYouSureYouWantToRemoveAllExceptions, PKSoft.Resources.Messages.TinyWall, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == System.Windows.Forms.DialogResult.No)
                return;

            TmpZoneConfig.AppExceptions = new AppExceptionSettings[0];
            RebuildExceptionsList();
        }
        
        private void btnAppModify_Click(object sender, EventArgs e)
        {
            ListViewItem li = listApplications.SelectedItems[0];
            AppExceptionSettings oldEx = (AppExceptionSettings)li.Tag;
            AppExceptionSettings newEx = Utils.DeepClone(oldEx);
            newEx.RegenerateID();
            using (ApplicationExceptionForm f = new ApplicationExceptionForm(newEx))
            {
                if (f.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    // Remove old rule
                    TmpZoneConfig.AppExceptions = Utils.ArrayRemoveItem(TmpZoneConfig.AppExceptions, oldEx);
                    // Add new rule
                    TmpZoneConfig.AppExceptions = Utils.ArrayAddItem(TmpZoneConfig.AppExceptions, f.ExceptionSettings);
                    TmpZoneConfig.Normalize();
                    RebuildExceptionsList();
                }
            }

            listApplications.Focus();
        }

        private void btnAppAdd_Click(object sender, EventArgs e)
        {
            AppExceptionSettings ex = new AppExceptionSettings();
            using (ApplicationExceptionForm f = new ApplicationExceptionForm(ex))
            {
                if (f.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    List<AppExceptionSettings> exceptions = AppExceptionSettings.CheckForAppDependencies(this, f.ExceptionSettings);
                    for (int i = 0; i < exceptions.Count; ++i)
                        TmpZoneConfig.AppExceptions = Utils.ArrayAddItem(TmpZoneConfig.AppExceptions, exceptions[i]);
                    TmpZoneConfig.Normalize();
                    RebuildExceptionsList();
                }
            }
        }

        private void chkEnablePassword_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.Enabled = txtPasswordAgain.Enabled = chkChangePassword.Checked;
        }

        private void btnSubmitAssoc_Click(object sender, EventArgs e)
        {
            // Get exception
            ListViewItem li = listApplications.SelectedItems[0];
            AppExceptionSettings ex = (AppExceptionSettings)li.Tag;

            // Construct association
            ProfileAssoc pa = ProfileAssoc.FromExecutable(ex.ExecutablePath, string.Empty);
            pa.Profiles = ex.Profiles;

            // Submit association
            string tmpfile = Path.GetTempFileName() + ".xml";
            SerializationHelper.SaveToXMLFile(pa, tmpfile);

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = tmpfile;
            psi.UseShellExecute = true;
            psi.RedirectStandardOutput = false;
            Process.Start(psi);
        }

        private void btnUninstall_Click(object sender, EventArgs e)
        {
            string msg = string.Empty;

            if (!Utils.RunningAsAdmin())
            {
                msg = string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.YouDoNotHaveAdministrativePrivilegesNeeded);
                MessageBox.Show(this, msg, PKSoft.Resources.Messages.TinyWall, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                return;
            }


             msg = string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.YouAreAboutToRemoveTinyWall);

            // Handle uninstall request
            bool UninstallFlag = MessageBox.Show(this, msg, PKSoft.Resources.Messages.UninstallTinyWall, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == System.Windows.Forms.DialogResult.Yes;
            if (UninstallFlag)
            {
                // Stop service
                // This is a message that does not return successfully even if it was successfull
                GlobalInstances.CommunicationMan.QueueMessageSimple(TWControllerMessages.STOP_DISABLE);
                System.Threading.Thread.Sleep(2000);

                // Get path to uninstaller and launch it
                string uninstaller = Path.Combine(Path.GetDirectoryName(Utils.ExecutablePath), "unins000.exe");
                if (File.Exists(uninstaller))
                {
                    Utils.StartProcess(uninstaller, "/SILENT", true);
                    System.Windows.Forms.Application.Exit();
                }
                else
                {
                    MessageBox.Show(this, PKSoft.Resources.Messages.CouldNotFindTheUninstaller, PKSoft.Resources.Messages.UninstallTinyWall, MessageBoxButtons.OK, MessageBoxIcon.Error);

                    // Start service back up
                    using (ServiceController sc = new ServiceController(TinyWallService.SERVICE_NAME))
                    {
                        if (sc.Status == ServiceControllerStatus.Stopped)
                            sc.Start();
                    }
                }
            }
        }

        private void SettingsForm_Shown(object sender, EventArgs e)
        {
            IconList.Images.Add("deleted", Resources.Icons.delete);

            lblVersion.Text = string.Format(CultureInfo.CurrentCulture, "{0} {1}", lblVersion.Text, FileVersionInfo.GetVersionInfo(Utils.ExecutablePath).ProductVersion.ToString());

            InitSettingsUI();

#if !DEBUG
            // TODO: Make submissions work
            btnSubmitAssoc.Visible = false;
            chkEnableDefaultWindowsRules.Visible = false;
#endif
            this.BringToFront();
            this.Activate();
        }

        private void btnWeb_Click(object sender, EventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo(@"http://tinywall.pados.hu");
            psi.UseShellExecute = true;
            Process.Start(psi);
        }

        private void listApplications_DoubleClick(object sender, EventArgs e)
        {
            if (listApplications.SelectedIndices.Count == 0)
                return;

            btnAppModify_Click(null, null);
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            UpdateForm.StartUpdate(this);
        }

        private void btnAppAutoDetect_Click(object sender, EventArgs e)
        {
            using (AppFinderForm aff = new AppFinderForm(TmpZoneConfig.Clone() as ZoneSettings))
            {
                if (aff.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    TmpZoneConfig = aff.TmpZoneSettings;
                    TmpZoneConfig.Normalize();
                    RebuildExceptionsList();
                }
            }
        }

        private void lblAboutHomepageLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            btnWeb_Click(sender, null);
        }

        private void lblLinkLicense_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(Path.Combine(Path.GetDirectoryName(Utils.ExecutablePath), "License.txt"));
                psi.UseShellExecute = true;
                Process.Start(psi);
            }
            catch { }
        }

        private void btnDonate_Click(object sender, EventArgs e)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(@"http://tinywall.pados.hu/donation/donate.php");
                psi.UseShellExecute = true;
                Process.Start(psi);
            }
            catch { }
        }

        private void btnDonate_MouseEnter(object sender, EventArgs e)
        {
            btnDonate.BorderStyle = BorderStyle.FixedSingle;
        }

        private void btnDonate_MouseLeave(object sender, EventArgs e)
        {
            btnDonate.BorderStyle = BorderStyle.None;
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            ofd.Filter = string.Format(CultureInfo.CurrentCulture, "{0} (*.tws)|*.tws|{1} (*.*)|*.*", PKSoft.Resources.Messages.TinyWallSettingsFileFilter, PKSoft.Resources.Messages.AllFilesFileFilter);
            if (ofd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                SettingsContainer sc = SerializationHelper.LoadFromXMLFile<SettingsContainer>(ofd.FileName);
                TmpControllerConfig = sc.ControllerConfig;
                TmpZoneConfig = sc.CurrentZone;
                TmpMachineConfig = sc.GlobalConfig;
                InitSettingsUI();
                MessageBox.Show(this, PKSoft.Resources.Messages.ConfigurationHasBeenImported, PKSoft.Resources.Messages.TinyWall, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            ofd.Filter = string.Format(CultureInfo.CurrentCulture, "{0} (*.tws)|*.tws|{1} (*.*)|*.*", PKSoft.Resources.Messages.TinyWallSettingsFileFilter, PKSoft.Resources.Messages.AllFilesFileFilter);
            sfd.DefaultExt = "tws";
            if (sfd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                SettingsContainer sc = new SettingsContainer();
                sc.ControllerConfig = TmpControllerConfig;
                sc.CurrentZone = TmpZoneConfig;
                sc.GlobalConfig = TmpMachineConfig;
                SerializationHelper.SaveToXMLFile(sc, sfd.FileName);
                MessageBox.Show(this, PKSoft.Resources.Messages.ConfigurationHasBeenExported, PKSoft.Resources.Messages.TinyWallSettingsFileFilter, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void chkHostsBlocklist_CheckedChanged(object sender, EventArgs e)
        {
            if (chkHostsBlocklist.Checked)
                chkLockHostsFile.Checked = true;

            chkLockHostsFile.Enabled = !chkHostsBlocklist.Checked;
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = TmpControllerConfig.ManageTabIndex;
        }

        private void txtExceptionListFilter_TextChanged(object sender, EventArgs e)
        {
            ApplyExceptionFilter();
        }

        private void listApplications_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListViewItemComparer oldSorter = listApplications.ListViewItemSorter as ListViewItemComparer;
            ListViewItemComparer newSorter = new ListViewItemComparer(e.Column);
            if ((oldSorter != null) && (oldSorter.Column == newSorter.Column))
                newSorter.Ascending = !oldSorter.Ascending;

            listApplications.ListViewItemSorter = newSorter;
        }

    }
}
