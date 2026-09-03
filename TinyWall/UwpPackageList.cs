using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using Windows.Management.Deployment;
using pylorak.Windows;
using System.Collections;
using System.Collections.ObjectModel;

namespace pylorak.TinyWall
{
    public class UwpPackageList : IReadOnlyList<UwpPackageList.Package>
    {
        public enum TamperedState
        {
            Unknown,
            No,
            Yes
        }

        public readonly struct Package : IEquatable<Package>
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
            public readonly string FamilyName;
            public readonly string Sid;
            public readonly TamperedState Tampered;

            public Package(global::Windows.ApplicationModel.Package p)
            {
                Name = p.Id.Name;
                Publisher = p.Id.Publisher;
                PublisherId = p.Id.PublisherId;
                FamilyName = p.Id.FamilyName;
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

            public override bool Equals(object? obj)
            {
                return obj is Package other && Equals(other);
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

        private List<Package>? _Packages;
        private List<Package> Packages
        {
            get
            {
                if (_Packages is null)
                {
                    try
                    {
                        _Packages = CreatePackageList();
                    }
                    catch
                    {
                        // Return an empty list if we cannot enumerate the packages on the system.
                        // This happens for exmaple when the AppXSVC service is disabled.
                        _Packages = new List<Package>();
                    }
                }
                return _Packages;
            }
        }

        public int Count => ((IReadOnlyCollection<Package>)Packages).Count;

        public Package this[int index] => ((IReadOnlyList<Package>)Packages)[index];

        private static List<Package> CreatePackageList()
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

            return resultList;
        }

        public enum FullTrustState
        {
            Unknown,
            No,
            Yes
        }

        /// <summary>
        /// Tells whether a package runs with full trust, i.e. is a packaged desktop application
        /// rather than a sandboxed UWP app.
        ///
        /// This matters because an exception for a package is enforced through
        /// FWPM_CONDITION_ALE_PACKAGE_ID, which is only present on the token of a process running
        /// inside an AppContainer. A full trust package - Spotify, Arc, and by now a good number
        /// of Microsoft's own apps - runs as an ordinary Win32 process with no such token, so a
        /// package-based rule can never match its traffic. Note that the package SID itself gives
        /// nothing away: DeriveAppContainerSidFromAppContainerName is a pure derivation from the
        /// family name and succeeds whether or not the app is ever sandboxed.
        ///
        /// Returns Unknown when the manifest cannot be read, so that callers can stay silent
        /// rather than warn on a guess.
        /// </summary>
        public static FullTrustState GetFullTrustState(string familyName)
        {
            try
            {
                var pm = new PackageManager();
                foreach (var p in pm.FindPackagesForUser(string.Empty, familyName))
                {
                    string manifestPath = System.IO.Path.Combine(p.InstalledLocation.Path, "AppxManifest.xml");
                    string manifest = System.IO.File.ReadAllText(manifestPath);

                    // A packaged desktop application declares the runFullTrust restricted
                    // capability, and its entry point is Windows.FullTrustApplication. Both
                    // strings are specific enough to test for without parsing the manifest,
                    // which would otherwise mean dealing with its several namespaces.
                    bool fullTrust =
                        (manifest.IndexOf("Windows.FullTrustApplication", StringComparison.OrdinalIgnoreCase) >= 0)
                        || (manifest.IndexOf("Name=\"runFullTrust\"", StringComparison.OrdinalIgnoreCase) >= 0);

                    return fullTrust ? FullTrustState.Yes : FullTrustState.No;
                }
            }
            catch
            {
                // Deliberately swallowed: the manifest lives under %ProgramFiles%\WindowsApps,
                // and not being able to read it is not a reason to fail the caller.
            }

            return FullTrustState.Unknown;
        }

        public Package? FindPackage(string? sid)
        {
            if (string.IsNullOrEmpty(sid))
                return null;

            foreach (var package in Packages)
            {
                if (package.Sid.Equals(sid))
                    return package;
            }

            return null;
        }

        public Package? FindPackageForProcess(uint pid)
        {
            return FindPackage(ProcessManager.GetAppContainerSid(pid));
        }

        public IEnumerator<Package> GetEnumerator()
        {
            return ((IEnumerable<Package>)Packages).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Packages).GetEnumerator();
        }
    }
}
