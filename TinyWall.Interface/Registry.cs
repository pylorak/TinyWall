using System;
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
    }
}
