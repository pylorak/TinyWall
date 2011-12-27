using System;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace PKSoft
{
    public partial class DevelToolForm : Form
    {
        public DevelToolForm()
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
                ProfileAssoc pa = ProfileAssoc.FromExecutable(txtAssocExePath.Text, string.Empty);
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
                ProfileAssocCollection pac = SerializationHelper.LoadFromXMLFile<ProfileAssocCollection>(fpath);
                foreach (ProfileAssoc pa in pac)
                    Manager.KnownApplications.Add(pa);
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
    }
}
