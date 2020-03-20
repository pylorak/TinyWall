using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Resources;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using TinyWall.Interface;
using TinyWall.Interface.Internal;

namespace PKSoft
{
    internal partial class DevelToolForm : Form
    {
        // Key - The primary resource
        // Value - List of satellite resources
        private List<KeyValuePair<string, string[]>> ResXInputs = new List<KeyValuePair<string, string[]>>();

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
            if (File.Exists(txtAssocExePath.Text))
            {
                var exe = new ExecutableSubject(txtAssocExePath.Text);
                var id = new DatabaseClasses.SubjectIdentity(exe);
                id.AllowedSha1 = new List<string>();
                id.AllowedSha1.Add(exe.HashSha1);
                if (exe.IsSigned && exe.CertValid)
                {
                    id.CertificateSubjects = new List<string>();
                    id.CertificateSubjects.Add(exe.CertSubject);
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    SerializationHelper.SerializeDC(ms, id);
                    ms.Seek(0, SeekOrigin.Begin);
                    using (StreamReader sr = new StreamReader(ms))
                    {
                        txtAssocResult.Text = sr.ReadToEnd();
                    }
                }
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
            var db = new DatabaseClasses.AppDatabase();

            string outputPath = txtAssocOutputPath.Text;
            string inputPath = txtDBFolderPath.Text;
            if (!Directory.Exists(inputPath))
            {
                MessageBox.Show(this, "Input database folder not found.", "Directory not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            /*
            // Merge profiles
            foreach (string fpath in files)
            {
                Obsolete.Profile p = Deprecated.SerializationHelper.LoadFromXMLFile<Obsolete.Profile>(fpath);
                Manager.AvailableProfiles.Add(p);
            }*/

            string[] files = Directory.GetFiles(inputPath, "*.xml", SearchOption.AllDirectories);
            foreach (string fpath in files)
            {
                try
                {
                    var app = SerializationHelper.LoadFromXMLFile<PKSoft.DatabaseClasses.Application>(fpath);
                    db.KnownApplications.Add(app);
                }
                catch
                {
                    try
                    {
                        var app = Deprecated.SerializationHelper.LoadFromXMLFile<PKSoft.Obsolete.Application>(fpath);
                        //SerializationHelper.SaveToXMLFile(app.ToNewFormat(), fpath + "2");
                        db.KnownApplications.Add(app.ToNewFormat());
                    }
                    catch { }
                }
            }

            db.Save(Path.Combine(outputPath, "profiles.xml"));
            //Manager.ToNewFormat().Save(Path.Combine(outputPath, Path.GetFileName(DatabaseClasses.AppDatabase.DBPath)));
            MessageBox.Show(this, "Creation of collections finished.", "Success.", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnStrongNameBrowse_Click(object sender, EventArgs e)
        {
            ofd.Filter = ".Net binaries (*.exe,*.dll)|*.dll;*.exe|All files (*)|*";
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
            ofd.Filter = "All files (*)|*";
            if (ofd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                txtUpdateTWInstaller.Text = ofd.FileName;
            }
        }

        private void btnUpdateDatabaseBrowse_Click(object sender, EventArgs e)
        {
            ofd.Filter = "All files (*)|*";
            if (ofd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                txtUpdateDatabase.Text = ofd.FileName;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ofd.Filter = "All files (*)|*";
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
            try
            {
                update.Modules[0].ComponentVersion = installerInfo.ProductVersion.ToString().Trim();
            }
            catch
            {
                update.Modules[0].ComponentVersion = "0.0.0";
            }
            update.Modules[0].DownloadHash = Hasher.HashFile(txtUpdateTWInstaller.Text);
            update.Modules[0].UpdateURL = txtUpdateURL.Text + installerFilename;

            update.Modules[1] = new UpdateModule();
            update.Modules[1].Component = "Database";
            update.Modules[1].ComponentVersion = PLACEHOLDER;
            update.Modules[1].DownloadHash = Hasher.HashFile(txtUpdateDatabase.Text);
            update.Modules[1].UpdateURL = txtUpdateURL.Text + DB_OUT_NAME;

            update.Modules[2] = new UpdateModule();
            update.Modules[2].Component = "HostsFile";
            update.Modules[2].ComponentVersion = PLACEHOLDER;
            update.Modules[2].DownloadHash = Hasher.HashFile(txtUpdateHosts.Text);
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

        private static int CountOccurence(string haystack, char needle)
        {
            int count = 0;
            foreach (char c in haystack)
                if (c == needle) count++;

            return count;
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
                if (CountOccurence(Path.GetFileName(primary), '.') != 1)
                    continue;   // This is not a primary at all...

                string dir = Path.GetDirectoryName(primary);
                string primaryBase = Path.GetFileNameWithoutExtension(primary);
                string primaryBasePath = Path.Combine(dir, primaryBase);
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

        private static Dictionary<string, ResXDataNode> ReadResXFile(string filePath)
        {
            Dictionary<string, ResXDataNode> resxContents = new Dictionary<string, ResXDataNode>();
            using (ResXResourceReader resxReader = new ResXResourceReader(filePath))
            {
                resxReader.UseResXDataNodes = true;
                IDictionaryEnumerator dict = resxReader.GetEnumerator();
                while (dict.MoveNext())
                {
                    ResXDataNode node = (ResXDataNode)dict.Value;
                    resxContents.Add(node.Name, node);
                }
            }

            return resxContents;
        }

        private void btnOptimize_Click(object sender, EventArgs e)
        {
            ITypeResolutionService trs = null;

            for (int i = 0; i < ResXInputs.Count; ++i)  // for each main resource file
            {
                KeyValuePair<string, string[]> pair = ResXInputs[i];

                Dictionary<string, ResXDataNode> primary = ReadResXFile(pair.Key);

                for (int s = 0; s < pair.Value.Length; ++s)  // for each localization
                {
                    { // Replace Windows Forms control versions to 2.0.0.0.
                        // (They were often rewritten to 4.0.0.0 wrongly during localization)
                        string a = null;
                        using (StreamReader sr = new StreamReader(pair.Value[s], Encoding.UTF8))
                        {
                            a = sr.ReadToEnd();
                        }
                        a = a.Replace(", Version=4.0.0.0,", ", Version=2.0.0.0,");
                        using (StreamWriter sw = new StreamWriter(pair.Value[s], false, Encoding.UTF8))
                        {
                            sw.Write(a);
                        }
                    }

                    Dictionary<string, ResXDataNode> satellite = ReadResXFile(pair.Value[s]);
                    Dictionary<string, ResXDataNode> newSatellite = new Dictionary<string, ResXDataNode>();

                    // Iterate over all contents of primary.
                    // For each entry, check if one with same name, type and contents is available in
                    // satellite, and if so, don't save it to output.
                    Dictionary<string, ResXDataNode>.Enumerator primaryEnum = primary.GetEnumerator();
                    while (primaryEnum.MoveNext())
                    {
                        ResXDataNode primaryItem = primaryEnum.Current.Value;
                        if (!satellite.ContainsKey(primaryItem.Name))
                            continue;

                        ResXDataNode satelliteItem = satellite[primaryItem.Name];

                        // Only save localized resource if it is different from the default 
                        if (!satelliteItem.GetValue(trs).Equals(primaryItem.GetValue(trs)))
                            newSatellite.Add(satelliteItem.Name, satelliteItem);
                        else
                        {
                        }
                    }

                    // Write output ResX file
                    string outPath = Path.Combine(txtOutputPath.Text, Path.GetFileName(pair.Value[s]));
                    using (ResXResourceWriter resxWriter = new ResXResourceWriter(outPath))
                    {
                        Dictionary<string, ResXDataNode>.Enumerator outputEnum = newSatellite.GetEnumerator();
                        while (outputEnum.MoveNext())
                            resxWriter.AddResource(outputEnum.Current.Value);
                        resxWriter.Generate();
                    }
                } // for each localization
            } // for each primary
        }

        private void btnCertBrowse_Click(object sender, EventArgs e)
        {
            ofd.InitialDirectory = Path.GetDirectoryName(txtCert.Text);
            ofd.Filter = "All files (*)|*";
            if (ofd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                txtCert.Text = ofd.FileName;
            }
        }

        private void btnSignDir_Click(object sender, EventArgs e)
        {
            fbd.SelectedPath = Path.GetDirectoryName(txtCert.Text);
            if (fbd.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
                return;

            txtSignDir.Text = fbd.SelectedPath;
        }

        private void btnBatchSign_Click(object sender, EventArgs e)
        {
            if (!File.Exists(txtCert.Text))
            {
                MessageBox.Show(this, "Certificate not found!");
                return;
            }
            if (!Directory.Exists(txtSignDir.Text))
            {
                MessageBox.Show(this, "Signing directory is invalid!");
                return;
            }
            if (!File.Exists(txtSigntool.Text))
            {
                MessageBox.Show(this, "Signtool.exe not found!");
                return;
            }

            SignFiles(txtSignDir.Text, "*.dll");
            SignFiles(txtSignDir.Text, "*.exe");
            SignFiles(txtSignDir.Text, "*.msi");
            MessageBox.Show(this, "Done signing!");
        }

        private void SignFiles(string dirPath, string filePattern)
        {
            string[] files = Directory.GetFiles(dirPath, filePattern, SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; ++i)
            {
//                string signParams = string.Format("sign /ac C:/Users/Dev/Desktop/scca.crt /ph /f \"{0}\" /p \"{1}\" /d TinyWall /du \"http://tinywall.pados.hu\" /tr \"{2}\" \"{3}\"",
                string signParams =   string.Format("sign /ph /f \"{0}\" /p \"{1}\" /d TinyWall /du \"http://tinywall.pados.hu\" /tr \"{2}\" \"{3}\"",
                        txtCert.Text,
                        txtCertPass.Text,
                        txtTimestampingServ.Text,
                        files[i]);

                // Because signing accesses the timestamping server over the web,
                // we retry a failed signing multiple times to account for
                // internet glitches.
                bool signed = false;
                for (int retry = 0; retry < 3; ++retry)
                {
                    using (Process p = Utils.StartProcess(txtSigntool.Text, signParams, false))
                    {
                        p.WaitForExit();
                        signed = signed || (p.ExitCode == 0);
                    }

                    if (signed)
                        break;

                    System.Threading.Thread.Sleep(1000);
                }
                if (!signed)
                {
                    MessageBox.Show(this, "Failed to sign: " + files[i]);
                    break;
                }
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
