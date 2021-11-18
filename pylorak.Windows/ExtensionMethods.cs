using System;
using Microsoft.Win32;

namespace pylorak.Windows
{
    public static class ExtensionMethods
    {
        public static string? GetReg64StrValue(this RegistryHive hive, string key, string val)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var subKey = baseKey?.OpenSubKey(key, false);
            return subKey?.GetValue(val) as string;
        }
    }
}
