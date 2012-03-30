using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace PKSoft
{

    internal partial class UpdateForm : Form
    {
        WebClient HTTPClient = new WebClient();

        internal UpdateForm()
        {
            InitializeComponent();
        }

        internal static void StartUpdate(IWin32Window owner)
        {
            if (Utils.RunningAsAdmin())
            {
                using (UpdateForm uf = new UpdateForm())
                {
                    uf.ShowDialog(owner);
                }
            }
            else
            {
                try
                {
                    Utils.StartProcess(Utils.ExecutablePath, "/updatenow", true);
                    System.Windows.Forms.Application.Exit();
                }
                catch
                {
                    string msg = PKSoft.Resources.Messages.CouldNotElevatePrivilegesForUpdate;
                    MessageBox.Show(owner, msg, PKSoft.Resources.Messages.TinyWallUpdate, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        internal void StartUpdate()
        {
            // To prevent blocking the UI, we use a thread from the ThreadPool.
            // We use invoke to be able to update controls from the background thread.
            ThreadPool.QueueUserWorkItem((WaitCallback)delegate(object state)
            {
                UpdateModule UpdateModule = null;
                try
                {
                    UpdateModule = UpdateChecker.GetMainAppModule(UpdateChecker.GetDescriptor());
                }
                catch
                {
                    Utils.Invoke(this, (MethodInvoker)delegate()
                    {
                        string msg = PKSoft.Resources.Messages.ErrorCheckingForUpdates;
                        MessageBox.Show(this, msg, PKSoft.Resources.Messages.TinyWallUpdate, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        this.Close();
                    });
                    return;
                }

                Utils.Invoke(this, (MethodInvoker)delegate()
                {
                    Version oldVersion = new Version(System.Windows.Forms.Application.ProductVersion);
                    Version newVersion = new Version(UpdateModule.ComponentVersion);
                    if (newVersion > oldVersion)
                    {
                        string prompt = string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.UpdateAvailable, UpdateModule.ComponentVersion);
                        if (MessageBox.Show(this, prompt, PKSoft.Resources.Messages.TinyWallUpdate, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.No)
                        {
                            this.Close();
                            return;
                        }

                        label1.Text = PKSoft.Resources.Messages.DownloadingUpdate;
                        progressBar1.Style = ProgressBarStyle.Blocks;

                        string tmpFile = Path.GetTempFileName() + ".exe";
                        Uri UpdateURL = new Uri(UpdateModule.UpdateURL);
                        HTTPClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Updater_DownloadFinished);
                        HTTPClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(Updater_DownloadProgressChanged);
                        HTTPClient.DownloadFileAsync(UpdateURL, tmpFile, tmpFile);
                    }
                    else
                    {
                        string prompt = PKSoft.Resources.Messages.NoUpdateAvailable;
                        MessageBox.Show(this, prompt, PKSoft.Resources.Messages.TinyWallUpdate, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Close();
                    }
                });

            }
            );
        }

        void Updater_DownloadFinished(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled || (e.Error != null))
            {
                MessageBox.Show(this, PKSoft.Resources.Messages.DownloadInterrupted, PKSoft.Resources.Messages.TinyWallUpdate, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                this.Close();
                return;
            }

            label1.Text = PKSoft.Resources.Messages.StartingUpdate;
            progressBar1.Value = progressBar1.Maximum;
            Message resp = GlobalInstances.CommunicationMan.QueueMessageSimple(TWControllerMessages.STOP_DISABLE);
            if (resp.Command == TWControllerMessages.RESPONSE_LOCKED)
            {
                MessageBox.Show(this, PKSoft.Resources.Messages.TinyWallIsCurrentlyLocked, PKSoft.Resources.Messages.TinyWallUpdate, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                this.Close();
                return;
            }

            ThreadPool.QueueUserWorkItem((WaitCallback)delegate(object state)
            {
                System.Threading.Thread.Sleep(2000);
                Utils.StartProcess((string)e.UserState, "/SILENT", true);
                System.Windows.Forms.Application.Exit();
            });
        }

        void Updater_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            label2.Text = string.Format(CultureInfo.InvariantCulture, "{0}KiB/{1}KiB", (e.BytesReceived >> 10).ToString(CultureInfo.InvariantCulture), (e.TotalBytesToReceive >> 10).ToString(CultureInfo.InvariantCulture));
            progressBar1.Maximum = (int)e.TotalBytesToReceive;
            progressBar1.Value = (int)e.BytesReceived;
        }

        private void UpdateForm_Load(object sender, EventArgs e)
        {
            label1.Text = PKSoft.Resources.Messages.PleaseWaitWhileTinyWallChecksForUpdates;
            progressBar1.Style = ProgressBarStyle.Marquee;
        }

        private void UpdateForm_Shown(object sender, EventArgs e)
        {
            this.StartUpdate();
        }
    }

}
