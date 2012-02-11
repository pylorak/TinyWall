using System;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace PKSoft.Parser
{
    internal class ParserFolderVariable : ParserVariable
    {
        internal const string OPENING_TAG = "{folder:";

        internal override string Resolve(string str)
        {
            try
            {
                // Registry path
                switch (str)
                {
                    case "pf":
                    case "pf64":
                        return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    case "pf32":
                        return Utils.ProgramFilesx86();
                    case "sys32":
                        return Environment.GetFolderPath(Environment.SpecialFolder.System);
                    case "twpath":
                        return Path.GetDirectoryName(Utils.ExecutablePath);
                    case "LocalAppData":
                        return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    default:
                        return str;
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
