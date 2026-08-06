using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Diagnostics;
using Microsoft.Win32;

namespace pylorak.Windows
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

        private static bool WinVerEqOrGr(int major, int minor, int build)
        {
            var winVersion = new Version(major, minor, build, 0);
            return (Environment.OSVersion.Platform == PlatformID.Win32NT)
                && (Environment.OSVersion.Version >= winVersion);
        }

        private static string GetWindowsVersionString()
        {
            const string UNKNOWN_RELEASE_STR = "????";

            Version winver = Environment.OSVersion.Version;
            try
            {
                string product = RegistryHive.LocalMachine.GetReg64StrValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName") ?? "Windows ??";
                if (Win11OrNewer)
                    product = product.Replace("Windows 10", "Windows 11");

                string releaseName = Win10OrNewer
                    ? (winver.Build > 19042)
                        ? RegistryHive.LocalMachine.GetReg64StrValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion") ?? UNKNOWN_RELEASE_STR
                        : $"v{RegistryHive.LocalMachine.GetReg64StrValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId") ?? UNKNOWN_RELEASE_STR}"
                    : string.Empty;

                if (!string.IsNullOrEmpty(releaseName))
                    product += $" {releaseName}";

                string arch = RuntimeInformation.OSArchitecture switch
                {
                    Architecture.X86 => "x86",
                    Architecture.X64 => "x64",
                    Architecture.Arm64 => "arm64",
                    _ => "unsupported arch"
                };

                string ret = $"{product} (build {winver.Build}) {arch}";
                return ret;
            }
            catch
            {
                return winver.ToString();
            }
        }

        public static Version LibraryVersion { get; } = typeof(VersionInfo).Assembly.GetName().Version;

        public static bool Win7OrNewer { get; } = WinVerEqOrGr(6, 1, 0);
        public static bool Win8OrNewer { get; } = WinVerEqOrGr(6, 2, 0);
        public static bool Win81OrNewer { get; } = WinVerEqOrGr(6, 3, 0);
        public static bool Win10OrNewer { get; } = WinVerEqOrGr(10, 0, 0);
        public static bool Win10v1903_OrNewer { get; } = Win10OrNewer && (Environment.OSVersion.Version.Build >= 18362);
        public static bool Win11OrNewer { get; } = WinVerEqOrGr(10, 0, 2200);

        public static bool IsWow64Process { get; } = SafeNativeMethods.InternalCheckIsWow64();
        public static bool Is64BitProcess { get; } = (IntPtr.Size == 8);
        public static bool Is64BitOs { get; } = Is64BitProcess || IsWow64Process;
        public static string WindowsVersionString { get; } = GetWindowsVersionString();

        public static bool IsWinServer { get; } = SafeNativeMethods.IsWindowsServer();
    }
}
