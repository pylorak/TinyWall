using System;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace pylorak.Windows.Services
{
    public sealed class SafeServiceHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseServiceHandle(IntPtr hSCObject);
        }

        public SafeServiceHandle()
            : this(IntPtr.Zero)
        { }

        public SafeServiceHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        override protected bool ReleaseHandle()
        {
            return NativeMethods.CloseServiceHandle(handle);
        }
    }

    public sealed class SafeHandlePowerSettingNotification : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("user32", SetLastError = true)]
            public static extern SafeHandlePowerSettingNotification RegisterPowerSettingNotification(
                IntPtr hRecipient,
                ref Guid PowerSettingGuid,
                DeviceNotifFlags Flags);

            [DllImport("user32")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            public static extern bool UnregisterPowerSettingNotification(IntPtr hPowerNotif);
        }

        public static SafeHandlePowerSettingNotification Create(IntPtr service, Guid powerSetting, DeviceNotifFlags flags)
        {
            return NativeMethods.RegisterPowerSettingNotification(service, ref powerSetting, flags);
        }

        public SafeHandlePowerSettingNotification()
            : this(IntPtr.Zero)
        { }

        public SafeHandlePowerSettingNotification(IntPtr ptr)
            : base(true)
        {
            SetHandle(ptr);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            return NativeMethods.UnregisterPowerSettingNotification(handle);
        }
    }

    public sealed class SafeHandleDeviceNotification : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("user32", SetLastError = true)]
            public static extern SafeHandleDeviceNotification RegisterDeviceNotification(
                IntPtr hRecipient,
                IntPtr NotificationFilter,
                DeviceNotifFlags Flags);

            [DllImport("user32")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            public static extern bool UnregisterDeviceNotification(IntPtr hDeviceNotif);
        }

        public static SafeHandleDeviceNotification Create(IntPtr recipient, Guid devIfaceClsGuid, DeviceNotifFlags flags)
        {
            var filter = new DEV_BROADCAST_DEVICEINTERFACE_Filter();
            filter.Size = Marshal.SizeOf<DEV_BROADCAST_DEVICEINTERFACE_Filter>();
            filter.DeviceType = DeviceBroadcastHdrDevType.DBT_DEVTYP_DEVICEINTERFACE;
            filter.ClassGuid = devIfaceClsGuid;
            filter.Name = 0;
            filter.Reserved = 0;
            using var filter_hndl = SafeHGlobalHandle.FromStruct(filter);

            return NativeMethods.RegisterDeviceNotification(recipient, filter_hndl.DangerousGetHandle(), flags);
        }

        public SafeHandleDeviceNotification()
            : this(IntPtr.Zero)
        { }

        public SafeHandleDeviceNotification(IntPtr ptr)
            : base(true)
        {
            SetHandle(ptr);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            return NativeMethods.UnregisterDeviceNotification(handle);
        }
    }
}
