using System;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace WFPdotNet
{
    public sealed class FwpmEngineSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class NativeMethods
        {
            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmEngineClose0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmEngineClose0(
                [In] IntPtr engineHandle);
        }

        public FwpmEngineSafeHandle()
            : base(true)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        protected override bool ReleaseHandle()
        {
            return (0 == NativeMethods.FwpmEngineClose0(handle));
        }
    }

    public sealed class FwpmMemorySafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class NativeMethods
        {
            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmFreeMemory0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern void FwpmFreeMemory0(
                [In] ref IntPtr p);
        }

        public FwpmMemorySafeHandle()
            : base(true)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [PrePrepareMethod]
        protected override bool ReleaseHandle()
        {
            NativeMethods.FwpmFreeMemory0(ref handle);
            return true;
        }
    }

    public sealed class FwpmFilterSubscriptionSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmFilterUnsubscribeChanges0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmFilterUnsubscribeChanges0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] IntPtr changeHandle);
        }

        private FwpmEngineSafeHandle _safeEngineHandle;
        private bool _releaseSafeEngineHandle;

        public FwpmFilterSubscriptionSafeHandle() : base(true) { }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        protected override bool ReleaseHandle()
        {
            uint ret = NativeMethods.FwpmFilterUnsubscribeChanges0(_safeEngineHandle, handle);
            if (_releaseSafeEngineHandle)
                _safeEngineHandle.DangerousRelease();
            return (0 == ret);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        public bool SetEngineReference(FwpmEngineSafeHandle safeEngineHandle)
        {
            _safeEngineHandle = safeEngineHandle;
            _safeEngineHandle.DangerousAddRef(ref _releaseSafeEngineHandle);
            return _releaseSafeEngineHandle;
        }
    }

    public sealed class FwpmNetEventSubscriptionSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmNetEventUnsubscribe0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmNetEventUnsubscribe0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] IntPtr changeHandle);
        }

        private FwpmEngineSafeHandle _safeEngineHandle;
        private bool _releaseSafeEngineHandle;

        public FwpmNetEventSubscriptionSafeHandle() : base(true) { }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        protected override bool ReleaseHandle()
        {
            uint ret = NativeMethods.FwpmNetEventUnsubscribe0(_safeEngineHandle, handle);
            if (_releaseSafeEngineHandle)
                _safeEngineHandle.DangerousRelease();
            return (0 == ret);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        public bool SetEngineReference(FwpmEngineSafeHandle safeEngineHandle)
        {
            _safeEngineHandle = safeEngineHandle;
            _safeEngineHandle.DangerousAddRef(ref _releaseSafeEngineHandle);
            return _releaseSafeEngineHandle;
        }
    }

    public sealed class FwpmFilterEnumSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmFilterDestroyEnumHandle0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmFilterDestroyEnumHandle0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] IntPtr enumHandle);
        }

        private FwpmEngineSafeHandle _safeEngineHandle;
        private bool _releaseSafeEngineHandle;

        public FwpmFilterEnumSafeHandle() : base(true) { }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        protected override bool ReleaseHandle()
        {
            uint ret = NativeMethods.FwpmFilterDestroyEnumHandle0(_safeEngineHandle, handle);
            if (_releaseSafeEngineHandle)
                _safeEngineHandle.DangerousRelease();
            return (0 == ret);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        public bool SetEngineReference(FwpmEngineSafeHandle safeEngineHandle)
        {
            _safeEngineHandle = safeEngineHandle;
            _safeEngineHandle.DangerousAddRef(ref _releaseSafeEngineHandle);
            return _releaseSafeEngineHandle;
        }
    }

    public sealed class FwpmProviderEnumSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmProviderDestroyEnumHandle0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmProviderDestroyEnumHandle0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] IntPtr enumHandle);
        }

        private FwpmEngineSafeHandle _safeEngineHandle;
        private bool _releaseSafeEngineHandle;

        public FwpmProviderEnumSafeHandle() : base(true) { }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        protected override bool ReleaseHandle()
        {
            uint ret = NativeMethods.FwpmProviderDestroyEnumHandle0(_safeEngineHandle, handle);
            if (_releaseSafeEngineHandle)
                _safeEngineHandle.DangerousRelease();
            return (0 == ret);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        public bool SetEngineReference(FwpmEngineSafeHandle safeEngineHandle)
        {
            _safeEngineHandle = safeEngineHandle;
            _safeEngineHandle.DangerousAddRef(ref _releaseSafeEngineHandle);
            return _releaseSafeEngineHandle;
        }
    }

    public sealed class FwpmSessionEnumSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmSessionDestroyEnumHandle0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmSessionDestroyEnumHandle0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] IntPtr enumHandle);
        }

        private FwpmEngineSafeHandle _safeEngineHandle;
        private bool _releaseSafeEngineHandle;

        public FwpmSessionEnumSafeHandle() : base(true) { }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        protected override bool ReleaseHandle()
        {
            uint ret = NativeMethods.FwpmSessionDestroyEnumHandle0(_safeEngineHandle, handle);
            if (_releaseSafeEngineHandle)
                _safeEngineHandle.DangerousRelease();
            return (0 == ret);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        public bool SetEngineReference(FwpmEngineSafeHandle safeEngineHandle)
        {
            _safeEngineHandle = safeEngineHandle;
            _safeEngineHandle.DangerousAddRef(ref _releaseSafeEngineHandle);
            return _releaseSafeEngineHandle;
        }
    }

    public sealed class FwpmSublayerEnumSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmSubLayerDestroyEnumHandle0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmSubLayerDestroyEnumHandle0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] IntPtr enumHandle);
        }

        private FwpmEngineSafeHandle _safeEngineHandle;
        private bool _releaseSafeEngineHandle;

        public FwpmSublayerEnumSafeHandle() : base(true) { }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        protected override bool ReleaseHandle()
        {
            uint ret = NativeMethods.FwpmSubLayerDestroyEnumHandle0(_safeEngineHandle, handle);
            if (_releaseSafeEngineHandle)
                _safeEngineHandle.DangerousRelease();
            return (0 == ret);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        public bool SetEngineReference(FwpmEngineSafeHandle safeEngineHandle)
        {
            _safeEngineHandle = safeEngineHandle;
            _safeEngineHandle.DangerousAddRef(ref _releaseSafeEngineHandle);
            return _releaseSafeEngineHandle;
        }
    }

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

        private Type MarshalDestroyType;

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

        public static SafeHGlobalHandle FromString(string str)
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
            Marshal.StructureToPtr(obj, this.handle + offset, NeedsMarshalDestroy);
            MarshalDestroyType = null;
        }

        public void MarshalFromManagedStruct<T>(T obj)
        {
            Marshal.StructureToPtr(obj, this.handle, NeedsMarshalDestroy);
            MarshalDestroyType = typeof(T);
        }

        public T ToStruct<T>() where T : unmanaged
        {
            return (T)Marshal.PtrToStructure(this.handle, typeof(T));
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

    public sealed class AllocHLocalSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("kernel32.dll")]
            internal static extern IntPtr LocalAlloc(uint uFlags, UIntPtr dwBytes);

            [DllImport("kernel32.dll")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern IntPtr LocalFree(IntPtr hMem);
        }

        public AllocHLocalSafeHandle(int nBytes)
            : base(true)
        {
            this.handle = NativeMethods.LocalAlloc(0, new UIntPtr((uint)nBytes));
        }

        public AllocHLocalSafeHandle(IntPtr ptr, bool ownsHandle)
            : base(ownsHandle)
        {
            this.handle = ptr;
        }

        public AllocHLocalSafeHandle()
            : base(true)
        {
            this.handle = IntPtr.Zero;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        protected override bool ReleaseHandle()
        {
            return (IntPtr.Zero == NativeMethods.LocalFree(handle));
        }
    }

    public sealed class SidSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("advapi32.dll", SetLastError = false)]
            internal static extern IntPtr FreeSid(IntPtr sid);
        }

        public SidSafeHandle(IntPtr ptr, bool ownsHandle)
            : base(ownsHandle)
        {
            this.handle = ptr;
        }

        public SidSafeHandle()
            : base(true)
        {
            this.handle = IntPtr.Zero;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        protected override bool ReleaseHandle()
        {
            return (IntPtr.Zero == NativeMethods.FreeSid(handle));
        }
    }
}
