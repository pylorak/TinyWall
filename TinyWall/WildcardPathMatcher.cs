using pylorak.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

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
        ErrorWildcardedFilename,
        ErrorFileSignatureFail,
    }

    public static class WildcardPathMatcher
    {
        [SuppressUnmanagedCodeSecurity]
        private static class SafeNativeMethods
        {
            [DllImport("userenv", SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetProfilesDirectory(StringBuilder lpProfileDir, ref uint lpcchSize);
        }

        private static readonly char[] WildcardCharacters = { '*', '?' };
        private static readonly IReadOnlyCollection<string> ProtectedPathRoots = BuildProtectedPathRoots();
        private static readonly string? UserProfileDirectoryRoot = GetUserProfileDirectoryRoot();

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
            else if (HasWildcardInUserProfileNameComponent(pattern))
            {
                return WildcardValidation.ErrorDisallowedFolder;
            }
            else if (HasWildcardInFilenameComponent(pattern))
            {
                return WildcardValidation.ErrorWildcardedFilename;
            }

            return WildcardValidation.Success;
        }

        // Returns true if the filename component (the last path segment) of a pattern contains a wildcard character.
        private static bool HasWildcardInFilenameComponent(string? pattern)
        {
            if (Utils.IsNullOrEmpty(pattern))
                return false;

            // Scan the final path segment backwards, up to the last directory separator.
            for (int i = pattern!.Length - 1; i >= 0; i--)
            {
                if (IsPathSeparator(pattern[i]))
                    break;
                else if (IsWildcardCharacter(pattern[i]))
                    return true;
            }

            return false;
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
                if (IsPathSeparator(pattern[i]))
                {
                    i++;
                    continue;
                }

                // Consume one path segment, remember whether it contained a wildcard
                bool hasWildcard = false;
                while ((i < pattern.Length) && !IsPathSeparator(pattern[i]))
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
                if (IsPathSeparator(value))
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

        public static WildcardValidation CheckPatternWithFile(string? pattern, string filePath, ref bool? sigVerifyPass)
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
                    && ((wildcardPattern[patternIndex] == '?' && !IsPathSeparator(candidatePath[pathIndex]))
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
                else if (lastStarIndex >= 0 && !IsPathSeparator(candidatePath[starMatchIndex]))
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

        // Returns true if the pattern would wildcard a user's profile directory (such as C:\Users\<name>)
        private static bool HasWildcardInUserProfileNameComponent(string pattern)
        {
            static bool TryGetNextSegment(string path, ref int index, out string segment)
            {
                while ((index < path.Length) && IsPathSeparator(path[index]))
                    index++;

                int start = index;
                while ((index < path.Length) && !IsPathSeparator(path[index]))
                    index++;

                if (start == index)
                {
                    segment = string.Empty;
                    return false;
                }

                segment = path.Substring(start, index - start);
                return true;
            }

            string? profileRoot = UserProfileDirectoryRoot;
            if (Utils.IsNullOrEmpty(profileRoot))
                return false;

            // Walk the profiles root and the pattern segment-by-segment
            int rootIndex = 0;
            int patternIndex = 0;
            while (TryGetNextSegment(profileRoot, ref rootIndex, out string rootSegment))
            {
                if (!TryGetNextSegment(pattern, ref patternIndex, out string patternSegment))
                {
                    // The pattern does not reach below the profiles root
                    return false;
                }

                if (patternSegment.IndexOfAny(WildcardCharacters) >= 0)
                {
                    if (!Matches(patternSegment, rootSegment))
                        return false;
                }
                else if (!string.Equals(patternSegment, rootSegment, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // The next pattern segment is at the position of the user profile name
            if (!TryGetNextSegment(pattern, ref patternIndex, out string nameSegment))
                return false;

            return nameSegment.IndexOfAny(WildcardCharacters) >= 0;
        }

        // The directory that contains the user profile directories of all local users
        // (e.g. C:\Users), or null if it could not be determined.
        private static string? GetUserProfileDirectoryRoot()
        {
            var profileDir = new StringBuilder(260);
            uint size = (uint)profileDir.Capacity;
            if (!SafeNativeMethods.GetProfilesDirectory(profileDir, ref size))
                return null;

            string path = profileDir.ToString();
            if (Utils.IsNullOrEmpty(path) || !Utils.IsPathFullyQualified(path))
                return null;

            return path;
        }

        private static IReadOnlyCollection<string> BuildProtectedPathRoots()
        {
            Environment.SpecialFolder[] folders =
            {
                Environment.SpecialFolder.ProgramFiles,
                Environment.SpecialFolder.ProgramFilesX86,
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
                && IsPathSeparator(candidate[root.Length]);
        }

        public static bool IsPathSeparator(char value)
        {
            return value == Path.DirectorySeparatorChar || value == Path.AltDirectorySeparatorChar;
        }
    }
}
