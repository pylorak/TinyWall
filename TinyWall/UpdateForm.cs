using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace PKSoft
{

    public partial class UpdateForm : Form
    {
        WebClient HTTPClient = new WebClient();

        public UpdateForm()
        {
            InitializeComponent();
        }

        public static void StartUpdate(IWin32Window owner)
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
                    Application.Exit();
                }
                catch
                {
                    string msg = "Could not elevate privileges necessary for update.";
                    MessageBox.Show(owner, msg, "Update error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        public void StartUpdate()
        {
            // To prevent blocking the UI, we use a thread from the ThreadPool.
            // We use invoke to be able to update controls from the backgorund thread.
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
                        string msg = "There was an error while checking for updates. " + Environment.NewLine +
                        "Please make sure that TinyWall has been granted internet access, then try again.";
                        MessageBox.Show(this, msg, "Update error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        this.Close();
                    });
                    return;
                }

                Utils.Invoke(this, (MethodInvoker)delegate()
                {
                    if (new Version(UpdateModule.Version) > new Version(Application.ProductVersion))
                    {
                        string prompt = "A newer version " + UpdateModule.Version + " of TinyWall is available. Do you want to update now?";
                        if (MessageBox.Show(this, prompt, "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.No)
                        {
                            this.Close();
                            return;
                        }

                        label1.Text = "Downloading update...";
                        progressBar1.Style = ProgressBarStyle.Blocks;

                        string tmpFile = Path.GetTempFileName() + ".exe";
                        Uri UpdateURL = new Uri(UpdateModule.UpdateURL);
                        HTTPClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Updater_DownloadFinished);
                        HTTPClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(Updater_DownloadProgressChanged);
                        HTTPClient.DownloadFileAsync(UpdateURL, tmpFile, tmpFile);
                    }
                    else
                    {
                        string prompt = "You have the newest version of TinyWall. No update necessary.";
                        MessageBox.Show(this, prompt, "TinyWall Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show(this, "Download interrupted.", "Update", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                this.Close();
                return;
            }

            label1.Text = "Starting update...";
            progressBar1.Value = progressBar1.Maximum;
            Message resp = GlobalInstances.CommunicationMan.QueueMessageSimple(TinyWallCommands.STOP_DISABLE);
            if (resp.Command == TinyWallCommands.RESPONSE_LOCKED)
            {
                MessageBox.Show(this, "TinyWall is current locked. Unlock and retry.", "Update error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                this.Close();
                return;
            }

            ThreadPool.QueueUserWorkItem((WaitCallback)delegate(object state)
            {
                System.Threading.Thread.Sleep(2000);
                Utils.StartProcess((string)e.UserState, "/SILENT", true);
                Application.Exit();
            });
        }

        void Updater_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            label2.Text = (e.BytesReceived >> 10).ToString(CultureInfo.InvariantCulture) + "kb/" + (e.TotalBytesToReceive >> 10).ToString(CultureInfo.InvariantCulture) + "kb";
            progressBar1.Maximum = (int)e.TotalBytesToReceive;
            progressBar1.Value = (int)e.BytesReceived;
        }

        private void UpdateForm_Load(object sender, EventArgs e)
        {
            label1.Text = "Please wait while TinyWall checks for available updates.";
            progressBar1.Style = ProgressBarStyle.Marquee;
        }

        private void UpdateForm_Shown(object sender, EventArgs e)
        {
            this.StartUpdate();
        }
    }

}
