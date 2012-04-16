using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Samples;

namespace PKSoft
{
    internal class Utils
    {
        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            internal static extern IntPtr WindowFromPoint(Point pt);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetProcessWorkingSetSize(IntPtr process,
                UIntPtr minimumWorkingSetSize, UIntPtr maximumWorkingSetSize);
        }

        private static readonly Random _rng = new Random();

        /*
        internal static List<string> GetDNSServers()
        {
            List<string> servers = new List<string>();

            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            for (int i = 0; i < interfaces.Length; ++i)
            {
                NetworkInterface iface = interfaces[i];
                IPAddressCollection dnscollection = iface.GetIPProperties().DnsAddresses;
                for (int j = 0; j < dnscollection.Count; ++j)
                    servers.Add(dnscollection[j].ToString());
            }

            return servers;
        }
        */

        internal static void CompressDeflate(string inputFile, string outputFile)
        {
            // Get the stream of the source file.
            using (FileStream inFile = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            {
                // Create the compressed file.
                FileStream outFile = null;
                try
                {
                    outFile = new FileStream(outputFile, FileMode.Create, FileAccess.Write);

                    using (DeflateStream Compress = new DeflateStream(outFile, CompressionMode.Compress, true))
                    {
                        outFile = null;

                        // Copy the source file into 
                        // the compression stream.
                        byte[] buffer = new byte[4096];
                        int numRead;
                        while ((numRead = inFile.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            Compress.Write(buffer, 0, numRead);
                        }
                    }
                }
                finally
                {
                    if (outFile != null)
                        outFile.Dispose();
                }
            }
        }

        internal static void DecompressDeflate(string inputFile, string outputFile)
        {
            // Create the decompressed file.
            using (FileStream outFile = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            {
                // Get the stream of the source file.
                FileStream inFile = null;
                try
                {
                    inFile = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
                    using (DeflateStream Decompress = new DeflateStream(inFile, CompressionMode.Decompress, true))
                    {
                        inFile = null;

                        //Copy the decompression stream into the output file.
                        byte[] buffer = new byte[4096];
                        int numRead;
                        while ((numRead = Decompress.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            outFile.Write(buffer, 0, numRead);
                        }
                    }
                }
                finally
                {
                    if (inFile != null)
                        inFile.Dispose();
                }
            }
        }

        internal static string GetProcessMainModulePath(Process p)
        {
            try
            {
                return p.MainModule.FileName;
            }
            catch
            {
                Message resp = GlobalInstances.CommunicationMan.QueueMessage(new Message(TWControllerMessages.GET_PROCESS_PATH, p.Id)).GetResponse();
                if (resp.Command == TWControllerMessages.RESPONSE_OK)
                    return resp.Arguments[0] as string;
                else
                    return null;
            }
        }
        
        internal static string GetExecutableUnderCursor(int x, int y)
        {
            // Get process id under cursor
            int ProcId;
            int dummy = NativeMethods.GetWindowThreadProcessId(NativeMethods.WindowFromPoint(new System.Drawing.Point(x, y)), out ProcId);

            // Get executable of process
            using (Process p = Process.GetProcessById(ProcId))
            {
                return Utils.GetProcessMainModulePath(p);
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

        internal static string HexEncode(byte[] binstr)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte oct in binstr)
                sb.Append(oct.ToString(@"X2", CultureInfo.InvariantCulture));

            return sb.ToString();
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

        internal static T DeepClone<T>(T obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }

        internal static bool ArrayContains<T>(T[] arr, T val)
        {
            for (int i = 0; i < arr.Length; ++i)
            {
                if (arr[i].Equals(val))
                    return true;
            }

            return false;
        }

        internal static void RunAtStartup(string ApplicationName, string ApplicationPath)
        {
            using (RegistryKey runKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", RegistryKeyPermissionCheck.ReadWriteSubTree, System.Security.AccessControl.RegistryRights.FullControl))
            {
                if (string.IsNullOrEmpty(ApplicationPath))
                    runKey.DeleteValue(ApplicationName, false);
                else
                    runKey.SetValue(ApplicationName, ApplicationPath);
            }
        }

        internal static Process StartProcess(string path, string args, bool asAdmin, bool hideWindow = false)
        {
            ProcessStartInfo psi = new ProcessStartInfo(path, args);
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
            WindowsIdentity wi = WindowsIdentity.GetCurrent();
            WindowsPrincipal wp = new WindowsPrincipal(wi);
            return wp.IsInRole(WindowsBuiltInRole.Administrator);
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

            Bitmap newImage = new Bitmap(originalImage, newWidth, newHeight);
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

        internal static Bitmap GetIcon(string filePath, int maxWidth, int maxHeight)
        {
            using (Icon icn = Icon.ExtractAssociatedIcon(filePath))
            using (Bitmap bmp = icn.ToBitmap())
            {
                return Utils.ResizeImage(bmp, maxWidth, maxHeight);
            }
        }

        internal static void CenterControlInParent(Control control)
        {
            Control parent = control.Parent;

            control.Location = new Point(
                parent.Width / 2 - control.Width / 2,
                parent.Height / 2 - control.Height / 2
                );
        }

        internal static void Invoke(Control ctrl, MethodInvoker method)
        {
            if (ctrl.InvokeRequired)
                ctrl.Invoke(method);
            else
                method();
        }

        internal static string ProgramFilesx86()
        {
            if ((8 == IntPtr.Size) || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            else
                return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        internal static void SplitFirstLine(string str, out string firstLine, out string restLines)
        {
            string[] lines = str.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            firstLine = lines[0];
            restLines = null;

            if (lines.Length > 1)
            {
                restLines = lines[1];
                for (int i = 2; i < lines.Length; ++i)
                    restLines += Environment.NewLine + lines[i];
            }
        }

        internal static DialogResult ShowMessageBox(IWin32Window owner, string msg, string title, TaskDialogCommonButtons buttons, TaskDialogIcon icon)
        {
            string firstLine, contentLines;
            Utils.SplitFirstLine(msg, out firstLine, out contentLines);

            TaskDialog taskDialog = new TaskDialog();
            taskDialog.WindowTitle = title;
            taskDialog.MainInstruction = firstLine;
            taskDialog.CommonButtons = buttons;
            taskDialog.MainIcon = icon;
            taskDialog.Content = contentLines;
            return (DialogResult)taskDialog.Show(owner);
        }

        private static string _ExecutablePath = null;
        internal static string ExecutablePath
        {
            get
            {
                if (_ExecutablePath == null)
                {
                    _ExecutablePath = System.Reflection.Assembly.GetEntryAssembly().Location;
                }
                return _ExecutablePath;
            }
        }

        private static Random RndGenerator = null;
        internal static int GetRandomNumber()
        {
            if (RndGenerator == null)
                RndGenerator = new Random();
            return RndGenerator.Next(int.MaxValue);
        }

        private static object logLocker = new object();
        internal static void LogCrash(Exception e)
        {
            lock (logLocker)
            {
                string logfile = Path.Combine(SettingsManager.AppDataPath, "errorlog");

                // Only log if log file has not yet reached a certain size
                if (File.Exists(logfile))
                {
                    FileInfo fi = new FileInfo(logfile);
                    if (fi.Length > 1024 * 1024)
                        return;
                }

                // Do the logging
                using (StreamWriter sw = new StreamWriter(logfile, true, Encoding.UTF8))
                {
                    sw.WriteLine();
                    sw.WriteLine("------- " + DateTime.Now.ToString(CultureInfo.InvariantCulture) + " -------");
                    sw.WriteLine(e.ToString());
                    sw.WriteLine();
                }
            }
        }

        internal static void MinimizeMemory()
        {
            System.Threading.ThreadPool.QueueUserWorkItem((WaitCallback)delegate(object dummy)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                NativeMethods.SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle,
                    (UIntPtr)0xFFFFFFFF, (UIntPtr)0xFFFFFFFF);
            });
        }
    }
}
