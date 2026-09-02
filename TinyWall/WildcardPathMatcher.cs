using System;
using System.Collections.Generic;
using System.IO;

namespace pylorak.TinyWall
{
    public static class WildcardPathMatcher
    {
        private static readonly char[] WildcardCharacters = { '*', '?' };

        public static bool IsValidFilter(string? pattern, string? originalPath)
        {
            if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(originalPath))
                return false;

            return pattern!.IndexOfAny(WildcardCharacters) >= 0
                && originalPath!.IndexOfAny(WildcardCharacters) < 0
                && !ContainsControlCharacter(pattern)
                && HasProtectedLiteralPrefix(pattern)
                && Matches(pattern, originalPath);
        }

        public static bool HasProtectedLiteralPrefix(string? pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern)
                || !string.Equals(pattern, pattern!.Trim(), StringComparison.Ordinal)
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

                string normalizedPrefix = NormalizePath(literalPrefix);
                bool wildcardStartsBelowPrefix = IsDirectorySeparator(literalPrefix[literalPrefix.Length - 1]);
                foreach (string protectedRoot in GetProtectedPathRoots())
                {
                    if (IsProtectedPrefix(normalizedPrefix, protectedRoot, wildcardStartsBelowPrefix))
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
            if (IsDirectorySeparator(left) && IsDirectorySeparator(right))
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

        private static IEnumerable<string> GetProtectedPathRoots()
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

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool IsProtectedPrefix(
            string candidate,
            string protectedRoot,
            bool wildcardStartsBelowPrefix)
        {
            return (wildcardStartsBelowPrefix
                    && string.Equals(candidate, protectedRoot, StringComparison.OrdinalIgnoreCase))
                || (candidate.Length > protectedRoot.Length
                    && candidate.StartsWith(protectedRoot, StringComparison.OrdinalIgnoreCase)
                    && IsDirectorySeparator(candidate[protectedRoot.Length]));
        }

        private static bool IsDirectorySeparator(char value)
        {
            return value == Path.DirectorySeparatorChar || value == Path.AltDirectorySeparatorChar;
        }
    }
}
