using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace PKSoft.Parser
{
    internal class ParserRegistryVariable : ParserVariable
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
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath, false))
                {
                    return (string)key.GetValue(keyValue);
                }
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
