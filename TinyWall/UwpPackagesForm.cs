using DarkModeForms;
using Microsoft.Samples.TaskDialog;
using pylorak.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace pylorak.TinyWall
{
    public partial class UwpPackagesForm : Form
    {
        private readonly List<UwpPackageList.Package> SelectedPackages = new();
        private readonly Size IconSize = new((int)Math.Round(16 * Utils.DpiScalingFactor), (int)Math.Round(16 * Utils.DpiScalingFactor));
        private readonly DarkModeCS? DarkMode;
        private readonly WmPaintFilter? ListRepaintFilter;

        public UwpPackagesForm(bool multiSelect)
        {
            InitializeComponent();
            Utils.SetRightToLeft(this);
            if (Utils.IsDarkModeActive(ActiveConfig.Controller))
            {
                this.DarkMode = new(this, false) { ColorMode = DarkModeCS.DisplayMode.DarkMode };
                this.ListRepaintFilter = new WmPaintFilter(listView);
            }
            this.listView.MultiSelect = multiSelect;
            this.Icon = Resources.Icons.firewall;
            this.btnOK.Image = GlobalInstances.ApplyBtnIcon;
            this.btnCancel.Image = GlobalInstances.CancelBtnIcon;

            IconList.ImageSize = IconSize;
            IconList.Images.Add("store", Resources.Icons.store);
        }

        internal static List<UwpPackageList.Package> ChoosePackage(IWin32Window parent, bool multiSelect)
        {
            using var pf = new UwpPackagesForm(multiSelect);
            var pathList = new List<UwpPackageList.Package>();

            return (pf.ShowDialog(parent) == DialogResult.Cancel) ? new List<UwpPackageList.Package>() : pf.SelectedPackages;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            var selection = new List<UwpPackageList.Package>();
            for (int i = 0; i < listView.SelectedItems.Count; ++i)
            {
                selection.Add((UwpPackageList.Package)listView.SelectedItems[i].Tag);
            }

            // A rule for a package matches on the AppContainer token of its processes. Packaged
            // desktop applications run with full trust and have no such token, so an exception
            // for one would be accepted here and then silently never match anything. Say so
            // instead, and point at the exception type that does work for them.
            var fullTrust = new List<string>();
            foreach (var package in selection)
            {
                if (UwpPackageList.GetFullTrustState(package.FamilyName) == UwpPackageList.FullTrustState.Yes)
                    fullTrust.Add(package.Name);
            }

            if (fullTrust.Count > 0)
            {
                string prompt = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Messages.FullTrustPackageWarning,
                    string.Join(", ", fullTrust.ToArray()));

                if (Utils.ShowMessageBox(prompt, Resources.Messages.TinyWall, TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No, TaskDialogIcon.Warning, this) != System.Windows.Forms.DialogResult.Yes)
                    return;
            }

            this.SelectedPackages.AddRange(selection);
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
            var packageList = new UwpPackageList();
            foreach (var package in packageList)
            {
                // Add list item
                var li = new ListViewItem(package.Name);
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
