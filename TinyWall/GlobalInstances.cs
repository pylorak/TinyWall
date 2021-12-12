using System;
using System.Drawing;
using System.Diagnostics.CodeAnalysis;
using pylorak.TinyWall.DatabaseClasses;

namespace pylorak.TinyWall
{
    internal static class GlobalInstances
    {
        [AllowNull]
        internal static AppDatabase AppDatabase;
        [AllowNull]
        internal static Controller Controller;
        internal static Guid ClientChangeset;
        internal static Guid ServerChangeset;

        public static void Cleanup()
        {
            Controller?.Dispose();
        }

        public static void InitClient()
        {
            if (Controller == null)
                Controller = new Controller("TinyWallController");
        }

        [AllowNull]
        private static Bitmap _ApplyBtnIcon = null;
        internal static Bitmap ApplyBtnIcon
        {
            get
            {
                if (null == _ApplyBtnIcon)
                    _ApplyBtnIcon = Utils.ScaleImage(Resources.Icons.accept, Utils.DpiScalingFactor, Utils.DpiScalingFactor);

                return _ApplyBtnIcon;
            }
        }

        [AllowNull]
        private static Bitmap _CancelBtnIcon = null;
        internal static Bitmap CancelBtnIcon
        {
            get
            {
                if (null == _CancelBtnIcon)
                    _CancelBtnIcon = Utils.ScaleImage(Resources.Icons.cancel, Utils.DpiScalingFactor, Utils.DpiScalingFactor);

                return _CancelBtnIcon;
            }
        }

        [AllowNull]
        private static Bitmap _UninstallBtnIcon = null;
        internal static Bitmap UninstallBtnIcon
        {
            get
            {
                if (null == _UninstallBtnIcon)
                    _UninstallBtnIcon = Utils.ScaleImage(Resources.Icons.uninstall, Utils.DpiScalingFactor, Utils.DpiScalingFactor);

                return _UninstallBtnIcon;
            }
        }

        [AllowNull]
        private static Bitmap _AddBtnIcon = null;
        internal static Bitmap AddBtnIcon
        {
            get
            {
                if (null == _AddBtnIcon)
                    _AddBtnIcon = Utils.ScaleImage(Resources.Icons.add, Utils.DpiScalingFactor, Utils.DpiScalingFactor);

                return _AddBtnIcon;
            }
        }

        [AllowNull]
        private static Bitmap _ModifyBtnIcon = null;
        internal static Bitmap ModifyBtnIcon
        {
            get
            {
                if (null == _ModifyBtnIcon)
                    _ModifyBtnIcon = Utils.ScaleImage(Resources.Icons.modify, Utils.DpiScalingFactor, Utils.DpiScalingFactor);

                return _ModifyBtnIcon;
            }
        }

        [AllowNull]
        private static Bitmap _RemoveBtnIcon = null;
        internal static Bitmap RemoveBtnIcon
        {
            get
            {
                if (null == _RemoveBtnIcon)
                    _RemoveBtnIcon = Utils.ScaleImage(Resources.Icons.remove, Utils.DpiScalingFactor, Utils.DpiScalingFactor);

                return _RemoveBtnIcon;
            }
        }

        [AllowNull]
        private static Bitmap _SubmitBtnIcon = null;
        internal static Bitmap SubmitBtnIcon
        {
            get
            {
                if (null == _SubmitBtnIcon)
                    _SubmitBtnIcon = Utils.ScaleImage(Resources.Icons.submit, Utils.DpiScalingFactor, Utils.DpiScalingFactor);

                return _SubmitBtnIcon;
            }
        }

        [AllowNull]
        private static Bitmap _ImportBtnIcon = null;
        internal static Bitmap ImportBtnIcon
        {
            get
            {
                if (null == _ImportBtnIcon)
                    _ImportBtnIcon = Utils.ScaleImage(Resources.Icons.import, Utils.DpiScalingFactor, Utils.DpiScalingFactor);

                return _ImportBtnIcon;
            }
        }

        [AllowNull]
        private static Bitmap _ExportBtnIcon = null;
        internal static Bitmap ExportBtnIcon
        {
            get
            {
                if (null == _ExportBtnIcon)
                    _ExportBtnIcon = Utils.ScaleImage(Resources.Icons.export, Utils.DpiScalingFactor, Utils.DpiScalingFactor);

                return _ExportBtnIcon;
            }
        }

        [AllowNull]
        private static Bitmap _UpdateBtnIcon = null;
        internal static Bitmap UpdateBtnIcon
        {
            get
            {
                if (null == _UpdateBtnIcon)
                    _UpdateBtnIcon = Utils.ScaleImage(Resources.Icons.update, Utils.DpiScalingFactor, Utils.DpiScalingFactor);

                return _UpdateBtnIcon;
            }
        }

        [AllowNull]
        private static Bitmap _WebBtnIcon = null;
        internal static Bitmap WebBtnIcon
        {
            get
            {
                if (null == _WebBtnIcon)
                    _WebBtnIcon = Utils.ScaleImage(Resources.Icons.web, Utils.DpiScalingFactor, Utils.DpiScalingFactor);

                return _WebBtnIcon;
            }
        }
    }
}
