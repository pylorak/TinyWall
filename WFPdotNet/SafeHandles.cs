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

    public sealed class AllocHGlobalSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("kernel32.dll")]
            internal static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

            [DllImport("kernel32.dll")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern IntPtr GlobalFree(IntPtr hMem);
        }

        public AllocHGlobalSafeHandle(int nBytes)
            : base(true)
        {
            this.handle = NativeMethods.GlobalAlloc(0, new UIntPtr((uint)nBytes));
        }

        public AllocHGlobalSafeHandle(IntPtr ptr, bool ownsHandle)
            : base(ownsHandle)
        {
            this.handle = ptr;
        }

        public AllocHGlobalSafeHandle()
            : base(true)
        {
            this.handle = IntPtr.Zero;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        protected override bool ReleaseHandle()
        {
            return (IntPtr.Zero == NativeMethods.GlobalFree(handle));
        }
    }
}
