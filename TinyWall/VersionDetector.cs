using System;

namespace pylorak.TinyWall
{
    internal static class VersionDetector
    {
        // Detects a version number in a file path and reports the span of the detected
        // version string within the input. Returns false if no unambiguous version
        // could be detected.
        //
        // The input is analyzed as a raw string (it does not have to be a valid path),
        // but it is assumed that directory separators separate segments in the input.
        // To avoid ambiguity, if more than one segment contains a version string,
        // the detector fails.
        //
        // Grammar of a version candidate (a contiguous substring of one segment):
        //   version = core [ tag ]
        //   core    = 2-4 components of 1-6 digits, separated by "."
        //   tag     = [ "." | "_" | "-" ] ( "alpha" | "beta" | "rc" )
        //             [ [ "." | "_" | "-" ] single digit ]
        //   Tag words and "v" prefixes match case-insensitively.
        //
        // Bounding rules (each side of a candidate must independently satisfy one):
        //   right: the candidate ends at the end of its segment, or the next character
        //          is "_" or "-".
        //   left:  the candidate starts at the start of its segment, or the previous
        //          character is "_" or "-", or the previous character is "v"/"V" whose
        //          own left neighbor is the segment start or one of ".", "-", "_", or
        //          the previous character is "." immediately preceded by such a "v"/"V"
        //          (the "v." form). Candidates of these last two shapes are v-prefixed;
        //          their span never includes the "v"/"v." itself.
        //
        // Selection rules (failure if zero, or two or more, segments hold candidates):
        //   1. if exactly one candidate is v-prefixed, that candidate wins;
        //   2. else if exactly one candidate has a tag, that candidate wins;
        //   3. else if no candidate has a tag and none is v-prefixed, the result spans
        //      from the leftmost candidate's start to the rightmost candidate's end,
        //      including any text between them;
        //   4. else failure.
        internal static bool TryFindVersionSpan(string? path, out int start, out int length)
        {
            start = 0;
            length = 0;

            if (Utils.IsNullOrEmpty(path))
                return false;

            // Selection state of the single segment that is allowed to hold candidates.
            bool foundVersionSegment = false;
            bool segmentHasCandidate = false;
            int vPrefixCount = 0;
            int vPrefixStart = 0;
            int vPrefixLength = 0;
            int tagCount = 0;
            int tagSpanStart = 0;
            int tagSpanLength = 0;
            int leftmostStart = 0;
            int rightmostEnd = 0;

            int pathLength = path.Length;
            int pos = 0;
            while (pos < pathLength)
            {
                // Skip path separators, empty path segments are not analyzed
                while ((pos < pathLength) && WildcardPathMatcher.IsPathSeparator(path[pos]))
                    pos++;
                if (pos == pathLength)
                    break;

                // Determine the extent of the current segment
                int segStart = pos;
                while ((pos < pathLength) && !WildcardPathMatcher.IsPathSeparator(path[pos]))
                    pos++;
                int segEnd = pos;

                segmentHasCandidate = false;

                // Scan the segment for candidates, left to right
                int i = segStart;
                while (i < segEnd)
                {
                    if (!char.IsDigit(path[i]))
                    {
                        i++;
                        continue;
                    }

                    // Measure the maximal digit run starting at i
                    int runStart = i;
                    do
                    {
                        i++;
                    }
                    while ((i < segEnd) && char.IsDigit(path[i]));

                    // A run longer than 6 digits cannot start a candidate
                    if ((i - runStart) > 6)
                        continue;

                    // Parse the longest possible core (up to 4 components)
                    int coreEnd = i;
                    int components = 1;
                    while ((components < 4) && (coreEnd < segEnd) && (path[coreEnd] == '.'))
                    {
                        int compStart = coreEnd + 1;
                        if ((compStart >= segEnd) || !char.IsDigit(path[compStart]))
                            break;

                        int runEnd = compStart;
                        while ((runEnd < segEnd) && char.IsDigit(path[runEnd]))
                            runEnd++;

                        if ((runEnd - compStart) > 6)
                            break;

                        coreEnd = runEnd;
                        components++;
                    }

                    if (components < 2)
                        continue; // i already sits at the end of the run

                    // All candidate variants at this position share their start,
                    // so they share their left bound.
                    if (!HasValidLeftBound(path, runStart, segStart, out bool vPrefixed))
                    {
                        i = coreEnd;
                        continue;
                    }

                    // Variant ends: core, core + tag word, core + tag word + digit.
                    // The delimiters are optional, and the tag-digit delimiter is
                    // consumed only when a digit actually follows.
                    int tagPos = coreEnd;
                    if ((tagPos < segEnd) && IsVersionDelimiter(path[tagPos]))
                        tagPos++;

                    int wordLength = MatchTagWord(path, tagPos, segEnd);
                    bool hasWord = wordLength > 0;
                    int wordEnd = hasWord ? (tagPos + wordLength) : coreEnd;

                    int digitEnd = -1;
                    if (hasWord)
                    {
                        if ((wordEnd < segEnd) && char.IsDigit(path[wordEnd]))
                            digitEnd = wordEnd + 1;
                        else if ((wordEnd + 1 < segEnd) && IsVersionDelimiter(path[wordEnd]) && char.IsDigit(path[wordEnd + 1]))
                            digitEnd = wordEnd + 2;
                    }

                    // Keep the longest variant whose right side is valid
                    int candidateEnd;
                    bool hasTag;
                    if ((digitEnd >= 0) && IsValidRightBound(path, segEnd, digitEnd))
                    {
                        candidateEnd = digitEnd;
                        hasTag = true;
                    }
                    else if (hasWord && IsValidRightBound(path, segEnd, wordEnd))
                    {
                        candidateEnd = wordEnd;
                        hasTag = true;
                    }
                    else if (IsValidRightBound(path, segEnd, coreEnd))
                    {
                        candidateEnd = coreEnd;
                        hasTag = false;
                    }
                    else
                    {
                        candidateEnd = -1;
                        hasTag = false;
                    }

                    if (candidateEnd >= 0)
                    {
                        if (foundVersionSegment)
                        {
                            // A second segment with a version is ambiguous
                            return false;
                        }

                        if (!segmentHasCandidate)
                        {
                            segmentHasCandidate = true;
                            leftmostStart = runStart;
                        }
                        rightmostEnd = candidateEnd;

                        if (vPrefixed)
                        {
                            vPrefixCount++;
                            if (vPrefixCount == 1)
                            {
                                vPrefixStart = runStart;
                                vPrefixLength = candidateEnd - runStart;
                            }
                        }

                        if (hasTag)
                        {
                            tagCount++;
                            if (tagCount == 1)
                            {
                                tagSpanStart = runStart;
                                tagSpanLength = candidateEnd - runStart;
                            }
                        }
                    }

                    // Runs inside the parsed core cannot start a candidate
                    i = coreEnd;
                }

                if (segmentHasCandidate)
                    foundVersionSegment = true;
            }

            if (!foundVersionSegment)
                return false;

            // Selection rules
            if (vPrefixCount == 1)
            {
                start = vPrefixStart;
                length = vPrefixLength;
                return true;
            }

            if (tagCount == 1)
            {
                start = tagSpanStart;
                length = tagSpanLength;
                return true;
            }

            if ((vPrefixCount == 0) && (tagCount == 0))
            {
                start = leftmostStart;
                length = rightmostEnd - leftmostStart;
                return true;
            }

            return false;
        }

        // The characters that may separate a core from a tag word or a tag word
        // from its digit, and that may precede a "v" prefix.
        private static bool IsVersionDelimiter(char value)
        {
            return (value == '.') || (value == '_') || (value == '-');
        }

        private static bool IsVersionBound(char value)
        {
            return (value == '_') || (value == '-');
        }

        private static bool IsValidRightBound(string path, int segmentEnd, int candidateEnd)
        {
            if (candidateEnd == segmentEnd)
                return true;

            return IsVersionBound(path[candidateEnd]);
        }

        private static bool HasValidLeftBound(string path, int candidateStart, int segmentStart, out bool vPrefixed)
        {
            vPrefixed = false;

            if (candidateStart == segmentStart)
                return true;

            int prev = candidateStart - 1;
            char prevChar = path[prev];

            if (IsVersionBound(prevChar))
                return true;

            if (('v' == prevChar) || ('V' == prevChar))
            {
                // The "v" itself must be bounded by the segment start or a delimiter
                if ((prev == segmentStart) || IsVersionDelimiter(path[prev - 1]))
                {
                    vPrefixed = true;
                    return true;
                }
                return false;
            }

            if (('.' == prevChar)
                && (prev - 1 >= segmentStart)
                && (('v' == path[prev - 1]) || ('V' == path[prev - 1]))
                && ((prev - 1 == segmentStart) || IsVersionDelimiter(path[prev - 2])))
            {
                // The "v." form
                vPrefixed = true;
                return true;
            }

            return false;
        }

        // Returns the length of the tag found at pos in path, or 0 if no tag found.
        private static int MatchTagWord(string path, int pos, int segmentEnd)
        {
            if (MatchesTagWord(path, pos, segmentEnd, "ALPHA"))
                return 5;
            if (MatchesTagWord(path, pos, segmentEnd, "BETA"))
                return 4;
            if (MatchesTagWord(path, pos, segmentEnd, "RC"))
                return 2;
            return 0;
        }

        private static bool MatchesTagWord(string path, int pos, int segmentEnd, string word)
        {
            if (pos + word.Length > segmentEnd)
                return false;

            for (int i = 0; i < word.Length; i++)
            {
                if (char.ToUpperInvariant(path[pos + i]) != word[i])
                    return false;
            }

            return true;
        }

#if DEBUG
        internal static void Test()
        {
            void Check(string? input, string? expected)
            {
                bool found = VersionDetector.TryFindVersionSpan(input, out int start, out int length);
                string? actual = found ? input!.Substring(start, length) : null;
                if (!string.Equals(expected, actual, StringComparison.Ordinal))
                    throw new Exception($"TryFindVersionSpan mismatch. Input: \"{input}\" Expected: \"{expected}\" Actual: \"{actual}\".");
            }

            // Basic detection
            Check("app-0.11.0", "0.11.0");
            Check("2.14.0", "2.14.0");
            Check("app-01.02", "01.02");
            Check("1.234567.8", "1.234567.8");
            Check("Claude_1.44121.2.0_x64__pzs8sxrjxfjjc", "1.44121.2.0");
            Check("anthropic.claude-code-2.1.173-win32-x64", "2.1.173");

            // Tag handling
            Check("app-1.2.3-beta.1", "1.2.3-beta.1");
            Check("app-1.2.3-beta1", "1.2.3-beta1");
            Check("1.2-RC1", "1.2-RC1");
            Check("x_1.2-beta_y", "1.2-beta");
            Check("x_1.2beta_y", "1.2beta");
            Check("x_1.2.beta_y", "1.2.beta");
            Check("x_1.2.3.4.beta_y", "1.2.3.4.beta");
            Check("x_1.2-beta-", "1.2-beta");
            Check("x_1.2-beta-3z_y", "1.2-beta");
            Check("x_1.2-beta-3_y", "1.2-beta-3");
            Check("x_1.2-beta-3.4_y", "1.2-beta");
            Check("x_1.2-beta12_y", "1.2");
            Check("x_1.2-beta.34_y", "1.2");
            Check("x_1.2-alphabet_y", "1.2");

            // "v" prefix
            Check("app-v2.3.1", "2.3.1");
            Check("MyApp.v2.3.1", "2.3.1");
            Check("v2.3", "2.3");
            Check("V.2.3", "2.3");
            Check("myapp-v2.3.1-beta1.2", "2.3.1");
            Check("app-v1.2.3-beta1", "1.2.3-beta1");
            Check("app-v1.2.3-beta-1", "1.2.3-beta-1");

            // Selection rules
            Check("1.2-3.4", "1.2-3.4");
            Check("1.2-4.5.6", "1.2-4.5.6");
            Check("1.2-3.4-5.6", "1.2-3.4-5.6");
            Check("x_1.2_y-3.4", "1.2_y-3.4");
            Check("1.2_V3.4", "3.4");
            Check("1.2-v3.4", "3.4");
            Check("v1.2-3.4.5.6_x", "1.2");
            Check("v1.2-3.4-beta_x", "1.2");
            Check("v1.2-beta-v3.4", "1.2-beta");
            Check("x_1.2.3-beta_x", "1.2.3-beta");
            Check("x_1.2.3-1.2-beta_y", "1.2-beta");
            Check("app-v1.2.3-beta1", "1.2.3-beta1");

            // Failures
            Check("dev2.3", null);
            Check("xv.2.3", null);
            Check("vv2.3", null);
            Check("1.2v3.4", null);
            Check("app-2.3.1.zip", null);
            Check("x-1.2.exe", null);
            Check("MyApp.2.3", null);
            Check("v1.2-v3.4", null);
            Check("v1.2.3-v4.5", null);
            Check("x_1.2-beta-3.4-rc_y", null);
            Check("1.2.3.4.5", null);
            Check("1234567.8", null);
            Check("x-1.2345678.9", null);
            Check("x_1.2.3.4.5-beta_y", null);
            Check("x_1.2.", null);
            Check("x_1.2alphabet_y", null);
            Check("app-1.2 ", null); // trailing space
            Check("app-123", null);
            Check(string.Empty, null);
            Check("   ", null);
            Check(null, null);

            // Paths and segments
            Check(@"app-0.9.3\claude.exe", "0.9.3");
            Check(@"app-1.0\app-2.0\x.exe", null);
            Check(@"app-1.0\app-1.0\x.exe", null);
            Check(@"C:\app-1.2/x-3.4", null);
            Check(@"\\srv\app-1.2\x", "1.2");
            Check(@"C:\app-1.2", "1.2");

            // Additional cases
            Check("x.V.2.3_y", "2.3");
            Check("01.02.03-rc2", "01.02.03-rc2");
            Check("1.2_beta", "1.2_beta");
            Check("1.2.3.4.5.6.7.8", null);
            Check(@"C:\Program Files\Claude_1.44121.2.0_x64__pzs8sxrjxfjjc\Claude.exe", "1.44121.2.0");
        }
#endif // DEBUG
    }
}
