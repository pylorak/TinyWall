using System;
using System.Collections.Generic;
using System.IO;

namespace PKSoft.Parser
{
    internal class ParserParentVariable : ParserVariable
    {
        internal const string OPENING_TAG = "{parent:";

        internal override string Resolve(string str)
        {
            try
            {
                return Path.GetDirectoryName(str);
            }
            catch
            {
                return str;
            }
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
            return (str.Length > pos + tagLen) && str.Substring(pos, tagLen).Equals(OPENING_TAG);
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
