using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace PKSoft
{
    internal partial class DevelToolForm : Form
    {
        internal DevelToolForm()
        {
            InitializeComponent();
        }

        private void btnAssocBrowse_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                txtAssocExePath.Text = ofd.FileName;
            }
        }

        private void btnAssocCreate_Click(object sender, EventArgs e)
        {
            if (File.Exists(txtAssocExePath.Text))
            {
                AppExceptionAssoc pa = AppExceptionAssoc.FromExecutable(txtAssocExePath.Text, string.Empty);
                string tmpfile = Path.GetTempFileName();
                SerializationHelper.SaveToXMLFile(pa, tmpfile);
                using (StreamReader sr = new StreamReader(tmpfile))
                {
                    txtAssocResult.Text = sr.ReadToEnd();
                }
                File.Delete(tmpfile);
            }
            else
            {
                MessageBox.Show(this, "No such file.", "File not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void btnProfileFolderBrowse_Click(object sender, EventArgs e)
        {
            if (fbd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                txtDBFolderPath.Text = fbd.SelectedPath;
        }

        private void btnCollectionsCreate_Click(object sender, EventArgs e)
        {
            // Common init
            ProfileManager Manager = new ProfileManager();
            Manager.AvailableProfiles.Clear();
            Manager.KnownApplications.Clear();

            string outputPath = txtAssocOutputPath.Text;
            string profilesFolder = Path.Combine(txtDBFolderPath.Text, "Profiles");
            string assocFolder = Path.Combine(txtDBFolderPath.Text, "Associations");
            if (!Directory.Exists(profilesFolder) || !Directory.Exists(assocFolder))
            {
                MessageBox.Show(this, "Profile or Associations folder not found.", "Directory not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // Merge profiles
            string[] files = Directory.GetFiles(profilesFolder);
            foreach (string fpath in files)
            {
                Profile p = SerializationHelper.LoadFromXMLFile<Profile>(fpath);
                Manager.AvailableProfiles.Add(p);
            }

            // Merge associations
            files = Directory.GetFiles(assocFolder);
            foreach (string fpath in files)
            {
                Application app = SerializationHelper.LoadFromXMLFile<PKSoft.Application>(fpath);
                Manager.KnownApplications.Add(app);
            }

            Manager.Save(Path.Combine(outputPath, Path.GetFileName(ProfileManager.DBPath)));
            MessageBox.Show(this, "Creation of collections finished.", "Success.", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnStrongNameBrowse_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    Assembly a = Assembly.ReflectionOnlyLoadFrom(ofd.FileName);
                    txtStrongName.Text = a.FullName;
                }
                catch
                {
                    txtStrongName.Text = "Bad assembly";
                }
            }
        }

        private void btnAssocOutputBrowse_Click(object sender, EventArgs e)
        {
            if (fbd.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
                return;

            txtAssocOutputPath.Text = fbd.SelectedPath;
        }

        private void DevelToolForm_Load(object sender, EventArgs e)
        {
            txtStrongName.Text = Assembly.GetExecutingAssembly().FullName;
        }

        private void btnUpdateInstallerBrowse_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                txtUpdateTWInstaller.Text = ofd.FileName;
            }
        }
        
        private void btnUpdateDatabaseBrowse_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                txtUpdateDatabase.Text = ofd.FileName;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                txtUpdateHosts.Text = ofd.FileName;
            }
        }

        private void btnUpdateOutputBrowse_Click(object sender, EventArgs e)
        {
            fbd.SelectedPath = txtUpdateOutput.Text;
            if (fbd.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
                return;

            txtUpdateOutput.Text = fbd.SelectedPath;
        }

        private void btnUpdateCreate_Click(object sender, EventArgs e)
        {
            const string PLACEHOLDER = "[Unset]";
            const string DB_OUT_NAME = "database.def";
            const string HOSTS_OUT_NAME = "hosts.def";
            const string XML_OUT_NAME = "update.xml";

            string installerFilename = Path.GetFileName(txtUpdateTWInstaller.Text);
            FileVersionInfo installerInfo = FileVersionInfo.GetVersionInfo(txtUpdateTWInstaller.Text);

            UpdateDescriptor update = new UpdateDescriptor();
            update.Modules = new UpdateModule[3];

            update.Modules[0] = new UpdateModule();
            update.Modules[0].Component = "TinyWall";
            update.Modules[0].ComponentVersion = installerInfo.ProductVersion.ToString().Trim();
            update.Modules[0].DownloadHash = Utils.HexEncode(Hasher.HashFile(txtUpdateTWInstaller.Text));
            update.Modules[0].UpdateURL = txtUpdateURL.Text + installerFilename;

            update.Modules[1] = new UpdateModule();
            update.Modules[1].Component = "Database";
            update.Modules[1].ComponentVersion = PLACEHOLDER;
            update.Modules[1].DownloadHash = Utils.HexEncode(Hasher.HashFile(txtUpdateDatabase.Text));
            update.Modules[1].UpdateURL = txtUpdateURL.Text + DB_OUT_NAME;

            update.Modules[2] = new UpdateModule();
            update.Modules[2].Component = "HostsFile";
            update.Modules[2].ComponentVersion = PLACEHOLDER;
            update.Modules[2].DownloadHash = Utils.HexEncode(Hasher.HashFile(txtUpdateHosts.Text));
            update.Modules[2].UpdateURL = txtUpdateURL.Text + HOSTS_OUT_NAME;

            File.Copy(txtUpdateTWInstaller.Text, Path.Combine(txtUpdateOutput.Text, installerFilename), true);

            string dbOut = Path.Combine(txtUpdateOutput.Text, DB_OUT_NAME);
            Utils.CompressDeflate(txtUpdateDatabase.Text, dbOut);

            string hostsOut = Path.Combine(txtUpdateOutput.Text, HOSTS_OUT_NAME);
            Utils.CompressDeflate(txtUpdateHosts.Text, hostsOut);

            string updOut = Path.Combine(txtUpdateOutput.Text, XML_OUT_NAME);
            SerializationHelper.SaveToXMLFile(update, updOut);
            MessageBox.Show(this, "Update created.", "Success.", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
