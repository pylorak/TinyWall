using System;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using Microsoft.Win32.SafeHandles;
using TinyWall.Interface;

namespace PKSoft
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
        public struct Package
        {
            public string Name;
            public string Publisher;
            public string PublisherId;
            public string Sid;
            public TamperedState Tampered;

            public AppContainerSubject ToExceptionSubject()
            {
                return new AppContainerSubject(Sid, Name, Publisher, PublisherId);
            }

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
                if (!(obj is Package))
                    return false;

                return Equals((Package)obj);
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
        protected static class NativeMethods
        {
            [DllImport("NativeHelper")]
            internal static extern void GetUwpPackageListing([Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out Package[] list, out int size);
        }

        public static Package[] GetList()
        {
            if (!TinyWall.Interface.VersionInfo.Win8OrNewer)
                return new Package[0];

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
            if (!TinyWall.Interface.VersionInfo.Win8OrNewer)
                return null;

            if (string.IsNullOrEmpty(sid))
                return null;

            NativeMethods.GetUwpPackageListing(out Package[] packages, out _);

            return FindPackageDetails(sid, packages);
        }

        public Package[] Packages { get; private set; }

        public UwpPackage()
        {
            Packages = UwpPackage.GetList();
        }

        public Package? FindPackage(string sid)
        {
            if (string.IsNullOrEmpty(sid))
                return null;

            return UwpPackage.FindPackageDetails(sid, Packages);
        }
    }
}
