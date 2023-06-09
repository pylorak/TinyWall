﻿using System;

namespace pylorak.Windows.WFP
{
    public static class VersionInfo
    {
        private static bool WinVerEqOrGr(int major, int minor)
        {
            var winVersion = new Version(major, minor, 0, 0);
            return (Environment.OSVersion.Platform == PlatformID.Win32NT)
                && (Environment.OSVersion.Version >= winVersion);
        }

        public static bool Win7OrNewer { get; } = WinVerEqOrGr(6, 1);
        public static bool Win8OrNewer { get; } = WinVerEqOrGr(6, 2);
        public static bool Win81OrNewer { get; } = WinVerEqOrGr(6, 3);
        public static bool Win10OrNewer { get; } = WinVerEqOrGr(10, 0);
    }
}
