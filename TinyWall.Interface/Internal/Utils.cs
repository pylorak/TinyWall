using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;
using Microsoft.Win32;

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

        public static string GetReg64StrValue(RegistryHive hive, string key, string val)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var subKey = baseKey?.OpenSubKey(key, false);
            return subKey?.GetValue(val) as string;
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
        public static string GetExactPath(string path)
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
