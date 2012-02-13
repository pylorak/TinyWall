using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PKSoft
{
    internal partial class ApplicationExceptionForm : Form
    {
        private AppExceptionSettings TmpExceptionSettings;
        internal AppExceptionSettings ExceptionSettings
        {
            get { return TmpExceptionSettings; }
        }

        private bool RecognizedApp
        {
            set
            {
                if (value)
                {
                    // Recognized app
                    panel1.BackgroundImage = Icons.green_banner;
                    transparentLabel1.Text = "Recognized application";
                }
                else
                {
                    // Unknown app
                    panel1.BackgroundImage = Icons.blue_banner;
                    transparentLabel1.Text = "Unknown application";
                }
                Utils.CenterControlInParent(transparentLabel1);
            }
        }

        internal ApplicationExceptionForm(AppExceptionSettings AppEx)
        {
            InitializeComponent();
            this.Icon = Icons.firewall;

            this.TmpExceptionSettings = AppEx;

            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Width = this.Width;
            panel2.Location = new System.Drawing.Point(0, panel1.Height);
            panel2.Width = this.Width;

            cmbTimer.SuspendLayout();
            foreach (AppExceptionTimer timerVal in Enum.GetValues(typeof(AppExceptionTimer)))
            {
                if (timerVal != AppExceptionTimer.Invalid)
                    cmbTimer.Items.Add(new KeyValuePair<string, AppExceptionTimer>(timerVal.ToString().Replace("_", " "), timerVal));
            }
            cmbTimer.DisplayMember = "Key";
            cmbTimer.ValueMember = "Value";
            cmbTimer.ResumeLayout(true);

            if (!TmpExceptionSettings.Recognized.HasValue)
                TmpExceptionSettings.TryRecognizeApp(true);
        }

        private void ApplicationExceptionForm_Load(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            this.RecognizedApp = TmpExceptionSettings.Recognized.Value;
            txtAppPath.Text = TmpExceptionSettings.ExecutablePath;
            txtSrvName.Text = TmpExceptionSettings.ServiceName;
            for (int i = 0; i < cmbTimer.Items.Count; ++i)
            {
                if (((KeyValuePair<string, AppExceptionTimer>)cmbTimer.Items[i]).Value == TmpExceptionSettings.Timer)
                {
                    cmbTimer.SelectedIndex = i;
                    break;
                }
            }

            listAllProfiles.SuspendLayout();
            listEnabledProfiles.SuspendLayout();

            listAllProfiles.Items.Clear();
            listEnabledProfiles.Items.Clear();


            ProfileCollection profiles = new ProfileCollection();
            ProfileAssoc appFile = null;
            if (System.IO.File.Exists(txtAppPath.Text))
            {
                Application app = GlobalInstances.ProfileMan.KnownApplications.TryGetRecognizedApp(TmpExceptionSettings.ExecutablePath, TmpExceptionSettings.ServiceName, out appFile);
            }

            if (appFile != null)
                profiles = GlobalInstances.ProfileMan.GetProfilesFor(appFile);

            // Add enabled profiles
            listEnabledProfiles.Items.AddRange(TmpExceptionSettings.Profiles);

            // Add disabled profiles
            for (int i = 0; i < profiles.Count; ++i)
            {
                bool alreadyEnabled = listEnabledProfiles.Items.Contains(profiles[i].Name);

                // Only add as available if this profile is not already enabled for the current application
                if (!alreadyEnabled)
                    listAllProfiles.Items.Add(profiles[i].Name);
            }

            // Add available profiles
            profiles = GlobalInstances.ProfileMan.AvailableProfiles;
            for (int i = 0; i < profiles.Count; ++i)
            {
                // Only add as available if this profile is not already enabled for the current application
                bool alreadyEnabled = listEnabledProfiles.Items.Contains(profiles[i].Name);
                bool alreadyAdded = listAllProfiles.Items.Contains(profiles[i].Name);
                bool appSpecific = profiles[i].AppSpecific;
                if (!alreadyEnabled && !alreadyAdded && !appSpecific)
                    listAllProfiles.Items.Add(profiles[i].Name);
            }

            listEnabledProfiles.ResumeLayout(true);
            listAllProfiles.ResumeLayout(true);

            UpdateOKButtonEnabled();
        }

        private void UpdateOKButtonEnabled()
        {
            btnOK.Enabled = System.IO.File.Exists(TmpExceptionSettings.ExecutablePath);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.TmpExceptionSettings.CreationDate = DateTime.Now;
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void listAllProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnAddProfile.Enabled = listAllProfiles.SelectedIndex != -1;
        }

        private void listEnabledProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnRemoveProfile.Enabled = listEnabledProfiles.SelectedIndex != -1;
        }

        private static string MoveItemBetweenLists(ListBox from, ListBox to)
        {
            // Get selected profile
            string profile = from.Items[from.SelectedIndex].ToString();

            // Remove from available
            from.Items.RemoveAt(from.SelectedIndex);

            // Add to enabled
            int newIdx = to.Items.Add(profile);

            // Select same item in new list
            to.SelectedIndex = newIdx;

            return profile;
        }

        private void btnAddProfile_Click(object sender, EventArgs e)
        {
            string profile = MoveItemBetweenLists(listAllProfiles, listEnabledProfiles);
            TmpExceptionSettings.Profiles = Utils.ArrayAddItem(TmpExceptionSettings.Profiles, profile);
        }

        private void btnRemoveProfile_Click(object sender, EventArgs e)
        {
            string profile = MoveItemBetweenLists(listEnabledProfiles, listAllProfiles);
            TmpExceptionSettings.Profiles = Utils.ArrayRemoveItem(TmpExceptionSettings.Profiles, profile);
        }

        private void btnProcess_Click(object sender, EventArgs e)
        {
            TmpExceptionSettings = ProcessesForm.ChooseProcess(this);
            if (TmpExceptionSettings == null) return;

            TmpExceptionSettings.TryRecognizeApp(true);
            UpdateUI();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog(this) != System.Windows.Forms.DialogResult.OK)
                return;

            TmpExceptionSettings.ExecutablePath = ofd.FileName;
            TmpExceptionSettings.ServiceName = string.Empty;
            TmpExceptionSettings.TryRecognizeApp(true);
            UpdateUI();
        }

        private void btnChooseService_Click(object sender, EventArgs e)
        {
            TmpExceptionSettings = ServicesForm.ChooseService(this);
            if (TmpExceptionSettings == null) return;

            TmpExceptionSettings.TryRecognizeApp(true);
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

        private void listAllProfiles_DoubleClick(object sender, EventArgs e)
        {
            if (btnAddProfile.Enabled)
            {
                btnAddProfile_Click(null, null);
            }
        }

        private void listEnabledProfiles_DoubleClick(object sender, EventArgs e)
        {
            if (btnRemoveProfile.Enabled)
            {
                btnRemoveProfile_Click(null, null);
            }
        }

        private void cmbTimer_SelectedIndexChanged(object sender, EventArgs e)
        {
            TmpExceptionSettings.Timer = ((KeyValuePair<string, AppExceptionTimer>)cmbTimer.SelectedItem).Value;
        }

        private void btnAdvSettings_Click(object sender, EventArgs e)
        {
            AppExceptionSettings app = TmpExceptionSettings.Clone() as AppExceptionSettings;
            using (AdvancedExceptionForm aef = new AdvancedExceptionForm(app))
            {
                if (aef.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    TmpExceptionSettings = aef.TmpAppException;
                }
            }
            this.DialogResult = System.Windows.Forms.DialogResult.None;
        }
    }
}
