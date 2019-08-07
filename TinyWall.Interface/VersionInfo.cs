using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Diagnostics;
using Microsoft.Win32;

namespace TinyWall.Interface
{
    public static class VersionInfo
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class SafeNativeMethods
        {
            [DllImport("kernel32", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool wow64Process);

            internal static bool InternalCheckIsWow64()
            {
                if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) ||
                    Environment.OSVersion.Version.Major >= 6)
                {
                    using (Process p = Process.GetCurrentProcess())
                    {
                        try
                        {
                            bool retVal;
                            if (!IsWow64Process(p.Handle, out retVal))
                            {
                                return false;
                            }
                            return retVal;
                        }
                        catch { return false; }
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        private static bool WinVerEqOrGr(int major, int minor)
        {
            Version winVersion = new Version(major, minor, 0, 0);
            return (Environment.OSVersion.Platform == PlatformID.Win32NT)
                && (Environment.OSVersion.Version >= winVersion);
        }

        private static string GetWindowsVersionString()
        {
            Version winver = Environment.OSVersion.Version;
            try
            {
                string product = Registry.ReadRegString(Registry.HKEY_LOCAL_MACHINE, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", RegWow64Options.KEY_WOW64_64KEY);
                string build = Registry.ReadRegString(Registry.HKEY_LOCAL_MACHINE, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuild", RegWow64Options.KEY_WOW64_64KEY);
                string releaseId = (winver.Major >= 10) ? Registry.ReadRegString(Registry.HKEY_LOCAL_MACHINE, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", RegWow64Options.KEY_WOW64_64KEY) : null;
                string bitness = Is64BitOs ? "64" : "32";

                string ret = $"{product} {bitness}-bits {winver.Major}.{winver.Minor}.{build}";
                if (!string.IsNullOrEmpty(releaseId))
                    ret += $" (v{releaseId})";

                return ret;
            }
            catch
            {
                return winver.ToString();
            }
        }

        public static Version LibraryVersion { get; } = typeof(VersionInfo).Assembly.GetName().Version;

        public static bool Win7OrNewer { get; } = WinVerEqOrGr(6, 1);
        public static bool Win8OrNewer { get; } = WinVerEqOrGr(6, 2);
        public static bool Win81OrNewer { get; } = WinVerEqOrGr(6, 3);
        public static bool Win10OrNewer { get; } = WinVerEqOrGr(10, 0);

        public static bool Is64BitProcess { get; } = (IntPtr.Size == 8);
        public static bool Is64BitOs { get; } = Is64BitProcess || SafeNativeMethods.InternalCheckIsWow64();
        public static string WindowsVersionString { get; } = GetWindowsVersionString();

    }
}
