using Microsoft.Win32;
using pylorak.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;

namespace pylorak.TinyWall
{
    public enum WildcardValidation
    {
        Success,
        ErrorGeneric,
        ErrorEmptyParameter,
        ErrorInvalidChars,
        ErrorNotFullyQualified,
        ErrorHasRelativeComponents,
        ErrorMissingWildcards,
        ErrorPathNotMatched,
        ErrorDisallowedFolder,
        ErrorFileSignatureFail,
    }

    public static class WildcardPathMatcher
    {
        private static readonly char[] WildcardCharacters = { '*', '?' };
        private static readonly IReadOnlyCollection<string> ProtectedPathRoots = BuildProtectedPathRoots();
        private static readonly IReadOnlyCollection<string> UserProfilePathRoots = BuildUserProfilePathRoots();

        public static WildcardValidation IsPatternSyntaxValid(string pattern)
        {
            pattern = pattern.Trim().Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            if (string.IsNullOrWhiteSpace(pattern))
            {
                return WildcardValidation.ErrorEmptyParameter;
            }
            else if (pattern.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            {
                return WildcardValidation.ErrorInvalidChars;
            }
            else if (!Utils.IsPathFullyQualified(pattern))
            {
                return WildcardValidation.ErrorNotFullyQualified;
            }
            else if (pattern.Contains("\\.\\") || pattern.Contains("\\..\\")
                  || pattern.EndsWith("\\.") || pattern.EndsWith("\\.."))
            {
                // Note: Since we also ensure that the path is fully qualified, this check is technically
                // unnecessary, as the presence of relative components do not pose a risk that way.
                // We'll require it anyway as a defense against future modifications.
                return WildcardValidation.ErrorHasRelativeComponents;
            }
            else if (pattern.IndexOfAny(WildcardCharacters) == -1)
            {
                return WildcardValidation.ErrorMissingWildcards;
            }

            return WildcardValidation.Success;
        }

        // Sort two wildcard patterns by how deeply each pins down a path.
        // Returns -1 (left arg deeper) / 0 (equal strings) / 1 (right arg deeper).
        public static int ComparePatterns(string p1, string p2)
        {
            if ((p1 is null) || (p2 is null))
                throw new ArgumentNullException("Arguments cannot be null.");

            MeasureLiteralDepth(p1, out int p1PrefixDepth, out int p1TotalDepth);
            MeasureLiteralDepth(p2, out int p2PrefixDepth, out int p2TotalDepth);

            // Primary sort decision:
            // the number of directory levels from the root that are fully
            // pinned (literal) before the first wildcard-containing segment is reached.
            // This is the depth to which the path is determined by the pattern.
            if (p1PrefixDepth != p2PrefixDepth)
                return (p1PrefixDepth > p2PrefixDepth) ? -1 : 1;

            // 1. tie-breaker: the total number of levels pinned in the whole string
            // e.g. c:\a\*\b\* pins one level deeper than c:\a\*
            if (p1TotalDepth != p2TotalDepth)
                return (p1TotalDepth > p2TotalDepth) ? -1 : 1;

            // 2. tie-breaker: ordinal case-insensitive comparison
            // while treating separator variants as equal.
            return ComparePatternOrdinals(p2, p1);
        }

        private static void MeasureLiteralDepth(string pattern, out int prefixDepth, out int totalDepth)
        {
            prefixDepth = 0;
            totalDepth = 0;

            bool prefixEnded = false;
            int i = 0;
            while (i < pattern.Length)
            {
                if (IsDirectorySeparator(pattern[i]))
                {
                    i++;
                    continue;
                }

                // Consume one path segment, remember whether it contained a wildcard
                bool hasWildcard = false;
                while ((i < pattern.Length) && !IsDirectorySeparator(pattern[i]))
                {
                    if (IsWildcardCharacter(pattern[i]))
                        hasWildcard = true;
                    i++;
                }

                if (hasWildcard)
                {
                    // A wildcard segment does not pin its directory level
                    prefixEnded = true;
                }
                else
                {
                    totalDepth++;
                    if (!prefixEnded)
                        prefixDepth++;
                }
            }
        }

        private static int ComparePatternOrdinals(string p1, string p2)
        {
            static int NormalizeForComparison(char value)
            {
                if (IsDirectorySeparator(value))
                    return Path.DirectorySeparatorChar;
                return char.ToUpperInvariant(value);
            }

            int compareLength = Math.Min(p1.Length, p2.Length);
            for (int i = 0; i < compareLength; i++)
            {
                int c1 = NormalizeForComparison(p1[i]);
                int c2 = NormalizeForComparison(p2[i]);
                if (c1 != c2)
                    return (c1 < c2) ? -1 : 1;
            }

            return p1.Length.CompareTo(p2.Length);
        }

        private static bool IsWildcardCharacter(char value)
        {
            foreach (char wildcard in WildcardCharacters)
            {
                if (wildcard == value)
                    return true;
            }
            return false;
        }

        public static WildcardValidation MatchPatternToPath(string? pattern, string filePath, ref bool? sigVerifyPass)
        {
            try
            {
                if (Utils.IsNullOrEmpty(pattern) || Utils.IsNullOrEmpty(filePath))
                    return WildcardValidation.ErrorEmptyParameter;

                if (filePath.IndexOfAny(WildcardCharacters) >= 0
                    || !TryGetLiteralPrefix(pattern, out string normalizedPrefix)
                    || !Matches(pattern, filePath))
                {
                    return WildcardValidation.ErrorPathNotMatched;
                }

                bool patternInUacProtectedPaths = HasLiteralPrefixInRoots(normalizedPrefix, ProtectedPathRoots);
                bool patternInUserProfilePaths = HasLiteralPrefixInRoots(normalizedPrefix, UserProfilePathRoots);
                bool isUncPaths = NetworkPath.IsUncPath(normalizedPrefix);
                bool isPatternPathAllowed = patternInUacProtectedPaths || patternInUserProfilePaths || isUncPaths;

                if (!isPatternPathAllowed)
                    return WildcardValidation.ErrorDisallowedFolder;

                return IsFileValidWildcardTarget(filePath, ref sigVerifyPass);
            }
            catch (Exception)
            {
                // Any error during wildcard verification results in rejection.
                return WildcardValidation.ErrorGeneric;
            }
        }

        private static WildcardValidation IsFileValidWildcardTarget(string filePath, ref bool? sigVerifyPass)
        {
            bool inUacProtectedPaths = HasLiteralPrefixInRoots(filePath, ProtectedPathRoots);
            bool inUserProfilePaths = HasLiteralPrefixInRoots(filePath, UserProfilePathRoots);
            bool isUncPaths = NetworkPath.IsUncPath(filePath);
            bool isPathAllowed = inUacProtectedPaths || inUserProfilePaths || isUncPaths;

            if (!isPathAllowed)
                return WildcardValidation.ErrorDisallowedFolder;

            if (inUacProtectedPaths)
                // No signature verification required
                return WildcardValidation.Success;

            if (!sigVerifyPass.HasValue)
            {
                sigVerifyPass = File.Exists(filePath)
                                && WinTrust.VerifyFileAuthenticode(filePath) == WinTrust.VerifyResult.SIGNATURE_VALID;
            }
            if (!sigVerifyPass.Value)
                return WildcardValidation.ErrorFileSignatureFail;

            return WildcardValidation.Success;
        }

        private static bool TryGetLiteralPrefix(
            string? pattern,
            out string normalizedPrefix)
        {
            normalizedPrefix = string.Empty;

            if (string.IsNullOrWhiteSpace(pattern)
                || char.IsWhiteSpace(pattern![0])
                || char.IsWhiteSpace(pattern[pattern.Length - 1]))
            {
                return false;
            }

            int wildcardIndex = pattern!.IndexOfAny(WildcardCharacters);
            if (wildcardIndex <= 0)
            {
                return false;
            }

            try
            {
                string literalPrefix = pattern.Substring(0, wildcardIndex);
                if (!Utils.IsPathFullyQualified(literalPrefix))
                {
                    return false;
                }

                string? pathRoot = Path.GetPathRoot(literalPrefix);
                if (string.IsNullOrEmpty(pathRoot)
                    || literalPrefix.IndexOf(':', pathRoot.Length) >= 0)
                {
                    return false;
                }

                // Keeping a separator char at the end of directory paths importantly allows us in later steps
                // to differentiate between a child directory entry and a sibling entry with the same prefix,
                // e.g. C:\Windows\ vs C:\WindowsXYZ
                normalizedPrefix = literalPrefix;
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException
                || exception is NotSupportedException
                || exception is PathTooLongException)
            {
                return false;
            }
        }

        private static bool Matches(string? pattern, string? path)
        {
            if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(path))
            {
                return false;
            }

            string wildcardPattern = pattern!;
            string candidatePath = path!;

            int patternIndex = 0;
            int pathIndex = 0;
            int lastStarIndex = -1;
            int starMatchIndex = 0;

            while (pathIndex < candidatePath.Length)
            {
                if (patternIndex < wildcardPattern.Length
                    && ((wildcardPattern[patternIndex] == '?' && !IsDirectorySeparator(candidatePath[pathIndex]))
                        || (char.ToUpperInvariant(wildcardPattern[patternIndex]) == char.ToUpperInvariant(candidatePath[pathIndex]))))
                {
                    patternIndex++;
                    pathIndex++;
                }
                else if (patternIndex < wildcardPattern.Length && wildcardPattern[patternIndex] == '*')
                {
                    lastStarIndex = patternIndex++;
                    starMatchIndex = pathIndex;
                }
                else if (lastStarIndex >= 0 && !IsDirectorySeparator(candidatePath[starMatchIndex]))
                {
                    patternIndex = lastStarIndex + 1;
                    pathIndex = ++starMatchIndex;
                }
                else
                {
                    return false;
                }
            }

            while (patternIndex < wildcardPattern.Length && wildcardPattern[patternIndex] == '*')
            {
                patternIndex++;
            }

            return patternIndex == wildcardPattern.Length;
        }

        private static IReadOnlyCollection<string> BuildProtectedPathRoots()
        {
            Environment.SpecialFolder[] folders =
            {
                Environment.SpecialFolder.Windows,
                Environment.SpecialFolder.System,
                Environment.SpecialFolder.SystemX86,
                Environment.SpecialFolder.ProgramFiles,
                Environment.SpecialFolder.ProgramFilesX86,
                Environment.SpecialFolder.CommonProgramFiles,
                Environment.SpecialFolder.CommonProgramFilesX86
            };

            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Environment.SpecialFolder folder in folders)
            {
                string path = Environment.GetFolderPath(folder);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    roots.Add(path);
                }
            }

            return roots;
        }

        private static IReadOnlyCollection<string> BuildUserProfilePathRoots()
        {
            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                if (IsUserProfileSid(identity.User?.Value))
                {
                    AddPathRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                    AddPathRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
                    AddPathRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
                }
            }

            // The installed service runs as LocalSystem, whose special folders
            // differ from the controller user's. Use registered profile paths
            // so both processes recognize patterns beneath user profiles.
            // Do not allow the entire Users directory: each profile must have
            // a literal prefix, and matching executables still require trust.
            using RegistryKey? profileList = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList");
            if (profileList is not null)
            {
                foreach (string profileName in profileList.GetSubKeyNames())
                {
                    if (!IsUserProfileSid(profileName))
                        continue;

                    using RegistryKey? profile = profileList.OpenSubKey(profileName);
                    AddPathRoot(roots, profile?.GetValue("ProfileImagePath") as string);
                }
            }

            return roots;
        }

        private static bool IsUserProfileSid(string? sidValue)
        {
            if (string.IsNullOrEmpty(sidValue))
                return false;

            try
            {
                var sid = new SecurityIdentifier(sidValue);
                return !sid.IsWellKnown(WellKnownSidType.LocalSystemSid)
                    && !sid.IsWellKnown(WellKnownSidType.LocalServiceSid)
                    && !sid.IsWellKnown(WellKnownSidType.NetworkServiceSid);
            }
            catch (ArgumentException)
            {
                // Ignore non-SID entries, including profile backup keys (.bak).
                return false;
            }
        }

        private static void AddPathRoot(ISet<string> roots, string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && Utils.IsPathFullyQualified(path))
            {
                roots.Add(path);
            }
        }

        private static bool HasLiteralPrefixInRoots(
            string normalizedPrefix,
            IEnumerable<string> allowedRoots)
        {
            foreach (string root in allowedRoots)
            {
                if (IsPathBelowRoot(normalizedPrefix, root))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPathBelowRoot(string candidate, string root)
        {
            return candidate.Length > root.Length
                && candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                && IsDirectorySeparator(candidate[root.Length]);
        }

        private static bool IsDirectorySeparator(char value)
        {
            return value == Path.DirectorySeparatorChar || value == Path.AltDirectorySeparatorChar;
        }
    }
}
