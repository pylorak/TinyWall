using System;
using System.Drawing;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace PKSoft
{
    internal sealed partial class AppFinderForm : Form
    {
        private Thread SearcherThread;
        private bool RunSearch;
        private ManualResetEvent ThreadEndedEvent = new ManualResetEvent(true);
        private Size IconSize = new Size((int)Math.Round(16 * Utils.DpiScalingFactor), (int)Math.Round(16 * Utils.DpiScalingFactor));

        internal ServiceSettings21 TmpSettings;

        internal AppFinderForm(ServiceSettings21 zoneSettings)
        {
            InitializeComponent();
            this.IconList.ImageSize = IconSize;
            this.Icon = Resources.Icons.firewall;
            this.btnCancel.Image = GlobalInstances.CancelBtnIcon;
            this.btnOK.Image = GlobalInstances.ApplyBtnIcon;
            this.btnStartDetection.Image = GlobalInstances.ApplyBtnIcon;
            TmpSettings = zoneSettings;
        }

        private void btnStartDetection_Click(object sender, EventArgs e)
        {
            if (!RunSearch)
            {
                ThreadEndedEvent.Reset();
                btnStartDetection.Text = PKSoft.Resources.Messages.Stop;
                RunSearch = true;
                SearcherThread = new Thread(SearcherWorkerMethod);
                SearcherThread.IsBackground = true;
                SearcherThread.Start();
                this.btnStartDetection.Image = GlobalInstances.CancelBtnIcon;
            }
            else
            {
                btnStartDetection.Text = PKSoft.Resources.Messages.Start;
                RunSearch = false;
                this.btnStartDetection.Image = GlobalInstances.ApplyBtnIcon;
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
                foreach (AppExceptionAssoc appFile in app.FileTemplates)
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
                    lblStatus.Text = PKSoft.Resources.Messages.SearchResults;
                    btnStartDetection.Text = PKSoft.Resources.Messages.Start;
                    btnStartDetection.Image = GlobalInstances.ApplyBtnIcon;
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
                    lblStatus.Text = string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.SearchingPath, path);
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
                        AppExceptionAssoc appFile;
                        Application app = allApps.TryGetRecognizedApp(file, null, out appFile);
                        if ((app != null) && (!app.Special) && (!appFile.IsSigned || appFile.IsSignatureValid))
                        {
                            foreach (AppExceptionAssoc template in app.FileTemplates)
                            {
                                if (!template.ExecutableRealizations.Contains(file))
                                {
                                    template.ExecutableRealizations.Add(appFile.Executable);
                                }
                            } 
                            
                            Utils.Invoke(list, (MethodInvoker)delegate()
                            {
                                AddRecognizedAppToList(app);
                            });
                        }
                    }
                }

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
                string iconPath = app.FileTemplates[0].ExecutableRealizations[0];
                if (!File.Exists(iconPath))
                    IconList.Images.Add(app.Name, Resources.Icons.window);
                else
                    IconList.Images.Add(app.Name, Utils.GetIconContained(iconPath, IconSize.Width, IconSize.Height));
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
                    foreach (AppExceptionAssoc template in app.FileTemplates)
                    {
                        foreach (string execPath in template.ExecutableRealizations)
                        {
                            try
                            {
                                if (AppExceptionAssoc.IsValidExecutablePath(execPath))
                                {
                                    TmpSettings.AppExceptions.Add(template.CreateException(execPath));
                                }
                            }
                            catch (ArgumentException) { }
                        }
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
