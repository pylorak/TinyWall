using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Diagnostics;
using Microsoft.Win32;
using TinyWall.Interface.Internal;

namespace TinyWall.Interface
{
    public static class VersionInfo
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct OsVersionInfoEx
        {
            public uint OSVersionInfoSize;
            public uint MajorVersion;
            public uint MinorVersion;
            public uint BuildNumber;
            public uint PlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string CSDVersion;
            public ushort ServicePackMajor;
            public ushort ServicePackMinor;
            public ushort SuiteMask;
            public byte ProductType;
            public byte Reserved;
        }

        [SuppressUnmanagedCodeSecurity]
        internal static class SafeNativeMethods
        {
            [DllImport("kernel32", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool wow64Process);

            [DllImport("kernel32")]
            static extern ulong VerSetConditionMask(ulong dwlConditionMask, uint dwTypeBitMask, byte dwConditionMask);

            [DllImport("kernel32")]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool VerifyVersionInfo([In] ref OsVersionInfoEx lpVersionInfo, uint dwTypeMask, ulong dwlConditionMask);

            internal static bool InternalCheckIsWow64()
            {
                if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) ||
                    Environment.OSVersion.Version.Major >= 6)
                {
                    try
                    {
                        using Process p = Process.GetCurrentProcess();
                        if (!IsWow64Process(p.Handle, out bool retVal))
                        {
                            return false;
                        }
                        return retVal;
                    }
                    catch { return false; }
                }
                else
                {
                    return false;
                }
            }

            internal static bool IsWindowsServer()
            {
                const byte VER_NT_WORKSTATION = 0x0000001;
                const uint VER_PRODUCT_TYPE = 0x0000080;
                const byte VER_EQUAL = 1;

                var osvi = new OsVersionInfoEx();
                osvi.OSVersionInfoSize = (uint)Marshal.SizeOf(osvi);
                osvi.ProductType = VER_NT_WORKSTATION;
                ulong dwlConditionMask = VerSetConditionMask(0, VER_PRODUCT_TYPE, VER_EQUAL);
                return !VerifyVersionInfo(ref osvi, VER_PRODUCT_TYPE, dwlConditionMask);
            }
        }

        private static bool WinVerEqOrGr(int major, int minor)
        {
            var winVersion = new Version(major, minor, 0, 0);
            return (Environment.OSVersion.Platform == PlatformID.Win32NT)
                && (Environment.OSVersion.Version >= winVersion);
        }

        private static string GetWindowsVersionString()
        {
            Version winver = Environment.OSVersion.Version;
            try
            {
                string product = Utils.GetReg64StrValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName") ?? "Windows ??";
                string build = Utils.GetReg64StrValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuild") ?? "?????";
                string bitness = Is64BitOs ? "64" : "32";

                string ret = $"{product} {bitness}-bit {winver.Major}.{winver.Minor}.{build}";

                // TODO: releaseId is unreliable, use CurrentBuild or otherwise DisplayVersion reg values
                string? releaseId = (winver.Major >= 10) ? Utils.GetReg64StrValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId") : null;
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

        public static bool IsWow64Process { get; } = SafeNativeMethods.InternalCheckIsWow64();
        public static bool Is64BitProcess { get; } = (IntPtr.Size == 8);
        public static bool Is64BitOs { get; } = Is64BitProcess || IsWow64Process;
        public static string WindowsVersionString { get; } = GetWindowsVersionString();

        public static bool IsWinServer { get; } = SafeNativeMethods.IsWindowsServer();
    }
}
