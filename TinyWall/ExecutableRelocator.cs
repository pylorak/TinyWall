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
    /// This helper detects that specific situation and nothing else. The replacement must live in a
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
                        var match = VersionedFolderRegex.Match(name);

                        // The parent must still exist. If it does not, we are not looking at an
                        // updated application but at a detached drive or a deleted install root.
                        if (match.Success && Directory.Exists(parent))
                        {
                            if (TryRelocateWithin(parent!, name, match, tail, out newPath))
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

        private static bool TryRelocateWithin(string parent, string originalName, Match originalMatch, string tail, out string newPath)
        {
            newPath = string.Empty;

            string stem = originalMatch.Groups["stem"].Value;
            string searchPattern = (stem.Length > 0) ? stem + "*" : "*";

            IReadOnlyList<int>? bestVersion = null;
            DateTime bestWriteTime = DateTime.MinValue;
            int scanned = 0;

            foreach (string sibling in Directory.EnumerateDirectories(parent, searchPattern))
            {
                if (++scanned > MaxSiblingsScanned)
                    break;

                string siblingName = Path.GetFileName(sibling);
                if (string.Equals(siblingName, originalName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var match = VersionedFolderRegex.Match(siblingName);
                if (!match.Success)
                    continue;

                // Only the version token may differ from the original directory name.
                if (!string.Equals(match.Groups["stem"].Value, stem, StringComparison.OrdinalIgnoreCase))
                    continue;

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
