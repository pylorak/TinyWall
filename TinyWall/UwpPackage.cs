using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using Windows.Management.Deployment;

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

        public readonly struct Package  // TODO: Use record struct from C# 10
        {
            [SuppressUnmanagedCodeSecurity]
            private static class NativeMethods
            {
                [DllImport("Userenv", CharSet = CharSet.Unicode)]
                public static extern int DeriveAppContainerSidFromAppContainerName(string pszAppContainerName, out SafeSidHandle ppsidAppContainerSid);
            }

            public readonly string Name;
            public readonly string Publisher;
            public readonly string PublisherId;
            public readonly string Sid;
            public readonly TamperedState Tampered;

            public Package(global::Windows.ApplicationModel.Package p)
            {
                Name = p.Id.Name;
                Publisher = p.Id.Publisher;
                PublisherId = p.Id.PublisherId;
                Tampered = p.Status.Tampered ? TamperedState.Yes : TamperedState.No;

                SafeSidHandle? pSid = null;
                try
                {
                    if (0 != NativeMethods.DeriveAppContainerSidFromAppContainerName(p.Id.FamilyName, out pSid))
                        throw new ArgumentException("Cannot determine package SID.");

                    Sid = pSid.GetStringSid() ?? string.Empty;
                }
                finally
                {
                    pSid?.Dispose();
                }
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

        public static Package[] GetPackages()
        {
            var pm = new PackageManager();
            var packageList = pm.FindPackagesForUser(string.Empty);
            var resultList = new List<Package>();
            foreach (var p in packageList)
            {
                try
                {
                    resultList.Add(new Package(p));
                }
                catch { }
            }

            return resultList.ToArray();
        }

        private static Package? FindPackageDetails(string? sid, Package[] list)
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

        public static Package? FindPackageDetails(string? sid)
        {
            if (string.IsNullOrEmpty(sid))
                return null;

            var packages = GetPackages();

            return FindPackageDetails(sid, packages);
        }

        public Package[] Packages { get; private set; }

        public UwpPackage()
        {
            Packages = GetPackages();
        }

        public Package? FindPackage(string? sid)
        {
            return FindPackageDetails(sid, Packages);
        }
    }
}
