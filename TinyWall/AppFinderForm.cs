using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace pylorak.TinyWall
{
    internal sealed partial class AppFinderForm : Form
    {
        private Thread? _searcherThread;
        private bool _runSearch;
        private Size _iconSize = new((int)Math.Round(16 * Utils.DpiScalingFactor), (int)Math.Round(16 * Utils.DpiScalingFactor));
        private DateTime _lastEnterDoSearchPath = DateTime.Now;
        private readonly SearchResults _searchResult = new();

        internal List<FirewallExceptionV3> SelectedExceptions { get; } = new List<FirewallExceptionV3>();

        internal AppFinderForm()
        {
            InitialiseComponent();
            Utils.SetRightToLeft(this);
            IconList.ImageSize = _iconSize;
            Icon = Resources.Icons.firewall;
            btnCancel.Image = GlobalInstances.CancelBtnIcon;
            btnOK.Image = GlobalInstances.ApplyBtnIcon;
            btnStartDetection.Image = GlobalInstances.ApplyBtnIcon;

            btnSelectImportant.Visible = false;
        }

        private void BtnStartDetection_Click(object sender, EventArgs e)
        {
            if (!_runSearch)
            {
                btnStartDetection.Text = Resources.Messages.Stop;
                btnStartDetection.Image = GlobalInstances.CancelBtnIcon;
                list.Items.Clear();

                _runSearch = true;
                _searcherThread = new Thread(SearcherWorkerMethod)
                {
                    Name = "AppFinder",
                    IsBackground = true
                };
                _searcherThread.Start();
            }
            else
            {
                btnStartDetection.Enabled = false;
                _runSearch = false;
            }
        }

        private sealed class SearchResults
        {
            private readonly Dictionary<DatabaseClasses.Application, List<ExecutableSubject>> _list = new();

            public void Clear()
            {
                _list.Clear();
            }

            public void AddEntry(DatabaseClasses.Application app, ExecutableSubject resolvedSubject)
            {
                if (!_list.ContainsKey(app))
                    _list.Add(app, new List<ExecutableSubject>());

                var subjList = _list[app];
                if (subjList.Any(subj => subj.Equals(resolvedSubject)))
                {
                    return;
                }

                _list[app].Add(resolvedSubject);
            }

            public List<DatabaseClasses.Application> GetFoundApps()
            {
                List<DatabaseClasses.Application> ret = new();
                ret.AddRange(_list.Keys);
                return ret;
            }

            public List<ExecutableSubject> GetFoundComponents(DatabaseClasses.Application app)
            {
                return _list[app];
            }
        }

        private void SearcherWorkerMethod()
        {
            _searchResult.Clear();

            foreach (DatabaseClasses.Application app in GlobalInstances.AppDatabase!.KnownApplications)
            {
                if (app.HasFlag("TWUI:Special"))
                    continue;

                foreach (var subject in app.Components.Select(id => id.SearchForFile()).SelectMany(subjects => subjects))
                {
                    if (subject is not ExecutableSubject exe) continue;

                    _searchResult.AddEntry(app, exe);
                    BeginInvoke((MethodInvoker)delegate
                    {
                        AddRecognisedAppToList(app, exe.ExecutablePath);
                    });
                }
            }

            // ------------------------------------
            //      And now do a slow search
            // ------------------------------------

            // List of all possible paths to search
            string[] searchPaths = new string[]{
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Utils.ProgramFilesx86(),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            };

            // Make sure we do not search the same path twice
            searchPaths = searchPaths.Distinct().ToArray();

            // Construct a list of all file extensions we are looking for
            var exts = new HashSet<string>();
            foreach (var subjTemplate in GlobalInstances.AppDatabase.KnownApplications.SelectMany(app => app.Components))
            {
                if (subjTemplate.Subject is not ExecutableSubject exesub) continue;

                var extFilter = "*" + Path.GetExtension(exesub.ExecutableName).ToUpperInvariant();
                if (extFilter != "*")
                    exts.Add(extFilter);
            }

            // Perform search for each path
            foreach (string path in searchPaths)
            {
                if (!_runSearch)
                    break;

                DoSearchPath(path, exts, GlobalInstances.AppDatabase);
            }

            try
            {
                // Update status
                _runSearch = false;
                BeginInvoke((MethodInvoker)delegate
                {
                    try
                    {
                        lblStatus.Text = Resources.Messages.SearchResults;
                        btnStartDetection.Text = Resources.Messages.Start;
                        btnStartDetection.Image = GlobalInstances.ApplyBtnIcon;
                        btnStartDetection.Enabled = true;
                    }
                    catch
                    {
                        // Ignore if the form was already disposed
                    }
                });
            }
            catch (ThreadInterruptedException)
            { }
        }

        private void DoSearchPath(string path, HashSet<string> exts, DatabaseClasses.AppDatabase db)
        {
            #region Update user feedback periodically
            DateTime now = DateTime.Now;
            if (now - _lastEnterDoSearchPath > TimeSpan.FromMilliseconds(500))
            {
                _lastEnterDoSearchPath = now;
                BeginInvoke((MethodInvoker)delegate
                {
                    lblStatus.Text = string.Format(CultureInfo.CurrentCulture, Resources.Messages.SearchingPath, path);
                });
            }
            #endregion

            try
            {
                // Inspect all interesting extensions in the current directory
                foreach (var files in exts.Select(extFilter => Directory.GetFiles(path, extFilter, SearchOption.TopDirectoryOnly)))
                {
                    foreach (var file in files)
                    {
                        // Abort if asked to
                        if (!_runSearch)
                            break;

                        // Try to match file
                        ExecutableSubject subject = (ExecutableSubject)ExceptionSubject.Construct(file);
                        DatabaseClasses.Application? app = db.TryGetApp(subject, out _, false);
                        if ((app != null) && (!subject.IsSigned || subject.CertValid))
                        {
                            _searchResult.AddEntry(app, subject);

                            // We have a match. This file belongs to a known application!
                            BeginInvoke((MethodInvoker)delegate
                            {
                                AddRecognisedAppToList(app, subject.ExecutablePath);
                            });
                        }
                    }
                }

                // Recurse into subdirectories
                string[] dirs = Directory.GetDirectories(path);
                foreach (string dir in dirs)
                {
                    // Abort if asked to
                    if (!_runSearch)
                        break;

                    DoSearchPath(dir, exts, db);
                }
            }
            catch
            {
                // ignored
            }
        }

        private void AddRecognisedAppToList(DatabaseClasses.Application app, string path)
        {
            // Check if we've already added this application
            for (int i = 0; i < list.Items.Count; ++i)
            {
                if (((DatabaseClasses.Application)list.Items[i].Tag).Name.Equals(app.Name))
                    return;
            }

            if (!IconList.Images.ContainsKey(app.Name))
            {
                var iconPath = path;
                IconList.Images.Add(app.Name,
                    !File.Exists(iconPath)
                        ? Resources.Icons.window
                        : Utils.GetIconContained(iconPath, _iconSize.Width, _iconSize.Height));
            }

            var li = new ListViewItem(app.Name)
            {
                ImageKey = app.Name,
                Tag = app,
                Checked = app.HasFlag("TWUI:Recommended")
            };

            list.Items.Add(li);
        }

        private void WaitForThread()
        {
            _runSearch = false;
            _searcherThread?.Join();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            WaitForThread();
            DialogResult = DialogResult.Cancel;
        }

        private void BtnSelectImportant_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem li in list.Items)
            {
                var app = (DatabaseClasses.Application)li.Tag;
                if (app.HasFlag("TWUI:Recommended"))
                    li.Checked = true;
            }
        }

        private void BtnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem li in list.Items)
            {
                li.Checked = true;
            }
        }

        private void BtnDeselectAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem li in list.Items)
            {
                li.Checked = false;
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            WaitForThread();

            // Populate settings
            foreach (ListViewItem li in list.Items)
            {
                if (!li.Checked) continue;
                var app = (DatabaseClasses.Application)li.Tag;
                var appFoundFiles = _searchResult.GetFoundComponents(app);
                foreach (ExecutableSubject subject in appFoundFiles)
                {
                    app = GlobalInstances.AppDatabase!.TryGetApp(subject, out FirewallExceptionV3? fwex, false);
                    if ((fwex != null) && (app != null) && (!subject.IsSigned || subject.CertValid))
                    {
                        SelectedExceptions.Add(fwex);
                    }
                }
            }

            DialogResult = DialogResult.OK;
        }

        private void AppFinderForm_Shown(object sender, EventArgs e)
        {
            Activate();
            BringToFront();
            BtnStartDetection_Click(btnStartDetection, EventArgs.Empty);
            Utils.SetDoubleBuffering(list, true);
        }
    }
}
