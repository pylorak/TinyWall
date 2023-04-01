using pylorak.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace pylorak.TinyWall
{
    internal partial class SettingsForm : Form
    {
        private class IdWithName
        {
            internal readonly string Id;
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

        private readonly List<ListViewItem> _exceptionItems = new();
        private readonly List<ListViewItem> _filteredExceptionItems = new();
        private bool _loadingSettings;
        private string? _mNewPassword;
        private Size _iconSize = new((int)Math.Round(16 * Utils.DpiScalingFactor), (int)Math.Round(16 * Utils.DpiScalingFactor));

        internal SettingsForm(ServerConfiguration service, ControllerSettings controller)
        {
            InitializeComponent();
            Utils.SetRightToLeft(this);
            this.IconList.ImageSize = _iconSize;
            this.Icon = Resources.Icons.firewall;
            this.btnOK.Image = GlobalInstances.ApplyBtnIcon;
            this.btnCancel.Image = GlobalInstances.CancelBtnIcon;
            this.btnAppAutoDetect.Image = GlobalInstances.UninstallBtnIcon;
            this.btnAppAdd.Image = GlobalInstances.AddBtnIcon;
            this.btnAppModify.Image = GlobalInstances.ModifyBtnIcon;
            this.btnAppRemove.Image = GlobalInstances.RemoveBtnIcon;
            this.btnAppRemoveAll.Image = GlobalInstances.RemoveBtnIcon;
            this.btnSubmitAssoc.Image = GlobalInstances.SubmitBtnIcon;
            this.btnImport.Image = GlobalInstances.ImportBtnIcon;
            this.btnExport.Image = GlobalInstances.ExportBtnIcon;
            this.btnUpdate.Image = GlobalInstances.UpdateBtnIcon;
            this.btnWeb.Image = GlobalInstances.WebBtnIcon;
            this.btnDonate.BackgroundImage = Resources.Icons.donate;

            listApplications.AllowDrop = true;
            listApplications.DragEnter += ListApplications_DragEnter;
            listApplications.DragDrop += ListApplications_DragDrop;

            TmpConfig = new ConfigContainer(service, controller);
            TmpConfig.Service.ActiveProfile.Normalize();
        }

        private void ListApplications_DragDrop(object sender, DragEventArgs e)
        {
            List<FirewallExceptionV3> list = new List<FirewallExceptionV3>();

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            foreach (string file in files)
            {
                try
                {
                    list.AddRange(GlobalInstances.AppDatabase.GetExceptionsForApp(new ExecutableSubject(file), true, out _));
                }
                catch
                {
                    // ignored
                }
            }

            TmpConfig.Service.ActiveProfile.AddExceptions(list);
            RebuildExceptionsList();
        }

        private void ListApplications_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.All : DragDropEffects.None;
        }

        internal string? NewPassword => _mNewPassword;

        private void InitSettingsUI()
        {
            _loadingSettings = true;
            try
            {
                // General page
                chkAutoUpdateCheck.Checked = TmpConfig.Service.AutoUpdateCheck;
                chkAskForExceptionDetails.Checked = TmpConfig.Controller.AskForExceptionDetails;
                chkEnableHotkeys.Checked = TmpConfig.Controller.EnableGlobalHotkeys;
                comboLanguages.SelectedIndex = 0;

                for (int i = 0; i < comboLanguages.Items.Count; ++i)
                {
                    IdWithName item = (IdWithName)comboLanguages.Items[i];

                    if (!item.Id.Equals(TmpConfig.Controller.Language, StringComparison.OrdinalIgnoreCase)) continue;

                    comboLanguages.SelectedIndex = i;
                    break;
                }

                // Fill Machine Settings tab
                chkDisplayOffBlock.Checked = TmpConfig.Service.ActiveProfile.DisplayOffBlock;
                chkLockHostsFile.Checked = TmpConfig.Service.LockHostsFile;
                chkHostsBlocklist.Checked = TmpConfig.Service.Blocklists.EnableHostsBlocklist;
                chkBlockMalwarePorts.Checked = TmpConfig.Service.Blocklists.EnablePortBlocklist;
                chkEnableBlocklists.Checked = TmpConfig.Service.Blocklists.EnableBlocklists;
                chkEnableBlocklists_CheckedChanged(this, EventArgs.Empty);

                // Fill lists of special exceptions
                listRecommendedGlobalProfiles.BeginUpdate();
                listOptionalGlobalProfiles.BeginUpdate();
                listRecommendedGlobalProfiles.Items.Clear();
                listOptionalGlobalProfiles.Items.Clear();
                foreach (DatabaseClasses.Application app in GlobalInstances.AppDatabase.KnownApplications)
                {
                    if (!app.HasFlag("TWUI:Special") || app.HasFlag("TWUI:Hidden")) continue;

                    // Get localised name
                    IdWithName item = new IdWithName(app.Name, app.LocalisedName);

                    // Construct default name in case no localization exists
                    if (string.IsNullOrEmpty(item.Name))
                        item.Name = item.Id.Replace('_', ' ');

                    CheckedListBox listBox = app.HasFlag("TWUI:Recommended") ? listRecommendedGlobalProfiles : listOptionalGlobalProfiles;
                    int itemIdx = listBox.Items.Add(item);
                    listBox.SetItemChecked(itemIdx, TmpConfig.Service.ActiveProfile.SpecialExceptions.Contains(item.Id));
                }

                listRecommendedGlobalProfiles.EndUpdate();
                listOptionalGlobalProfiles.EndUpdate();

                // Fill list of applications
                RebuildExceptionsList();
            }
            finally
            {
                _loadingSettings = false;
            }
        }

        private void RebuildExceptionsList()
        {
            UwpPackage packageList = new UwpPackage();
            _exceptionItems.Clear();

            foreach (var ex in TmpConfig.Service.ActiveProfile.AppExceptions)
            {
                _exceptionItems.Add(ListItemFromAppException(ex, packageList));
            }

            _exceptionItems.Sort(listApplications.ListViewItemSorter as ListViewItemComparer);

            ApplyExceptionFilter();
        }

        private void ApplyExceptionFilter()
        {
            string filter = txtExceptionListFilter.Text.Trim().ToUpperInvariant();
            _filteredExceptionItems.Clear();

            if (string.IsNullOrEmpty(filter))
            {
                // No filter, add everything
                foreach (var t in _exceptionItems)
                {
                    _filteredExceptionItems.Add(t);
                }
            }
            else
            {
                // Apply filter
                foreach (var t in _exceptionItems)
                {
                    string sub0 = t.SubItems[0].Text.ToUpperInvariant();
                    string sub1 = t.SubItems[1].Text.ToUpperInvariant();
                    if (sub0.Contains(filter) || sub1.Contains(filter))
                        _filteredExceptionItems.Add(t);
                }
            }

            // Update visible list
            listApplications.VirtualListSize = _filteredExceptionItems.Count;
            listApplications.Refresh();

            // Update buttons
            listApplications_SelectedIndexChanged(listApplications, EventArgs.Empty);
        }

        private ListViewItem ListItemFromAppException(FirewallExceptionV3 ex, UwpPackage packageList)
        {
            var li = new ListViewItem { Tag = ex };

            var exeSubj = ex.Subject as ExecutableSubject;
            var srvSubj = ex.Subject as ServiceSubject;
            var uwpSubj = ex.Subject as AppContainerSubject;

            switch (ex.Subject.SubjectType)
            {
                case SubjectType.Executable:
                    li.Text = exeSubj!.ExecutableName;
                    li.SubItems.Add(Resources.Messages.SubjectTypeExecutable);
                    li.SubItems.Add(exeSubj.ExecutablePath);
                    break;
                case SubjectType.Service:
                    li.Text = srvSubj!.ServiceName;
                    li.SubItems.Add(Resources.Messages.SubjectTypeService);
                    li.SubItems.Add(srvSubj.ExecutablePath);
                    break;
                case SubjectType.Global:
                    li.Text = Resources.Messages.AllApplications;
                    li.SubItems.Add(Resources.Messages.SubjectTypeGlobal);
                    li.SubItems.Add(string.Empty);
                    li.ImageIndex = IconList.Images.IndexOfKey("window");
                    break;
                case SubjectType.AppContainer:
                    li.Text = uwpSubj!.DisplayName;
                    li.SubItems.Add(Resources.Messages.SubjectTypeUwpApp);
                    li.SubItems.Add(uwpSubj.PublisherId + ", " + uwpSubj.Publisher);
                    li.ImageIndex = IconList.Images.IndexOfKey("store");
                    break;
                case SubjectType.Invalid:
                default:
                    throw new NotImplementedException();
            }


            if (ex.Policy.PolicyType == PolicyType.HardBlock)
            {
                li.BackColor = System.Drawing.Color.LightPink;
            }

            if (uwpSubj is not null)
            {
                if (!packageList.FindPackage(uwpSubj.Sid).HasValue)
                {
                    li.ImageIndex = IconList.Images.IndexOfKey("deleted");
                    li.BackColor = System.Drawing.Color.LightGray;
                }
            }

            if (exeSubj is not null)
            {
                if (NetworkPath.IsNetworkPath(exeSubj.ExecutablePath))
                {
                    /* We do not load icons from network drives, to avoid 30s timeout if the drive is unavailable.
                     * If this is ever changed in the future, also remember that .Net's Icon.ExtractAssociatedIcon()
                     * does not work with UNC paths. For workaround see:
                     * http://stackoverflow.com/questions/1842226/how-to-get-the-associated-icon-from-a-network-share-file
                     */
                    li.ImageIndex = IconList.Images.IndexOfKey("network-drive");
                }
                else if (File.Exists(exeSubj.ExecutablePath))
                {
                    if (!IconList.Images.ContainsKey(exeSubj.ExecutablePath))
                        IconList.Images.Add(exeSubj.ExecutablePath, Utils.GetIconContained(exeSubj.ExecutablePath, _iconSize.Width, _iconSize.Height));
                    li.ImageIndex = IconList.Images.IndexOfKey(exeSubj.ExecutablePath);
                }
                else if (exeSubj.ExecutablePath == "System")
                {
                    li.ImageIndex = IconList.Images.IndexOfKey("system");
                }
                else
                {
                    li.ImageIndex = IconList.Images.IndexOfKey("deleted");
                    li.BackColor = System.Drawing.Color.LightGray;
                }
            }

            return li;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Check password input
            if (chkChangePassword.Checked)
            {
                if (txtPassword.Text != txtPasswordAgain.Text)
                {
                    MessageBox.Show(this, Resources.Messages.PasswordFieldsDoNotMatch, Resources.Messages.TinyWall, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }

            // Set password
            _mNewPassword = chkChangePassword.Checked ? txtPassword.Text : null;

            // Save settings
            TmpConfig.Controller.AskForExceptionDetails = chkAskForExceptionDetails.Checked;
            TmpConfig.Controller.EnableGlobalHotkeys = chkEnableHotkeys.Checked;
            TmpConfig.Service.AutoUpdateCheck = chkAutoUpdateCheck.Checked;
            TmpConfig.Controller.SettingsTabIndex = tabControl1.SelectedIndex;
            TmpConfig.Service.LockHostsFile = chkLockHostsFile.Checked;
            TmpConfig.Service.Blocklists.EnablePortBlocklist = chkBlockMalwarePorts.Checked;
            TmpConfig.Service.Blocklists.EnableHostsBlocklist = chkHostsBlocklist.Checked;
            TmpConfig.Service.Blocklists.EnableBlocklists = chkEnableBlocklists.Checked;
            TmpConfig.Service.ActiveProfile.DisplayOffBlock = chkDisplayOffBlock.Checked;

            TmpConfig.Controller.Language = ((IdWithName)comboLanguages.SelectedItem).Id;

            this.DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void listRecommendedGlobalProfiles_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_loadingSettings) return;

            CheckedListBox clb = (CheckedListBox)sender;
            IdWithName item = (IdWithName)clb.Items[e.Index];

            if (e.NewValue == CheckState.Checked)
            {
                TmpConfig.Service.ActiveProfile.SpecialExceptions.Add(item.Id);
            }
            else
            {
                TmpConfig.Service.ActiveProfile.SpecialExceptions.Remove(item.Id);
            }
        }

        private void listOptionalGlobalProfiles_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // The code is exactly the same as for listRecommendedGlobalProfiles
            listRecommendedGlobalProfiles_ItemCheck(sender, e);
        }

        private void btnAppRemove_Click(object sender, EventArgs e)
        {
            for (int i = listApplications.SelectedIndices.Count - 1; i >= 0; --i)
            {
                ListViewItem li = _filteredExceptionItems[listApplications.SelectedIndices[i]];
                TmpConfig.Service.ActiveProfile.AppExceptions.Remove((FirewallExceptionV3)li.Tag);
            }

            listApplications.SelectedIndices.Clear();
            RebuildExceptionsList();
        }

        private void btnAppRemoveAll_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, Resources.Messages.AreYouSureYouWantToRemoveAllExceptions, Resources.Messages.TinyWall, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.No)
                return;

            TmpConfig.Service.ActiveProfile.AppExceptions.Clear();
            RebuildExceptionsList();
        }

        private void btnAppModify_Click(object sender, EventArgs e)
        {
            ListViewItem li = _filteredExceptionItems[listApplications.SelectedIndices[0]];
            FirewallExceptionV3 oldEx = (FirewallExceptionV3)li.Tag;
            FirewallExceptionV3 newEx = Utils.DeepClone(oldEx);
            newEx.RegenerateId();

            using (ApplicationExceptionForm f = new ApplicationExceptionForm(newEx))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    // Remove old rule
                    TmpConfig.Service.ActiveProfile.AppExceptions.Remove(oldEx);
                    // Add new rule
                    TmpConfig.Service.ActiveProfile.AddExceptions(f.ExceptionSettings);
                    RebuildExceptionsList();
                }
            }

            listApplications.Focus();
        }

        private void btnAppAdd_Click(object sender, EventArgs e)
        {
            //using var f = new ApplicationExceptionForm(FirewallExceptionV3.Default);
            using var f = new ApplicationExceptionForm();

            if (f.ShowDialog(this) != DialogResult.OK) return;

            TmpConfig.Service.ActiveProfile.AddExceptions(f.ExceptionSettings);
            RebuildExceptionsList();
        }

        private void chkEnablePassword_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.Enabled = txtPasswordAgain.Enabled = chkChangePassword.Checked;
        }

        private void btnSubmitAssoc_Click(object sender, EventArgs e)
        {
            /* Not implemented */
        }

        private void SettingsForm_Shown(object sender, EventArgs e)
        {
            this.BringToFront();
            this.Activate();
        }

        private void btnWeb_Click(object sender, EventArgs e)
        {
            var psi = new ProcessStartInfo(@"https://tinywall.pados.hu") { UseShellExecute = true };
            Process.Start(psi);
        }

        private void listApplications_DoubleClick(object sender, EventArgs e)
        {
            if (listApplications.SelectedIndices.Count == 0)
                return;

            btnAppModify_Click(this, EventArgs.Empty);
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            Updater.StartUpdate();
        }

        private void btnAppAutoDetect_Click(object sender, EventArgs e)
        {
            using var aff = new AppFinderForm();

            if (aff.ShowDialog(this) != DialogResult.OK) return;

            TmpConfig.Service.ActiveProfile.AddExceptions(aff.SelectedExceptions);
            RebuildExceptionsList();
        }

        private void lblAboutHomepageLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            btnWeb_Click(sender, EventArgs.Empty);
        }

        private void lblLinkLicense_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo(Path.Combine(Path.GetDirectoryName(Utils.ExecutablePath), "License.rtf"))
                {
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch
            {
                // ignored
            }
        }

        private void btnDonate_Click(object sender, EventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo(@"https://tinywall.pados.hu/donate.php") { UseShellExecute = true };
                Process.Start(psi);
            }
            catch
            {
                // ignored
            }
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
            ofd.Filter = string.Format(CultureInfo.CurrentCulture, @"{0} (*.tws)|*.tws|{1} (*)|*", Resources.Messages.TinyWallSettingsFileFilter, Resources.Messages.AllFilesFileFilter);

            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                TmpConfig = SerialisationHelper.DeserialiseFromFile(ofd.FileName, new ConfigContainer(), true);
            }
            catch
            {
                // Fail import.
                MessageBox.Show(this, Resources.Messages.ConfigurationImportError, Resources.Messages.TinyWall, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            InitSettingsUI();
            MessageBox.Show(this, Resources.Messages.ConfigurationHasBeenImported, Resources.Messages.TinyWall, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            ofd.Filter = string.Format(CultureInfo.CurrentCulture, @"{0} (*.tws)|*.tws|{1} (*)|*", Resources.Messages.TinyWallSettingsFileFilter, Resources.Messages.AllFilesFileFilter);
            sfd.DefaultExt = "tws";
            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                SerialisationHelper.SerialiseToFile(this.TmpConfig, sfd.FileName);
                MessageBox.Show(this, Resources.Messages.ConfigurationHasBeenExported, Resources.Messages.TinyWallSettingsFileFilter, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
#if DEBUG
            //DataCollection.StartProfile(ProfileLevel.Global, DataCollection.CurrentId);
#endif
            if (TmpConfig.Controller.SettingsFormWindowSize.Width != 0)
                this.Size = TmpConfig.Controller.SettingsFormWindowSize;
            if (TmpConfig.Controller.SettingsFormWindowLoc.X != 0)
            {
                this.Location = TmpConfig.Controller.SettingsFormWindowLoc;
                Utils.FixupFormPosition(this);
            }

            foreach (ColumnHeader col in listApplications.Columns)
            {
                if (ActiveConfig.Controller.SettingsFormAppListColumnWidths.TryGetValue((string)col.Tag, out int width))
                    col.Width = width;
            }

            Utils.SetDoubleBuffering(listApplications, true);
            listApplications.ListViewItemSorter = new ListViewItemComparer(0, IconList);
            tabControl1.SelectedIndex = TmpConfig.Controller.SettingsTabIndex;

            comboLanguages.Items.Add(new IdWithName("auto", "Automatic"));
            comboLanguages.Items.Add(new IdWithName("bg", "български"));
            comboLanguages.Items.Add(new IdWithName("cs", "Čeština"));
            comboLanguages.Items.Add(new IdWithName("de", "Deutsch"));
            comboLanguages.Items.Add(new IdWithName("en", "English"));
            comboLanguages.Items.Add(new IdWithName("es", "Español"));
            comboLanguages.Items.Add(new IdWithName("fr", "Français"));
            comboLanguages.Items.Add(new IdWithName("it", "Italiano"));
            comboLanguages.Items.Add(new IdWithName("he-IL", "עברית"));
            comboLanguages.Items.Add(new IdWithName("hu", "Magyar"));
            comboLanguages.Items.Add(new IdWithName("nl", "Nederlands"));
            comboLanguages.Items.Add(new IdWithName("pl", "Polski"));
            comboLanguages.Items.Add(new IdWithName("pt-BR", "Português Brasileiro"));
            comboLanguages.Items.Add(new IdWithName("ru", "Русский"));
            comboLanguages.Items.Add(new IdWithName("tr", "Türkçe"));
            comboLanguages.Items.Add(new IdWithName("ja", "日本語"));
            comboLanguages.Items.Add(new IdWithName("ko", "한국어"));
            comboLanguages.Items.Add(new IdWithName("zh", "汉语"));

            IconList.Images.Add("deleted", Resources.Icons.delete);
            IconList.Images.Add("network-drive", Resources.Icons.network_drive_small);
            IconList.Images.Add("window", Resources.Icons.window);
            IconList.Images.Add("store", Resources.Icons.store);
            IconList.Images.Add("system", Resources.Icons.windows_small);

            lblVersion.Text = string.Format(CultureInfo.CurrentCulture, @"{0} {1}", lblVersion.Text, Application.ProductVersion.ToString());

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
            ListViewItemComparer oldSorter = (ListViewItemComparer)listApplications.ListViewItemSorter;
            ListViewItemComparer newSorter = new ListViewItemComparer(e.Column, IconList);
            if ((oldSorter != null) && (oldSorter.Column == newSorter.Column))
                newSorter.Ascending = !oldSorter.Ascending;

            listApplications.ListViewItemSorter = newSorter;
            RebuildExceptionsList();
        }

        private void chkEnableBlocklists_CheckedChanged(object sender, EventArgs e)
        {
            chkHostsBlocklist.Enabled = chkEnableBlocklists.Checked;
            chkBlockMalwarePorts.Enabled = chkEnableBlocklists.Checked;
        }

        private void lblLinkAttributions_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo(Path.Combine(Path.GetDirectoryName(Utils.ExecutablePath), "Attributions.txt")) { UseShellExecute = true };
                Process.Start(psi);
            }
            catch
            {
                // ignored
            }
        }

        private void SettingsForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (listApplications.Focused && (e.KeyCode == Keys.Delete))
            {
                btnAppRemove_Click(btnAppRemove, EventArgs.Empty);
                e.Handled = true;
            }
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            TmpConfig.Controller.SettingsFormWindowSize = this.Size;
            TmpConfig.Controller.SettingsFormWindowLoc = this.Location;
            ActiveConfig.Controller.SettingsFormWindowSize = this.Size;
            ActiveConfig.Controller.SettingsFormWindowLoc = this.Location;

            TmpConfig.Controller.SettingsFormAppListColumnWidths.Clear();
            ActiveConfig.Controller.SettingsFormAppListColumnWidths.Clear();
            foreach (ColumnHeader col in listApplications.Columns)
            {
                TmpConfig.Controller.SettingsFormAppListColumnWidths.Add((string)col.Tag, col.Width);
                ActiveConfig.Controller.SettingsFormAppListColumnWidths.Add((string)col.Tag, col.Width);
            }

            ActiveConfig.Controller.Save();
        }

        private void listApplications_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            e.Item = _filteredExceptionItems[e.ItemIndex];
        }

        private void listApplications_VirtualItemsSelectionRangeChanged(object sender, ListViewVirtualItemsSelectionRangeChangedEventArgs e)
        {
            listApplications_SelectedIndexChanged(sender, EventArgs.Empty);
        }

        private void listApplications_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool anyItemSelected = listApplications.SelectedIndices.Count != 0;
            bool singleItemSelected = listApplications.SelectedIndices.Count == 1;
            btnAppModify.Enabled = singleItemSelected;
            btnAppRemove.Enabled = anyItemSelected;
            btnSubmitAssoc.Enabled = anyItemSelected;
        }

        private void btnGithub_Click(object sender, EventArgs e)
        {
            var psi = new ProcessStartInfo(@"https://github.com/pylorak/tinywall") { UseShellExecute = true };
            Process.Start(psi);
        }
    }
}
