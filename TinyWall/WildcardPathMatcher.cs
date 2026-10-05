using pylorak.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;

namespace pylorak.TinyWall
{
    public static class WildcardPathMatcher
    {
        private static readonly char[] WildcardCharacters = { '*', '?' };
        private static readonly Lazy<IReadOnlyCollection<string>> ProtectedPathRoots =
            new(BuildProtectedPathRoots);
        private static readonly Lazy<IReadOnlyCollection<string>> UserProfilePathRoots =
            new(BuildUserProfilePathRoots);

        public static bool IsValidFilter(string? pattern, string? originalPath)
        {
            if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(originalPath))
                return false;

            if (originalPath!.IndexOfAny(WildcardCharacters) >= 0
                || !TryGetLiteralPrefix(pattern, out string normalizedPrefix, out bool wildcardStartsBelowPrefix)
                || !Matches(pattern, originalPath))
            {
                return false;
            }

            if (HasLiteralPrefixInRoots(normalizedPrefix, wildcardStartsBelowPrefix, ProtectedPathRoots.Value))
            {
                return true;
            }

            return HasLiteralPrefixInRoots(normalizedPrefix, wildcardStartsBelowPrefix, UserProfilePathRoots.Value)
                && IsUserProfilePath(originalPath)
                && HasTrustedSignature(originalPath);
        }

        public static bool HasProtectedLiteralPrefix(string? pattern)
        {
            return TryGetLiteralPrefix(pattern, out string normalizedPrefix, out bool wildcardStartsBelowPrefix)
                && HasLiteralPrefixInRoots(normalizedPrefix, wildcardStartsBelowPrefix, ProtectedPathRoots.Value);
        }

        public static bool HasUserProfileLiteralPrefix(string? pattern)
        {
            return TryGetLiteralPrefix(pattern, out string normalizedPrefix, out bool wildcardStartsBelowPrefix)
                && HasLiteralPrefixInRoots(normalizedPrefix, wildcardStartsBelowPrefix, UserProfilePathRoots.Value);
        }

        public static bool RequiresTrustedSignature(string? pattern)
        {
            return TryGetLiteralPrefix(pattern, out string normalizedPrefix, out bool wildcardStartsBelowPrefix)
                && !HasLiteralPrefixInRoots(normalizedPrefix, wildcardStartsBelowPrefix, ProtectedPathRoots.Value)
                && HasLiteralPrefixInRoots(normalizedPrefix, wildcardStartsBelowPrefix, UserProfilePathRoots.Value);
        }

        public static bool IsUserProfilePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)
                || path!.IndexOfAny(WildcardCharacters) >= 0
                || ContainsControlCharacter(path))
            {
                return false;
            }

            try
            {
                string expandedPath = Environment.ExpandEnvironmentVariables(path);
                if (!IsFullyQualifiedLocalPath(expandedPath))
                {
                    return false;
                }

                string normalizedPath = NormalizePath(expandedPath);
                foreach (string profileRoot in UserProfilePathRoots.Value)
                {
                    if (IsPathBelowRoot(normalizedPath, profileRoot))
                    {
                        return true;
                    }
                }
            }
            catch (Exception exception) when (exception is ArgumentException
                || exception is NotSupportedException
                || exception is PathTooLongException)
            {
                return false;
            }

            return false;
        }

        public static bool HasTrustedSignature(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            try
            {
                string expandedPath = Environment.ExpandEnvironmentVariables(filePath);
                return File.Exists(expandedPath)
                    && WinTrust.VerifyFileAuthenticode(expandedPath)
                    == WinTrust.VerifyResult.SIGNATURE_VALID;
            }
            catch (Exception)
            {
                // Wildcard filters in user-profile folders are permitted only
                // for executables with valid, trusted signatures. Treat any
                // verification failure as a rejection to preserve that
                // security boundary.
                return false;
            }
        }

        private static bool TryGetLiteralPrefix(
            string? pattern,
            out string normalizedPrefix,
            out bool wildcardStartsBelowPrefix)
        {
            normalizedPrefix = string.Empty;
            wildcardStartsBelowPrefix = false;

            if (string.IsNullOrWhiteSpace(pattern)
                || char.IsWhiteSpace(pattern![0])
                || char.IsWhiteSpace(pattern[pattern.Length - 1])
                || ContainsControlCharacter(pattern))
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
                string literalPrefix = Environment.ExpandEnvironmentVariables(pattern.Substring(0, wildcardIndex));
                if (!IsFullyQualifiedLocalPath(literalPrefix))
                {
                    return false;
                }

                string? pathRoot = Path.GetPathRoot(literalPrefix);
                if (string.IsNullOrEmpty(pathRoot)
                    || literalPrefix.IndexOf(':', pathRoot.Length) >= 0)
                {
                    return false;
                }

                normalizedPrefix = NormalizePath(literalPrefix);
                wildcardStartsBelowPrefix = IsDirectorySeparator(literalPrefix[literalPrefix.Length - 1]);
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException
                || exception is NotSupportedException
                || exception is PathTooLongException)
            {
                return false;
            }
        }

        public static bool Matches(string? pattern, string? path)
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
                    && (wildcardPattern[patternIndex] == '?'
                        || PathCharactersEqual(wildcardPattern[patternIndex], candidatePath[pathIndex])))
                {
                    patternIndex++;
                    pathIndex++;
                }
                else if (patternIndex < wildcardPattern.Length && wildcardPattern[patternIndex] == '*')
                {
                    lastStarIndex = patternIndex++;
                    starMatchIndex = pathIndex;
                }
                else if (lastStarIndex >= 0)
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

        private static bool PathCharactersEqual(char left, char right)
        {
            if (left == right || (IsDirectorySeparator(left) && IsDirectorySeparator(right)))
            {
                return true;
            }

            return char.ToUpperInvariant(left) == char.ToUpperInvariant(right);
        }

        private static bool ContainsControlCharacter(string value)
        {
            foreach (char character in value)
            {
                if (char.IsControl(character))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsFullyQualifiedLocalPath(string path)
        {
            string? root = Path.GetPathRoot(path);
            return !string.IsNullOrEmpty(root)
                && root.Length >= 3
                && char.IsLetter(root[0])
                && root[1] == ':'
                && IsDirectorySeparator(root[2]);
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
                    roots.Add(NormalizePath(path));
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
            // so both processes recognize filters beneath user profiles.
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

        private static void AddPathRoot(ISet<string> roots, string? path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                string expandedPath = Environment.ExpandEnvironmentVariables(path);
                if (IsFullyQualifiedLocalPath(expandedPath))
                {
                    roots.Add(NormalizePath(expandedPath));
                }
            }
        }

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool HasLiteralPrefixInRoots(
            string normalizedPrefix,
            bool wildcardStartsBelowPrefix,
            IEnumerable<string> allowedRoots)
        {
            foreach (string root in allowedRoots)
            {
                if ((wildcardStartsBelowPrefix
                        && string.Equals(normalizedPrefix, root, StringComparison.OrdinalIgnoreCase))
                    || IsPathBelowRoot(normalizedPrefix, root))
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
