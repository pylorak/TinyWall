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
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        internal static async Task<T> WaitAsync<T>(this Task<T> task, CancellationToken ct)
        {
            if (ct == CancellationToken.None) return await task;
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (ct.Register(s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task))
                {
                    ct.ThrowIfCancellationRequested();
                }
            }
            return await task;
        }

        internal static async Task WaitAsync(this Task task, CancellationToken ct)
        {
            if (ct == CancellationToken.None) { await task; return; }
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (ct.Register(s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task))
                {
                    ct.ThrowIfCancellationRequested();
                }
            }
            await task;
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
            internal static extern bool GetNamedPipeClientProcessId(IntPtr pipe, out ulong clientProcessId);

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
                Hresult GetAppVisibilityOnMonitor([In] IntPtr hMonitor, [Out] out MonitorAppVisibility pMode);
                Hresult IsLauncherVisible([Out] out bool pfVisible);
                Hresult Advise([In] IAppVisibilityEvents pCallback, [Out] out int pdwCookie);
                Hresult Unadvise([In] int dwCookie);
            }

            internal enum Hresult : long
            {
                S_FALSE = 0x0001,
                S_OK = 0x0000,
                E_INVALIDARG = 0x80070057,
                E_OUTOFMEMORY = 0x8007000E
            }

            internal enum MonitorAppVisibility
            {
                MAV_UNKNOWN = 0,         // The mode for the monitor is unknown
                MAV_NO_APP_VISIBLE = 1,
                MAV_APP_VISIBLE = 2
            }

            [ComImport, Guid("6584CE6B-7D82-49C2-89C9-C6BC02BA8C38"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            internal interface IAppVisibilityEvents
            {
                Hresult AppVisibilityOnMonitorChanged(
                    [In] IntPtr hMonitor,
                    [In] MonitorAppVisibility previousMode,
                    [In] MonitorAppVisibility currentMode);

                Hresult LauncherVisibilityChange([In] bool currentVisibleState);
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
                var x = (uint)Cursor.Position.X;
                var y = (uint)Cursor.Position.Y;
                mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, x, y, 0, IntPtr.Zero);
            }
            #endregion
        }

        private static readonly Random Rng = new();

        public static string ExecutablePath { get; } = System.Reflection.Assembly.GetEntryAssembly()!.Location;

        public static string HexEncode(byte[] binstr)
        {
            var sb = new StringBuilder();

            foreach (byte oct in binstr)
                sb.Append(oct.ToString(@"X2", CultureInfo.InvariantCulture));

            return sb.ToString();
        }

        public static bool IsNullOrEmpty([NotNullWhen(false)] string? str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

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

                if (path != null)
                {
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
                    var root = dir.FullName;

                    if (!root.Contains(":")) return path;

                    // Drive letter
                    root = root.ToUpperInvariant();
                    result = Path.Combine(root, result);
                    return result;

                    // Error
                }
            }
            catch
            {
                return path;
            }

            return path;
        }

        internal static void SetRightToLeft(Control ctrl)
        {
            var rtl = Application.CurrentCulture.TextInfo.IsRightToLeft ? RightToLeft.Yes : RightToLeft.No;
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
            if ((8 == IntPtr.Size) || (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)")!;

            return Environment.GetEnvironmentVariable("ProgramFiles")!;
        }

        internal static void CompressDeflate(string inputFile, string outputFile)
        {
            using var inFile = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
            using var outFile = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
            using var compressedOutFile = new DeflateStream(outFile, CompressionMode.Compress, true);

            var buffer = new byte[4096];
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

            var buffer = new byte[4096];
            int numRead;

            while ((numRead = decompressedInFile.Read(buffer, 0, buffer.Length)) != 0)
            {
                outFile.Write(buffer, 0, numRead);
            }
        }

        internal static string GetPathOfProcessUseTwService(uint pid, Controller? controller)
        {
            // Shortcut for special case
            if (pid is 0 or 4)
                return "System";

            var ret = GetLongPathName(ProcessManager.GetProcessPath(pid));

            if (string.IsNullOrEmpty(ret) && controller != null)
                ret = controller.TryGetProcessPath(pid);

            return ret ?? string.Empty;
        }

        internal static string GetPathOfProcess(uint pid)
        {
            // Shortcut for special case
            return pid is 0 or 4 ? "System" : GetLongPathName(ProcessManager.GetProcessPath(pid));
        }

        internal static uint GetPidUnderCursor(int x, int y)
        {
            _ = SafeNativeMethods.GetWindowThreadProcessId(SafeNativeMethods.WindowFromPoint(new Point(x, y)), out uint procId);
            return procId;
        }

        /// <summary>
        /// Converts a short path to a long path.
        /// </summary>
        /// <param name="shortPath">A path that may contain short path elements (~1).</param>
        /// <returns>The long path. Null or empty if the input is null or empty. Returns the input path in case of error.</returns>
        internal static string GetLongPathName(string? shortPath)
        {
            if (IsNullOrEmpty(shortPath))
                return string.Empty;

            var builder = new StringBuilder(255);
            var result = SafeNativeMethods.GetLongPathName(shortPath, builder, builder.Capacity);

            switch (result)
            {
                case > 0 when (result < builder.Capacity):
                    return builder.ToString(0, result);
                case > 0:
                    builder = new StringBuilder(result);
                    result = SafeNativeMethods.GetLongPathName(shortPath, builder, builder.Capacity);
                    return builder.ToString(0, result);
                default:
                    // Path not found or other error
                    return shortPath;
            }
        }

        internal static string RandomString(int length)
        {
            const string CHARS = @"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var buffer = new char[length];

            for (var i = 0; i < length; i++)
            {
                buffer[i] = CHARS[Rng.Next(CHARS.Length)];
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
            return arr.Any(t => string.Equals(t, val, opts));
        }

        internal static Process StartProcess(string path, string args, bool asAdmin, bool hideWindow = false)
        {
            var psi = new ProcessStartInfo(path, args)
            {
                WorkingDirectory = Path.GetDirectoryName(path)!
            };

            if (asAdmin)
            {
                psi.Verb = "runas";
                psi.UseShellExecute = true;
            }

            if (hideWindow)
                psi.WindowStyle = ProcessWindowStyle.Hidden;

            return Process.Start(psi)!;
        }

        internal static bool RunningAsAdmin()
        {
            var wi = WindowsIdentity.GetCurrent();
            var wp = new WindowsPrincipal(wi);
            return wp.IsInRole(WindowsBuiltInRole.Administrator);
        }

        internal static Bitmap ScaleImage(Bitmap originalImage, float scaleX, float scaleY)
        {
            var newWidth = (int)Math.Round(originalImage.Width * scaleX);
            var newHeight = (int)Math.Round(originalImage.Height * scaleY);

            var newImage = new Bitmap(originalImage, newWidth, newHeight);
            try
            {
                using var g = Graphics.FromImage(newImage);

                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(originalImage, 0, 0, newImage.Width, newImage.Height);

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
            var newWidth = originalImage.Width;
            var newHeight = originalImage.Height;
            var aspectRatio = originalImage.Width / originalImage.Height;

            switch (aspectRatio)
            {
                case <= 1 when originalImage.Width > maxWidth:
                    newWidth = maxWidth;
                    newHeight = newWidth / aspectRatio;
                    break;
                case > 1 when originalImage.Height > maxHeight:
                    newHeight = maxHeight;
                    newWidth = newHeight * aspectRatio;
                    break;
            }

            var newImage = new Bitmap(originalImage, newWidth, newHeight);
            try
            {
                using var g = Graphics.FromImage(newImage);

                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(originalImage, 0, 0, newImage.Width, newImage.Height);

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
            var icnSize = IconTools.ShellIconSize.LargeIcon;

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
                return ResizeImage(bmp, targetWidth, targetHeight);
            }
            else
            {
                using var bmp = icon.ToBitmap();
                var scale = Math.Min((float)targetWidth / icon.Width, (float)targetHeight / icon.Height);
                return ScaleImage(bmp, (int)Math.Round(scale * icon.Width), (int)Math.Round(scale * icon.Height));
            }
        }

        private static float? _dpiScalingFactor;
        internal static float DpiScalingFactor
        {
            get
            {
                if (_dpiScalingFactor.HasValue) return _dpiScalingFactor.Value;

                using var graphics = Graphics.FromHwnd(IntPtr.Zero);

                var dpiX = graphics.DpiX;
                //var dpiY = graphics.DpiY;

                _dpiScalingFactor = dpiX / 96.0f;

                return _dpiScalingFactor.Value;
            }
        }

        internal static void CentreControlInParent(Control control)
        {
            var parent = control.Parent;

            control.Location = new Point(
                parent.Width / 2 - control.Width / 2,
                parent.Height / 2 - control.Height / 2
                );
        }

        internal static void FixupFormPosition(Form form)
        {
            // Place window to top-left corner of working area if window is too much off-screen
            var formVisibleArea = Rectangle.Intersect(SystemInformation.VirtualScreen, form.Bounds);

            if ((formVisibleArea.Width < 100) || (formVisibleArea.Height < 100))
                form.Location = Screen.PrimaryScreen.WorkingArea.Location;
        }

        internal static void Invoke(SynchronizationContext? syncCtx, SendOrPostCallback method)
        {
            syncCtx?.Send(method, null);
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
            var lines = str.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            firstLine = lines[0];
            restLines = string.Empty;

            if (lines.Length <= 1) return;

            restLines = lines[1];
            for (var i = 2; i < lines.Length; ++i)
                restLines += Environment.NewLine + lines[i];
        }

        internal static DialogResult ShowMessageBox(string msg, string title, TaskDialogCommonButtons buttons, TaskDialogIcon icon, IWin32Window? parent = null)
        {
            SplitFirstLine(msg, out var firstLine, out var contentLines);

            var taskDialogue = new TaskDialog
            {
                WindowTitle = title,
                MainInstruction = firstLine,
                CommonButtons = buttons,
                MainIcon = icon,
                Content = contentLines
            };

            if (parent is null)
                return (DialogResult)taskDialogue.Show();

            return (DialogResult)taskDialogue.Show(parent);
        }

        internal static int GetRandomNumber()
        {
            return Rng.Next(0, int.MaxValue);
        }

        internal static Version TinyWallVersion { get; } = typeof(Utils).Assembly.GetName().Version;

        private static readonly object LogLocker = new();
        internal static readonly string LOG_ID_SERVICE = "service";
        internal static readonly string LOG_ID_GUI = "gui";
        internal static readonly string LOG_ID_INSTALLER = "installer";
        internal static void LogException(Exception e, string logname)
        {
            Log(string.Join(Environment.NewLine, $"TinyWall version: {Utils.TinyWallVersion}", $"Windows version: {VersionInfo.WindowsVersionString}", e.ToString()), logname);
        }
        internal static void Log(string info, string logname)
        {
            try
            {
                lock (LogLocker)
                {
                    // First, remove deprecated log files if any is found
                    // TODO: This can probably be removed in the future
                    string[] oldLogs = {
                        Path.Combine(AppDataPath, "errorlog"),
                        Path.Combine(AppDataPath, "service.log"),
                        Path.Combine(AppDataPath, "client.log"),
                    };

                    foreach (var file in oldLogs)
                    {
                        try
                        {
                            if (File.Exists(file))
                                File.Delete(file);
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    // Name of the current log file
                    var logdir = Path.Combine(Utils.AppDataPath, "logs");
                    var logfile = Path.Combine(logdir, $"{logname}.log");

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
                    sw.WriteLine($"------- {DateTime.Now.ToString(CultureInfo.InvariantCulture)} -------");
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
                doubleBufferPropertyInfo!.SetValue(control, enable, null);
            }
            catch
            {
                // ignored
            }
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
                return Path.GetDirectoryName(Utils.ExecutablePath)!;
#else
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TinyWall");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
#endif
            }
        }

        public static bool EqualsCaseInsensitive(string? a, string b)
        {
            if (a == b)
                return true;

            return (a != null) && a.Equals(b, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
