using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace TinyWall.Interface
{
    public enum RegWow64Options
    {
        KEY_WOW64_AUTO = 0,
        KEY_WOW64_64KEY = 0x0100,
        KEY_WOW64_32KEY = 0x0200
    }

    public class Registry
    {
        public static readonly IntPtr HKEY_CURRENT_USER = new IntPtr(-2147483647);
        public static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(-2147483646);

        internal static class NativeMethods
        {
            internal enum RegistryRights : uint
            {
                KEY_READ = 0x20019
            }

            [DllImport("advapi32", CharSet = CharSet.Auto)]
            internal static extern int RegOpenKeyEx(IntPtr hKey, string subKey, uint ulOptions, uint samDesired, out IntPtr hkResult);

            [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Auto)]
            internal static extern uint RegQueryValueEx(IntPtr hKey, string lpValueName, IntPtr lpReserved, ref RegistryValueKind lpType, StringBuilder lpData, ref uint lpcbData);

            [DllImport("advapi32", CharSet = CharSet.Auto, BestFitMapping = false)]
            internal static extern int RegQueryValueEx(IntPtr hKey, string lpValueName, int[] lpReserved, ref RegistryValueKind lpType, [Out] char[] lpData, ref int lpcbData);
    
            [DllImport("advapi32", SetLastError = true)]
            internal static extern int RegCloseKey(IntPtr hKey);
        }

        public static string ReadRegString(IntPtr rootKey, string keyPath, string valueName, RegWow64Options view)
        {
            if (NativeMethods.RegOpenKeyEx(rootKey, keyPath, 0, (uint)NativeMethods.RegistryRights.KEY_READ | (uint)view, out IntPtr hKey) == 0)
            {
                try
                {
                    uint size = 1024;

                    RegistryValueKind type = RegistryValueKind.String;
                    if (NativeMethods.RegQueryValueEx(hKey, valueName, IntPtr.Zero, ref type, null, ref size) == 0)
                    {
                        if (type != RegistryValueKind.String)
                            return null;

                        StringBuilder keyBuffer = new StringBuilder((int)size);
                        if (NativeMethods.RegQueryValueEx(hKey, valueName, IntPtr.Zero, ref type, keyBuffer, ref size) == 0)
                            return keyBuffer.ToString();
                    }
                }
                finally
                {
                    NativeMethods.RegCloseKey(hKey);
                }
            }

            return null;  // Return null if the value could not be read
        }
        public static string[] ReadRegMultiString(IntPtr rootKey, string keyPath, string valueName, RegWow64Options view)
        {
            if (NativeMethods.RegOpenKeyEx(rootKey, keyPath, 0, (uint)NativeMethods.RegistryRights.KEY_READ | (uint)view, out IntPtr hKey) == 0)
            {
                try
                {
                    int size = 1024;

                    RegistryValueKind type = RegistryValueKind.MultiString;
                    if (NativeMethods.RegQueryValueEx(hKey, valueName, null, ref type, null, ref size) == 0)
                    {
                        if (type != RegistryValueKind.MultiString)
                            return null;

                        if (size % 2 == 1) size = checked(size + 1);
                        var blob = new char[size/2];
                        if (NativeMethods.RegQueryValueEx(hKey, valueName, null, ref type, blob, ref size) == 0)
                        {
                            blob[blob.Length - 1] = (char)0;

                            var strings = new List<String>();
                            int cur = 0;
                            int len = blob.Length;

                            while (cur < len)
                            {
                                int nextNull = cur;
                                while (nextNull < len && blob[nextNull] != (char)0)
                                    nextNull++;

                                if (nextNull < len)
                                {
                                    System.Diagnostics.Debug.Assert(blob[nextNull] == (char)0, "blob[nextNull] should be 0");
                                    if (nextNull - cur > 0)
                                        strings.Add(new string(blob, cur, nextNull - cur));
                                    else
                                    {
                                        // we found an empty string.  But if we're at the end of the data, 
                                        // it's just the extra null terminator. 
                                        if (nextNull != len - 1)
                                            strings.Add(string.Empty);
                                    }
                                }
                                else
                                {
                                    strings.Add(new string(blob, cur, len - cur));
                                }
                                cur = nextNull + 1;
                            }

                            var data = new string[strings.Count];
                            strings.CopyTo((string[])data, 0);
                            return data;
                        }
                    }
                }
                finally
                {
                    NativeMethods.RegCloseKey(hKey);
                }
            }

            return null;  // Return null if the value could not be read
        }
    }
}
