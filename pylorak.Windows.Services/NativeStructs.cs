using System;
using System.Runtime.InteropServices;

namespace pylorak.Windows.Services
{
    internal delegate int ServiceCtrlHandlerExDelegate(int command, int eventType, IntPtr eventData, IntPtr eventContext);

    public enum ServiceControlCommand
    {
        SERVICE_CONTROL_STOP = 0x00000001,
        SERVICE_CONTROL_PAUSE = 0x00000002,
        SERVICE_CONTROL_CONTINUE = 0x00000003,
        SERVICE_CONTROL_INTERROGATE = 0x00000004,
        SERVICE_CONTROL_SHUTDOWN = 0x00000005,
        SERVICE_CONTROL_PARAMCHANGE = 0x00000006,
        SERVICE_CONTROL_NETBINDADD = 0x00000007,
        SERVICE_CONTROL_NETBINDREMOVE = 0x00000008,
        SERVICE_CONTROL_NETBINDENABLE = 0x00000009,
        SERVICE_CONTROL_NETBINDDISABLE = 0x0000000A,
        SERVICE_CONTROL_DEVICEEVENT = 0x0000000B,
        SERVICE_CONTROL_HARDWAREPROFILECHANGE = 0x0000000C,
        SERVICE_CONTROL_POWEREVENT = 0x0000000D,
        SERVICE_CONTROL_SESSIONCHANGE = 0x0000000E,
        SERVICE_CONTROL_PRESHUTDOWN = 0x0000000F,
    }

    [Flags]
    public enum ServiceAcceptedControl
    {
        None = 0,
        SERVICE_ACCEPT_NETBINDCHANGE = 0x00000010,
        SERVICE_ACCEPT_PARAMCHANGE = 0x00000008,
        SERVICE_ACCEPT_PAUSE_CONTINUE = 0x00000002,
        SERVICE_ACCEPT_PRESHUTDOWN = 0x00000100,
        SERVICE_ACCEPT_SHUTDOWN = 0x00000004,
        SERVICE_ACCEPT_STOP = 0x00000001,
        SERVICE_ACCEPT_HARDWAREPROFILECHANGE = 0x00000020,
        SERVICE_ACCEPT_POWEREVENT = 0x00000040,
        SERVICE_ACCEPT_SESSIONCHANGE = 0x00000080,
        SERVICE_ACCEPT_TIMECHANGE = 0x00000200,
        SERVICE_ACCEPT_TRIGGEREVENT = 0x00000400,
        SERVICE_ACCEPT_USERMODEREBOOT = 0x00000800
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SERVICE_STATUS
    {
        public ServiceType serviceType;
        public ServiceState currentState;
        public ServiceAcceptedControl controlsAccepted;
        public int win32ExitCode;
        public int serviceSpecificExitCode;
        public int checkPoint;
        public int waitHint;
    }

    [Flags]
    public enum ServiceType
    {
        Invalid = 0,
        SERVICE_TYPE_ADAPTER = 0x00000004,
        SERVICE_TYPE_FILE_SYSTEM_DRIVER = 0x00000002,
        SERVICE_TYPE_INTERACTIVE_PROCESS = 0x00000100,
        SERVICE_TYPE_KERNEL_DRIVER = 0x00000001,
        SERVICE_TYPE_RECOGNIZER_DRIVER = 0x00000008,
        SERVICE_TYPE_WIN32_OWN_PROCESS = 0x00000010,
        SERVICE_TYPE_WIN32_SHARE_PROCESS = 0x00000020,
    }

    public enum ServiceState
    {
        Invalid = 0,
        StartPending = 0x00000002,
        ContinuePending = 0x00000005,
        Running = 0x00000004,
        PausePending = 0x00000006,
        Paused = 0x00000007,
        StopPending = 0x00000003,
        Stopped = 0x00000001
    }

    [Flags]
    public enum ServiceControlAccessRights : int
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
    public enum ServiceAccessRights : int
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

    public enum ServiceConfig2InfoLevel : int
    {
        SERVICE_CONFIG_DESCRIPTION = 0x00000001, // The lpBuffer parameter is a pointer to a SERVICE_DESCRIPTION structure.
        SERVICE_CONFIG_FAILURE_ACTIONS = 0x00000002 // The lpBuffer parameter is a pointer to a SERVICE_FAILURE_ACTIONS structure.
    }

    public enum SC_ACTION_TYPE : uint
    {
        SC_ACTION_NONE = 0x00000000, // No action.
        SC_ACTION_RESTART = 0x00000001, // Restart the service.
        SC_ACTION_REBOOT = 0x00000002, // Reboot the computer.
        SC_ACTION_RUN_COMMAND = 0x00000003 // Run a command.
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct QUERY_SERVICE_CONFIG
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
    public sealed class SERVICE_STATUS_PROCESS
    {
        internal ServiceType dwServiceType;
        internal ServiceState dwCurrentState;
        internal ServiceAcceptedControl dwControlsAccepted;
        internal uint dwWin32ExitCode;
        internal uint dwServiceSpecificExitCode;
        internal uint dwCheckPoint;
        internal uint dwWaitHint;
        internal uint dwProcessId;
        internal uint dwServiceFlags;
    }

    public struct SERVICE_FAILURE_ACTIONS
    {
        internal uint dwResetPeriod;
        [MarshalAs(UnmanagedType.LPStr)]
        internal string? lpRebootMsg;
        [MarshalAs(UnmanagedType.LPStr)]
        internal string? lpCommand;
        internal uint cActions;
        internal IntPtr lpsaActions;
    }

    public struct SC_ACTION
    {
        internal SC_ACTION_TYPE Type;
        internal uint Delay;
    }

    [Flags]
    public enum DeviceNotifFlags
    {
        DEVICE_NOTIFY_WINDOW_HANDLE = 0,
        DEVICE_NOTIFY_SERVICE_HANDLE = 1,
        DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 4
    }

    public enum ServiceInfoLevel : int
    {
        SC_STATUS_PROCESS_INFO = 0
    }

    public enum PowerEventType
    {
        PowerStatusChange = 10,
        ResumeAutomatic = 18,
        ResumeUser = 7,
        Suspend = 4,
        PowerSettingChange = 32787
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct POWERBROADCAST_SETTING_NODATA
    {
        public Guid PowerSetting;
        public int DataLength;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct POWERBROADCAST_SETTING_DWORD
    {
        public Guid PowerSetting;
        public int DataLength;
        public int Data;
    }

    public enum DeviceBroadcastHdrDevType
    {
        DBT_DEVTYP_DEVICEINTERFACE = 5,
        DBT_DEVTYP_HANDLE = 6,
        DBT_DEVTYP_OEM = 0,
        DBT_DEVTYP_PORT = 3,
        DBT_DEVTYP_VOLUME =2
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DEV_BROADCAST_HDR
    {
        public int Size;
        public DeviceBroadcastHdrDevType DeviceType;
        public int Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DEV_BROADCAST_DEVICEINTERFACE_Filter
    {
        public int Size;
        public DeviceBroadcastHdrDevType DeviceType;
        public int Reserved;
        public Guid ClassGuid;
        public short Name;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct DEV_BROADCAST_DEVICEINTERFACE
    {
        public int Size;
        public DeviceBroadcastHdrDevType DeviceType;
        public int Reserved;
        public Guid ClassGuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
        public string Name;
    }

    [Flags]
    public enum DevBroadcastVolumeFlags : short
    {
        DBTF_MEDIA = 1,
        DBTF_NET = 2
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct DEV_BROADCAST_VOLUME
    {
        public int Size;
        public DeviceBroadcastHdrDevType DeviceType;
        public int Reserved;
        public uint UnitMask;
        public DevBroadcastVolumeFlags Flags; 
    }

    public enum DeviceEventType
    {
        DeviceArrival = 0x8000,
        DeviceRemoveComplete = 0x8004,
        DeviceQueryRemove = 0x8001,
        DeviceQueryRemoveFailed = 0x8002,
        DeviceRemovePending = 0x8003,
        DeviceTypeSpecific = 0x8005,
        CustomEvent = 0x8006,
    }
}
