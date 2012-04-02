using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Drawing;
using System.IO;
using System.Text;
using System.Net;
using System.Windows.Forms;

namespace PKSoft
{

    public partial class UpdateForm : Form
    {
        TinyWallUpdater Updater = new TinyWallUpdater();

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
                Version UpdateVersion = new Version();
                try
                {
                    UpdateVersion = Updater.CheckForNewVersion();
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
                    if (UpdateVersion >= new Version(2, 0))
                    {
                        string prompt = "A new major version of TinyWall is available.\r\n" +
                            "An automatic update procedure is not supported in this case, so please follow the steps below carefully:\r\n" +
                            "\r\n" +
                           "1. Uninstall the current version. Use the Uninstall button in the Manage window, Maintenance tab.\r\n" +
                           "2. Download the latest version from the website. You will be automatically taken to the website after you close this message.\r\n" +
                           "3. Install the latest version of TinyWall by starting the file you downloaded in the previous step.";

                        System.Windows.Forms.MessageBox.Show(prompt, "Update available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Utils.StartProcess(@"http://tinywall.pados.hu", string.Empty, false, true);
                        this.Close();
                        return;
                    }
                    else if (UpdateVersion > new Version(Application.ProductVersion))
                    {
                        string prompt = "A newer version " + UpdateVersion.ToString() + " of TinyWall is available. Do you want to update now?";
                        if (MessageBox.Show(this, prompt, "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.No)
                        {
                            this.Close();
                            return;
                        }

                        label1.Text = "Downloading update...";
                        progressBar1.Style = ProgressBarStyle.Blocks;
                        Updater.DownloadProgressChanged += new TinyWallUpdater.UpdateDownloadProgressChangedEvent(Updater_DownloadProgressChanged);
                        Updater.DownloadFinished += new TinyWallUpdater.UpdateDownloadFinishedEvent(Updater_DownloadFinished);
                        Updater.StartUpdateDownload();
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

        void Updater_DownloadFinished(string file, AsyncCompletedEventArgs e)
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
                Utils.StartProcess(file, "/SILENT", true);
                Application.Exit();
            });
        }

        void Updater_DownloadProgressChanged(DownloadProgressChangedEventArgs e)
        {
            label2.Text = (e.BytesReceived >> 10).ToString() + "kb/" + (e.TotalBytesToReceive >> 10).ToString() + "kb";
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

    public class TinyWallUpdater
    {
        private const int UPDATER_VERSION = 1;
        private const string URL_UPDATE_DESCRIPTOR = @"http://tinywall.pados.hu/updates/UpdVer{0}/updesc.txt";
        private string UpdateDownloadURL;

        public delegate void UpdateDownloadProgressChangedEvent(DownloadProgressChangedEventArgs e);
        public event UpdateDownloadProgressChangedEvent DownloadProgressChanged;
        public delegate void UpdateDownloadFinishedEvent(string file, AsyncCompletedEventArgs e);
        public event UpdateDownloadFinishedEvent DownloadFinished;

        private WebClient HTTPClient;

        public Version CheckForNewVersion()
        {
            string url = string.Format(URL_UPDATE_DESCRIPTOR, UPDATER_VERSION);
            string tmpFile = Path.GetTempFileName();
            HTTPClient = new WebClient();
            HTTPClient.DownloadFile(url, tmpFile);

            using (StreamReader sr = new StreamReader(tmpFile))
            {
                string line = sr.ReadLine();
                if (line != "TinyWall Update Descriptor")
                    throw new ApplicationException("Bad update descriptor file.");

                Version ver = new Version(sr.ReadLine());
                UpdateDownloadURL = sr.ReadLine();
                return ver;
            }
        }

        public void StartUpdateDownload()
        {
            if (string.IsNullOrEmpty(UpdateDownloadURL))
                throw new InvalidOperationException("Download path must first be retrieved by calling CheckForNewVersion().");

            string tmpFile = Path.GetTempFileName() + ".exe";

            HTTPClient = new WebClient();
            HTTPClient.DownloadFileCompleted += new AsyncCompletedEventHandler(HTTPClient_DownloadFileCompleted);
            HTTPClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(HTTPClient_DownloadProgressChanged);

            Uri UpdateURL = new Uri(UpdateDownloadURL);
            HTTPClient.DownloadFileAsync(UpdateURL, tmpFile, tmpFile);
        }

        void HTTPClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.DownloadProgressChanged(e);
        }

        void HTTPClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.DownloadFinished(e.UserState as string, e);
        }
    }
}
