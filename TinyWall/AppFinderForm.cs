using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace PKSoft
{
    internal partial class AppFinderForm : Form
    {
        private Thread SearcherThread;
        private bool RunSearch;
        private ManualResetEvent ThreadEndedEvent = new ManualResetEvent(true);

        internal ZoneSettings TmpZoneSettings;

        internal AppFinderForm(ZoneSettings zoneSettings)
        {
            InitializeComponent();
            this.Icon = Icons.firewall;
            TmpZoneSettings = zoneSettings;
        }

        private void btnStartDetection_Click(object sender, EventArgs e)
        {
            if (!RunSearch)
            {
                ThreadEndedEvent.Reset();
                btnStartDetection.Text = "Stop";
                RunSearch = true;
                SearcherThread = new Thread(SearcherWorkerMethod);
                SearcherThread.IsBackground = true;
                SearcherThread.Start();
                btnStartDetection.Image = Icons.cancel;
            }
            else
            {
                btnStartDetection.Text = "Start";
                RunSearch = false;
                btnStartDetection.Image = Icons.accept;
            }
        }

        private void SearcherWorkerMethod()
        {
            // Clear list
            Utils.Invoke(list, (MethodInvoker)delegate()
            {
                list.Items.Clear();
            });

            ApplicationCollection allApps = Utils.DeepClone(GlobalInstances.ProfileMan.KnownApplications);

            // List of all possible paths to search
            string[] SearchPaths = new string[]{
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Utils.ProgramFilesx86(),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            };

            // Make sure we do not search the same path twice
            SearchPaths = SearchPaths.Distinct().ToArray();

            // Construct a list of all file extensions we are looking for
            HashSet<string> exts = new HashSet<string>();
            foreach (Application app in allApps)
            {
                foreach (ProfileAssoc appFile in app.FileTemplates)
                {
                    string extFilter = "*" + Path.GetExtension(appFile.Executable).ToUpperInvariant();
                    if (extFilter != "*")
                        exts.Add(extFilter);
                }
            }

            // Perform search for each path
            foreach (string path in SearchPaths)
            {
                if (!RunSearch)
                    break;

                DoSearchPath(path, exts, allApps);
            }

            try
            {
                // Update status
                ThreadEndedEvent.Set();
                RunSearch = false;
                Utils.Invoke(list, (MethodInvoker)delegate()
                {
                    lblStatus.Text = "Search results:";
                    btnStartDetection.Text = "Start";
                    btnStartDetection.Image = Icons.accept;
                });
            }
            catch (ThreadInterruptedException)
            { }
        }

        DateTime LastEnterDoSearchPath = DateTime.Now;
        private void DoSearchPath(string path, HashSet<string> exts, ApplicationCollection allApps)
        {
            #region Update user feedback periodically
            DateTime now = DateTime.Now;
            if (now - LastEnterDoSearchPath > TimeSpan.FromMilliseconds(500))
            {
                LastEnterDoSearchPath = now;
                Utils.Invoke(list, (MethodInvoker)delegate()
                {
                    lblStatus.Text = "Searching: " + path;
                });
            }
            #endregion

            try
            {
                // Inspect all interesting extensions in the current directory
                foreach (string extFilter in exts)
                {
                    string[] files = Directory.GetFiles(path, extFilter, SearchOption.TopDirectoryOnly);
                    foreach (string file in files)
                    {
                        // Abort if asked to
                        if (!RunSearch)
                            break;

                        // Try to match file
                        ProfileAssoc appFile;
                        Application app = allApps.TryGetRecognizedApp(file, null, out appFile);
                        if ((app != null) && (!app.Special))
                        {
                            if (!app.FileRealizations.ContainsFileRealization(file))
                            {
                                app.FileRealizations.Add(appFile);
                            }
                            Utils.Invoke(list, (MethodInvoker)delegate()
                            {
                                AddRecognizedAppToList(app);
                            });
                        }
                    }
                }
            }
            catch { }

            try
            {
                // Recurse into subdirectories
                string[] dirs = Directory.GetDirectories(path);
                foreach (string dir in dirs)
                {
                    // Abort if asked to
                    if (!RunSearch)
                        break;

                    DoSearchPath(dir, exts, allApps);
                }
            }
            catch { }
        }

        private void AddRecognizedAppToList(Application app)
        {
            // Check if we've already added this application
            for (int i = 0; i < list.Items.Count; ++i)
            {
                if ((list.Items[i].Tag as Application).Name.Equals(app.Name))
                    return;
            }

            if (!IconList.Images.ContainsKey(app.Name))
            {
                string iconPath = app.FileRealizations[0].Executable;
                if (!File.Exists(iconPath))
                    IconList.Images.Add(app.Name, Icons.window);
                else
                    IconList.Images.Add(app.Name, Utils.GetIcon(iconPath, 16, 16));
            }

            ListViewItem li = new ListViewItem(app.Name);
            li.ImageKey = app.Name;
            li.Tag = app;
            if (app.Recommended)
                li.Checked = true;

            list.Items.Add(li);
        }

        private void WaitForThread()
        {
            RunSearch = false;
            ThreadEndedEvent.WaitOne();
            if (SearcherThread != null)
                SearcherThread.Interrupt();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            WaitForThread();
            this.DialogResult = DialogResult.Cancel;
        }

        private void btnSelectImportant_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem li in list.Items)
            {
                Application app = li.Tag as Application;
                if (app.Recommended)
                    li.Checked = true;
            }
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem li in list.Items)
            {
                li.Checked = true;
            }
        }

        private void btnDeselectAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem li in list.Items)
            {
                li.Checked = false;
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            WaitForThread();

            // Populate settings
            foreach (ListViewItem li in list.Items)
            {
                if (li.Checked)
                {
                    Application app = li.Tag as Application;
                    foreach (ProfileAssoc pa in app.FileRealizations)
                    {
                        try
                        {
                            if (ProfileAssoc.IsValidExecutablePath(pa.Executable))
                                TmpZoneSettings.AppExceptions = Utils.ArrayAddItem(TmpZoneSettings.AppExceptions, pa.ToExceptionSetting());
                        }
                        catch (ArgumentException) { }
                    }
                }
            }

            this.DialogResult = DialogResult.OK;
        }

        private void AppFinderForm_Shown(object sender, EventArgs e)
        {
            this.Activate();
            this.BringToFront();
            btnStartDetection_Click(btnStartDetection, null);
        }
    }
}
