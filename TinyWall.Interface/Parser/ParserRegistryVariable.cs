using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace TinyWall.Interface.Parser
{
    public sealed class ParserRegistryVariable : ParserVariable
    {
        internal static class NativeMethods
        {
            internal static readonly IntPtr HKEY_CURRENT_USER = new IntPtr(-2147483647);
            internal static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(-2147483646);

            internal enum RegWow64Options : uint
            {
                None = 0,
                KEY_WOW64_64KEY = 0x0100,
                KEY_WOW64_32KEY = 0x0200
            }

            private enum RegistryRights : uint
            {
                KEY_READ = 0x20019
            }

            [DllImport("advapi32", CharSet = CharSet.Auto)]
            private static extern int RegOpenKeyEx(IntPtr hKey, string subKey, uint ulOptions, uint samDesired, out IntPtr hkResult);

            [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Auto)]
            private static extern uint RegQueryValueEx(IntPtr hKey, string lpValueName, IntPtr lpReserved, ref RegistryValueKind lpType, StringBuilder lpData, ref uint lpcbData);

            [DllImport("advapi32", SetLastError = true)]
            private static extern int RegCloseKey(IntPtr hKey);

            internal static string ReadRegKey(IntPtr rootKey, string keyPath, string valueName, RegWow64Options view)
            {
                if (RegOpenKeyEx(rootKey, keyPath, 0, (uint)RegistryRights.KEY_READ | (uint)view, out IntPtr hKey) == 0)
                {
                    try
                    {
                        uint size = 1024;

                        RegistryValueKind type = RegistryValueKind.String;
                        if (RegQueryValueEx(hKey, valueName, IntPtr.Zero, ref type, null, ref size) == 0)
                        {
                            if (type != RegistryValueKind.String)
                                return null;

                            StringBuilder keyBuffer = new StringBuilder((int)size);
                            if (RegQueryValueEx(hKey, valueName, IntPtr.Zero, ref type, keyBuffer, ref size) == 0)
                                return keyBuffer.ToString();
                        }
                    }
                    finally
                    {
                        RegCloseKey(hKey);
                    }
                }

                return null;  // Return null if the value could not be read
            }
        }

        internal const string OPENING_TAG = "{reg:";

        internal override string Resolve(string str)
        {
            try
            {
                // Registry path
                string[] tokens = str.Split(':');
                string keyPath = tokens[0];
                string keyValue = tokens[1];

                // We use a custom function to access 64-bit registry view on dotNet 3.5.
                string val = NativeMethods.ReadRegKey(NativeMethods.HKEY_LOCAL_MACHINE, keyPath, keyValue, NativeMethods.RegWow64Options.KEY_WOW64_64KEY);
                return val ?? str;
            }
            catch
            {
                return str;
            }
        }

        internal static int OpeningTagLength
        {
            get
            {
                return OPENING_TAG.Length;
            }
        }

        internal static bool IsStartTag(string str, int pos)
        {
            int tagLen = OpeningTagLength;
            return (str.Length > pos + tagLen) && (string.CompareOrdinal(OPENING_TAG, 0, str, pos, tagLen)==0);
        }

        internal override string GetOpeningTag()
        {
            return OPENING_TAG;
        }
        internal override int GetOpeningTagLength()
        {
            return OpeningTagLength;
        }

    }
}
