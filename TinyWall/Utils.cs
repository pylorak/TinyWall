using Microsoft.Samples;
using pylorak.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace pylorak.TinyWall
{
    internal static class ExtensionMethods
    {
        internal static void Append(this StringBuilder sb, ReadOnlySpan<char> str)
        {
            for (int i = 0; i < str.Length; ++i)
                sb.Append(str[i]);
        }
    }

    internal static class Utils
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class SafeNativeMethods
        {
            [DllImport("user32.dll")]
            internal static extern IntPtr WindowFromPoint(Point pt);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

            [DllImport("user32.dll")]
            internal static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsImmersiveProcess(IntPtr hProcess);

            [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
            internal static extern uint DnsFlushResolverCache();

            [DllImport("User32.dll", SetLastError = true)]
            internal static extern int GetSystemMetrics(int nIndex);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetNamedPipeClientProcessId(IntPtr Pipe, out ulong ClientProcessId);

            [DllImport("Wer.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
            internal static extern void WerAddExcludedApplication(
                [MarshalAs(UnmanagedType.LPWStr)]
                string pwzExeName,
                [MarshalAs(UnmanagedType.Bool)]
                bool bAllUsers
            );

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.U4)]
            internal static extern int GetLongPathName(
                [MarshalAs(UnmanagedType.LPWStr)]
                string lpszShortPath,
                [MarshalAs(UnmanagedType.LPWStr)]
                StringBuilder lpszLongPath,
                [MarshalAs(UnmanagedType.U4)]
                int cchBuffer
            );

            #region IsMetroActive
            [ComImport, Guid("2246EA2D-CAEA-4444-A3C4-6DE827E44313"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            internal interface IAppVisibility
            {
                HRESULT GetAppVisibilityOnMonitor([In] IntPtr hMonitor, [Out] out MONITOR_APP_VISIBILITY pMode);
                HRESULT IsLauncherVisible([Out] out bool pfVisible);
                HRESULT Advise([In] IAppVisibilityEvents pCallback, [Out] out int pdwCookie);
                HRESULT Unadvise([In] int dwCookie);
            }
            //...
            internal enum HRESULT : long
            {
                S_FALSE = 0x0001,
                S_OK = 0x0000,
                E_INVALIDARG = 0x80070057,
                E_OUTOFMEMORY = 0x8007000E
            }
            internal enum MONITOR_APP_VISIBILITY
            {
                MAV_UNKNOWN = 0,         // The mode for the monitor is unknown
                MAV_NO_APP_VISIBLE = 1,
                MAV_APP_VISIBLE = 2
            }
            [ComImport, Guid("6584CE6B-7D82-49C2-89C9-C6BC02BA8C38"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            internal interface IAppVisibilityEvents
            {
                HRESULT AppVisibilityOnMonitorChanged(
                    [In] IntPtr hMonitor,
                    [In] MONITOR_APP_VISIBILITY previousMode,
                    [In] MONITOR_APP_VISIBILITY currentMode);

                HRESULT LauncherVisibilityChange([In] bool currentVisibleState);
            }
            #endregion

            #region DoMouseRightClick
            [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, IntPtr dwExtraInfo);
            //private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
            //private const uint MOUSEEVENTF_LEFTUP = 0x04;
            private const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
            private const uint MOUSEEVENTF_RIGHTUP = 0x10;
            internal static void DoMouseRightClick()
            {
                //Call the imported function with the cursor's current position
                uint X = (uint)System.Windows.Forms.Cursor.Position.X;
                uint Y = (uint)System.Windows.Forms.Cursor.Position.Y;
                mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, X, Y, 0, IntPtr.Zero);
            }
            #endregion
        }

        private static readonly Random _rng = new();

        public static string ExecutablePath { get; } = System.Reflection.Assembly.GetEntryAssembly().Location;

        public static string HexEncode(byte[] binstr)
        {
            var sb = new StringBuilder();
            foreach (byte oct in binstr)
                sb.Append(oct.ToString(@"X2", CultureInfo.InvariantCulture));

            return sb.ToString();
        }

#if NET48
        // Use string.IsNullOrEmpty() on .Net 5 and newer
        public static bool IsNullOrEmpty([NotNullWhen(false)] string? str)
        {
            return (str is null) || (str == string.Empty);
        }
#endif

        public static T OnlyFirst<T>(IEnumerable<T> items)
        {
            using IEnumerator<T> iter = items.GetEnumerator();
            iter.MoveNext();
            return iter.Current;
        }

        /// <summary>
        /// Returns the correctly cased version of a local file or directory path. Returns the input path on error.
        /// </summary>
        public static string? GetExactPath(string? path)
        {
            try
            {
                // DirectoryInfo accepts either a file path or a directory path,
                // and most of its properties work for either.
                // However, its Exists property only works for a directory path.
                if (!(Directory.Exists(path) || File.Exists(path)))
                    return path;

                var dir = new DirectoryInfo(path);
                var parent = dir.Parent;    // will be null if there is no parent
                var result = string.Empty;

                while (parent != null)
                {
                    result = Path.Combine(OnlyFirst(parent.EnumerateFileSystemInfos(dir.Name)).Name, result);

                    dir = parent;
                    parent = parent.Parent;
                }

                // Handle the root part (i.e., drive letter)
                string root = dir.FullName;
                if (root.Contains(":"))
                {
                    // Drive letter
                    root = root.ToUpperInvariant();
                    result = Path.Combine(root, result);
                    return result;
                }
                else
                {
                    // Error
                    return path;
                }
            }
            catch
            {
                return path;
            }
        }

        internal static void SetRightToLeft(Control ctrl)
        {
            RightToLeft rtl = System.Windows.Forms.Application.CurrentCulture.TextInfo.IsRightToLeft ? RightToLeft.Yes : RightToLeft.No;
            ctrl.RightToLeft = rtl;
        }

        internal static bool IsSystemShuttingDown()
        {
            const int SM_SHUTTINGDOWN = 0x2000;
            return 0 != SafeNativeMethods.GetSystemMetrics(SM_SHUTTINGDOWN);
        }

        internal static uint GetForegroundProcessPid()
        {
            IntPtr hwnd = SafeNativeMethods.GetForegroundWindow();
            _ = SafeNativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
            return pid;
        }

        internal static string ProgramFilesx86()
        {
            if ((8 == IntPtr.Size) || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            else
                return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        internal static void CompressDeflate(string inputFile, string outputFile)
        {
            using var inFile = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
            using var outFile = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
            using var compressedOutFile = new DeflateStream(outFile, CompressionMode.Compress, true);

            byte[] buffer = new byte[4096];
            int numRead;
            while ((numRead = inFile.Read(buffer, 0, buffer.Length)) != 0)
            {
                compressedOutFile.Write(buffer, 0, numRead);
            }
        }

        internal static void DecompressDeflate(string inputFile, string outputFile)
        {
            using var outFile = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
            using var inFile = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
            using var decompressedInFile = new DeflateStream(inFile, CompressionMode.Decompress, true);

            byte[] buffer = new byte[4096];
            int numRead;
            while ((numRead = decompressedInFile.Read(buffer, 0, buffer.Length)) != 0)
            {
                outFile.Write(buffer, 0, numRead);
            }
        }

        internal static string GetPathOfProcessUseTwService(uint pid, Controller controller)
        {
            // Shortcut for special case
            if ((pid == 0) || (pid == 4))
                return "System";

            string ret = GetLongPathName(ProcessManager.GetProcessPath(pid));
            if (string.IsNullOrEmpty(ret))
                ret = controller.TryGetProcessPath(pid);

            return ret;
        }

        internal static string GetPathOfProcess(uint pid)
        {
            // Shortcut for special case
            if ((pid == 0) || (pid == 4))
                return "System";

            return GetLongPathName(ProcessManager.GetProcessPath(pid));
        }

        internal static uint GetPidUnderCursor(int x, int y)
        {
            _ = SafeNativeMethods.GetWindowThreadProcessId(SafeNativeMethods.WindowFromPoint(new System.Drawing.Point(x, y)), out uint procId);
            return procId;
        }

        /// <summary>
        /// Converts a short path to a long path.
        /// </summary>
        /// <param name="shortPath">A path that may contain short path elements (~1).</param>
        /// <returns>The long path. Null or empty if the input is null or empty. Returns the input path in case of error.</returns>
        internal static string GetLongPathName(string? shortPath)
        {
            if (Utils.IsNullOrEmpty(shortPath))
                return string.Empty;

            var builder = new StringBuilder(255);
            int result = SafeNativeMethods.GetLongPathName(shortPath, builder, builder.Capacity);
            if ((result > 0) && (result < builder.Capacity))
            {
                return builder.ToString(0, result);
            }
            else
            {
                if (result > 0)
                {
                    builder = new StringBuilder(result);
                    result = SafeNativeMethods.GetLongPathName(shortPath, builder, builder.Capacity);
                    return builder.ToString(0, result);
                }
                else
                {
                    // Path not found or other error
                    return shortPath;
                }
            }
        }

        internal static string RandomString(int length)
        {
            const string chars = @"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            char[] buffer = new char[length];
            for (int i = 0; i < length; i++)
            {
                buffer[i] = chars[_rng.Next(chars.Length)];
            }
            return new string(buffer);
        }

        /*
        private static void PadToMultipleOf(ref byte[] src, int pad)
        {
            int len = (src.Length + pad - 1) / pad * pad;
            Array.Resize(ref src, len);
        }

        internal static byte[] ProtectString(string s)
        {
            byte[] rawString = Encoding.UTF8.GetBytes(s);
            PadToMultipleOf(ref rawString, 16);
            ProtectedMemory.Protect(rawString, MemoryProtectionScope.SameProcess);
            return rawString;
        }

        internal static string UnprotectString(byte[] raw)
        {
            byte[] copy = new byte[raw.Length];
            Array.Copy(raw, copy, raw.Length);
            ProtectedMemory.Unprotect(copy, MemoryProtectionScope.SameProcess);
            return Encoding.UTF8.GetString(copy);
        }
        */

        /*
        internal static void EncryptToStream(byte[] data, string key, string IV, Stream stream)
        {
            using (AesCryptoServiceProvider symmetricKey = new AesCryptoServiceProvider())
            {

                // It is reasonable to set encryption mode to Cipher Block Chaining
                // (CBC). Use default options for other symmetric key parameters.
                symmetricKey.Mode = CipherMode.CBC;
                symmetricKey.Key = Encoding.ASCII.GetBytes(key);
                symmetricKey.IV = Encoding.ASCII.GetBytes(IV);

                // Define cryptographic stream (always use Write mode for encryption).
                using (CryptoStream cryptoStream = new CryptoStream(stream, symmetricKey.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    // Start encrypting.
                    cryptoStream.Write(data, 0, data.Length);

                    // Finish encrypting.
                    cryptoStream.FlushFinalBlock();
                }
            }
        }

        internal static byte[] DecryptFromStream(int nBytes, Stream stream, string key, string IV)
        {
            byte[] data = new byte[nBytes];
            using (AesCryptoServiceProvider symmetricKey = new AesCryptoServiceProvider())
            {

                // It is reasonable to set encryption mode to Cipher Block Chaining
                // (CBC). Use default options for other symmetric key parameters.
                symmetricKey.Mode = CipherMode.CBC;
                symmetricKey.Key = Encoding.ASCII.GetBytes(key);
                symmetricKey.IV = Encoding.ASCII.GetBytes(IV);

                // Define cryptographic stream (always use Write mode for encryption).
                using (CryptoStream cryptoStream = new CryptoStream(stream, symmetricKey.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    // Start encrypting.
                    cryptoStream.Read(data, 0, data.Length);
                }

                return data;
            }
        }
        */

        internal static T DeepClone<T>(T obj) where T : ISerializable<T>
        {
            return SerialisationHelper.Deserialise(SerialisationHelper.Serialise(obj), obj);
        }

        internal static bool StringArrayContains(string[] arr, string val, StringComparison opts = StringComparison.Ordinal)
        {
            for (int i = 0; i < arr.Length; ++i)
            {
                if (string.Equals(arr[i], val, opts))
                    return true;
            }

            return false;
        }

        internal static Process StartProcess(string path, string args, bool asAdmin, bool hideWindow = false)
        {
            var psi = new ProcessStartInfo(path, args);
            psi.WorkingDirectory = Path.GetDirectoryName(path);
            if (asAdmin)
            {
                psi.Verb = "runas";
                psi.UseShellExecute = true;
            }
            if (hideWindow)
                psi.WindowStyle = ProcessWindowStyle.Hidden;

            return Process.Start(psi);
        }

        internal static bool RunningAsAdmin()
        {
            var wi = WindowsIdentity.GetCurrent();
            var wp = new WindowsPrincipal(wi);
            return wp.IsInRole(WindowsBuiltInRole.Administrator);
        }

        internal static Bitmap ScaleImage(Bitmap originalImage, float scaleX, float scaleY)
        {
            int newWidth = (int)Math.Round(originalImage.Width * scaleX);
            int newHeight = (int)Math.Round(originalImage.Height * scaleY);

            var newImage = new Bitmap(originalImage, newWidth, newHeight);
            try
            {
                using (Graphics g = Graphics.FromImage(newImage))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(originalImage, 0, 0, newImage.Width, newImage.Height);
                }

                return newImage;
            }
            catch
            {
                newImage.Dispose();
                throw;
            }
        }

        internal static Bitmap ResizeImage(Bitmap originalImage, int maxWidth, int maxHeight)
        {
            int newWidth = originalImage.Width;
            int newHeight = originalImage.Height;
            double aspectRatio = (double)originalImage.Width / (double)originalImage.Height;

            if (aspectRatio <= 1 && originalImage.Width > maxWidth)
            {
                newWidth = maxWidth;
                newHeight = (int)Math.Round(newWidth / aspectRatio);
            }
            else if (aspectRatio > 1 && originalImage.Height > maxHeight)
            {
                newHeight = maxHeight;
                newWidth = (int)Math.Round(newHeight * aspectRatio);
            }

            var newImage = new Bitmap(originalImage, newWidth, newHeight);
            try
            {
                using (Graphics g = Graphics.FromImage(newImage))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(originalImage, 0, 0, newImage.Width, newImage.Height);
                }

                return newImage;
            }
            catch
            {
                newImage.Dispose();
                throw;
            }
        }

        internal static Bitmap GetIconContained(string filePath, int targetWidth, int targetHeight)
        {
            IconTools.ShellIconSize icnSize = IconTools.ShellIconSize.LargeIcon;
            if ((targetHeight == 16) && (targetWidth == 16))
                icnSize = IconTools.ShellIconSize.SmallIcon;

            using var icon = IconTools.GetIconForExtension(filePath, icnSize);
            if ((icon.Width == targetWidth) && (icon.Height == targetHeight))
            {
                return icon.ToBitmap();
            }
            if ((icon.Height > targetHeight) || (icon.Width > targetWidth))
            {
                using var bmp = icon.ToBitmap();
                return Utils.ResizeImage(bmp, targetWidth, targetHeight);
            }
            else
            {
                using var bmp = icon.ToBitmap();
                float scale = Math.Min((float)targetWidth / icon.Width, (float)targetHeight / icon.Height);
                return Utils.ScaleImage(bmp, (int)Math.Round(scale * icon.Width), (int)Math.Round(scale * icon.Height));
            }
        }

        private static float? _DpiScalingFactor;
        internal static float DpiScalingFactor
        {
            get
            {
                if (!_DpiScalingFactor.HasValue)
                {
                    using System.Drawing.Graphics graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
                    float dpiX = graphics.DpiX;
                    float dpiY = graphics.DpiY;
                    _DpiScalingFactor = dpiX / 96.0f;
                }

                return _DpiScalingFactor.Value;
            }
        }

        internal static void CentreControlInParent(Control control)
        {
            Control parent = control.Parent;

            control.Location = new Point(
                parent.Width / 2 - control.Width / 2,
                parent.Height / 2 - control.Height / 2
                );
        }

        internal static void FixupFormPosition(Form form)
        {
            // Place window to top-left corner of working area if window is too much off-screen
            Rectangle formVisibleArea = Rectangle.Intersect(SystemInformation.VirtualScreen, form.Bounds);
            if ((formVisibleArea.Width < 100) || (formVisibleArea.Height < 100))
                form.Location = Screen.PrimaryScreen.WorkingArea.Location;
        }

        internal static void Invoke(SynchronizationContext syncCtx, SendOrPostCallback method)
        {
            if (syncCtx != null)
                syncCtx.Send(method, null);
        }

        internal static void Invoke(Control ctrl, MethodInvoker method)
        {
            if (ctrl.InvokeRequired)
                ctrl.BeginInvoke(method);
            else
                method.Invoke();
        }

        internal static void SplitFirstLine(string str, out string firstLine, out string restLines)
        {
            string[] lines = str.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            firstLine = lines[0];
            restLines = string.Empty;

            if (lines.Length > 1)
            {
                restLines = lines[1];
                for (int i = 2; i < lines.Length; ++i)
                    restLines += Environment.NewLine + lines[i];
            }
        }

        internal static DialogResult ShowMessageBox(string msg, string title, TaskDialogueCommonButtons buttons, TaskDialogueIcon icon, IWin32Window? parent = null)
        {
            Utils.SplitFirstLine(msg, out string firstLine, out string contentLines);

            var taskDialog = new TaskDialogue();
            taskDialog.WindowTitle = title;
            taskDialog.MainInstruction = firstLine;
            taskDialog.CommonButtons = buttons;
            taskDialog.MainIcon = icon;
            taskDialog.Content = contentLines;
            if (parent is null)
                return (DialogResult)taskDialog.Show();
            else
                return (DialogResult)taskDialog.Show(parent);
        }

        internal static int GetRandomNumber()
        {
            return _rng.Next(0, int.MaxValue);
        }

        internal static Version TinyWallVersion { get; } = typeof(Utils).Assembly.GetName().Version;

        private readonly static object logLocker = new();
        internal static readonly string LOG_ID_SERVICE = "service";
        internal static readonly string LOG_ID_GUI = "gui";
        internal static readonly string LOG_ID_INSTALLER = "installer";
        internal static void LogException(Exception e, string logname)
        {
            Utils.Log(
                string.Join(
                    Environment.NewLine, new string[] {
                    $"TinyWall version: {Utils.TinyWallVersion}",
                    $"Windows version: {VersionInfo.WindowsVersionString}",
                    e.ToString()
                }),
                logname
            );
        }
        internal static void Log(string info, string logname)
        {
            try
            {
                lock (logLocker)
                {
                    // First, remove deprecated log files if any is found
                    // TODO: This can probably be removed in the future
                    string[] old_logs = new string[] {
                        Path.Combine(Utils.AppDataPath, "errorlog"),
                        Path.Combine(Utils.AppDataPath, "service.log"),
                        Path.Combine(Utils.AppDataPath, "client.log"),
                    };

                    foreach (string file in old_logs)
                    {
                        try
                        {
                            if (File.Exists(file))
                                File.Delete(file);
                        }
                        catch { }
                    }

                    // Name of the current log file
                    string logdir = Path.Combine(Utils.AppDataPath, "logs");
                    string logfile = Path.Combine(logdir, $"{logname}.log");

                    if (!Directory.Exists(logdir))
                        Directory.CreateDirectory(logdir);

                    // Only log if log file has not yet reached a certain size
                    if (File.Exists(logfile))
                    {
                        var fi = new FileInfo(logfile);
                        if (fi.Length > 512 * 1024)
                        {
                            // Truncate file back to zero
                            using var fs = new FileStream(logfile, FileMode.Truncate, FileAccess.Write);
                        }
                    }

                    // Do the logging
                    using var sw = new StreamWriter(logfile, true, Encoding.UTF8);
                    sw.WriteLine();
                    sw.WriteLine("------- " + DateTime.Now.ToString(CultureInfo.InvariantCulture) + " -------");
                    sw.WriteLine(info);
                    sw.WriteLine();
                }
            }
            catch
            {
                // Ignore exceptions - logging should not itself cause new problems
            }
        }

        internal static void SetDoubleBuffering(Control control, bool enable)
        {
            try
            {
                var doubleBufferPropertyInfo = control.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                doubleBufferPropertyInfo.SetValue(control, enable, null);
            }
            catch { }
        }

        internal static void FlushDnsCache()
        {
            _ = SafeNativeMethods.DnsFlushResolverCache();
        }

        internal static string AppDataPath
        {
            get
            {
#if DEBUG
                return Path.GetDirectoryName(Utils.ExecutablePath);
#else
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TinyWall");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
#endif
            }
        }

        public static bool EqualsCaseInsensitive(string a, string b)
        {
            if (a == b)
                return true;

            return (a != null) && a.Equals(b, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
