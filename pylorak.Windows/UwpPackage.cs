using System;
using System.Runtime.InteropServices;
using System.Security;

namespace pylorak.Windows
{
    public class UwpPackage
    {
        public enum TamperedState
        {
            Unknown,
            No,
            Yes
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public readonly struct Package  // TODO: Use record struct from C# 10
        {
            public readonly string Name;
            public readonly string Publisher;
            public readonly string PublisherId;
            public readonly string Sid;
            public readonly TamperedState Tampered;

            public override int GetHashCode()
            {
                return Name.GetHashCode()
                    ^ Publisher.GetHashCode()
                    ^ PublisherId.GetHashCode()
                    ^ Sid.GetHashCode()
                    ^ Tampered.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is Package other)
                    return Equals(other);
                else
                    return false;
            }

            public bool Equals(Package other)
            {
                return
                    (Name == other.Name)
                    && (Publisher == other.Publisher)
                    && (PublisherId == other.PublisherId)
                    && (Sid == other.Sid)
                    && (Tampered == other.Tampered);
            }

            public static bool operator ==(Package o1, Package o2)
            {
                return o1.Equals(o2);
            }

            public static bool operator !=(Package o1, Package o2)
            {
                return !o1.Equals(o2);
            }
        }

        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("NativeHelper32", EntryPoint = "GetUwpPackageListing")]
            private static extern void GetUwpPackageListing32([Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out Package[] list, out int size);

            [DllImport("NativeHelper64", EntryPoint = "GetUwpPackageListing")]
            private static extern void GetUwpPackageListing64([Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out Package[] list, out int size);

            public static void GetUwpPackageListing(out Package[] list, out int size)
            {
                if (IntPtr.Size == 8)
                    GetUwpPackageListing64(out list, out size);
                else
                    GetUwpPackageListing32(out list, out size);
            }
        }

        private static bool IsGetUwpPackageListingSupported()
        {
            try
            {
                NativeMethods.GetUwpPackageListing(out Package[] packages, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool PlatformSupport { get; } = IsGetUwpPackageListingSupported();

        public static Package[] GetList()
        {
            if (!PlatformSupport)
                return Array.Empty<Package>();

            NativeMethods.GetUwpPackageListing(out Package[] packages, out _);
            return packages;
        }

        private static Package? FindPackageDetails(string sid, Package[] list)
        {
            if (string.IsNullOrEmpty(sid))
                return null;

            foreach (var package in list)
            {
                if (package.Sid.Equals(sid))
                    return package;
            }

            return null;
        }

        public static Package? FindPackageDetails(string sid)
        {
            if (!PlatformSupport)
                return null;

            if (string.IsNullOrEmpty(sid))
                return null;

            NativeMethods.GetUwpPackageListing(out Package[] packages, out _);

            return FindPackageDetails(sid, packages);
        }

        public Package[] Packages { get; private set; }

        public UwpPackage()
        {
            Packages = GetList();
        }

        public Package? FindPackage(string sid)
        {
            return FindPackageDetails(sid, Packages);
        }
    }
}
