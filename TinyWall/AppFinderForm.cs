using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Threading;
using System.IO;
using System.Linq;
using System.Text;
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
            foreach (ProfileAssoc app in GlobalInstances.ProfileMan.KnownApplications)
            {
                string extFilter = "*" + Path.GetExtension(app.Executable).ToUpperInvariant();
                if (extFilter != "*")
                    exts.Add(extFilter);
            }

            // Perform search for each path
            foreach (string path in SearchPaths)
            {
                if (!RunSearch)
                    break;

                DoSearchPath(path, exts);
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
        private void DoSearchPath(string path, HashSet<string> exts)
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
                        ProfileAssoc app = GlobalInstances.ProfileMan.TryGetRecognizedApp(file, null);
                        if ((app != null) && (!app.Special))
                        {
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

                    DoSearchPath(dir, exts);
                }
            }
            catch { }
        }

        private void AddRecognizedAppToList(ProfileAssoc app)
        {
            if (!IconList.Images.ContainsKey(app.Executable))
                IconList.Images.Add(app.Executable, Utils.GetIcon(app.Executable, 16, 16));

            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(app.Executable);
            ListViewItem li = new ListViewItem(fvi.ProductName);
            li.ImageKey = app.Executable;
            li.SubItems.Add(Path.GetFileName(app.Executable));
            li.SubItems.Add(app.Executable);
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
                ProfileAssoc app = li.Tag as ProfileAssoc;
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
                    ProfileAssoc app = li.Tag as ProfileAssoc;
                    TmpZoneSettings.AppExceptions = Utils.ArrayAddItem(TmpZoneSettings.AppExceptions, app.ToExceptionSetting());
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
