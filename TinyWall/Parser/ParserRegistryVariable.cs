using System;
using System.Collections.Generic;
using System.Text;
using TinyWall.Interface.Internal;
using Microsoft.Win32;

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

                return Utils.GetReg64StrValue(RegistryHive.LocalMachine, keyPath, keyValue) ?? str;
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
