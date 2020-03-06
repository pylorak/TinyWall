using System;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace PKSoft
{
    public static class UwpPackage
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct Package
        {
            public string Name;
            public string Publisher;
            public string Sid;
        }

        [SuppressUnmanagedCodeSecurity]
        protected static class NativeMethods
        {
            [DllImport("NativeHelper")]
            internal static extern void GetUwpPackageListing([Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out Package[] list, out int size);
        }

        public static Package[] GetList()
        {
            if (!TinyWall.Interface.VersionInfo.Win8OrNewer)
                throw new NotSupportedException();

            NativeMethods.GetUwpPackageListing(out Package[] packages, out _);
            return packages;
        }

        public static Package? FindPackageDetails(string sid)
        {
            if (string.IsNullOrEmpty(sid))
                return null;

            if (!TinyWall.Interface.VersionInfo.Win8OrNewer)
                throw new NotSupportedException();

            NativeMethods.GetUwpPackageListing(out Package[] packages, out _);

            foreach (var package in packages)
            {
                if (package.Sid.Equals(sid))
                    return package;
            }

            return null;
        }
    }
}
