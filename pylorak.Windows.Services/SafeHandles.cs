using System;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace pylorak.Windows.Services
{
    public sealed class SafeHandleAllocHGlobal : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("kernel32.dll")]
            public static extern SafeHandleAllocHGlobal GlobalAlloc(uint uFlags, UIntPtr dwBytes);

            [DllImport("kernel32.dll")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            public static extern IntPtr GlobalFree(IntPtr hMem);
        }

        public static SafeHandleAllocHGlobal Alloc(uint nBytes, bool zeroInit = false)
        {
            const uint GMEM_ZEROINIT = 0x0040;
            return NativeMethods.GlobalAlloc(zeroInit ? GMEM_ZEROINIT : 0, new UIntPtr(nBytes));
        }
        public static SafeHandleAllocHGlobal Alloc(int nBytes, bool zeroInit = false)
        {
            return Alloc((uint)nBytes, zeroInit);
        }
        public static SafeHandleAllocHGlobal FromString(string? str)
        {
            return (str == null)
                ? new SafeHandleAllocHGlobal()
                : new SafeHandleAllocHGlobal(Marshal.StringToHGlobalUni(str));
        }

        public static SafeHandleAllocHGlobal FromStruct<T>(T obj)
        {
            var size = Marshal.SizeOf(typeof(T));
            var ret = Alloc(size, true);
            Marshal.StructureToPtr(obj, ret.DangerousGetHandle(), false);
            return ret;
        }

        public SafeHandleAllocHGlobal()
            : this(IntPtr.Zero)
        { }

        public SafeHandleAllocHGlobal(IntPtr ptr)
            : base(true)
        {
            SetHandle(ptr);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            return (IntPtr.Zero == NativeMethods.GlobalFree(handle));
        }
    }

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
            filter.Size = Marshal.SizeOf(typeof(DEV_BROADCAST_DEVICEINTERFACE_Filter));
            filter.DeviceType = DeviceBroadcastHdrDevType.DBT_DEVTYP_DEVICEINTERFACE;
            filter.ClassGuid = devIfaceClsGuid;
            filter.Name = 0;
            filter.Reserved = 0;
            using var filter_hndl = SafeHandleAllocHGlobal.FromStruct(filter);

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
