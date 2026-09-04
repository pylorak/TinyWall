using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace pylorak.TinyWall
{
    /// <summary>
    /// Recovers application exceptions that were invalidated by an application update.
    ///
    /// Squirrel/Electron-style installers (Claude, Discord, Slack, GitHub Desktop, ...) place every
    /// release into its own versioned directory beside the previous one, for example:
    ///     %LOCALAPPDATA%\AnthropicClaude\app-0.9.3\claude.exe
    ///     %LOCALAPPDATA%\AnthropicClaude\app-0.11.0\claude.exe
    /// WFP matches executables on their exact path (FWPM_CONDITION_ALE_APP_ID), so each update
    /// silently invalidates the user's exception and the application loses network access until
    /// the user notices and re-whitelists it by hand. Such applications update frequently, which
    /// makes this a recurring annoyance rather than a one-off.
    ///
    /// MSIX / Microsoft Store packages behave the same way, but put the version in the middle of
    /// the folder name instead of at its end:
    ///     C:\Program Files\WindowsApps\Claude_1.44121.2.0_x64__pzs8sxrjxfjjc\app\claude.exe
    /// There the package name, architecture and publisher id must all be preserved and only the
    /// version may change, which is a stricter test than the generic one below.
    ///
    /// A third layout puts a platform tag after the version, as VS Code extensions do:
    ///     %USERPROFILE%\.vscode\extensions\anthropic.claude-code-2.1.173-win32-x64\...\claude.exe
    /// There the platform tag must be carried over unchanged and only the version may differ.
    ///
    /// This helper detects those specific situations and nothing else. The replacement must live in a
    /// *sibling* directory of the original one whose name differs from it only in a version token,
    /// and the path below that directory - including the file name - must be identical. The trust
    /// boundary is therefore unchanged: anyone able to plant a file that this code would relocate to
    /// already has write access to the directory holding the originally whitelisted executable, and
    /// could simply have overwritten that file in place.
    /// </summary>
    internal static class ExecutableRelocator
    {
        // Matches directory names that carry a version token, capturing the part before it.
        //   "app-0.11.0"   -> stem "app",      version "0.11.0"
        //   "app-1.2.3-beta.1" -> stem "app",  version "1.2.3", suffix "-beta.1"
        //   "2.14.0"       -> stem "",         version "2.14.0"
        private static readonly Regex VersionedFolderRegex = new(
            @"^(?<stem>.*?)[-_. ]?v?(?<version>\d+(?:\.\d+)*)(?<suffix>[-+][0-9A-Za-z.]+)?$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        // Matches MSIX / Microsoft Store package folders, which carry the version in the middle
        // of the name rather than at its end and so are never matched by the rule above:
        //   "Claude_1.44121.2.0_x64__pzs8sxrjxfjjc"
        //       -> name "Claude", version "1.44121.2.0", arch "x64", publisher "pzs8sxrjxfjjc"
        // The layout is Name_Version_Arch__PublisherId. Name and architecture cannot contain an
        // underscore, which is what keeps the fields unambiguous.
        private static readonly Regex MsixPackageFolderRegex = new(
            @"^(?<name>[^_]+)_(?<version>\d+(?:\.\d+){0,3})_(?<arch>[^_]+)__(?<pub>[A-Za-z0-9]+)$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        // Matches names that carry a platform tag after the version, as VS Code extensions do:
        //   "anthropic.claude-code-2.1.173-win32-x64"
        //       -> stem "anthropic.claude-code", version "2.1.173", suffix "-win32-x64"
        // The rule above cannot express this: with the version no longer at the end it latches
        // onto the "32" of "win32" and leaves the real version buried in the stem, so two
        // releases of the same extension end up with different stems and never match.
        //
        // Two restrictions keep this from swallowing names the first rule handles correctly.
        // The version must contain a dot, so a bare number inside a word - the "32" again -
        // cannot be mistaken for one; and the suffix must have at least two dash-separated
        // segments, which distinguishes a platform tag from the single-segment prerelease
        // suffix of "app-1.2.3-beta.1". Widening the first rule's suffix instead would have
        // been unsound: "v2-tools-1.5.0" and "v3-other-9.9.9" both reduce to an empty stem,
        // and would then relocate onto each other.
        private static readonly Regex PlatformSuffixedFolderRegex = new(
            @"^(?<stem>.*?)[-_. ]?v?(?<version>\d+(?:\.\d+)+)(?<suffix>(?:-[0-9A-Za-z][0-9A-Za-z.]*){2,})$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        // Which rule claimed a directory name, and therefore what has to stay equal between the
        // original directory and the sibling that replaces it.
        private enum FolderKind
        {
            Msix,
            PlatformSuffixed,
            Generic
        }

        // How many directory levels above the executable may be the versioned one.
        // Covers layouts like "app-1.2.3\resources\bin\helper.exe".
        private const int MaxWalkDepth = 5;

        // Upper bound on sibling directories inspected, so that a pathological directory
        // cannot turn the periodic check into a long scan.
        private const int MaxSiblingsScanned = 512;

        /// <summary>
        /// Tries to locate <paramref name="oldPath"/> in a newer sibling version directory.
        /// Returns false - leaving <paramref name="newPath"/> empty - when no unambiguous,
        /// tightly-matching replacement exists.
        /// </summary>
        public static bool TryFindRelocatedPath(string oldPath, out string newPath)
        {
            newPath = string.Empty;

            if (Utils.IsNullOrEmpty(oldPath))
                return false;

            try
            {
                // Only ever act on a path that is actually gone.
                if (File.Exists(oldPath))
                    return false;

                // Network and removable locations can be "missing" merely because they are
                // offline. Relocating those would be guesswork, so leave them alone.
                if (pylorak.Windows.NetworkPath.IsNetworkPath(oldPath))
                    return false;

                string? dir = Path.GetDirectoryName(oldPath);
                string tail = Path.GetFileName(oldPath);
                if (Utils.IsNullOrEmpty(tail))
                    return false;

                // Walk upwards from the executable, trying the deepest versioned directory first.
                for (int depth = 0; (dir is not null) && (depth < MaxWalkDepth); ++depth)
                {
                    string name = Path.GetFileName(dir);
                    string? parent = Path.GetDirectoryName(dir);

                    if (!Utils.IsNullOrEmpty(name) && (parent is not null))
                    {
                        // The rules are tried from most specific to least, and the first match
                        // decides which identity has to be preserved among the siblings. A folder
                        // name is therefore handled by exactly one of them, never by two.
                        var match = MsixPackageFolderRegex.Match(name);
                        var kind = FolderKind.Msix;

                        if (!match.Success)
                        {
                            match = PlatformSuffixedFolderRegex.Match(name);
                            kind = FolderKind.PlatformSuffixed;
                        }
                        if (!match.Success)
                        {
                            match = VersionedFolderRegex.Match(name);
                            kind = FolderKind.Generic;
                        }

                        // The parent must still exist. If it does not, we are not looking at an
                        // updated application but at a detached drive or a deleted install root.
                        if (match.Success && Directory.Exists(parent))
                        {
                            if (TryRelocateWithin(parent!, name, match, kind, tail, out newPath))
                                return true;
                        }
                    }

                    tail = Path.Combine(name, tail);
                    dir = parent;
                }
            }
            catch (Exception e)
            {
                // Never let a filesystem hiccup take down the caller; a missed relocation
                // just means the user re-whitelists by hand, as before.
                Utils.LogException(e, Utils.LOG_ID_SERVICE);
            }

            return false;
        }

        private static bool TryRelocateWithin(string parent, string originalName, Match originalMatch, FolderKind kind, string tail, out string newPath)
        {
            newPath = string.Empty;

            // Narrow the enumeration to the family the original folder belongs to. For a package
            // that is the invariant part before the version; otherwise the stem.
            string stem = (kind == FolderKind.Msix) ? string.Empty : originalMatch.Groups["stem"].Value;
            string searchPattern = (kind == FolderKind.Msix)
                ? originalMatch.Groups["name"].Value + "_*"
                : ((stem.Length > 0) ? stem + "*" : "*");

            IReadOnlyList<int>? bestVersion = null;
            DateTime bestWriteTime = DateTime.MinValue;

            foreach (string sibling in EnumerateDirectoriesOrNone(parent, searchPattern))
            {
                string siblingName = Path.GetFileName(sibling);
                if (string.Equals(siblingName, originalName, StringComparison.OrdinalIgnoreCase))
                    continue;

                Match match;
                if (kind == FolderKind.Msix)
                {
                    match = MsixPackageFolderRegex.Match(siblingName);
                    if (!match.Success)
                        continue;

                    // Same package, same architecture, same publisher - only the version may
                    // differ. A package identity that differs in any other field is a different
                    // application as far as Windows is concerned, so it is never followed.
                    if (!IsSameMsixIdentity(originalMatch, match))
                        continue;
                }
                else if (kind == FolderKind.PlatformSuffixed)
                {
                    match = PlatformSuffixedFolderRegex.Match(siblingName);
                    if (!match.Success)
                        continue;

                    // The platform tag has to be carried over as-is. Following a build for one
                    // architecture to a build for another would hand network access to a
                    // different binary than the one that was whitelisted.
                    if (!string.Equals(match.Groups["stem"].Value, stem, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!string.Equals(match.Groups["suffix"].Value, originalMatch.Groups["suffix"].Value, StringComparison.OrdinalIgnoreCase))
                        continue;
                }
                else
                {
                    // Keep the rules disjoint: a packaged folder is the MSIX rule's business even
                    // when its publisher id happens to be all digits, and a platform-tagged one
                    // belongs to its own rule, since the generic pattern would otherwise match
                    // both - and mis-parse the latter.
                    if (MsixPackageFolderRegex.IsMatch(siblingName))
                        continue;
                    if (PlatformSuffixedFolderRegex.IsMatch(siblingName))
                        continue;

                    match = VersionedFolderRegex.Match(siblingName);
                    if (!match.Success)
                        continue;

                    // Only the version token may differ from the original directory name.
                    if (!string.Equals(match.Groups["stem"].Value, stem, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                // The path below the versioned directory must be reproduced exactly.
                string candidate = Path.Combine(sibling, tail);
                if (!File.Exists(candidate))
                    continue;

                var version = ParseVersion(match.Groups["version"].Value);
                DateTime writeTime;
                try { writeTime = File.GetLastWriteTimeUtc(candidate); }
                catch { writeTime = DateTime.MinValue; }

                // Highest version wins; equal versions are broken by the newer file.
                if (bestVersion is null)
                {
                    bestVersion = version;
                    bestWriteTime = writeTime;
                    newPath = candidate;
                }
                else
                {
                    int cmp = CompareVersions(version, bestVersion);
                    if ((cmp > 0) || ((cmp == 0) && (writeTime > bestWriteTime)))
                    {
                        bestVersion = version;
                        bestWriteTime = writeTime;
                        newPath = candidate;
                    }
                }
            }

            return newPath.Length > 0;
        }

        private static bool IsSameMsixIdentity(Match left, Match right)
        {
            return string.Equals(left.Groups["name"].Value, right.Groups["name"].Value, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.Groups["arch"].Value, right.Groups["arch"].Value, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.Groups["pub"].Value, right.Groups["pub"].Value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Lists sibling directories, treating an unreadable parent as simply having none.
        ///
        /// Directory.EnumerateDirectories is lazy, so a directory that denies listing - as
        /// C:\Program Files\WindowsApps does, being ACL'd to TrustedInstaller - throws while the
        /// caller iterates rather than when the enumerator is created. Left unhandled that would
        /// escape TryRelocateWithin and abort the whole upward walk, losing the levels above.
        /// Materialising the names here confines the failure to the level that caused it.
        /// </summary>
        private static IReadOnlyList<string> EnumerateDirectoriesOrNone(string parent, string searchPattern)
        {
            var ret = new List<string>();
            try
            {
                foreach (string dir in Directory.EnumerateDirectories(parent, searchPattern))
                {
                    if (ret.Count >= MaxSiblingsScanned)
                        break;
                    ret.Add(dir);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Deliberately discards whatever was collected before the failure, so that the
                // outcome does not depend on how far the enumeration happened to get.
                return Array.Empty<string>();
            }
            catch (IOException)
            {
                return Array.Empty<string>();
            }
            return ret;
        }

        private static IReadOnlyList<int> ParseVersion(string version)
        {
            var parts = version.Split('.');
            var ret = new int[parts.Length];
            for (int i = 0; i < parts.Length; ++i)
                ret[i] = int.TryParse(parts[i], out int val) ? val : 0;
            return ret;
        }

        private static int CompareVersions(IReadOnlyList<int> left, IReadOnlyList<int> right)
        {
            int len = Math.Max(left.Count, right.Count);
            for (int i = 0; i < len; ++i)
            {
                // Missing trailing components count as zero, so "1.2" == "1.2.0".
                int l = (i < left.Count) ? left[i] : 0;
                int r = (i < right.Count) ? right[i] : 0;
                if (l != r)
                    return l.CompareTo(r);
            }
            return 0;
        }
    }
}
