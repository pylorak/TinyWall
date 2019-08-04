using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace TinyWall.Interface.Parser
{
    public sealed class ParserFolderVariable : ParserVariable
    {
        internal static class NativeMethods
        {
            internal static bool is64BitProcess = (IntPtr.Size == 8);
            internal static bool is64BitOperatingSystem = is64BitProcess || InternalCheckIsWow64();

            [DllImport("kernel32", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool wow64Process);

            private static bool InternalCheckIsWow64()
            {
                if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) ||
                    Environment.OSVersion.Version.Major >= 6)
                {
                    using (Process p = Process.GetCurrentProcess())
                    {
                        try
                        {
                            bool retVal;
                            if (!IsWow64Process(p.Handle, out retVal))
                            {
                                return false;
                            }
                            return retVal;
                        }
                        catch { return false; }
                    }
                }
                else
                {
                    return false;
                }
            }
        }


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
                        return ProgramFilesx64();
                    case "pf32":
                        return ProgramFilesx86();
                    case "sys32":
                        return NativeSys32();
                    case "twpath":
                        return Path.GetDirectoryName(TinyWall.Interface.Internal.Utils.ExecutablePath);
                    case "LocalAppData":
                        return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    case "windows":
                        return Environment.GetEnvironmentVariable("windir");
                    default:
                        return str;
                }
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
            if (NativeMethods.is64BitOperatingSystem)
                return Environment.GetEnvironmentVariable("ProgramW6432");
            else
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        }

        private static string NativeSys32()
        {
            if (NativeMethods.is64BitOperatingSystem)
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
