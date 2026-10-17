using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;
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

        private readonly bool SelectableOnly;

        /// <param name="selectableOnly">
        /// Leave out packages that shouldn't be pickable by the user. The default
        /// is the complete list to make sure that packages persisted previously keep resolving
        /// through FindPackage, whether or not that package would be offered for selection today.
        /// </param>
        public UwpPackageList(bool selectableOnly = false)
        {
            SelectableOnly = selectableOnly;
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
                        _Packages = CreatePackageList(SelectableOnly);
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

        private static List<Package> CreatePackageList(bool selectableOnly)
        {
            var pm = new PackageManager();
            var packageList = pm.FindPackagesForUser(string.Empty);
            var resultList = new List<Package>();
            foreach (var p in packageList)
            {
                try
                {
                    if (selectableOnly && !IsSelectable(p))
                        continue;

                    resultList.Add(new Package(p));
                }
                catch { }
            }

            return resultList;
        }

        /// <summary>
        /// Whether this package is eligible to be picked newly from a list of packages.
        /// Pickable packages are those that are expected to run in an AppContainer,
        /// that is, packages that are not full-trust and not a framework.
        /// </summary>
        private static bool IsSelectable(global::Windows.ApplicationModel.Package p)
        {
            // A framework package is a shared library other packages depend on, not an
            // application that runs. Checked first because the property is cheap.
            if (p.IsFramework)
                return false;

            // A full trust package runs outside an AppContainer, so its processes carry no
            // package identity for FWPM_CONDITION_ALE_PACKAGE_ID to match and a rule for it
            // would never do anything. An undeterminable package is kept, so that a manifest
            // we cannot read never silently removes a working entry from the list.
            return GetFullTrustState(p) != FullTrustState.Yes;
        }

        private enum FullTrustState
        {
            Unknown,
            No,
            Yes
        }

        // A packaged desktop application gives itself away in the manifest in one of three ways:
        // the runFullTrust restricted capability, an entry point of Windows.FullTrustApplication,
        // or an explicit medium integrity trust level. Each marker is specific enough to test for
        // without parsing the document, which would otherwise mean handling its several
        // namespaces; whitespace around the equals sign and either quote style are tolerated,
        // since both are valid XML and appear in practice.
        private static readonly Regex FullTrustMarkerRegex = new(
            @"Windows\.FullTrustApplication|Name\s*=\s*[""']runFullTrust[""']|TrustLevel\s*=\s*[""']mediumIL[""']",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
        /// Returns Unknown when the manifest cannot be read, so that an unreadable manifest never
        /// decides the question by itself.
        /// </summary>
        private static FullTrustState GetFullTrustState(global::Windows.ApplicationModel.Package p)
        {
            try
            {
                string manifestPath = System.IO.Path.Combine(p.InstalledLocation.Path, "AppxManifest.xml");
                string manifest = System.IO.File.ReadAllText(manifestPath);

                return FullTrustMarkerRegex.IsMatch(manifest) ? FullTrustState.Yes : FullTrustState.No;
            }
            catch
            {
                // Deliberately swallowed: the manifest lives under %ProgramFiles%\WindowsApps,
                // and not being able to read it is not a reason to fail the caller.
                return FullTrustState.Unknown;
            }
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
