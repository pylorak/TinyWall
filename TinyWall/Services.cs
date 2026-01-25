using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace pylorak.TinyWall
{
    internal partial class ServicesForm : Form
    {
        private string? _selectedServiceName;
        private string? _selectedServiceExec;
        private string _searchItem = string.Empty;

        internal static ServiceSubject? ChooseService(IWin32Window? parent = null)
        {
            using var sf = new ServicesForm();

            if (sf.ShowDialog(parent) == DialogResult.Cancel)
                return null;

            if ((sf._selectedServiceName is not null) && (sf._selectedServiceExec is not null))
                return new ServiceSubject(sf._selectedServiceExec, sf._selectedServiceName);
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
            string imagePath;
            using (RegistryKey keyHklm = Registry.LocalMachine)
            {
                using RegistryKey key = keyHklm.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + serviceName) ??
                                        throw new InvalidOperationException();
                imagePath = (string)key.GetValue("ImagePath");
            }

            // Remove quotes
            imagePath = imagePath.Replace("\"", string.Empty);

            // ImagePath often contains command line arguments.
            // Try to get only the executable path.
            // We use a heuristic approach where we strip off
            // parts of the string (each delimited by spaces)
            // one-by-one, each time checking if we have a valid file path.
            while (true)
            {
                if (System.IO.File.Exists(imagePath))
                    return imagePath;

                int i = imagePath.LastIndexOf(' ');
                if (i == -1)
                    break;

                imagePath = imagePath.Substring(0, i);
            }

            // Could not find executable path
            return string.Empty;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this._selectedServiceName = listView.SelectedItems[0].SubItems[1].Text;
            this._selectedServiceExec = listView.SelectedItems[0].SubItems[2].Text;
            this.DialogResult = DialogResult.OK;
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

        private async void ServicesForm_Load(object sender, EventArgs e)
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

            await UpdateListAsync();
        }

        private async Task UpdateListAsync()
        {
            // Load column widths ahead of time to avoid accessing UI thread during background processing
            Dictionary<string, int> columnWidths = new Dictionary<string, int>();
            foreach (ColumnHeader col in listView.Columns)
            {
                if (ActiveConfig.Controller.ServicesFormColumnWidths.TryGetValue((string)col.Tag, out int width))
                    columnWidths[(string)col.Tag] = width;
            }

            var itemColl = await Task.Run(() => {
                var items = new List<ListViewItem>();

                ServiceController[] services = ServiceController.GetServices();

                if (!string.IsNullOrWhiteSpace(_searchItem))
                    services = services.Where(s =>
                        s.ServiceName.ToLower().Contains(_searchItem.ToLower())
                        || s.DisplayName.ToLower().Contains(_searchItem.ToLower())
                    ).ToArray();

                foreach (var srv in services)
                {
                    // Check if we need to cancel the operation
                    if (!this.IsHandleCreated) break;
                    
                    try
                    {
                        var li = new ListViewItem(srv.DisplayName);
                        li.SubItems.Add(srv.ServiceName);
                        li.SubItems.Add(GetServiceExecutable(srv.ServiceName));
                        items.Add(li);
                    }
                    catch
                    {
                        // ignored
                    }
                }

                return items;
            });

            Utils.Invoke(this, () => {
                // Apply column widths
                foreach (ColumnHeader col in listView.Columns)
                {
                    if (columnWidths.TryGetValue((string)col.Tag, out int width))
                        col.Width = width;
                }

                Utils.SetDoubleBuffering(listView, true);
                listView.BeginUpdate();
                listView.ListViewItemSorter = new ListViewItemComparer(0);

                listView.Items.Clear();
                listView.Items.AddRange(itemColl.ToArray());
                listView.EndUpdate();
            });
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

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBxSearch.Text))
            {
                return;
            }

            _searchItem = txtBxSearch.Text.ToLower();
            await UpdateListAsync();
        }

        private async void btnClear_Click(object sender, EventArgs e)
        {
            _searchItem = string.Empty;
            txtBxSearch.Text = string.Empty;

            await UpdateListAsync();
        }

        private void txtBxSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Enter or Keys.Return)
            {
                btnSearch.PerformClick();
            }
        }
    }
}
