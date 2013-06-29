using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

#if DEBUG
//using Microsoft.VisualStudio.Profiler;
#endif

namespace PKSoft
{
    internal partial class SettingsForm : Form
    {
        private class IdWithName
        {
            internal string Id;
            internal string Name;

            internal IdWithName(string id, string name)
            {
                Id = id;
                Name = name;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        internal ConfigContainer TmpConfig;

        private List<ListViewItem> ExceptionItems = new List<ListViewItem>();
        private bool LoadingSettings;
        private string m_NewPassword;

        internal SettingsForm(ConfigContainer config)
        {
            InitializeComponent();
            this.Icon = Resources.Icons.firewall;

            TmpConfig = config;
            TmpConfig.Service.Normalize();
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
                chkAutoUpdateCheck.Checked = TmpConfig.Service.AutoUpdateCheck;
                chkAskForExceptionDetails.Checked = TmpConfig.Controller.AskForExceptionDetails;
                chkEnableHotkeys.Checked = TmpConfig.Controller.EnableGlobalHotkeys;
                comboLanguages.SelectedIndex = 0;
                for(int i = 0; i < comboLanguages.Items.Count; ++i)
                {
                    IdWithName item = comboLanguages.Items[i] as IdWithName;
                    if (item.Id.Equals(TmpConfig.Controller.Language, StringComparison.OrdinalIgnoreCase))
                    {
                        comboLanguages.SelectedIndex = i;
                        break;
                    }
                }

                // Fill Machine Settings tab
                chkLockHostsFile.Checked = TmpConfig.Service.LockHostsFile;
                chkHostsBlocklist.Checked = TmpConfig.Service.Blocklists.EnableHostsBlocklist;
                chkBlockMalwarePorts.Checked = TmpConfig.Service.Blocklists.EnablePortBlocklist;
                chkEnableBlocklists.Checked = TmpConfig.Service.Blocklists.EnableBlocklists;
                chkEnableBlocklists_CheckedChanged(null, null);

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
                        // Get localized name
                        IdWithName item = new IdWithName(app.Name, PKSoft.Resources.Exceptions.ResourceManager.GetString(app.Name, PKSoft.Resources.Exceptions.Culture));

                        // Construct default name in case no localization exists
                        if (string.IsNullOrEmpty(item.Name))
                            item.Name = item.Id.Replace('_', ' ');

                        if (app.Recommended)
                        {
                            int itemIdx = listRecommendedGlobalProfiles.Items.Add(item);
                            listRecommendedGlobalProfiles.SetItemChecked(itemIdx, TmpConfig.Service.SpecialExceptions.Contains(item.Id));
                        }
                        else
                        {
                            int itemIdx = listOptionalGlobalProfiles.Items.Add(item);
                            listOptionalGlobalProfiles.SetItemChecked(itemIdx, TmpConfig.Service.SpecialExceptions.Contains(item.Id));
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
            for (int i = 0; i < TmpConfig.Service.AppExceptions.Count; ++i)
            {
                FirewallException ex = TmpConfig.Service.AppExceptions[i];
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
                    if (sub0.Contains(filter) || sub1.Contains(filter))
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

        private ListViewItem ListItemFromAppException(FirewallException ex)
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
            TmpConfig.Controller.AskForExceptionDetails = chkAskForExceptionDetails.Checked;
            TmpConfig.Controller.EnableGlobalHotkeys = chkEnableHotkeys.Checked;
            TmpConfig.Service.AutoUpdateCheck = chkAutoUpdateCheck.Checked;
            TmpConfig.Controller.ManageTabIndex = tabControl1.SelectedIndex;
            TmpConfig.Service.LockHostsFile = chkLockHostsFile.Checked;
            TmpConfig.Service.Blocklists.EnablePortBlocklist = chkBlockMalwarePorts.Checked;
            TmpConfig.Service.Blocklists.EnableHostsBlocklist = chkHostsBlocklist.Checked;
            TmpConfig.Service.Blocklists.EnableBlocklists = chkEnableBlocklists.Checked;

            TmpConfig.Controller.Language = (comboLanguages.SelectedItem as IdWithName).Id;

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
            IdWithName item = clb.Items[e.Index] as IdWithName;
            if (e.NewValue == CheckState.Checked)
            {
                TmpConfig.Service.SpecialExceptions.Add(item.Id);
            }
            else
            {
                TmpConfig.Service.SpecialExceptions.Remove(item.Id);
            }
        }

        private void listOptionalGlobalProfiles_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // The code is exactly the same as for listRecommendedGlobalProfiles
            listRecommendedGlobalProfiles_ItemCheck(sender, e);
        }

        private void listApplications_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool AnyItemSelected = listApplications.SelectedItems.Count != 0;
            bool SingleItemSelected = listApplications.SelectedItems.Count == 1;
            btnAppModify.Enabled = SingleItemSelected;
            btnAppRemove.Enabled = AnyItemSelected;
            btnSubmitAssoc.Enabled = AnyItemSelected;
        }

        private void btnAppRemove_Click(object sender, EventArgs e)
        {
            for (int i = listApplications.SelectedItems.Count - 1; i >= 0; --i)
            {
                ListViewItem li = listApplications.SelectedItems[i];
                TmpConfig.Service.AppExceptions.Remove((FirewallException)li.Tag);
            }
            RebuildExceptionsList();
        }

        private void btnAppRemoveAll_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, PKSoft.Resources.Messages.AreYouSureYouWantToRemoveAllExceptions, PKSoft.Resources.Messages.TinyWall, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == System.Windows.Forms.DialogResult.No)
                return;

            TmpConfig.Service.AppExceptions.Clear();
            RebuildExceptionsList();
        }
        
        private void btnAppModify_Click(object sender, EventArgs e)
        {
            ListViewItem li = listApplications.SelectedItems[0];
            FirewallException oldEx = (FirewallException)li.Tag;
            FirewallException newEx = Utils.DeepClone(oldEx);
            newEx.RegenerateID();
            using (ApplicationExceptionForm f = new ApplicationExceptionForm(newEx))
            {
                if (f.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    // Remove old rule
                    TmpConfig.Service.AppExceptions.Remove(oldEx);
                    // Add new rule
                    TmpConfig.Service.AppExceptions.Add(f.ExceptionSettings);
                    TmpConfig.Service.Normalize();
                    RebuildExceptionsList();
                }
            }

            listApplications.Focus();
        }

        private void btnAppAdd_Click(object sender, EventArgs e)
        {
            FirewallException ex = new FirewallException();
            using (ApplicationExceptionForm f = new ApplicationExceptionForm(ex))
            {
                if (f.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    List<FirewallException> exceptions = FirewallException.CheckForAppDependencies(f.ExceptionSettings, true, true, this);
                    for (int i = 0; i < exceptions.Count; ++i)
                        TmpConfig.Service.AppExceptions.Add(exceptions[i]);
                    TmpConfig.Service.Normalize();
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
            /* TODO
            // Get exception
            ListViewItem li = listApplications.SelectedItems[0];
            FirewallException ex = (FirewallException)li.Tag;

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
            */
        }

        private void SettingsForm_Shown(object sender, EventArgs e)
        {
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
            Updater.StartUpdate(this);
        }

        private void btnAppAutoDetect_Click(object sender, EventArgs e)
        {
            using (AppFinderForm aff = new AppFinderForm(Utils.DeepClone(TmpConfig.Service)))
            {
                if (aff.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    TmpConfig.Service = aff.TmpSettings;
                    TmpConfig.Service.Normalize();
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
                ProcessStartInfo psi = new ProcessStartInfo(Path.Combine(Path.GetDirectoryName(Utils.ExecutablePath), "License.rtf"));
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
            ofd.Filter = string.Format(CultureInfo.CurrentCulture, "{0} (*.tws)|*.tws|{1} (*)|*", PKSoft.Resources.Messages.TinyWallSettingsFileFilter, PKSoft.Resources.Messages.AllFilesFileFilter);
            if (ofd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    TmpConfig = SerializationHelper.LoadFromXMLFile<ConfigContainer>(ofd.FileName);
                }
                catch
                {
                    // Try loading from older export file format.
                    try
                    {
                        SettingsContainer sc = SerializationHelper.LoadFromXMLFile<SettingsContainer>(ofd.FileName);
                        TmpConfig.Controller = sc.ControllerConfig;
                        TmpConfig.Service.AllowLocalSubnet = sc.CurrentZone.AllowLocalSubnet;
                        TmpConfig.Service.AppExceptions = sc.CurrentZone.AppExceptions;
                        TmpConfig.Service.AutoUpdateCheck = sc.GlobalConfig.AutoUpdateCheck;
                        TmpConfig.Service.Blocklists = sc.GlobalConfig.Blocklists;
                        TmpConfig.Service.LastUpdateCheck = sc.GlobalConfig.LastUpdateCheck;
                        TmpConfig.Service.LockHostsFile = sc.GlobalConfig.LockHostsFile;
                        TmpConfig.Service.SpecialExceptions = sc.CurrentZone.SpecialExceptions;
                        TmpConfig.Service.StartupMode = sc.GlobalConfig.StartupMode;
                    }
                    catch
                    {
                        // Fail import.
                        MessageBox.Show(this, PKSoft.Resources.Messages.ConfigurationImportError, PKSoft.Resources.Messages.TinyWall, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                InitSettingsUI();
                MessageBox.Show(this, PKSoft.Resources.Messages.ConfigurationHasBeenImported, PKSoft.Resources.Messages.TinyWall, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            ofd.Filter = string.Format(CultureInfo.CurrentCulture, "{0} (*.tws)|*.tws|{1} (*)|*", PKSoft.Resources.Messages.TinyWallSettingsFileFilter, PKSoft.Resources.Messages.AllFilesFileFilter);
            sfd.DefaultExt = "tws";
            if (sfd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                SerializationHelper.SaveToXMLFile(this.TmpConfig, sfd.FileName);
                MessageBox.Show(this, PKSoft.Resources.Messages.ConfigurationHasBeenExported, PKSoft.Resources.Messages.TinyWallSettingsFileFilter, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
//            ReferencedBool loadingDone = MarqueeForm.ShowProgress();

#if DEBUG
//            DataCollection.StartProfile(ProfileLevel.Global, DataCollection.CurrentId);
#endif
            listApplications.ListViewItemSorter = new ListViewItemComparer(0);
            tabControl1.SelectedIndex = TmpConfig.Controller.ManageTabIndex;

            comboLanguages.Items.Add(new IdWithName("auto", "Automatic"));
            comboLanguages.Items.Add(new IdWithName("de", "Deutsch"));
            comboLanguages.Items.Add(new IdWithName("en", "English"));
            comboLanguages.Items.Add(new IdWithName("es", "Español"));
            comboLanguages.Items.Add(new IdWithName("fr", "Français"));
            comboLanguages.Items.Add(new IdWithName("hu", "Magyar"));
            comboLanguages.Items.Add(new IdWithName("nl", "Nederlands"));
            comboLanguages.Items.Add(new IdWithName("pt-BR", "Português Brasileiro"));
            comboLanguages.Items.Add(new IdWithName("ru", "Русский"));
            comboLanguages.Items.Add(new IdWithName("ja", "日本語"));

            IconList.Images.Add("deleted", Resources.Icons.delete);

            lblVersion.Text = string.Format(CultureInfo.CurrentCulture, "{0} {1}", lblVersion.Text, System.Windows.Forms.Application.ProductVersion.ToString());

            InitSettingsUI();

#if DEBUG
//          DataCollection.StopProfile(ProfileLevel.Global, DataCollection.CurrentId);
#endif

#if !DEBUG
            // TODO: Make submissions work
            btnSubmitAssoc.Visible = false;
#endif
//            loadingDone.Value = true;
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

        private void chkEnableBlocklists_CheckedChanged(object sender, EventArgs e)
        {
            chkHostsBlocklist.Enabled = chkEnableBlocklists.Checked;
            chkBlockMalwarePorts.Enabled = chkEnableBlocklists.Checked;
        }
    }
}
