using System;
using System.Runtime.InteropServices;

namespace pylorak.Windows.Services
{
    internal static class NativeMethods
    {
        [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);

        [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
        public extern static IntPtr RegisterServiceCtrlHandlerEx(string serviceName, ServiceCtrlHandlerExDelegate callback, IntPtr userData);

        [DllImport("advapi32", SetLastError = true)]
        public static extern bool SetServiceStatus(IntPtr hServiceStatus, ref SERVICE_STATUS lpServiceStatus);

        [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
        public extern static bool StartServiceCtrlDispatcher(IntPtr entry);

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeServiceHandle OpenSCManager(
            string? machineName,
            string? databaseName,
            ServiceControlAccessRights desiredAccess);

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr OpenService(
            SafeServiceHandle hSCManager,
            string serviceName,
            ServiceAccessRights desiredAccess);

        [DllImport("advapi32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryServiceConfig(
            SafeServiceHandle hService,
            IntPtr intPtrQueryConfig,
            uint cbBufSize,
            out uint pcbBytesNeeded);

        /*
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int QueryServiceConfig2(
            IntPtr hService,
            ServiceConfig2InfoLevel dwInfoLevel,
            IntPtr lpBuffer,
            int cbBufSize,
            out int pcbBytesNeeded);
         */

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ChangeServiceConfig(
            SafeServiceHandle hService,
            uint nServiceType,
            uint nStartType,
            uint nErrorControl,
            string? lpBinaryPathName,
            string? lpLoadOrderGroup,
            IntPtr lpdwTagId,
            [In] char[]? lpDependencies,
            string? lpServiceStartName,
            string? lpPassword,
            string? lpDisplayName);

        [DllImport("advapi32", SetLastError = true)]
        public static extern int ChangeServiceConfig2(
            SafeServiceHandle hService,
            ServiceConfig2InfoLevel dwInfoLevel,
            IntPtr lpInfo);

        [DllImport("advapi32", SetLastError = true)]
        public static extern bool QueryServiceStatus(SafeServiceHandle hServiceStatus, ref SERVICE_STATUS lpServiceStatus);

        [DllImport("advapi32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryServiceStatusEx(
            SafeServiceHandle hService,
            ServiceInfoLevel InfoLevel,
            IntPtr lpBuffer,
            uint cbBufSize,
            out uint pcbBytesNeeded);
    }
}
