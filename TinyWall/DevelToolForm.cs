using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using pylorak.Utilities;

namespace pylorak.TinyWall
{
    internal partial class DevelToolForm : Form
    {
        // Key - The primary resource
        // Value - List of satellite resources
        private readonly List<KeyValuePair<string, string[]>> ResXInputs = new();

        internal DevelToolForm()
        {
            System.Windows.Forms.MessageBox.Show(
                "This tool is not meant for end-users. Only use this tool when instructed to do so by the application developer.",
                "Warning: Not for users!",
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation
                );

            InitializeComponent();
        }

        private void btnAssocBrowse_Click(object sender, EventArgs e)
        {
            ofd.Filter = "All files (*)|*";
            if (ofd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                txtAssocExePath.Text = ofd.FileName;
            }
        }

        private void btnAssocCreate_Click(object sender, EventArgs e)
        {
            try
            {
                var result = DevelToolCli.CreateProfile(txtAssocExePath.Text);
                txtAssocResult.Text = result;
            }
            catch (FileNotFoundException)
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
            try
            {
                DevelToolCli.CreateDatabase(txtDBFolderPath.Text, txtAssocOutputPath.Text);
                MessageBox.Show(this, "Creation of collections finished.", "Success.", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (DirectoryNotFoundException ex)
            {
                MessageBox.Show(this, ex.Message, "Directory not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message + "\n\nProfile creation aborted.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAssocOutputBrowse_Click(object sender, EventArgs e)
        {
            if (fbd.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
                return;

            txtAssocOutputPath.Text = fbd.SelectedPath;
        }

        private void btnUpdateInstallerBrowse_Click(object sender, EventArgs e)
        {
            ofd.Filter = "All files (*)|*";
            if (ofd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                txtUpdateInstallerProjectDir.Text = ofd.FileName;
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
            try
            {
                DevelToolCli.CreateUpdate(txtUpdateURL.Text, txtUpdateInstallerProjectDir.Text, txtUpdateOutput.Text);
                MessageBox.Show(this, "Update created.", "Success.", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show(this, $"File or directory\n\n{ex?.FileName ?? "null"}\n\nnot found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (DirectoryNotFoundException ex)
            {
                MessageBox.Show(this, $"File or directory\n\n{ex.Message}\n\nnot found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddPrimaries_Click(object sender, EventArgs e)
        {
            ofd.Filter = "XML resources (*.resx)|*.resx|All files (*)|*";
            ofd.AutoUpgradeEnabled = true;
            ofd.Multiselect = true;
            if (ofd.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
                return;

            for (int i = 0; i < ofd.FileNames.Length; ++i)
            {
                string primary = ofd.FileNames[i];
                if (Path.GetFileName(primary).AsSpan().CountCharOccurrence('.') != 1)
                    continue;

                string dir = Path.GetDirectoryName(primary);
                string primaryBase = Path.GetFileNameWithoutExtension(primary);
                string[] satellites = Directory.GetFiles(dir, primaryBase + ".*.resx", SearchOption.TopDirectoryOnly);
                ResXInputs.Add(new KeyValuePair<string, string[]>(primary, satellites));
            }

            listPrimaries.Items.Clear();
            for (int i = 0; i < ResXInputs.Count; ++i)
                listPrimaries.Items.Add(Path.GetFileName(ResXInputs[i].Key));
        }

        private void listPrimaries_SelectedIndexChanged(object sender, EventArgs e)
        {
            listSatellites.Items.Clear();
            if (listPrimaries.SelectedIndices.Count > 0)
            {
                KeyValuePair<string, string[]> pair = ResXInputs[listPrimaries.SelectedIndex];
                object[] sats = new object[pair.Value.Length];
                for (int i = 0; i < sats.Length; ++i)
                    sats[i] = Path.GetFileName(pair.Value[i]);
                listSatellites.Items.AddRange(sats);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            listPrimaries.Items.Clear();
            listSatellites.Items.Clear();
            ResXInputs.Clear();
        }

        private void btnOptimize_Click(object sender, EventArgs e)
        {
            DevelToolCli.OptimizeResX(ResXInputs, txtOutputPath.Text, false);
            MessageBox.Show(this, "Success.", "ResX Optimizer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnCertBrowse_Click(object sender, EventArgs e)
        {
            ofd.Filter = "All files (*)|*";
            if (File.Exists(txtCert.Text) || Directory.Exists(txtCert.Text))
            {
                ofd.InitialDirectory = Path.GetDirectoryName(txtCert.Text);
            }
            if (ofd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                txtCert.Text = ofd.FileName;
            }
        }

        private void btnSignDir_Click(object sender, EventArgs e)
        {
            fbd.SelectedPath = txtSignDir.Text;
            if (fbd.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
                return;

            txtSignDir.Text = fbd.SelectedPath;
        }

        private void btnBatchSign_Click(object sender, EventArgs e)
        {
            btnBatchSign.Enabled = false;
            try
            {
                bool success = DevelToolCli.BatchSign(
                    txtCert.Text,
                    txtSignDir.Text,
                    txtSigntool.Text,
                    txtTimestampingServ.Text);
                if (success)
                    MessageBox.Show(this, "Files successfully signed.", "Signing result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show(this, "Some files couldn't be signed.", "Signing result", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Signing result", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnBatchSign.Enabled = true;
            }
        }

        private void btnSigntoolBrowse_Click(object sender, EventArgs e)
        {
            ofd.Filter = "Executables (*.exe)|*.exe|All files (*)|*";
            if (ofd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                txtSigntool.Text = ofd.FileName;
            }

        }
    }
}
