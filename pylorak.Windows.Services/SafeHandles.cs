using System;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace pylorak.Windows.Services
{
    public sealed class SafeHGlobalHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("kernel32")]
            public static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

            [DllImport("kernel32")]
            public static extern IntPtr GlobalFree(IntPtr hMem);
        }

        private Type? MarshalDestroyType;

        private bool NeedsMarshalDestroy => (MarshalDestroyType != null);

        private static IntPtr AllocNativeMem(uint nBytes, bool zeroInit = false)
        {
            const uint GMEM_ZEROINIT = 0x0040;
            return NativeMethods.GlobalAlloc(zeroInit ? GMEM_ZEROINIT : 0, new UIntPtr(nBytes));
        }

        public static SafeHGlobalHandle Alloc(uint nBytes, bool zeroInit = false)
        {
            return new SafeHGlobalHandle(AllocNativeMem(nBytes, zeroInit));
        }

        public static SafeHGlobalHandle Alloc(int nBytes, bool zeroInit = false)
        {
            return Alloc((uint)nBytes, zeroInit);
        }

        public static SafeHGlobalHandle FromString(string? str)
        {
            return (str == null)
                ? new SafeHGlobalHandle()
                : new SafeHGlobalHandle(Marshal.StringToHGlobalUni(str));
        }

        public static SafeHGlobalHandle FromStruct<T>(T obj) where T : unmanaged
        {
            var size = Marshal.SizeOf(typeof(T));
            var ret = Alloc(size, true);
            ret.MarshalFromStruct(obj);
            return ret;
        }

        public static SafeHGlobalHandle FromManagedStruct<T>(T obj)
        {
            var size = Marshal.SizeOf(typeof(T));
            var ret = Alloc(size, true);
            ret.MarshalFromManagedStruct(obj);
            return ret;
        }

        public void MarshalFromStruct<T>(T obj, int offset = 0) where T : unmanaged
        {
            if (NeedsMarshalDestroy)
                Marshal.DestroyStructure(this.handle, MarshalDestroyType);

            int size = Marshal.SizeOf(typeof(T));
            unsafe
            {
                Buffer.MemoryCopy(&obj, (byte*)this.handle.ToPointer() + offset, size, size);
            }
            MarshalDestroyType = null;
        }

        public void MarshalFromManagedStruct<T>(T obj)
        {
            Marshal.StructureToPtr(obj, this.handle, NeedsMarshalDestroy);
            MarshalDestroyType = typeof(T);
        }

        public T ToStruct<T>() where T : unmanaged
        {
            return Marshal.PtrToStructure<T>(this.handle);
        }

        public void ForgetAndResize(uint newSize, bool zeroInit = false)
        {
            if (this.IsClosed)
                throw new InvalidOperationException("The SafeHandle is already closed.");

            var newHndl = AllocNativeMem(newSize, zeroInit);
            if (newHndl == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception();

            if (!this.IsInvalid)
            {
                if (NeedsMarshalDestroy)
                {
                    Marshal.DestroyStructure(this.handle, MarshalDestroyType);
                    MarshalDestroyType = null;
                }
                NativeMethods.GlobalFree(this.handle);
            }

            SetHandle(newHndl);
        }

        public SafeHGlobalHandle()
            : this(IntPtr.Zero)
        { }

        public SafeHGlobalHandle(IntPtr ptr)
            : base(true)
        {
            SetHandle(ptr);
        }

        protected override bool ReleaseHandle()
        {
            if (NeedsMarshalDestroy)
            {
                Marshal.DestroyStructure(this.handle, MarshalDestroyType);
                MarshalDestroyType = null;
            }
            bool ret = (IntPtr.Zero == NativeMethods.GlobalFree(handle));
            SetHandle(IntPtr.Zero);
            return ret;
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
