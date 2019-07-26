using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace PKSoft
{
    [SuppressUnmanagedCodeSecurityAttribute]
    internal static class SafeNativeMethods
    {
        [Flags]
        internal enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            Synchronize = 0x00100000,
            ReadControl = 0x00020000,
            QueryLimitedInformation = 0x00001000,
        }

        [DllImport("kernel32.dll")]
        internal static extern bool QueryFullProcessImageName(IntPtr hprocess, int dwFlags,
                       StringBuilder lpExeName, out int size);
        [DllImport("kernel32.dll")]
        internal static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess,
                       bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr hHandle);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.U4)]
        internal static extern int GetLongPathName(
            [MarshalAs(UnmanagedType.LPTStr)]
                string lpszShortPath,
            [MarshalAs(UnmanagedType.LPTStr)]
                StringBuilder lpszLongPath,
            [MarshalAs(UnmanagedType.U4)]
                int cchBuffer);
    }
}
