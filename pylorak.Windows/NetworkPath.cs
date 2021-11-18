using System;
using System.Runtime.InteropServices;
using System.Security;

namespace pylorak.Windows
{
    public static class NetworkPath
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static unsafe extern bool PathIsNetworkPath(char* pszPath);

            #region WNetGetUniversalName
            internal const int UNIVERSAL_NAME_INFO_LEVEL = 0x00000001;
            internal const int ERROR_MORE_DATA = 234;
            internal const int ERROR_NOT_CONNECTED = 2250;
            internal const int NOERROR = 0;

            [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.U4)]
            internal static extern int WNetGetUniversalName(
                string lpLocalPath,
                [MarshalAs(UnmanagedType.U4)] int dwInfoLevel,
                IntPtr lpBuffer,
                [MarshalAs(UnmanagedType.U4)] ref int lpBufferSize);
            #endregion
        }

        public static bool IsUncPath(string path)
        {
            return IsUncPath(path.AsSpan());
        }

        public static bool IsUncPath(ReadOnlySpan<char> path)
        {
            return (path.Length > 2) && (path[0] == '\\') && (path[1] == '\\');
        }

        public static string GetUncPath(string localPath)
        {
            // The pointer in memory to the structure.
            IntPtr buffer = IntPtr.Zero;

            // Wrap in a try/catch block for cleanup.
            try
            {
                // First, call WNetGetUniversalName to get the size.
                int size = 0;

                // Make the call.
                // Pass IntPtr.Size because the API doesn't like null, even though
                // size is zero.  We know that IntPtr.Size will be
                // aligned correctly.
                int apiRetVal = NativeMethods.WNetGetUniversalName(localPath, NativeMethods.UNIVERSAL_NAME_INFO_LEVEL, (IntPtr)IntPtr.Size, ref size);
                if (apiRetVal != NativeMethods.ERROR_MORE_DATA)
                    throw new System.ComponentModel.Win32Exception(apiRetVal);

                // Allocate the memory.
                buffer = Marshal.AllocCoTaskMem(size);

                // Now make the call.
                apiRetVal = NativeMethods.WNetGetUniversalName(localPath, NativeMethods.UNIVERSAL_NAME_INFO_LEVEL, buffer, ref size);
                if (apiRetVal != NativeMethods.NOERROR)
                    throw new System.ComponentModel.Win32Exception(apiRetVal);

                // Now get the string.  It's all in the same buffer, but
                // the pointer is first, so offset the pointer by IntPtr.Size
                // and pass to PtrToStringAnsi.
                return Marshal.PtrToStringUni(new IntPtr(buffer.ToInt64() + IntPtr.Size));
            }
            finally
            {
                // Release the buffer.
                Marshal.FreeCoTaskMem(buffer);
            }
        }

        public static bool IsNetworkPath(string path)
        {
            return IsNetworkPath(path.AsSpan());
        }

        public static bool IsNetworkPath(ReadOnlySpan<char> path)
        {
            unsafe
            {
                fixed (char* ptr = path)
                {
                    return NativeMethods.PathIsNetworkPath(ptr);
                }
            }
        }
    }
}
