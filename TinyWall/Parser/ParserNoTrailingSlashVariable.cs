using System;
using System.IO;

namespace pylorak.TinyWall.Parser
{
    public sealed class ParserNoTrailingSlashVariable : ParserVariable
    {
        internal const string OPENING_TAG = "{NoTrailingSlash:";

        internal override string Resolve(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            if (str[str.Length - 1] == '\\')
                return str.Substring(0, str.Length - 1);
            else
                return str;
        }

        internal static int OpeningTagLength
        {
            get
            {
                return OPENING_TAG.Length;
            }
        }

        internal static bool IsStartTag(string str, int pos)
        {
            int tagLen = OpeningTagLength;
            return (str.Length > pos + tagLen) && (string.CompareOrdinal(OPENING_TAG, 0, str, pos, tagLen) == 0);
        }

        internal override string GetOpeningTag()
        {
            return OPENING_TAG;
        }
        internal override int GetOpeningTagLength()
        {
            return OpeningTagLength;
        }

    }
}
