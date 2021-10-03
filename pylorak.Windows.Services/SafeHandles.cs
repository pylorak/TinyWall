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
            public enum PowerNotifFlags
            {
                DEVICE_NOTIFY_WINDOW_HANDLE = 0,
                DEVICE_NOTIFY_SERVICE_HANDLE = 1
            }

            [DllImport("user32", SetLastError = true)]
            public static extern SafeHandlePowerSettingNotification RegisterPowerSettingNotification(
                IntPtr hRecipient,
                ref Guid PowerSettingGuid,
                PowerNotifFlags Flags);

            [DllImport("user32")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            public static extern bool UnregisterPowerSettingNotification(IntPtr hPowerNotif);
        }

        public static SafeHandlePowerSettingNotification CreateForService(IntPtr service, Guid powerSetting)
        {
            return NativeMethods.RegisterPowerSettingNotification(service, ref powerSetting, NativeMethods.PowerNotifFlags.DEVICE_NOTIFY_SERVICE_HANDLE);
        }

        public static SafeHandlePowerSettingNotification CreateForWindow(IntPtr hwnd, Guid powerSetting)
        {
            return NativeMethods.RegisterPowerSettingNotification(hwnd, ref powerSetting, NativeMethods.PowerNotifFlags.DEVICE_NOTIFY_WINDOW_HANDLE);
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
}
