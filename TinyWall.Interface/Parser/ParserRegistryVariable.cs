using System;
using System.Collections.Generic;
using System.Text;

namespace TinyWall.Interface.Parser
{
    public sealed class ParserRegistryVariable : ParserVariable
    {
        internal const string OPENING_TAG = "{reg:";

        internal override string Resolve(string str)
        {
            try
            {
                // Registry path
                string[] tokens = str.Split(':');
                string keyPath = tokens[0];
                string keyValue = tokens[1];

                // We use a custom function to access 64-bit registry view on dotNet 3.5.
                string val = Registry.ReadRegString(Registry.HKEY_LOCAL_MACHINE, keyPath, keyValue, RegWow64Options.KEY_WOW64_64KEY);
                return val ?? str;
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
            return (str.Length > pos + tagLen) && (string.CompareOrdinal(OPENING_TAG, 0, str, pos, tagLen)==0);
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
