using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using pylorak.Windows;

namespace pylorak.TinyWall
{
    public partial class UwpPackagesForm : Form
    {
        private readonly List<UwpPackage.Package> SelectedPackages = new ();
        private readonly Size IconSize = new ((int)Math.Round(16 * Utils.DpiScalingFactor), (int)Math.Round(16 * Utils.DpiScalingFactor));

        public UwpPackagesForm(bool multiSelect)
        {
            InitializeComponent();
            Utils.SetRightToLeft(this);
            this.listView.MultiSelect = multiSelect;
            this.Icon = Resources.Icons.firewall;
            this.btnOK.Image = GlobalInstances.ApplyBtnIcon;
            this.btnCancel.Image = GlobalInstances.CancelBtnIcon;

            IconList.ImageSize = IconSize;
            IconList.Images.Add("store", Resources.Icons.store);
        }

        internal static List<UwpPackage.Package> ChoosePackage(IWin32Window parent, bool multiSelect)
        {
            using var pf = new UwpPackagesForm(multiSelect);
            var pathList = new List<UwpPackage.Package>();

            if (pf.ShowDialog(parent) == DialogResult.Cancel)
                return pathList;

            pathList.AddRange(pf.SelectedPackages);
            return pathList;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listView.SelectedItems.Count; ++i)
            {
                this.SelectedPackages.Add((UwpPackage.Package)listView.SelectedItems[i].Tag);
            }
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void listView_DoubleClick(object sender, EventArgs e)
        {
            if (btnOK.Enabled)
            {
                btnOK_Click(btnOK, EventArgs.Empty);
            }
        }

        private void UwpPackages_Load(object sender, EventArgs e)
        {
            this.Icon = Resources.Icons.firewall;
            if (ActiveConfig.Controller.UwpPackagesFormWindowSize.Width != 0)
                this.Size = ActiveConfig.Controller.UwpPackagesFormWindowSize;
            if (ActiveConfig.Controller.UwpPackagesFormWindowLoc.X != 0)
            {
                this.Location = ActiveConfig.Controller.UwpPackagesFormWindowLoc;
                Utils.FixupFormPosition(this);
            }
            this.WindowState = ActiveConfig.Controller.UwpPackagesFormWindowState;

            foreach (ColumnHeader col in listView.Columns)
            {
                if (ActiveConfig.Controller.UwpPackagesFormColumnWidths.TryGetValue((string)col.Tag, out int width))
                    col.Width = width;
            }

            var itemColl = new List<ListViewItem>();

            var packages = UwpPackage.GetList();
            foreach (var package in packages)
            {
                // Add list item
                ListViewItem li = new ListViewItem(package.Name);
                li.SubItems.Add(package.PublisherId + ", " + package.Publisher);
                li.ImageKey = "store";
                li.Tag = package;
                itemColl.Add(li);
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

        private void UwpPackages_FormClosing(object sender, FormClosingEventArgs e)
        {
            ActiveConfig.Controller.UwpPackagesFormWindowState = this.WindowState;
            if (this.WindowState == FormWindowState.Normal)
            {
                ActiveConfig.Controller.UwpPackagesFormWindowSize = this.Size;
                ActiveConfig.Controller.UwpPackagesFormWindowLoc = this.Location;
            }
            else
            {
                ActiveConfig.Controller.UwpPackagesFormWindowSize = this.RestoreBounds.Size;
                ActiveConfig.Controller.UwpPackagesFormWindowLoc = this.RestoreBounds.Location;
            }

            ActiveConfig.Controller.UwpPackagesFormColumnWidths.Clear();
            foreach (ColumnHeader col in listView.Columns)
                ActiveConfig.Controller.UwpPackagesFormColumnWidths.Add((string)col.Tag, col.Width);

            ActiveConfig.Controller.Save();
        }

        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnOK.Enabled = listView.SelectedItems.Count > 0;
        }
    }
}
