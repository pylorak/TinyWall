using System;
using System.IO;
using pylorak.Windows;

namespace pylorak.TinyWall.Parser
{
    public sealed class ParserFolderVariable : ParserVariable
    {
        internal const string OPENING_TAG = "{folder:";

        internal override string Resolve(string str)
        {
            try
            {
                // Registry path
                return str switch
                {
                    "pf" or "pf64" => ProgramFilesx64(),
                    "pf32" => ProgramFilesx86(),
                    "sys32" => NativeSys32(),
                    "twpath" => Path.GetDirectoryName(Utils.ExecutablePath),
                    "LocalAppData" => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "windows" => Environment.GetEnvironmentVariable("windir"),
                    _ => str,
                };
            }
            catch
            {
                return str;
            }
        }

        private static string ProgramFilesx86()
        {
            if ((8 == IntPtr.Size) || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            else
                return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        private static string ProgramFilesx64()
        {
            if (VersionInfo.Is64BitOs)
                return Environment.GetEnvironmentVariable("ProgramW6432");
            else
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        }

        private static string NativeSys32()
        {
            if (VersionInfo.Is64BitOs)
                return Path.Combine(Environment.GetEnvironmentVariable("windir"), "System32");
            else
                return Environment.GetFolderPath(Environment.SpecialFolder.System);
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
