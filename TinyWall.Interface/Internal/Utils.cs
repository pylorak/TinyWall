using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;

namespace TinyWall.Interface.Internal
{
    public static class Utils
    {
        private static readonly Random _rng = new Random();

        public static string HexEncode(byte[] binstr)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte oct in binstr)
                sb.Append(oct.ToString(@"X2", CultureInfo.InvariantCulture));

            return sb.ToString();
        }

        /// <summary>
        /// Returns the correctly cased version of a local file or directory path. Returns the input path on error.
        /// </summary>
        public static string GetExactPath(string path)
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
                // TODO: For .NET 4.8+, GetFileSystemInfos() should be replaced with EnumerateFileSystemInfos()
                result = Path.Combine(parent.GetFileSystemInfos(dir.Name)[0].Name, result);

                dir = parent;
                parent = parent.Parent;
            }

            // Handle the root part (i.e., drive letter)
            string root = dir.FullName;
            if (root.Contains(":"))
            {
                // drive letter
                root = root.ToUpperInvariant();
                result = Path.Combine(root, result);
            }
            else
            {
                // Error
                return path;
            }

            return result;
        }

        public static string ExecutablePath { get; } = System.Reflection.Assembly.GetEntryAssembly().Location;

        public static string RandomString(int length)
        {
            const string chars = @"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            char[] buffer = new char[length];
            for (int i = 0; i < length; i++)
            {
                buffer[i] = chars[_rng.Next(chars.Length)];
            }
            return new string(buffer);
        }
    }
}
