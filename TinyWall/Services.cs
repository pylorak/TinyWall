using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Windows.Forms;
using Microsoft.Win32;

namespace pylorak.TinyWall
{
    internal partial class ServicesForm : Form
    {
        private string? SelectedServiceName;
        private string? SelectedServiceExec;

        internal static ServiceSubject? ChooseService(IWin32Window? parent = null)
        {
            using var sf = new ServicesForm();

            if (sf.ShowDialog(parent) == DialogResult.Cancel)
                return null;

            if ((sf.SelectedServiceName is not null) && (sf.SelectedServiceExec is not null))
                return new ServiceSubject(sf.SelectedServiceExec, sf.SelectedServiceName);
            else
                return null;
        }

        internal ServicesForm()
        {
            InitializeComponent();
            Utils.SetRightToLeft(this);
            this.Icon = Resources.Icons.firewall;
            this.btnOK.Image = GlobalInstances.ApplyBtnIcon;
            this.btnCancel.Image = GlobalInstances.CancelBtnIcon;
        }

        private static string GetServiceExecutable(string serviceName)
        {
            string ImagePath = string.Empty;
            using (RegistryKey KeyHKLM = Microsoft.Win32.Registry.LocalMachine)
            {
                using RegistryKey Key = KeyHKLM.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + serviceName);
                ImagePath = (string)Key.GetValue("ImagePath");
            }

            // Remove quotes
            ImagePath = ImagePath.Replace("\"", string.Empty);

            // ImagePath often contains command line arguments.
            // Try to get only the executable path.
            // We use a heuristic approach where we strip off 
            // parts of the string (each delimited by spaces) 
            // one-by-one, each time checking if we have a valid file path.
            while (true)
            {
                if (System.IO.File.Exists(ImagePath))
                    return ImagePath;

                int i = ImagePath.LastIndexOf(' ');
                if (i == -1)
                    break;

                ImagePath = ImagePath.Substring(0, i);
            }

            // Could not find executable path
            return string.Empty;
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
                btnOK_Click(btnOK, EventArgs.Empty);
            }
        }

        private void ServicesForm_Load(object sender, EventArgs e)
        {
            this.Icon = Resources.Icons.firewall;
            if (ActiveConfig.Controller.ServicesFormWindowSize.Width != 0)
                this.Size = ActiveConfig.Controller.ServicesFormWindowSize;
            if (ActiveConfig.Controller.ServicesFormWindowLoc.X != 0)
            {
                this.Location = ActiveConfig.Controller.ServicesFormWindowLoc;
                Utils.FixupFormPosition(this);
            }
            this.WindowState = ActiveConfig.Controller.ServicesFormWindowState;

            foreach (ColumnHeader col in listView.Columns)
            {
                if (ActiveConfig.Controller.ServicesFormColumnWidths.TryGetValue((string)col.Tag, out int width))
                    col.Width = width;
            }

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

            Utils.SetDoubleBuffering(listView, true);
            listView.BeginUpdate();
            listView.ListViewItemSorter = new ListViewItemComparer(0);
            listView.Items.AddRange(itemColl.ToArray());
            listView.EndUpdate();
        }

        private void listView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            var oldSorter = (ListViewItemComparer)listView.ListViewItemSorter;
            var newSorter = new ListViewItemComparer(e.Column);
            if ((oldSorter != null) && (oldSorter.Column == newSorter.Column))
                newSorter.Ascending = !oldSorter.Ascending;

            listView.ListViewItemSorter = newSorter;
        }

        private void ServicesForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ActiveConfig.Controller.ServicesFormWindowState = this.WindowState;
            if (this.WindowState == FormWindowState.Normal)
            {
                ActiveConfig.Controller.ServicesFormWindowSize = this.Size;
                ActiveConfig.Controller.ServicesFormWindowLoc = this.Location;
            }
            else
            {
                ActiveConfig.Controller.ServicesFormWindowSize = this.RestoreBounds.Size;
                ActiveConfig.Controller.ServicesFormWindowLoc = this.RestoreBounds.Location;
            }

            ActiveConfig.Controller.ServicesFormColumnWidths.Clear();
            foreach (ColumnHeader col in listView.Columns)
                ActiveConfig.Controller.ServicesFormColumnWidths.Add((string)col.Tag, col.Width);

            ActiveConfig.Controller.Save();
        }
    }
}
