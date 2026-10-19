using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;
using Windows.Management.Deployment;
using pylorak.Windows;
using System.Collections;

namespace pylorak.TinyWall
{
    public class UwpPackageList : IReadOnlyCollection<UwpPackageList.Package>
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

            public readonly string Sid;
            public readonly string DisplayName;
            public readonly string Name;
            public readonly string Publisher;
            public readonly string PublisherId;
            public readonly TamperedState Tampered;

            public Package(global::Windows.ApplicationModel.Package p)
            {
                Name = p.Id.Name;
                DisplayName = Utils.IsNullOrEmpty(p.DisplayName) ? p.Id.Name : p.DisplayName;
                Publisher = p.Id.Publisher;
                PublisherId = p.Id.PublisherId;
                Tampered = p.Status.Tampered ? TamperedState.Yes : TamperedState.No;

                SafeSidHandle? pSid = null;
                try
                {
                    if (0 != NativeMethods.DeriveAppContainerSidFromAppContainerName(p.Id.FamilyName, out pSid))
                        throw new ArgumentException("Cannot determine package SID.");

                    Sid = pSid.GetStringSid() ?? throw new ArgumentException("Cannot determine package SID.");
                }
                finally
                {
                    pSid?.Dispose();
                }
            }

            public override int GetHashCode()
            {
                return Sid.GetHashCode();
            }

            public override bool Equals(object? obj)
            {
                return obj is Package other && Equals(other);
            }

            public bool Equals(Package other)
            {
                return Sid == other.Sid;
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

        private class PackageCacheEntry
        {
            public readonly Package Package;
            public readonly global::Windows.ApplicationModel.Package NativePackage;
            public bool? IsAppContainerApp;

            public PackageCacheEntry(Package twp, global::Windows.ApplicationModel.Package winp)
            {
                Package = twp;
                NativePackage = winp;
            }
        }

        private Dictionary<string, PackageCacheEntry>? _PackageCache;

        private Dictionary<string, PackageCacheEntry> PackageCache
        {
            get
            {
                if (_PackageCache is null)
                {
                    try
                    {
                        _PackageCache = CreatePackageList();
                    }
                    catch
                    {
                        // Return an empty list if we cannot enumerate the packages on the system.
                        // This happens for exmaple when the AppXSVC service is disabled.
                        _PackageCache = new (StringComparer.OrdinalIgnoreCase);
                    }
                }
                return _PackageCache;
            }
        }

        public int Count => PackageCache.Count;

        private static Dictionary<string, PackageCacheEntry> CreatePackageList()
        {
            var pm = new PackageManager();
            var packageList = pm.FindPackagesForUserWithPackageTypes(string.Empty, PackageTypes.Main);
            var cache = new Dictionary<string, PackageCacheEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in packageList)
            {
                try
                {
                    var entry = new Package(p);
                    if (!cache.ContainsKey(entry.Sid))
                        cache.Add(entry.Sid, new PackageCacheEntry(entry, p));
                }
                catch { }
            }

            return cache;
        }

        /// <summary>
        /// Whether the package runs in an AppContainer, i.e. is not marked as FullTrust/mediumIL.
        /// </summary>
        public bool IsAppContainerApp(Package p)
        {
            try
            {
                if (PackageCache.TryGetValue(p.Sid, out var cacheEntry))
                {
                    if (!cacheEntry.IsAppContainerApp.HasValue)
                        cacheEntry.IsAppContainerApp = IsAppContainerApp(cacheEntry.NativePackage);
                    return cacheEntry.IsAppContainerApp.Value;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsAppContainerApp(global::Windows.ApplicationModel.Package p)
        {
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
            if (Utils.IsNullOrEmpty(sid))
                return null;

            if (PackageCache.TryGetValue(sid, out var cacheEntry))
                return cacheEntry.Package;

            return null;
        }

        public Package? FindPackageForProcess(uint pid)
        {
            return FindPackage(ProcessManager.GetAppContainerSid(pid));
        }

        public IEnumerator<Package> GetEnumerator()
        {
            foreach (var pair in PackageCache)
                yield return pair.Value.Package;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
