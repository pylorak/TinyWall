using Microsoft.Win32;
using System.Drawing;
using System;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml.Linq;


namespace DarkModeForms
{
    /// <summary>This tries to automatically apply Windows Dark Mode (if enabled) to a Form.
    /// <para>Author: BlueMystic (bluemystic.play@gmail.com)  2024</para>
    /// Source <see href="https://github.com/BlueMystical/Dark-Mode-Forms"/>
    /// </summary>
    public class DarkModeCS
    {
        #region Win32 API Declarations

        public struct DWMCOLORIZATIONcolors
        {
            public uint ColorizationColor,
              ColorizationAfterglow,
              ColorizationColorBalance,
              ColorizationAfterglowBalance,
              ColorizationBlurBalance,
              ColorizationGlassReflectionIntensity,
              ColorizationOpaqueBlend;
        }

        [Flags]
        public enum DWMWINDOWATTRIBUTE : uint
        {
            /// <summary>
            /// Use with DwmGetWindowAttribute. Discovers whether non-client rendering is enabled. The retrieved value is of type BOOL. TRUE if non-client rendering is enabled; otherwise, FALSE.
            /// </summary>
            DWMWA_NCRENDERING_ENABLED = 1,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Sets the non-client rendering policy. The pvAttribute parameter points to a value from the DWMNCRENDERINGPOLICY enumeration.
            /// </summary>
            DWMWA_NCRENDERING_POLICY,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Enables or forcibly disables DWM transitions. The pvAttribute parameter points to a value of type BOOL. TRUE to disable transitions, or FALSE to enable transitions.
            /// </summary>
            DWMWA_TRANSITIONS_FORCEDISABLED,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Enables content rendered in the non-client area to be visible on the frame drawn by DWM. The pvAttribute parameter points to a value of type BOOL. TRUE to enable content rendered in the non-client area to be visible on the frame; otherwise, FALSE.
            /// </summary>
            DWMWA_ALLOW_NCPAINT,

            /// <summary>
            /// Use with DwmGetWindowAttribute. Retrieves the bounds of the caption button area in the window-relative space. The retrieved value is of type RECT. If the window is minimized or otherwise not visible to the user, then the value of the RECT retrieved is undefined. You should check whether the retrieved RECT contains a boundary that you can work with, and if it doesn't then you can conclude that the window is minimized or otherwise not visible.
            /// </summary>
            DWMWA_CAPTION_BUTTON_BOUNDS,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Specifies whether non-client content is right-to-left (RTL) mirrored. The pvAttribute parameter points to a value of type BOOL. TRUE if the non-client content is right-to-left (RTL) mirrored; otherwise, FALSE.
            /// </summary>
            DWMWA_NONCLIENT_RTL_LAYOUT,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Forces the window to display an iconic thumbnail or peek representation (a static bitmap), even if a live or snapshot representation of the window is available. This value is normally set during a window's creation, and not changed throughout the window's lifetime. Some scenarios, however, might require the value to change over time. The pvAttribute parameter points to a value of type BOOL. TRUE to require a iconic thumbnail or peek representation; otherwise, FALSE.
            /// </summary>
            DWMWA_FORCE_ICONIC_REPRESENTATION,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Sets how Flip3D treats the window. The pvAttribute parameter points to a value from the DWMFLIP3DWINDOWPOLICY enumeration.
            /// </summary>
            DWMWA_FLIP3D_POLICY,

            /// <summary>
            /// Use with DwmGetWindowAttribute. Retrieves the extended frame bounds rectangle in screen space. The retrieved value is of type RECT.
            /// </summary>
            DWMWA_EXTENDED_FRAME_BOUNDS,

            /// <summary>
            /// Use with DwmSetWindowAttribute. The window will provide a bitmap for use by DWM as an iconic thumbnail or peek representation (a static bitmap) for the window. DWMWA_HAS_ICONIC_BITMAP can be specified with DWMWA_FORCE_ICONIC_REPRESENTATION. DWMWA_HAS_ICONIC_BITMAP normally is set during a window's creation and not changed throughout the window's lifetime. Some scenarios, however, might require the value to change over time. The pvAttribute parameter points to a value of type BOOL. TRUE to inform DWM that the window will provide an iconic thumbnail or peek representation; otherwise, FALSE. Windows Vista and earlier: This value is not supported.
            /// </summary>
            DWMWA_HAS_ICONIC_BITMAP,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Do not show peek preview for the window. The peek view shows a full-sized preview of the window when the mouse hovers over the window's thumbnail in the taskbar. If this attribute is set, hovering the mouse pointer over the window's thumbnail dismisses peek (in case another window in the group has a peek preview showing). The pvAttribute parameter points to a value of type BOOL. TRUE to prevent peek functionality, or FALSE to allow it. Windows Vista and earlier: This value is not supported.
            /// </summary>
            DWMWA_DISALLOW_PEEK,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Prevents a window from fading to a glass sheet when peek is invoked. The pvAttribute parameter points to a value of type BOOL. TRUE to prevent the window from fading during another window's peek, or FALSE for normal behavior. Windows Vista and earlier: This value is not supported.
            /// </summary>
            DWMWA_EXCLUDED_FROM_PEEK,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Cloaks the window such that it is not visible to the user. The window is still composed by DWM. Using with DirectComposition: Use the DWMWA_CLOAK flag to cloak the layered child window when animating a representation of the window's content via a DirectComposition visual that has been associated with the layered child window. For more details on this usage case, see How to animate the bitmap of a layered child window. Windows 7 and earlier: This value is not supported.
            /// </summary>
            DWMWA_CLOAK,

            /// <summary>
            /// Use with DwmGetWindowAttribute. If the window is cloaked, provides one of the following values explaining why. DWM_CLOAKED_APP (value 0x0000001). The window was cloaked by its owner application. DWM_CLOAKED_SHELL(value 0x0000002). The window was cloaked by the Shell. DWM_CLOAKED_INHERITED(value 0x0000004). The cloak value was inherited from its owner window. Windows 7 and earlier: This value is not supported.
            /// </summary>
            DWMWA_CLOAKED,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Freeze the window's thumbnail image with its current visuals. Do no further live updates on the thumbnail image to match the window's contents. Windows 7 and earlier: This value is not supported.
            /// </summary>
            DWMWA_FREEZE_REPRESENTATION,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Enables a non-UWP window to use host backdrop brushes. If this flag is set, then a Win32 app that calls Windows::UI::Composition APIs can build transparency effects using the host backdrop brush (see Compositor.CreateHostBackdropBrush). The pvAttribute parameter points to a value of type BOOL. TRUE to enable host backdrop brushes for the window, or FALSE to disable it. This value is supported starting with Windows 11 Build 22000.
            /// </summary>
            DWMWA_USE_HOSTBACKDROPBRUSH,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Allows the window frame for this window to be drawn in dark mode colors when the dark mode system setting is enabled. For compatibility reasons, all windows default to light mode regardless of the system setting. The pvAttribute parameter points to a value of type BOOL. TRUE to honor dark mode for the window, FALSE to always use light mode. This value is supported starting with Windows 10 Build 17763.
            /// </summary>
            DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Allows the window frame for this window to be drawn in dark mode colors when the dark mode system setting is enabled. For compatibility reasons, all windows default to light mode regardless of the system setting. The pvAttribute parameter points to a value of type BOOL. TRUE to honor dark mode for the window, FALSE to always use light mode. This value is supported starting with Windows 11 Build 22000.
            /// </summary>
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Specifies the rounded corner preference for a window. The pvAttribute parameter points to a value of type DWM_WINDOW_CORNER_PREFERENCE. This value is supported starting with Windows 11 Build 22000.
            /// </summary>
            DWMWA_WINDOW_CORNER_PREFERENCE = 33,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Specifies the color of the window border. The pvAttribute parameter points to a value of type COLORREF. The app is responsible for changing the border color according to state changes, such as a change in window activation. This value is supported starting with Windows 11 Build 22000.
            /// </summary>
            DWMWA_BORDER_COLOR,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Specifies the color of the caption. The pvAttribute parameter points to a value of type COLORREF. This value is supported starting with Windows 11 Build 22000.
            /// </summary>
            DWMWA_CAPTION_COLOR,

            /// <summary>
            /// Use with DwmSetWindowAttribute. Specifies the color of the caption text. The pvAttribute parameter points to a value of type COLORREF. This value is supported starting with Windows 11 Build 22000.
            /// </summary>
            DWMWA_TEXT_COLOR,

            /// <summary>
            /// Use with DwmGetWindowAttribute. Retrieves the width of the outer border that the DWM would draw around this window. The value can vary depending on the DPI of the window. The pvAttribute parameter points to a value of type UINT. This value is supported starting with Windows 11 Build 22000.
            /// </summary>
            DWMWA_VISIBLE_FRAME_BORDER_THICKNESS,

            /// <summary>
            /// The maximum recognized DWMWINDOWATTRIBUTE value, used for validation purposes.
            /// </summary>
            DWMWA_LAST,
        }

        [Flags]
        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public Rectangle ToRectangle()
            {
                return Rectangle.FromLTRB(Left, Top, Right, Bottom);
            }
        }

        public const int EM_SETCUEBANNER = 5377;
        public const int WM_SETTINGSCHANGE = 0x001A;
        public const int WM_THEMECHANGED = 0x031A;
        public const int GWLP_WNDPROC = -4;


        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        [DllImport("DwmApi")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        [DllImport("dwmapi.dll", EntryPoint = "#127")]
        public static extern void DwmGetColorizationParameters(ref DWMCOLORIZATIONcolors colors);

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
          int nLeftRect,     // x-coordinate of upper-left corner
          int nTopRect,      // y-coordinate of upper-left corner
          int nRightRect,    // x-coordinate of lower-right corner
          int nBottomRect,   // y-coordinate of lower-right corner
          int nWidthEllipse, // height of ellipse
          int nHeightEllipse // width of ellipse
        );

        [DllImport("user32")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32")]
        private static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);

        private static IntPtr GetHeaderControl(ListView list)
        {
            const int LVM_GETHEADER = 0x1000 + 31;
            return SendMessage(list.Handle, LVM_GETHEADER, IntPtr.Zero, "");
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        // This helper static method is required because the 32-bit version of user32.dll does not contain this API
        // (on any versions of Windows), so linking the method will fail at run-time. The bridge dispatches the request
        // to the correct function (GetWindowLong in 32-bit mode and GetWindowLongPtr in 64-bit mode)
        public static IntPtr SetWindowLongPtr(HandleRef hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(HandleRef hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(HandleRef hWnd, int nIndex, IntPtr dwNewLong);
        //		If that doesn't work, the following signature can be used alternatively.
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        //[DllImport("user32.dll", SetLastError = true)]
        //private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);


        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);


        #endregion Win32 API Declarations

        #region Private Static Members

        /// <summary>
        /// Stores additional info related to the Controls
        /// </summary>
        private static readonly ControlStatusStorage controlStatusStorage = new ControlStatusStorage();

        /// <summary>
        /// stores the event handler reference in order to prevent its uncontrolled multiple addition
        /// </summary>
        private static ControlEventHandler ownerFormControlAdded;

        /// <summary>
        /// stores the event handler reference in order to prevent its uncontrolled multiple addition
        /// </summary>
        private static EventHandler controlHandleCreated;

        /// <summary>
        /// stores the event handler reference in order to prevent its uncontrolled multiple addition
        /// </summary>
        private static ControlEventHandler controlControlAdded;


        private IntPtr originalWndProc;
        private WndProc newWndProcDelegate;
        private IntPtr formHandle;
        private bool applyingTheme; // Flag to prevent recursion
        #endregion


        #region Public Members

        public enum DisplayMode
        {
            /// <summary>Uses the Color Mode of the System, set by the User in Windows Settings.</summary>
            SystemDefault,
            /// <summary>Forces to use Clear Mode</summary>
            ClearMode,
            /// <summary>Forces to use Dark Mode</summary>
            DarkMode
        }


        private DisplayMode _colorMode = DisplayMode.SystemDefault;
        /// <summary>Gets or Sets the Display color mode applied to the Form and Controls.</summary>
        public DisplayMode ColorMode
        {
            get
            {
                return _colorMode;
            }
            set
            {
                if (value == _colorMode) return;
                _colorMode = value;
                ApplyColorMode();
                ApplyTheme();
            }
        }

        ///// <summary>[Read Only] 'true' if Dark Mode Color is applied to the Form.</summary>
        public bool IsDarkMode { get; private set; }

        /// <summary>Option to re-colorize all Icons in Toolbars and Menus.</summary>
        public bool ColorizeIcons { get; set; } = true;

        /// <summary>Option to make all Panels Borders Rounded</summary>
        public bool RoundedPanels { get; set; } = false;

        /// <summary>The Parent form for them all.</summary>
        public Form OwnerForm { get; set; }

        /// <summary>The Parent components.</summary>
        public ComponentCollection Components { get; set; }

        /// <summary>Windows Colors. Can be customized.</summary>
        public OSThemeColors OScolors { get; private set; }

        #endregion Public Members

        #region Constructors

        /// <summary>This tries to automatically apply Windows Dark Mode (if enabled) to a Form.</summary>
        /// <param name="_Form">The Form to become Dark</param>
        /// <param name="_ColorizeIcons">[OPTIONAL] re-colorize all Icons in Toolbars and Menus.</param>
        /// <param name="_RoundedPanels">[OPTIONAL] make all Panels Borders Rounded</param>
        public DarkModeCS(Form _Form, bool _ColorizeIcons = true, bool _RoundedPanels = false)
        {

            //Sets the Properties:
            OwnerForm = _Form;
            Components = null;
            ColorizeIcons = _ColorizeIcons;
            RoundedPanels = _RoundedPanels;
            ApplyColorMode();
            if (originalWndProc == IntPtr.Zero)
            {
                _Form.HandleCreated += (sender, e) =>
                {
                    HandleRef handleRef = new HandleRef(_Form, _Form.Handle);
                    newWndProcDelegate = CustomWndProc;
                    originalWndProc = SetWindowLongPtr(handleRef, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(newWndProcDelegate));
                };
            }
            ApplyTheme();
        }
        private void ApplyColorMode()
        {
            IsDarkMode = ColorMode == DisplayMode.DarkMode;
            if (ColorMode == DisplayMode.SystemDefault)
            {
                IsDarkMode = IsSystemDarkMode(); //<- Gets the current color mode from Windows
            }
            OScolors = GetSystemColors(IsDarkMode ? 0 : 1);

        }
        private IntPtr CustomWndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            // Handle the WM_THEMECHANGED message
            // Debug.WriteLine($"Message Code: {msg}"); // <-- should not be enabled in production as it slows everything down!
            if (msg == WM_SETTINGSCHANGE && !applyingTheme)
            {
                applyingTheme = true; // Set the flag to prevent recursion
                ApplyTheme();
                applyingTheme = false; // Reset the flag
            }

            // Call the original WndProc
            return CallWindowProc(originalWndProc, hWnd, msg, wParam, lParam);
        }


        public bool IsSystemDarkMode()
        {
            return GetWindowsColorMode() <= 0;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>Apply the Theme into the Window and all its controls.</summary>
        private void ApplyTheme()
        {
            try
            {
                if (OScolors != null)
                {
                    //Apply Window's Dark Mode to the Form's Title bar:
                    ApplySystemDarkTheme(OwnerForm, IsDarkMode);

                    OwnerForm.BackColor = OScolors.Background;
                    OwnerForm.ForeColor = OScolors.TextInactive;

                    if (OwnerForm != null && OwnerForm.Controls != null)
                    {
                        foreach (Control _control in OwnerForm.Controls)
                        {
                            ThemeControl(_control);
                        }

                        if (ownerFormControlAdded == null)
                            ownerFormControlAdded = (sender, e) =>
                            {
                                ThemeControl(e.Control);
                            };
                        OwnerForm.ControlAdded -= ownerFormControlAdded; //prevent uncontrolled multiple addition
                        OwnerForm.ControlAdded += ownerFormControlAdded;
                    }

                    if (Components != null)
                    {
                        foreach (var item in Components.OfType<ContextMenuStrip>())
                            ThemeControl(item);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Recursively apply the Colors from 'OScolors' to the Control and all its childs.</summary>
        /// <param name="control">Can be a Form or any Winforms Control.</param>
        public void ThemeControl(Control control)
        {
            var info = controlStatusStorage.GetControlStatusInfo(control);
            if (info != null)
            {
                //we already have some information about this Control

                //if the user chose to skip the control, exit
                if (info.IsExcluded) return;

                //prevent applying a theme multiple times to the same control
                //without this, it happens at least is some MDI forms
                //if the Control already has the current theme, exit (otherwise we are going to re-theme it)
                if (info.LastThemeAppliedIsDark == IsDarkMode) return;

                //we remember it will soon have the current theme
                info.LastThemeAppliedIsDark = IsDarkMode;
            }
            else
            {
                //this is the first time we see this Control

                //we remember it will soon have the current theme
                controlStatusStorage.RegisterProcessedControl(control, IsDarkMode);
            }

            BorderStyle BStyle = (IsDarkMode ? BorderStyle.FixedSingle : BorderStyle.Fixed3D);
            FlatStyle FStyle = (IsDarkMode ? FlatStyle.Flat : FlatStyle.Standard);

            if (controlHandleCreated == null) controlHandleCreated = (sender, e) =>
            {
                ApplySystemDarkTheme((Control)sender, IsDarkMode);
            };
            control.HandleCreated -= controlHandleCreated; //prevent uncontrolled multiple addition
            control.HandleCreated += controlHandleCreated;

            if (controlControlAdded == null) controlControlAdded = (sender, e) =>
            {
                ThemeControl(e.Control);
            };
            control.ControlAdded -= controlControlAdded; //prevent uncontrolled multiple addition
            control.ControlAdded += controlControlAdded;

            bool isBackColorTransparent = (control.BackColor == Color.Transparent);
            if (!isBackColorTransparent) control.BackColor = OScolors.Control;
            control.ForeColor = OScolors.TextActive;

            /* Here we Finetune individual Controls  */
            if ((control is Label lbl) && (control is not LinkLabel))
            {
                if (!isBackColorTransparent) control.BackColor = control.Parent.BackColor;
                lbl.BorderStyle = BorderStyle.None;
            }
            if (control is LinkLabel linkLbl)
            {
                if (!isBackColorTransparent) control.BackColor = control.Parent.BackColor;
                linkLbl.BorderStyle = BorderStyle.None;
                linkLbl.LinkColor = OScolors.TextInAccent;
                linkLbl.VisitedLinkColor = OScolors.Primary;
            }
            if (control is TextBox txtBox)
            {
                //SetRoundBorders(tb, 4, OScolors.SurfaceDark, 1);
                txtBox.BorderStyle = BStyle;
            }
            if (control is NumericUpDown)
            {
                SetWindowTheme(control.Handle, IsDarkMode ? "DarkMode_ItemsView" : "ClearMode_ItemsView", null);
            }
            if (control is Button button)
            {
                button.FlatStyle = IsDarkMode ? FlatStyle.Flat : FlatStyle.Standard;
                button.FlatAppearance.CheckedBackColor = OScolors.Accent;
                button.BackColor = OScolors.Control;
                button.FlatAppearance.BorderColor = (OwnerForm.AcceptButton == button) ?
                  OScolors.Accent : OScolors.Control;
            }
            if (control is ComboBox comboBox)
            {
                // Fixing a glitch that makes all instances of the ComboBox showing as having a Selected value, even when they dont
                if (comboBox.DropDownStyle != ComboBoxStyle.DropDownList)
                {
                    comboBox.SelectionStart = comboBox.Text.Length;
                }
                //control.BeginInvoke(() => (control as ComboBox).SelectionLength = 0);
                control.BeginInvoke(new Action(() =>
                {
                    if (!((ComboBox)control).DropDownStyle.Equals(ComboBoxStyle.DropDownList))
                        ((ComboBox)control).SelectionLength = 0;
                }));

                // Fixes a glitch showing the Combo Backgroud white when the control is Disabled:
                if (!control.Enabled && IsDarkMode)
                {
                    comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                }

                // Apply Windows Color Mode:
                SetWindowTheme(control.Handle, IsDarkMode ? "DarkMode_CFD" : "ClearMode_CFD", null);
            }
            if (control is Panel panel)
            {
                // Process the panel within the container
                panel.BackColor = OScolors.Background;
                panel.BorderStyle = BorderStyle.None;
                if ((panel.Parent is not TabControl) || (panel.Parent is not TableLayoutPanel))
                {
                    if (RoundedPanels)
                    {
                        SetRoundBorders(panel, 6, OScolors.SurfaceDark, 1);
                    }
                }
            }
            if (control is GroupBox)
            {
                control.BackColor = control.Parent.BackColor;
                control.ForeColor = OScolors.TextActive;
                PaintEventHandler groupBoxPainteventHandler = (sender, eargs) =>
                {
                    using var e = eargs;
                    if (control.Enabled == false && IsDarkMode)
                    {
                        var radio = (sender as GroupBox)!;
                        using Brush B = new SolidBrush(control.ForeColor);
                        e.Graphics.DrawString(radio.Text, radio.Font, B, new PointF(6, 0));
                    }
                };
                control.Paint -= groupBoxPainteventHandler;
                control.Paint += groupBoxPainteventHandler;
            }
            if (control is TableLayoutPanel tblLayoutPanel)
            {
                tblLayoutPanel.BackColor = control.Parent.BackColor;
                tblLayoutPanel.ForeColor = OScolors.TextInactive;
                tblLayoutPanel.BorderStyle = BorderStyle.None;
            }
            if (control is TabControl tab)
            {
                tab.Appearance = TabAppearance.Normal;
                tab.DrawMode = TabDrawMode.OwnerDrawFixed;
                DrawItemEventHandler tabControlPaintEventHandelr = (sender, e) =>
                {
                    //Draw the background of the main control
                    using (SolidBrush backColor = new SolidBrush(tab.Parent.BackColor))
                    {
                        e.Graphics.FillRectangle(backColor, tab.ClientRectangle);
                    }

                    using (Brush tabBack = new SolidBrush(OScolors.SurfaceDark))
                    {
                        for (int i = 0; i < tab.TabPages.Count; i++)
                        {
                            TabPage tabPage = tab.TabPages[i];
                            tabPage.BorderStyle = BorderStyle.FixedSingle;

                            tabPage.ControlAdded -= tabPageAdded; //prevent uncontrolled multiple addition
                            tabPage.ControlAdded += tabPageAdded;

                            var tabRect = tab.GetTabRect(i);

                            bool IsSelected = (tab.SelectedIndex == i);
                            if (IsSelected)
                            {
                                e.Graphics.FillRectangle(tabBack, tabRect);
                                TextRenderer.DrawText(e.Graphics, tabPage.Text, tabPage.Font, tabRect, OScolors.TextActive);
                            }
                            else
                            {
                                TextRenderer.DrawText(e.Graphics, tabPage.Text, tabPage.Font, tabRect, OScolors.TextInactive);
                            }
                        }
                    }
                };
                tab.DrawItem -= tabControlPaintEventHandelr;
                tab.DrawItem += tabControlPaintEventHandelr;
            }
            //if (control is FlatTabControl)
            //{
            //	control.GetType().GetProperty("BackColor")?.SetValue(control, OScolors.Background);
            //	control.GetType().GetProperty("TabColor")?.SetValue(control, OScolors.Surface);
            //	control.GetType().GetProperty("SelectTabColor")?.SetValue(control, OScolors.Control);
            //	control.GetType().GetProperty("SelectedForeColor")?.SetValue(control, OScolors.TextActive);
            //	control.GetType().GetProperty("ForeColor")?.SetValue(control, OScolors.TextInactive);
            //	control.GetType().GetProperty("LineColor")?.SetValue(control, OScolors.Background);
            //}
            if (control is PictureBox picBox)
            {
                picBox.BackColor = control.Parent.BackColor;
                picBox.ForeColor = OScolors.TextActive;
                picBox.BorderStyle = BorderStyle.None;
            }
            if (control is CheckBox)
            {
                control.BackColor = control.Parent.BackColor;
                control.ForeColor = control.Enabled ? OScolors.TextActive : OScolors.TextInactive;
                PaintEventHandler checkBoxPaintEventHandler = (sender, eargs) =>
                {
                    using var e = eargs;
                    if (control.Enabled == false && IsDarkMode)
                    {
                        var radio = (sender as CheckBox)!;
                        using Brush B = new SolidBrush(control.ForeColor);
                        e.Graphics.DrawString(radio.Text, radio.Font, B, new PointF(16, 0));
                    }
                };
                control.Paint -= checkBoxPaintEventHandler;
                control.Paint += checkBoxPaintEventHandler;
            }
            if (control is RadioButton)
            {
                control.BackColor = control.Parent.BackColor;
                control.ForeColor = control.Enabled ? OScolors.TextActive : OScolors.TextInactive;
                PaintEventHandler radioButtonPaintEventHandler = (sender, eargs) =>
                {
                    using var e = eargs;
                    if (control.Enabled == false && IsDarkMode)
                    {
                        var radio = (sender as RadioButton)!;
                        using Brush B = new SolidBrush(control.ForeColor);
                        e.Graphics.DrawString(radio.Text, radio.Font, B, new PointF(16, 0));
                    }
                };
                control.Paint -= radioButtonPaintEventHandler;
                control.Paint += radioButtonPaintEventHandler;
            }
            if (control is MenuStrip mnuStrip)
            {
                mnuStrip.RenderMode = ToolStripRenderMode.Professional;
                mnuStrip.Renderer = new MyRenderer(new CustomColorTable(OScolors), ColorizeIcons)
                {
                    MyColors = OScolors
                };
            }
            if (control is ToolStrip toolStrip)
            {
                toolStrip.RenderMode = ToolStripRenderMode.Professional;
                toolStrip.Renderer = new MyRenderer(new CustomColorTable(OScolors), ColorizeIcons) { MyColors = OScolors };
            }
            if (control is ToolStripPanel) //<- empty area around ToolStrip
            {
                control.BackColor = control.Parent.BackColor;
            }
            if (control is ToolStripDropDown toolStripDD)
            {
                toolStripDD.Opening -= Tsdd_Opening; //just to make sure
                toolStripDD.Opening += Tsdd_Opening;
            }
            if (control is ToolStripDropDownMenu toolStripDDN)
            {
                toolStripDDN.Opening -= Tsdd_Opening; //just to make sure
                toolStripDDN.Opening += Tsdd_Opening;
            }
            if (control is ContextMenuStrip ctxMnuStrip)
            {
                ctxMnuStrip.RenderMode = ToolStripRenderMode.Professional;
                ctxMnuStrip.Renderer = new MyRenderer(new CustomColorTable(OScolors), ColorizeIcons) { MyColors = OScolors };
                ctxMnuStrip.Opening -= Tsdd_Opening; //just to make sure
                ctxMnuStrip.Opening += Tsdd_Opening;
            }
            if (control is MdiClient) //<- empty area of MDI container window
            {
                control.BackColor = OScolors.Surface;
            }
            if (control is PropertyGrid pGrid)
            {
                pGrid.BackColor = OScolors.Control;
                pGrid.ViewBackColor = OScolors.Control;
                pGrid.LineColor = OScolors.Surface;
                pGrid.ViewForeColor = OScolors.TextActive;
                pGrid.ViewBorderColor = OScolors.ControlDark;
                pGrid.CategoryForeColor = OScolors.TextActive;
                pGrid.CategorySplitterColor = OScolors.ControlLight;
            }
            if (control is ListView lView)
            {
                if (IsDarkMode) lView.GridLines = false;
                SetWindowTheme(control.Handle, IsDarkMode ? "DarkMode_Explorer" : "ClearMode_Explorer", null);

                if (lView.View == View.Details)
                {
                    //lView.BackColor = OScolors.Surface;
                    if (lView.Items.Count > 0) lView.Items[0].UseItemStyleForSubItems = false;
                    lView.OwnerDraw = true;
                    DrawListViewColumnHeaderEventHandler listViewDrawHeaderEventHandler = (sender, e) =>
                    {
                        using var backBrush = new SolidBrush(OScolors.Header);
                        e.Graphics.FillRectangle(backBrush, e.Bounds);

                        using var columnSeparatorPen = new Pen(Color.DimGray);
                        e.Graphics.DrawLine(columnSeparatorPen, new Point(e.Bounds.Right - 1, e.Bounds.Top), new Point(e.Bounds.Right - 1, e.Bounds.Bottom));

                        //Draws the Column's Text
                        using (SolidBrush foreBrush = new SolidBrush(OScolors.TextActive))
                        {
                            var textBounds = e.Bounds;
                            textBounds.X += 4;

                            using var sf = new StringFormat(StringFormatFlags.NoWrap);
                            sf.Trimming = StringTrimming.EllipsisCharacter;
                            sf.LineAlignment = StringAlignment.Center;
                            e.Graphics.DrawString(e.Header.Text, lView.Font, foreBrush, textBounds, sf);
                        }
                    };
                    lView.DrawColumnHeader -= listViewDrawHeaderEventHandler;
                    lView.DrawColumnHeader += listViewDrawHeaderEventHandler;

                    DrawListViewSubItemEventHandler listViewDrawSubItemEventHandler = (sender, e) =>
                    {
                        bool isFirstRow = e.ItemIndex == 0;
                        bool isFirstSubItem = e.ColumnIndex == 0;
                        bool isLastSubItem = e.ColumnIndex == e.Item.SubItems.Count - 1;

                        var contentBounds = e.Bounds;
                        contentBounds.X += 4;
                        contentBounds.Width -= 8;

                        // Draw cell background
                        var backColor =
                            e.Item.Selected
                            ? e.Item.ListView.Focused
                                ? OScolors.AccentLight
                                : OScolors.BackgroundLight
                            : e.Item.BackColor;
                        using (var backBrush = new SolidBrush(backColor))
                        {
                            e.Graphics.FillRectangle(backBrush, e.Bounds);
                        }

                        // Draw vertical separators
                        e.Graphics.DrawLine(Pens.DimGray, new Point(e.Bounds.Right - 1, e.Bounds.Top), new Point(e.Bounds.Right - 1, e.Item.ListView.Bounds.Bottom));

                        // Draw checkbox
                        if (isFirstSubItem && lView.CheckBoxes)
                        {
                            var cbState = e.Item.Checked ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal;
                            var cbSize = SystemInformation.MenuCheckSize;
                            int cbX = contentBounds.Left + 4;
                            int cbY = contentBounds.Top + (contentBounds.Height - cbSize.Height) / 2;
                            Rectangle cbRect = new Rectangle(cbX, cbY, cbSize.Width, cbSize.Height);
                            CheckBoxRenderer.DrawCheckBox(e.Graphics, cbRect.Location, cbState);
                            contentBounds.X += cbSize.Width + 4;
                            contentBounds.Width -= cbSize.Width + 4;
                        }

                        // Draw icon
                        if (isFirstSubItem)
                        {
                            if (e.Item.ImageList is not null)
                            {
                                Image? img = null;
                                if ((e.Item.ImageIndex >= 0) && (e.Item.ImageIndex < e.Item.ImageList.Images.Count))
                                    img = e.Item.ImageList.Images[e.Item.ImageIndex];
                                else if (e.Item.ImageKey is not null)
                                    img = e.Item.ImageList.Images[e.Item.ImageKey];

                                if (img is not null)
                                    e.Graphics.DrawImage(img, contentBounds.Location);

                                contentBounds.X += e.Item.ImageList.ImageSize.Width;
                                contentBounds.Width -= e.Item.ImageList.ImageSize.Width;
                            }
                        }

                        // Draw text
                        using (var foreBrush = new SolidBrush(OScolors.TextActive))
                        {
                            using var sf = new StringFormat(StringFormatFlags.NoWrap);
                            sf.Trimming = StringTrimming.EllipsisCharacter;
                            sf.LineAlignment = StringAlignment.Center;
                            e.Graphics.DrawString(e.SubItem.Text, lView.Font, foreBrush, contentBounds, sf);
                        }

                        // Draw focus highlight
                        if (e.ItemState.HasFlag(ListViewItemStates.Focused))
                        {
                            using var focusPen = new Pen(Brushes.LightGray, 1) { DashStyle = DashStyle.Dot };
                            e.Graphics.DrawLine(focusPen, e.Bounds.Left + 1, e.Bounds.Top + 1, e.Bounds.Right - 1, e.Bounds.Top + 1);
                            e.Graphics.DrawLine(focusPen, e.Bounds.Left + 1, e.Bounds.Bottom - 1, e.Bounds.Right - 1, e.Bounds.Bottom - 1);
                            if (isFirstSubItem)
                                e.Graphics.DrawLine(focusPen, e.Bounds.Left + 1, e.Bounds.Top + 1, e.Bounds.Left + 1, e.Bounds.Bottom - 1);
                            if (isLastSubItem)
                                e.Graphics.DrawLine(focusPen, e.Bounds.Right - 1, e.Bounds.Top + 1, e.Bounds.Right - 1, e.Bounds.Bottom - 1);
                        }
                    };
                    lView.DrawSubItem -= listViewDrawSubItemEventHandler;
                    lView.DrawSubItem += listViewDrawSubItemEventHandler;

                    // This makes sure unused header space after the last column is also painted
                    IntPtr headerHandle = GetHeaderControl(lView);
                    if (headerHandle != IntPtr.Zero)
                        SetWindowTheme(headerHandle, IsDarkMode ? "DarkMode_ItemsView" : "ItemsView", null);
                }

            }
            if (control is TreeView treeView)
            {
                treeView.BorderStyle = BorderStyle.None;
                //tree.DrawNode += (object? sender, DrawTreeNodeEventArgs e) =>
                //{
                //  if (e.Node.ImageIndex != -1)
                //  {
                //	Image image = tree.ImageList.Images[e.Node.ImageIndex];
                //	using (Graphics g = Graphics.FromImage(image))
                //	{
                //	  g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                //	  g.CompositingQuality = CompositingQuality.HighQuality;
                //	  g.SmoothingMode = SmoothingMode.HighQuality;

                //	  g.DrawImage(DarkModeCS.ChangeToColor(image, OScolors.TextInactive), new Point(0,0));
                //	}
                //	tree.ImageList.Images[e.Node.ImageIndex] = image;
                //  }
                //  tree.Invalidate();
                //};
            }
            if (control is DataGridView grid)
            {
                grid.EnableHeadersVisualStyles = false;
                grid.BorderStyle = BorderStyle.FixedSingle;
                grid.BackgroundColor = OScolors.Control;
                grid.GridColor = OScolors.Control;

                //paint the bottom right corner where the scrollbars meet
                PaintEventHandler dataGridPaintEventHandler = (sender, eargs) =>
                {
                    using var e = eargs;
                    DataGridView dgv = (DataGridView)sender;

                    //get the value of dgv.HorizontalScrollBar protected property
                    HScrollBar hs = (HScrollBar)typeof(DataGridView).GetProperty("HorizontalScrollBar", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(dgv);
                    if (hs.Visible)
                    {
                        //get the value of dgv.VerticalScrollBar protected property
                        VScrollBar vs = (VScrollBar)typeof(DataGridView).GetProperty("VerticalScrollBar", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(dgv);

                        if (vs.Visible)
                        {
                            //only when both the scrollbars are visible, do the actual painting
                            using Brush brush = new SolidBrush(OScolors.SurfaceDark);
                            var w = vs.Size.Width;
                            var h = hs.Size.Height;
                            e.Graphics.FillRectangle(brush, dgv.ClientRectangle.X + dgv.ClientRectangle.Width - w - 1,
                              dgv.ClientRectangle.Y + dgv.ClientRectangle.Height - h - 1, w, h);
                        }
                    }
                };
                grid.Paint -= dataGridPaintEventHandler;
                grid.Paint += dataGridPaintEventHandler;

                grid.DefaultCellStyle.BackColor = OScolors.Surface;
                grid.DefaultCellStyle.ForeColor = OScolors.TextActive;

                grid.ColumnHeadersDefaultCellStyle.BackColor = OScolors.Surface;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = OScolors.TextActive;
                grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = OScolors.Surface;
                grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

                grid.RowHeadersDefaultCellStyle.BackColor = OScolors.Surface;
                grid.RowHeadersDefaultCellStyle.ForeColor = OScolors.TextActive;
                grid.RowHeadersDefaultCellStyle.SelectionBackColor = OScolors.Surface;
                grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            }
            if (control is RichTextBox richText)
            {
                richText.BackColor = richText.Parent.BackColor;
                richText.BorderStyle = BorderStyle.None;
            }
            if (control is FlowLayoutPanel flowLayout)
            {
                flowLayout.BackColor = flowLayout.Parent.BackColor;
                flowLayout.BorderStyle = BorderStyle.None;
            }


            //Debug.Print(string.Format("{0}: {1}", control.Name, control.GetType().Name));

            if (control.ContextMenuStrip != null)
                ThemeControl(control.ContextMenuStrip);

            foreach (Control childControl in control.Controls)
            {
                // Recursively process its children
                ThemeControl(childControl);
            }
        }

        private void tabPageAdded(object _s, ControlEventArgs _e)
        {
            ThemeControl(_e.Control);
        }

        /// <summary>
        /// Registers the Control as processed. Prevents applying theme to the Control.
        /// Call it before applying the theme to your Form (or to any other Control containing (directly or indirectly) this Control)
        /// </summary>
        public static void ExcludeFromProcessing(Control control)
        {
            controlStatusStorage.ExcludeFromProcessing(control);
        }

        /// <summary>Returns Windows Color Mode for Applications.
        /// <para>0=dark theme, 1=light theme</para>
        /// </summary>
        public static int GetWindowsColorMode(bool GetSystemColorModeInstead = false)
        {
            try
            {
                return (int)Registry.GetValue(
                  @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                  GetSystemColorModeInstead ? "SystemUsesLightTheme" : "AppsUseLightTheme",
                  -1);
            }
            catch
            {
                return 1;
            }
        }

        /// <summary>Returns the Accent Color used by Windows.</summary>
        /// <returns>a Color</returns>
        public static Color GetWindowsAccentColor()
        {
            try
            {
                DWMCOLORIZATIONcolors colors = new DWMCOLORIZATIONcolors();
                DwmGetColorizationParameters(ref colors);

                //get the theme --> only if Windows 10 or newer
                if (OSVersion.IsWindows10_OrGreater())
                {
                    var color = colors.ColorizationColor;

                    var colorValue = long.Parse(color.ToString(), System.Globalization.NumberStyles.HexNumber);

                    var transparency = (colorValue >> 24) & 0xFF;
                    var red = (colorValue >> 16) & 0xFF;
                    var green = (colorValue >> 8) & 0xFF;
                    var blue = (colorValue >> 0) & 0xFF;

                    return Color.FromArgb((int)transparency, (int)red, (int)green, (int)blue);
                }
                else
                {
                    return Color.CadetBlue;
                }
            }
            catch (Exception)
            {
                return Color.CadetBlue;
            }
        }

        /// <summary>Returns the Accent Color used by Windows.</summary>
        /// <returns>an opaque Color</returns>
        public static Color GetWindowsAccentOpaqueColor()
        {
            DWMCOLORIZATIONcolors colors = new DWMCOLORIZATIONcolors();
            DwmGetColorizationParameters(ref colors);

            //get the theme --> only if Windows 10 or newer
            if (OSVersion.IsWindows10_OrGreater())
            {
                var color = colors.ColorizationColor;

                var colorValue = long.Parse(color.ToString(), System.Globalization.NumberStyles.HexNumber);

                var red = (colorValue >> 16) & 0xFF;
                var green = (colorValue >> 8) & 0xFF;
                var blue = (colorValue >> 0) & 0xFF;

                return Color.FromArgb(255, (int)red, (int)green, (int)blue);
            }
            else
            {
                return Color.CadetBlue;
            }
        }

        /// <summary>Returns Windows's System Colors for UI components following Google Material Design concepts.</summary>
        /// <returns>List of Colors:  Background, OnBackground, Surface, OnSurface, Primary, OnPrimary, Secondary, OnSecondary</returns>
        public static OSThemeColors GetSystemColors(int ColorMode = 0) //<- O: DarkMode, 1: LightMode
        {
            OSThemeColors _ret = new OSThemeColors();

            if (ColorMode <= 0)
            {
                _ret.Background = Color.FromArgb(32, 32, 32);   //<- Negro Claro
                _ret.BackgroundDark = Color.FromArgb(18, 18, 18);
                _ret.BackgroundLight = ControlPaint.Light(_ret.Background);

                _ret.Surface = Color.FromArgb(43, 43, 43);      //<- Gris Oscuro
                _ret.SurfaceLight = Color.FromArgb(50, 50, 50);
                _ret.SurfaceDark = Color.FromArgb(29, 29, 29);

                _ret.TextActive = Color.White;
                _ret.TextInactive = Color.FromArgb(176, 176, 176);  //<- Blanco Palido
                _ret.TextInAccent = GetReadableColor(_ret.Accent);

                _ret.Control = Color.FromArgb(55, 55, 55);       //<- Gris Oscuro
                _ret.ControlDark = ControlPaint.Dark(_ret.Control);
                _ret.ControlLight = Color.FromArgb(67, 67, 67);

                _ret.Primary = Color.FromArgb(3, 218, 198);   //<- Verde Pastel
                _ret.Secondary = Color.MediumSlateBlue;         //<- Magenta Claro

                _ret.Header = OSVersion.IsWindows11_OrGreater() ? Color.FromArgb(25, 25, 25) : Color.FromArgb(32, 32, 32);
            }

            return _ret;
        }

        /// <summary>Apply Round Corners to the indicated Control or Form.</summary>
        /// <param name="_Control">the one who will have rounded Corners. Set BorderStyle = None.</param>
        /// <param name="Radius">Radious for the Corners</param>
        /// <param name="borderColor">Color for the Border</param>
        /// <param name="borderSize">Size in pixels of the border line</param>
        /// <param name="underlinedStyle"></param>
        public static void SetRoundBorders(Control _Control, int Radius = 10, Color? borderColor = null, int borderSize = 2, bool underlinedStyle = false)
        {
            borderColor = borderColor ?? Color.MediumSlateBlue;

            if (_Control != null)
            {
                _Control.GetType().GetProperty("BorderStyle")?.SetValue(_Control, BorderStyle.None);
                _Control.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, _Control.Width, _Control.Height, Radius, Radius));
                PaintEventHandler roundBordersPaintEventHandler = (sender, eargs) =>
                {
                    using var e = eargs;

                    //base.OnPaint(e);
                    Graphics graph = e.Graphics;

                    if (Radius > 1)//Rounded TextBox
                    {
                        //-Fields
                        var rectBorderSmooth = _Control.ClientRectangle;
                        var rectBorder = Rectangle.Inflate(rectBorderSmooth, -borderSize, -borderSize);
                        int smoothSize = borderSize > 0 ? borderSize : 1;

                        using (GraphicsPath pathBorderSmooth = GetFigurePath(rectBorderSmooth, Radius))
                        using (GraphicsPath pathBorder = GetFigurePath(rectBorder, Radius - borderSize))
                        using (Pen penBorderSmooth = new Pen(_Control.Parent.BackColor, smoothSize))
                        using (Pen penBorder = new Pen((Color)borderColor, borderSize))
                        {
                            //-Drawing
                            _Control.Region = new Region(pathBorderSmooth);//Set the rounded region of UserControl
                            if (Radius > 15) //Set the rounded region of TextBox component
                            {
                                using (GraphicsPath pathTxt = GetFigurePath(_Control.ClientRectangle, borderSize * 2))
                                {
                                    _Control.Region = new Region(pathTxt);
                                }
                            }
                            graph.SmoothingMode = SmoothingMode.AntiAlias;
                            penBorder.Alignment = PenAlignment.Center;
                            //if (isFocused) penBorder.Color = borderFocusColor;

                            if (underlinedStyle) //Line Style
                            {
                                //Draw border smoothing
                                graph.DrawPath(penBorderSmooth, pathBorderSmooth);
                                //Draw border
                                graph.SmoothingMode = SmoothingMode.None;
                                graph.DrawLine(penBorder, 0, _Control.Height - 1, _Control.Width, _Control.Height - 1);
                            }
                            else //Normal Style
                            {
                                //Draw border smoothing
                                graph.DrawPath(penBorderSmooth, pathBorderSmooth);
                                //Draw border
                                graph.DrawPath(penBorder, pathBorder);
                            }
                        }
                    }
                };
                _Control.Paint -= roundBordersPaintEventHandler;
                _Control.Paint += roundBordersPaintEventHandler;
            }
        }

        /// <summary>Colorea una imagen usando una Matrix de Color.</summary>
        /// <param name="bmp">Imagen a Colorear</param>
        /// <param name="c">Color a Utilizar</param>
        public static Bitmap ChangeToColor(Bitmap bmp, Color c)
        {
            Bitmap bmp2 = new Bitmap(bmp.Width, bmp.Height);
            using (Graphics g = Graphics.FromImage(bmp2))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;

                float tR = c.R / 255f;
                float tG = c.G / 255f;
                float tB = c.B / 255f;

                System.Drawing.Imaging.ColorMatrix colorMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][]
                {
                    new float[] { 1,    0,  0,  0,  0 },
                    new float[] { 0,    1,  0,  0,  0 },
                    new float[] { 0,    0,  1,  0,  0 },
                    new float[] { 0,    0,  0,  1,  0 },  //<- not changing alpha
                    new float[] { tR,   tG, tB, 0,  1 }
                });


                System.Drawing.Imaging.ImageAttributes attributes = new System.Drawing.Imaging.ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);

                g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height),
                  0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);
            }
            return bmp2;
        }
        public static Image ChangeToColor(Image bmp, Color c) => ChangeToColor((Bitmap)bmp, c);

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// handle hierarchical context menus (otherwise, only the root level gets themed)
        /// </summary>
        private void Tsdd_Opening(object sender, CancelEventArgs e)
        {
            ToolStripDropDown tsdd = sender as ToolStripDropDown;
            if (tsdd == null) return; //should not occur

            foreach (ToolStripMenuItem toolStripMenuItem in tsdd.Items.OfType<ToolStripMenuItem>())
            {
                toolStripMenuItem.DropDownOpening -= Tsmi_DropDownOpening; //just to make sure
                toolStripMenuItem.DropDownOpening += Tsmi_DropDownOpening;
            }
        }

        /// <summary>
        /// handle hierarchical context menus (otherwise, only the root level gets themed)
        /// </summary>
        private void Tsmi_DropDownOpening(object sender, EventArgs e)
        {
            ToolStripMenuItem tsmi = sender as ToolStripMenuItem;
            if (tsmi == null) return; //should not occur

            if (tsmi.DropDown.Items.Count > 0) ThemeControl(tsmi.DropDown);

            //once processed, remove itself to prevent multiple executions (when user leaves and reenters the sub-menu)
            tsmi.DropDownOpening -= Tsmi_DropDownOpening;
        }

        /// <summary>Attemps to apply Window's Dark Style to the Control and all its childs.</summary>
        /// <param name="control"></param>
        private static void ApplySystemDarkTheme(Control control = null, bool IsDarkMode = true)
        {
            /*
                  DWMWA_USE_IMMERSIVE_DARK_MODE:   https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute

                  Use with DwmSetWindowAttribute. Allows the window frame for this window to be drawn in dark mode colors when the dark mode system setting is enabled.
                  For compatibility reasons, all windows default to light mode regardless of the system setting.
                  The pvAttribute parameter points to a value of type BOOL. TRUE to honor dark mode for the window, FALSE to always use light mode.

                  This value is supported starting with Windows 11 Build 22000.

                  SetWindowTheme:     https://learn.microsoft.com/en-us/windows/win32/api/uxtheme/nf-uxtheme-setwindowtheme
                  Causes a window to use a different set of visual style information than its class normally uses. Fix for Scrollbars!
            */
            int[] DarkModeOn = IsDarkMode ? new[] { 0x01 } : new[] { 0x00 }; //<- 1=True, 0=False
            string Mode = IsDarkMode ? "DarkMode_Explorer" : "ClearMode_Explorer";

            if (DwmSetWindowAttribute(control.Handle, (int)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, DarkModeOn, 4) != 0)
                _ = DwmSetWindowAttribute(control.Handle, (int)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, DarkModeOn, 4);

            foreach (Control child in control.Controls)
            {
                if (child.Controls.Count != 0)
                    ApplySystemDarkTheme(child, IsDarkMode);
            }
        }

        private static Color GetReadableColor(Color backgroundColor)
        {
            // Calculate the relative luminance of the background color.
            // Normalize values to 0-1 range first.
            double normalizedR = backgroundColor.R / 255.0;
            double normalizedG = backgroundColor.G / 255.0;
            double normalizedB = backgroundColor.B / 255.0;
            double luminance = 0.299 * normalizedR + 0.587 * normalizedG + 0.114 * normalizedB;

            // Choose a contrasting foreground color based on the luminance,
            // with a slight bias towards lighter colors for better readability.
            return luminance < 0.5 ? Color.FromArgb(182, 180, 215) : Color.FromArgb(34, 34, 34); // Dark gray for light backgrounds
        }

        // For Rounded Corners:
        private static GraphicsPath GetFigurePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float curveSize = radius * 2F;

            path.StartFigure();
            path.AddArc(rect.X, rect.Y, curveSize, curveSize, 180, 90);
            path.AddArc(rect.Right - curveSize, rect.Y, curveSize, curveSize, 270, 90);
            path.AddArc(rect.Right - curveSize, rect.Bottom - curveSize, curveSize, curveSize, 0, 90);
            path.AddArc(rect.X, rect.Bottom - curveSize, curveSize, curveSize, 90, 90);
            path.CloseFigure();
            return path;
        }

        #endregion Private Methods
    }

    /// <summary>Windows 10+ System Colors for Clear Color Mode.</summary>
    public class OSThemeColors
    {
        public OSThemeColors()
        {
        }

        /// <summary>For the very back of the Window</summary>
        public Color Background { get; set; } = SystemColors.Control;

        /// <summary>For Borders around the Background</summary>
        public Color BackgroundDark { get; set; } = SystemColors.ControlDark;

        /// <summary>For hightlights over the Background</summary>
        public Color BackgroundLight { get; set; } = SystemColors.ControlLight;

        /// <summary>For Container above the Background</summary>
        public Color Surface { get; set; } = SystemColors.ControlLightLight;

        /// <summary>For Borders around the Surface</summary>
        public Color SurfaceDark { get; set; } = SystemColors.ControlLight;

        /// <summary>For Highligh over the Surface</summary>
        public Color SurfaceLight { get; set; } = Color.White;

        /// <summary>For Main Texts</summary>
        public Color TextActive { get; set; } = SystemColors.ControlText;

        /// <summary>For Inactive Texts</summary>
        public Color TextInactive { get; set; } = SystemColors.GrayText;

        /// <summary>For Hightligh Texts</summary>
        public Color TextInAccent { get; set; } = SystemColors.HighlightText;

        /// <summary>For the background of any Control</summary>
        public Color Control { get; set; } = SystemColors.ButtonFace;

        /// <summary>For Bordes of any Control</summary>
        public Color ControlDark { get; set; } = SystemColors.ButtonShadow;

        /// <summary>For Highlight elements in a Control</summary>
        public Color ControlLight { get; set; } = SystemColors.ButtonHighlight;

        /// <summary>Windows 10+ Chosen Accent Color</summary>
        public Color Accent { get; set; } = DarkModeCS.GetWindowsAccentColor();

        public Color AccentOpaque { get; set; } = DarkModeCS.GetWindowsAccentOpaqueColor();

        public Color AccentDark { get { return ControlPaint.Dark(Accent); } }

        public Color AccentLight { get { return ControlPaint.Light(Accent); } }

        /// <summary>the color displayed most frequently across your app's screens and components.</summary>
        public Color Primary { get; set; } = SystemColors.Highlight;

        public Color PrimaryDark { get { return ControlPaint.Dark(Primary); } }

        public Color PrimaryLight { get { return ControlPaint.Light(Primary); } }

        /// <summary>to accent select parts of your UI.</summary>
        public Color Secondary { get; set; } = SystemColors.HotTrack;

        public Color SecondaryDark { get { return ControlPaint.Dark(Secondary); } }

        public Color SecondaryLight { get { return ControlPaint.Light(Secondary); } }

        public Color Header { get; set; } = SystemColors.Window;
    }

    /* Custom Renderers for Menus and ToolBars */
    public class MyRenderer : ToolStripProfessionalRenderer
    {
        public bool ColorizeIcons { get; set; } = true;
        public OSThemeColors MyColors { get; set; } //<- Your Custom Colors Colection

        public MyRenderer(ProfessionalColorTable table, bool pColorizeIcons = true) : base(table)
        {
            ColorizeIcons = pColorizeIcons;
        }

        private void DrawTitleBar(Graphics g, Rectangle rect)
        {
            // Assign the image for the grip.
            //Image titlebarGrip = titleBarGripBmp;

            // Fill the titlebar.
            // This produces the gradient and the rounded-corner effect.
            //g.DrawLine(new Pen(titlebarColor1), rect.X, rect.Y, rect.X + rect.Width, rect.Y);
            //g.DrawLine(new Pen(titlebarColor2), rect.X, rect.Y + 1, rect.X + rect.Width, rect.Y + 1);
            //g.DrawLine(new Pen(titlebarColor3), rect.X, rect.Y + 2, rect.X + rect.Width, rect.Y + 2);
            //g.DrawLine(new Pen(titlebarColor4), rect.X, rect.Y + 3, rect.X + rect.Width, rect.Y + 3);
            //g.DrawLine(new Pen(titlebarColor5), rect.X, rect.Y + 4, rect.X + rect.Width, rect.Y + 4);
            //g.DrawLine(new Pen(titlebarColor6), rect.X, rect.Y + 5, rect.X + rect.Width, rect.Y + 5);
            //g.DrawLine(new Pen(titlebarColor7), rect.X, rect.Y + 6, rect.X + rect.Width, rect.Y + 6);

            // Center the titlebar grip.
            //g.DrawImage(
            //  titlebarGrip,
            //  new Point(rect.X + ((rect.Width / 2) - (titlebarGrip.Width / 2)),
            //  rect.Y + 1));
        }

        // This method handles the RenderGrip event.
        protected override void OnRenderGrip(ToolStripGripRenderEventArgs e)
        {
            DrawTitleBar(
              e.Graphics,
              new Rectangle(0, 0, e.ToolStrip.Width, 7));
        }

        // This method handles the RenderToolStripBorder event.
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            DrawTitleBar(
              e.Graphics,
              new Rectangle(0, 0, e.ToolStrip.Width, 7));
        }

        // Background of the whole ToolBar Or MenuBar:
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.ToolStrip.BackColor = MyColors.Background;
            base.OnRenderToolStripBackground(e);
        }

        // For Normal Buttons on a ToolBar:
        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);

            Color gradientBegin = MyColors.Background; // Color.FromArgb(203, 225, 252);
            Color gradientEnd = MyColors.Background;

            Pen BordersPencil = new Pen(MyColors.Background);

            ToolStripButton button = e.Item as ToolStripButton;
            if (button.Pressed || button.Checked)
            {
                gradientBegin = MyColors.Control;
                gradientEnd = MyColors.Control;
            }
            else if (button.Selected)
            {
                gradientBegin = MyColors.Accent;
                gradientEnd = MyColors.Accent;
            }

            using (Brush b = new LinearGradientBrush(
              bounds,
              gradientBegin,
              gradientEnd,
              LinearGradientMode.Vertical))
            {
                g.FillRectangle(b, bounds);
            }

            e.Graphics.DrawRectangle(
              BordersPencil,
              bounds);

            g.DrawLine(
              BordersPencil,
              bounds.X,
              bounds.Y,
              bounds.Width - 1,
              bounds.Y);

            g.DrawLine(
              BordersPencil,
              bounds.X,
              bounds.Y,
              bounds.X,
              bounds.Height - 1);

            ToolStrip toolStrip = button.Owner;

            if (!(button.Owner.GetItemAt(button.Bounds.X, button.Bounds.Bottom + 1) is ToolStripButton nextItem))
            {
                g.DrawLine(
                  BordersPencil,
                  bounds.X,
                  bounds.Height - 1,
                  bounds.X + bounds.Width - 1,
                  bounds.Height - 1);
            }
        }

        // For DropDown Buttons on a ToolBar:
        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);
            Color gradientBegin = MyColors.Background; // Color.FromArgb(203, 225, 252);
            Color gradientEnd = MyColors.Background;

            Pen BordersPencil = new Pen(MyColors.Background);

            //1. Determine the colors to use:
            if (e.Item.Pressed)
            {
                gradientBegin = MyColors.Control;
                gradientEnd = MyColors.Control;
            }
            else if (e.Item.Selected)
            {
                gradientBegin = MyColors.Accent;
                gradientEnd = MyColors.Accent;
            }

            //2. Draw the Box around the Control
            using (Brush b = new LinearGradientBrush(
              bounds,
              gradientBegin,
              gradientEnd,
              LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(b, bounds);
            }

            //3. Draws the Chevron:

            #region Chevron

            //int Padding = 2; //<- From the right side
            //Size cSize = new Size(8, 4); //<- Size of the Chevron: 8x4 px
            //Pen ChevronPen = new Pen(MyColors.TextInactive, 2); //<- Color and Border Width
            //Point P1 = new Point(bounds.Width - (cSize.Width + Padding), (bounds.Height / 2) - (cSize.Height / 2));
            //Point P2 = new Point(bounds.Width - Padding, (bounds.Height / 2) - (cSize.Height / 2));
            //Point P3 = new Point(bounds.Width - (cSize.Width / 2 + Padding), (bounds.Height / 2) + (cSize.Height / 2));

            //e.Graphics.DrawLine(ChevronPen, P1, P3);
            //e.Graphics.DrawLine(ChevronPen, P2, P3);

            #endregion Chevron
        }

        // For SplitButtons on a ToolBar:
        protected override void OnRenderSplitButtonBackground(ToolStripItemRenderEventArgs e)
        {
            Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);
            Color gradientBegin = MyColors.Background; // Color.FromArgb(203, 225, 252);
            Color gradientEnd = MyColors.Background;

            //1. Determine the colors to use:
            if (e.Item.Pressed)
            {
                gradientBegin = MyColors.Control;
                gradientEnd = MyColors.Control;
            }
            else if (e.Item.Selected)
            {
                gradientBegin = MyColors.Accent;
                gradientEnd = MyColors.Accent;
            }

            //2. Draw the Box around the Control
            using (Brush b = new LinearGradientBrush(
              bounds,
              gradientBegin,
              gradientEnd,
              LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(b, bounds);
            }

            //3. Draws the Chevron:

            #region Chevron

            int Padding = 2; //<- From the right side
            Size cSize = new Size(8, 4); //<- Size of the Chevron: 8x4 px
            Pen ChevronPen = new Pen(MyColors.TextInactive, 2); //<- Color and Border Width
            Point P1 = new Point(bounds.Width - (cSize.Width + Padding), (bounds.Height / 2) - (cSize.Height / 2));
            Point P2 = new Point(bounds.Width - Padding, (bounds.Height / 2) - (cSize.Height / 2));
            Point P3 = new Point(bounds.Width - (cSize.Width / 2 + Padding), (bounds.Height / 2) + (cSize.Height / 2));

            e.Graphics.DrawLine(ChevronPen, P1, P3);
            e.Graphics.DrawLine(ChevronPen, P2, P3);

            #endregion Chevron
        }

        // For the Text Color of all Items:
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item.Enabled)
            {
                e.TextColor = MyColors.TextActive;
            }
            else
            {
                e.TextColor = MyColors.TextInactive;
            }
            base.OnRenderItemText(e);
        }

        protected override void OnRenderItemBackground(ToolStripItemRenderEventArgs e)
        {
            base.OnRenderItemBackground(e);

            // Only draw border for ComboBox items
            if (e.Item is ComboBox)
            {
                Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);
                e.Graphics.DrawRectangle(new Pen(MyColors.ControlLight, 1), rect);
            }
        }

        // For Menu Items BackColor:
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);

            Color gradientBegin = MyColors.Background; // Color.FromArgb(203, 225, 252);
            Color gradientEnd = MyColors.Background; // Color.FromArgb(125, 165, 224);

            bool DrawIt = false;
            var _menu = e.Item;
            if (_menu.Pressed)
            {
                gradientBegin = MyColors.Control; // Color.FromArgb(254, 128, 62);
                gradientEnd = MyColors.Control; // Color.FromArgb(255, 223, 154);
                DrawIt = true;
            }
            else if (_menu.Selected)
            {
                gradientBegin = MyColors.Accent;// Color.FromArgb(255, 255, 222);
                gradientEnd = MyColors.Accent; // Color.FromArgb(255, 203, 136);
                DrawIt = true;
            }

            if (DrawIt)
            {
                using (Brush b = new LinearGradientBrush(
                bounds,
                gradientBegin,
                gradientEnd,
                LinearGradientMode.Vertical))
                {
                    g.FillRectangle(b, bounds);
                }
            }
        }

        // Re-Colors the Icon Images to a Clear color:
        protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
        {
            if (e.Item.GetType().FullName == "System.Windows.Forms.MdiControlStrip+ControlBoxMenuItem")
            {
                //Window Controls - Minimize, Maximize, Close button of a maximized MDI child windows
                //are realized as ControlBoxMenuItem contained in the MenuStrip
                //by default they would be painted black on a dark surface
                //so to make them more visible, we paint them ourselves:
                Image image = e.Image;
                Color _ClearColor = e.Item.Enabled ? MyColors.TextActive : MyColors.SurfaceDark;

                using (Image adjustedImage = DarkModeCS.ChangeToColor(image, _ClearColor))
                {
                    e.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    e.Graphics.CompositingQuality =
                      CompositingQuality.AssumeLinear; //looks thinner and less fuzzy than HighQuality
                    e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                    e.Graphics.DrawImage(adjustedImage, e.ImageRectangle);
                }

                return;
            }

            if (ColorizeIcons && e.Image != null)
            {
                // Get the current icon
                Image image = e.Image;
                Color _ClearColor = e.Item.Enabled ? MyColors.TextInactive : MyColors.SurfaceDark;

                // Create a new image with the desired color adjustments
                using (Image adjustedImage = DarkModeCS.ChangeToColor(image, _ClearColor))
                {
                    e.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                    e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                    e.Graphics.DrawImage(adjustedImage, e.ImageRectangle);
                }
            }
            else
            {
                base.OnRenderItemImage(e);
            }
        }
    }

    public class CustomColorTable : ProfessionalColorTable
    {
        public OSThemeColors Colors { get; set; }

        public CustomColorTable(OSThemeColors _Colors)
        {
            Colors = _Colors;
            UseSystemColors = false;
        }

        public override Color ImageMarginGradientBegin
        {
            get { return Colors.Control; }
        }

        public override Color ImageMarginGradientMiddle
        {
            get { return Colors.Control; }
        }

        public override Color ImageMarginGradientEnd
        {
            get { return Colors.Control; }
        }
    }

    /// <summary>
    /// Stores additional info related to the Controls
    /// </summary>
    public class ControlStatusStorage
    {
        /// <summary>
        /// Storage for the data. ConditionalWeakTable ensures there are no unnecessary references left, preventing garbage collection.
        /// </summary>
        private readonly ConditionalWeakTable<Control, ControlStatusInfo> _controlsProcessed = new ConditionalWeakTable<Control, ControlStatusInfo>();

        /// <summary>
        /// Registers the Control as processed. Prevents applying theme to the Control.
        /// Call it before applying the theme to your Form (or to any other Control containing (directly or indirectly) this Control)
        /// </summary>
        public void ExcludeFromProcessing(Control control)
        {
            _controlsProcessed.Remove(control);
            _controlsProcessed.Add(control, new ControlStatusInfo() { IsExcluded = true });
        }

        /// <summary>
        /// Gets the additional info associated with a Control
        /// </summary>
        /// <returns>a ControlStatusInfo object if the control has been already processed or marked for exclusion, null otherwise</returns>
        public ControlStatusInfo GetControlStatusInfo(Control control)
        {
            _controlsProcessed.TryGetValue(control, out ControlStatusInfo info);
            return info;
        }

        public void RegisterProcessedControl(Control control, bool isDarkMode)
        {
            _controlsProcessed.Add(control,
                new ControlStatusInfo() { IsExcluded = false, LastThemeAppliedIsDark = isDarkMode });
        }
    }

    /// <summary>
    /// Additional information related to the Controls
    /// </summary>
    public class ControlStatusInfo
    {
        /// <summary>
        /// true if the user wants to skip theming the Control
        /// </summary>
        public bool IsExcluded { get; set; }

        /// <summary>
        /// whether the last theme applied was dark
        /// </summary>
        public bool LastThemeAppliedIsDark { get; set; }
    }

    /// <summary>
    /// Get the current Windows Operating System Version.
    /// </summary>
    /// <remarks>Reduced version of <a href="https://github.com/dotnet/winforms">dotnet/winforms</a> .NET 10 implementation. (.NET Foundation, MIT license)</remarks>
    public static class OSVersion
    {

        public static bool IsWindows8_OrGreater()
            => OperatingSystem.IsWindowsVersionAtLeast(major: 6, minor: 2);

        public static bool IsWindows10_OrGreater()
            => OperatingSystem.IsWindowsVersionAtLeast(major: 10, build: 10240);

        public static bool IsWindows11_OrGreater()
            => OperatingSystem.IsWindowsVersionAtLeast(major: 10, build: 22000);

        public static bool IsWindows11_22H2OrGreater()
            => OperatingSystem.IsWindowsVersionAtLeast(major: 10, build: 22621);

        /// <summary>
        /// Reduced version of <a href="https://github.com/dotnet/runtime">dotnet/runtime</a> .NET 10 implementation. (.NET Foundation, MIT license)
        /// </summary>
        private class OperatingSystem
        {
            public static bool IsWindows() => true;

            /// <summary>
            /// Check for the Windows version (returned by 'RtlGetVersion') with a >= version comparison. Used to guard APIs that were added in the given Windows release.
            /// </summary>
            public static bool IsWindowsVersionAtLeast(int major, int minor = 0, int build = 0, int revision = 0)
                => IsWindows() && IsOSVersionAtLeast(major, minor, build, revision);

            private static bool IsOSVersionAtLeast(int major, int minor, int build, int revision)
            {
                Version current = Environment.OSVersion.Version;

                if (current.Major != major)
                    return current.Major > major;
                if (current.Minor != minor)
                    return current.Minor > minor;

                // Unspecified build component is to be treated as zero
                int currentBuild = current.Build < 0 ? 0 : current.Build;
                if (currentBuild != build)
                    return currentBuild > build;

                // Unspecified revision component is to be treated as zero
                int currentRevision = current.Revision < 0 ? 0 : current.Revision;
                return currentRevision >= revision;
            }
        }
    }
}