using System;
using System.ComponentModel;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.ServiceProcess;
using Microsoft.Win32.SafeHandles;
using TinyWall.Interface.Internal;

namespace PKSoft
{
    #region Win32 API Declarations

    [Flags]
    enum ServiceControlAccessRights : int
    {
        SC_MANAGER_CONNECT = 0x0001, // Required to connect to the service control manager. 
        SC_MANAGER_CREATE_SERVICE = 0x0002, // Required to call the CreateService function to create a service object and add it to the database. 
        SC_MANAGER_ENUMERATE_SERVICE = 0x0004, // Required to call the EnumServicesStatusEx function to list the services that are in the database. 
        SC_MANAGER_LOCK = 0x0008, // Required to call the LockServiceDatabase function to acquire a lock on the database. 
        SC_MANAGER_QUERY_LOCK_STATUS = 0x0010, // Required to call the QueryServiceLockStatus function to retrieve the lock status information for the database
        SC_MANAGER_MODIFY_BOOT_CONFIG = 0x0020, // Required to call the NotifyBootConfigStatus function. 
        SC_MANAGER_ALL_ACCESS = 0xF003F // Includes STANDARD_RIGHTS_REQUIRED, in addition to all access rights in this table. 
    }

    [Flags]
    enum ServiceAccessRights : int
    {
        SERVICE_QUERY_CONFIG = 0x0001, // Required to call the QueryServiceConfig and QueryServiceConfig2 functions to query the service configuration. 
        SERVICE_CHANGE_CONFIG = 0x0002, // Required to call the ChangeServiceConfig or ChangeServiceConfig2 function to change the service configuration. Because this grants the caller the right to change the executable file that the system runs, it should be granted only to administrators. 
        SERVICE_QUERY_STATUS = 0x0004, // Required to call the QueryServiceStatusEx function to ask the service control manager about the status of the service. 
        SERVICE_ENUMERATE_DEPENDENTS = 0x0008, // Required to call the EnumDependentServices function to enumerate all the services dependent on the service. 
        SERVICE_START = 0x0010, // Required to call the StartService function to start the service. 
        SERVICE_STOP = 0x0020, // Required to call the ControlService function to stop the service. 
        SERVICE_PAUSE_CONTINUE = 0x0040, // Required to call the ControlService function to pause or continue the service. 
        SERVICE_INTERROGATE = 0x0080, // Required to call the ControlService function to ask the service to report its status immediately. 
        SERVICE_USER_DEFINED_CONTROL = 0x0100, // Required to call the ControlService function to specify a user-defined control code.
        SERVICE_ALL_ACCESS = 0xF01FF // Includes STANDARD_RIGHTS_REQUIRED in addition to all access rights in this table. 
    }

    enum ServiceConfig2InfoLevel : int
    {
        SERVICE_CONFIG_DESCRIPTION = 0x00000001, // The lpBuffer parameter is a pointer to a SERVICE_DESCRIPTION structure.
        SERVICE_CONFIG_FAILURE_ACTIONS = 0x00000002 // The lpBuffer parameter is a pointer to a SERVICE_FAILURE_ACTIONS structure.
    }

    enum SC_ACTION_TYPE : uint
    {
        SC_ACTION_NONE = 0x00000000, // No action.
        SC_ACTION_RESTART = 0x00000001, // Restart the service.
        SC_ACTION_REBOOT = 0x00000002, // Reboot the computer.
        SC_ACTION_RUN_COMMAND = 0x00000003 // Run a command.
    }

    [StructLayout(LayoutKind.Sequential)]
    struct QUERY_SERVICE_CONFIG
    {
        internal uint dwServiceType;
        internal uint dwStartType;
        internal uint dwErrorControl;
        [MarshalAs(UnmanagedType.LPTStr)]
        internal string lpBinaryPathName;
        [MarshalAs(UnmanagedType.LPTStr)]
        internal string lpLoadOrderGroup;
        internal uint dwTagID;
        [MarshalAs(UnmanagedType.LPTStr)]
        internal string lpDependencies;
        [MarshalAs(UnmanagedType.LPTStr)]
        internal string lpServiceStartName;
        [MarshalAs(UnmanagedType.LPTStr)]
        internal string lpDisplayName;
    };

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class SERVICE_STATUS_PROCESS
    {
        internal uint dwServiceType;
        internal uint dwCurrentState;
        internal uint dwControlsAccepted;
        internal uint dwWin32ExitCode;
        internal uint dwServiceSpecificExitCode;
        internal uint dwCheckPoint;
        internal uint dwWaitHint;
        internal uint dwProcessId;
        internal uint dwServiceFlags;
    }

    struct SERVICE_FAILURE_ACTIONS 
    {
        internal uint dwResetPeriod;
        [MarshalAs(UnmanagedType.LPStr)]
        internal string lpRebootMsg;
        [MarshalAs(UnmanagedType.LPStr)]
        internal string lpCommand;
        internal uint cActions;
        internal IntPtr lpsaActions;
    }

    struct SC_ACTION
    {
        internal SC_ACTION_TYPE Type;
        internal uint Delay;
    }

    public enum ServiceState : int
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    public enum ServiceInfoLevel : int
    {
        SC_STATUS_PROCESS_INFO = 0
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SERVICE_STATUS
    {
        public int serviceType;
        public ServiceState currentState;
        public int controlsAccepted;
        public int win32ExitCode;
        public int serviceSpecificExitCode;
        public int checkPoint;
        public int waitHint;
    };
    #endregion

    #region Native Methods

    internal static class NativeMethods
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeServiceHandle OpenSCManager(
            string machineName,
            string databaseName,
            ServiceControlAccessRights desiredAccess);

        [DllImport("advapi32.dll", SetLastError = true, CharSet=CharSet.Unicode)]
        internal static extern IntPtr OpenService(
            SafeServiceHandle hSCManager,
            string serviceName,
            ServiceAccessRights desiredAccess);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryServiceConfig(
            SafeServiceHandle hService,
            IntPtr intPtrQueryConfig,
            uint cbBufSize,
            out uint pcbBytesNeeded);

        /*
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern int QueryServiceConfig2(
            IntPtr hService,
            ServiceConfig2InfoLevel dwInfoLevel,
            IntPtr lpBuffer,
            int cbBufSize,
            out int pcbBytesNeeded);
         */

        [DllImport("advapi32.dll", SetLastError = true, CharSet=CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ChangeServiceConfig(
            SafeServiceHandle hService,
            uint nServiceType,
            uint nStartType,
            uint nErrorControl,
            string lpBinaryPathName,
            string lpLoadOrderGroup,
            IntPtr lpdwTagId,
            [In] char[] lpDependencies,
            string lpServiceStartName,
            string lpPassword,
            string lpDisplayName);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern int ChangeServiceConfig2(
            SafeServiceHandle hService,
            ServiceConfig2InfoLevel dwInfoLevel,
            IntPtr lpInfo);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool SetServiceStatus(IntPtr hServiceStatus, ref SERVICE_STATUS lpServiceStatus);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool QueryServiceStatus(SafeServiceHandle hServiceStatus, ref SERVICE_STATUS lpServiceStatus);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryServiceStatusEx(
            SafeServiceHandle hService,
            ServiceInfoLevel InfoLevel,
            IntPtr lpBuffer,
            uint cbBufSize,
            out uint pcbBytesNeeded);
    }

    #endregion

    internal class SafeServiceHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [System.Security.SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseServiceHandle(IntPtr hSCObject);

        }

        // Create a SafeHandle, informing the base class
        // that this SafeHandle instance "owns" the handle,
        // and therefore SafeHandle should call
        // our ReleaseHandle method when the SafeHandle
        // is no longer in use.
        private SafeServiceHandle()
            : base(true)
        {
        }

        internal SafeServiceHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        override protected bool ReleaseHandle()
        {
            // Here, we must obey all rules for constrained execution regions.
            return NativeMethods.CloseServiceHandle(handle);
            // If ReleaseHandle failed, it can be reported via the
            // "releaseHandleFailed" managed debugging assistant (MDA).  This
            // MDA is disabled by default, but can be enabled in a debugger
            // or during testing to diagnose handle corruption problems.
            // We do not throw an exception because most code could not recover
            // from the problem.
        }
    }
    
    internal class ServiceControlManager : Disposable
    {
        private const uint SERVICE_NO_CHANGE = 0xFFFFFFFF;
        private const uint ERROR_INSUFFICIENT_BUFFER = 122;

        private bool disposed = false;
        private SafeServiceHandle SCManager;

        internal bool SetServiceState(string serviceName, IntPtr privateHndl, ServiceState state, int exitCode)
        {
            SERVICE_STATUS status = new SERVICE_STATUS();
            using (var serviceHndl = OpenService(serviceName, ServiceAccessRights.SERVICE_QUERY_STATUS))
            {
                if (!NativeMethods.QueryServiceStatus(serviceHndl, ref status))
                    throw new Win32Exception();
            }

            status.currentState = state;
            status.win32ExitCode = exitCode;
            if (!NativeMethods.SetServiceStatus(privateHndl, ref status))
                throw new Win32Exception();
            return true;
        }

        /// <summary>
        /// Calls the Win32 OpenService function and performs error checking.
        /// </summary>
        /// <exception cref="ComponentModel.Win32Exception">"Unable to open the requested Service."</exception>
        private SafeServiceHandle OpenService(string serviceName, ServiceAccessRights desiredAccess)
        {
            // Open the service
            IntPtr service = NativeMethods.OpenService(
                SCManager,
                serviceName,
                desiredAccess);

            // Verify if the service is opened
            if (service == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return new SafeServiceHandle(service);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceControlManager"/> class.
        /// </summary>
        /// <exception cref="ComponentModel.Win32Exception">"Unable to open Service Control Manager."</exception>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        internal ServiceControlManager()
        {
            // Open the service control manager
            SCManager = NativeMethods.OpenSCManager(
                null,
                null,
                ServiceControlAccessRights.SC_MANAGER_CONNECT);

            // Verify if the SC is opened
            if (SCManager.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        /*
        /// <summary>
        /// Dertermines whether the nominated service is set to restart on failure.
        /// </summary>
        /// <exception cref="ComponentModel.Win32Exception">"Unable to query the Service configuration."</exception>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        internal bool HasRestartOnFailure(string serviceName)
        {
            const int bufferSize = 1024 * 8;

            IntPtr service = IntPtr.Zero;
            IntPtr bufferPtr = IntPtr.Zero;
            bool result = false;

            try
            {
                // Open the service
                service = OpenService(serviceName, ServiceAccessRights.SERVICE_QUERY_CONFIG);

                int dwBytesNeeded = 0;

                // Allocate memory for struct
                bufferPtr = Marshal.AllocHGlobal(bufferSize);
                int queryResult = NativeMethods.QueryServiceConfig2(
                    service,
                    ServiceConfig2InfoLevel.SERVICE_CONFIG_FAILURE_ACTIONS,
                    bufferPtr,
                    bufferSize,
                    out dwBytesNeeded);

                if (queryResult == 0)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                // Cast the buffer to a QUERY_SERVICE_CONFIG struct
                SERVICE_FAILURE_ACTIONS config =
                    (SERVICE_FAILURE_ACTIONS)Marshal.PtrToStructure(bufferPtr, typeof(SERVICE_FAILURE_ACTIONS));

                // Determine whether the service is set to auto restart
                if (config.cActions != 0)
                {
                    SC_ACTION action = (SC_ACTION)Marshal.PtrToStructure(config.lpsaActions, typeof(SC_ACTION));
                    result = (action.Type == SC_ACTION_TYPE.SC_ACTION_RESTART);
                }                

                return result;
            }
            finally
            {
                // Clean up
                if (bufferPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(bufferPtr);
                }

                if (service != IntPtr.Zero)
                {
                    NativeMethods.CloseServiceHandle(service);
                }
            }
        }
        */

        /// <summary>
        /// Sets the nominated service to restart on failure.
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        internal void SetRestartOnFailure(string serviceName, bool restartOnFailure)
        {
            const uint delay = 1000;
            const int MAX_ACTIONS = 2;
            int SC_ACTION_SIZE = Marshal.SizeOf(typeof(SC_ACTION));

            // Open the service
            using (var failureActionsPtr = new AllocHLocalSafeHandle(Marshal.SizeOf(typeof(SERVICE_FAILURE_ACTIONS))))
            using (var actionPtr = new AllocHLocalSafeHandle(SC_ACTION_SIZE * MAX_ACTIONS))
            using (var service = OpenService(serviceName,
                ServiceAccessRights.SERVICE_CHANGE_CONFIG |
                ServiceAccessRights.SERVICE_START))
            {

                SERVICE_FAILURE_ACTIONS failureActions = new SERVICE_FAILURE_ACTIONS();
                int actionCount;

                if (restartOnFailure)
                {
                    actionCount = 2;

                    // Set up the restart action
                    SC_ACTION action1 = new SC_ACTION();
                    action1.Type = SC_ACTION_TYPE.SC_ACTION_RESTART;
                    action1.Delay = delay;
                    Marshal.StructureToPtr(action1, actionPtr.DangerousGetHandle(), false);

                    // Set up the "do nothing" action
                    SC_ACTION action2 = new SC_ACTION();
                    action2.Type = SC_ACTION_TYPE.SC_ACTION_NONE;
                    action2.Delay = delay;
                    Marshal.StructureToPtr(action2, (IntPtr)((Int64)actionPtr.DangerousGetHandle() + SC_ACTION_SIZE), false);

                    // Set up the failure actions
                    failureActions.dwResetPeriod = 0;
                    failureActions.cActions = (uint)actionCount;
                    failureActions.lpsaActions = actionPtr.DangerousGetHandle();
                    failureActions.lpRebootMsg = null;
                    failureActions.lpCommand = null;
                }
                else
                {
                    actionCount = 1;

                    // Set up the "do nothing" action
                    SC_ACTION action1 = new SC_ACTION();
                    action1.Type = SC_ACTION_TYPE.SC_ACTION_NONE;
                    action1.Delay = delay;
                    Marshal.StructureToPtr(action1, actionPtr.DangerousGetHandle(), false);

                    // Set up the failure actions
                    failureActions.dwResetPeriod = 0;
                    failureActions.cActions = (uint)actionCount;
                    failureActions.lpsaActions = actionPtr.DangerousGetHandle();
                }

                Marshal.StructureToPtr(failureActions, failureActionsPtr.DangerousGetHandle(), false);

                // Make the change
                int changeResult = NativeMethods.ChangeServiceConfig2(
                    service,
                    ServiceConfig2InfoLevel.SERVICE_CONFIG_FAILURE_ACTIONS,
                    failureActionsPtr.DangerousGetHandle());

                // Check that the change occurred
                if (changeResult == 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }
        
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        internal void SetStartupMode(string serviceName, ServiceStartMode mode)
        {
            using (var service = OpenService(serviceName,
                ServiceAccessRights.SERVICE_CHANGE_CONFIG |
                ServiceAccessRights.SERVICE_QUERY_CONFIG))
            {
                var result = NativeMethods.ChangeServiceConfig(
                    service,
                    SERVICE_NO_CHANGE,
                    (uint)mode,
                    SERVICE_NO_CHANGE,
                    null,
                    null,
                    IntPtr.Zero,
                    null,
                    null,
                    null,
                    null);

                if (result == false)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        internal void SetLoadOrderGroup(string serviceName, string group)
        {
            using (var service = OpenService(serviceName,
                ServiceAccessRights.SERVICE_CHANGE_CONFIG |
                ServiceAccessRights.SERVICE_QUERY_CONFIG))
            {
                var result = NativeMethods.ChangeServiceConfig(
                    service,
                    SERVICE_NO_CHANGE,
                    SERVICE_NO_CHANGE,
                    SERVICE_NO_CHANGE,
                    null,
                    group,
                    IntPtr.Zero,
                    null,
                    null,
                    null,
                    null);

                if (result == false)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        internal uint GetStartupMode(string serviceName)
        {
            // Open the service
            using (var service = OpenService(serviceName, ServiceAccessRights.SERVICE_QUERY_CONFIG))
            {
                uint structSize;
                var result = NativeMethods.QueryServiceConfig(service, IntPtr.Zero, 0, out structSize);

                using (var buff = new AllocHLocalSafeHandle((int)structSize))
                {
                    result = NativeMethods.QueryServiceConfig(service, buff.DangerousGetHandle(), structSize, out structSize);
                    if (result == false)
                        throw new Win32Exception(Marshal.GetLastWin32Error());

                    QUERY_SERVICE_CONFIG query_srv_config = (QUERY_SERVICE_CONFIG)Marshal.PtrToStructure(buff.DangerousGetHandle(), typeof(QUERY_SERVICE_CONFIG));
                    return query_srv_config.dwStartType;
                }
            }
        }

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        internal uint GetServicePid(string serviceName)
        {
            using (var service = OpenService(serviceName, ServiceAccessRights.SERVICE_QUERY_STATUS))
            {
                uint structSize;
                var result = NativeMethods.QueryServiceStatusEx(service, ServiceInfoLevel.SC_STATUS_PROCESS_INFO, IntPtr.Zero, 0, out structSize);

                using (var buff = new AllocHLocalSafeHandle((int)structSize))
                {
                    result = NativeMethods.QueryServiceStatusEx(service, ServiceInfoLevel.SC_STATUS_PROCESS_INFO, buff.DangerousGetHandle(), structSize, out structSize);
                    if (result == false)
                        throw new Win32Exception(Marshal.GetLastWin32Error());

                    SERVICE_STATUS_PROCESS query_srv_status = (SERVICE_STATUS_PROCESS)Marshal.PtrToStructure(buff.DangerousGetHandle(), typeof(SERVICE_STATUS_PROCESS));
                    return query_srv_status.dwProcessId;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Release managed resources

                SCManager.Dispose();
            }

            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.

            SCManager = null;
            disposed = true;
            base.Dispose(disposing);
        }
    }
}
