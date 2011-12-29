using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PKSoft
{
    internal partial class ServicesForm : Form
    {
        public string SelectedServiceName;
        public string SelectedServiceExec;

        internal static AppExceptionSettings ChooseService(IWin32Window parent)
        {
            using (ServicesForm sf = new ServicesForm())
            {
                if (sf.ShowDialog(parent) == DialogResult.Cancel)
                    return null;

                AppExceptionSettings ex = new AppExceptionSettings(sf.SelectedServiceExec);
                ex.ServiceName = sf.SelectedServiceName;
                return ex;
            }
        }

        internal ServicesForm()
        {
            InitializeComponent();
            this.Icon = Icons.firewall;
        }

        private static string GetServiceExecutable(string serviceName)
        {
            string ImagePath = string.Empty;
            using (RegistryKey KeyHKLM = Registry.LocalMachine)
            {
                using (RegistryKey Key = KeyHKLM.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\"+serviceName))
                {
                    ImagePath = Key.GetValue("ImagePath") as string;
                }
            }

            // ImagePath often contains command line arguments. Try to get only the executable path.
            // ------------------------

            // Remove quotes
            ImagePath = ImagePath.Replace("\"", string.Empty);

            // Remove args if the begin with "/"
            if (ImagePath.Contains(".exe -"))
            {
                int argpos = ImagePath.IndexOf(".exe -", StringComparison.OrdinalIgnoreCase);
                ImagePath = ImagePath.Substring(0, argpos + 4);
            }

            // Remove args if the begin with "-"
            if (ImagePath.Contains(".exe /"))
            {
                int argpos = ImagePath.IndexOf(".exe /", StringComparison.OrdinalIgnoreCase);
                ImagePath = ImagePath.Substring(0, argpos + 4);
            }

            return ImagePath;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.SelectedServiceName = listView.SelectedItems[0].SubItems[1].Text;
            this.SelectedServiceExec = listView.SelectedItems[0].SubItems[2].Text;
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnOK.Enabled = listView.SelectedItems.Count > 0;
        }

        private void listView_DoubleClick(object sender, EventArgs e)
        {
            if (btnOK.Enabled)
            {
                btnOK_Click(btnOK, null);
            }
        }

        private void ServicesForm_Load(object sender, EventArgs e)
        {
            this.Icon = Icons.firewall;

            List<ListViewItem> itemColl = new List<ListViewItem>();

            ServiceController[] services = ServiceController.GetServices();
            for (int i = 0; i < services.Length; ++i)
            {
                ServiceController srv = services[i];
                try
                {
                    ListViewItem li = new ListViewItem(srv.DisplayName);
                    li.SubItems.Add(srv.ServiceName);
                    li.SubItems.Add(GetServiceExecutable(srv.ServiceName));
                    itemColl.Add(li);
                }
                catch
                {
                }
            }

            listView.SuspendLayout();
            listView.Items.AddRange(itemColl.ToArray());
            listView.ResumeLayout(true);
        }
    }
}
