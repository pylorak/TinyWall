using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Security;
using System.Runtime.ConstrainedExecution;

namespace WFPdotNet
{
    public sealed class Engine : IDisposable
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class NativeMethods
        {
            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmEngineOpen0")]
            internal static extern uint FwpmEngineOpen0(
                [In, MarshalAs(UnmanagedType.LPWStr)] string serverName,
                [In] uint authnService,
                [In] IntPtr authIdentity,
                [In] AllocHGlobalSafeHandle session,
                [Out] out FwpmEngineSafeHandle engineHandle);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmEngineOpen0")]
            internal static extern uint FwpmEngineOpen0(
                [In, MarshalAs(UnmanagedType.LPWStr)] string serverName,
                [In] uint authnService,
                [In] IntPtr authIdentity,
                [In] ref Interop.FWPM_SESSION0 session,
                [Out] out FwpmEngineSafeHandle engineHandle);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmProviderAdd0")]
            internal static extern uint FwpmProviderAdd0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] ref Interop.FWPM_PROVIDER0 provider,
                [In] IntPtr sd);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmProviderDeleteByKey0")]
            internal static extern uint FwpmProviderDeleteByKey0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] ref Guid key);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmSubLayerAdd0")]
            internal static extern uint FwpmSubLayerAdd0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] ref Interop.FWPM_SUBLAYER0 subLayer,
                [In] IntPtr sd);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmSubLayerDeleteByKey0")]
            internal static extern uint FwpmSubLayerDeleteByKey0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] ref Guid key);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmFilterAdd0")]
            internal static extern uint FwpmFilterAdd0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] ref WFPdotNet.Interop.FWPM_FILTER0 filter,
                [In] IntPtr sd,
                [Out] out ulong id);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmFilterDeleteByKey0")]
            internal static extern uint FwpmFilterDeleteByKey0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] ref Guid key);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmFilterGetByKey0")]
            internal static extern uint FwpmFilterGetByKey0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] ref Guid key,
                [Out] out FwpmMemorySafeHandle filter);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmFilterGetById0")]
            internal static extern uint FwpmFilterGetById0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] ulong id,
                [Out] out FwpmMemorySafeHandle filter);
        }

        private readonly FwpmEngineSafeHandle _nativeEngineHandle;
        private readonly Guid _sessionKey;

        public FwpmEngineSafeHandle NativePtr
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                return _nativeEngineHandle;
            }
        }

        public Engine()
        {
            uint error = NativeMethods.FwpmEngineOpen0(null, (uint)Interop.RPC_C_AUTHN.RPC_C_AUTHN_WINNT, IntPtr.Zero, new AllocHGlobalSafeHandle(IntPtr.Zero, true), out _nativeEngineHandle);
            if (0 != error)
                throw new WfpException(error, "FwpmEngineOpen0");
        }

        public Engine(string displName, string displDescr, Interop.FWPM_SESSION_FLAGS flags, uint txnTimeoutMsec)
        {
            Interop.FWPM_SESSION0 session = new Interop.FWPM_SESSION0();
            _sessionKey = Guid.NewGuid();
            session.sessionKey = _sessionKey;
            session.displayData.name = displName;
            session.displayData.description = displDescr;
            session.flags = flags;
            session.txnWaitTimeoutInMSec = txnTimeoutMsec;

            uint error = NativeMethods.FwpmEngineOpen0(null, (uint)Interop.RPC_C_AUTHN.RPC_C_AUTHN_WINNT, IntPtr.Zero, ref session, out _nativeEngineHandle);
            if (0 != error)
                throw new WfpException(error, "FwpmEngineOpen0");
        }

        public Guid SessionKey
        {
            get { return _sessionKey; }
        }

        public SessionCollection GetSessions()
        {
            return new SessionCollection(this);
        }

        public ProviderCollection GetProviders()
        {
            return new ProviderCollection(this);
        }

        public SublayerCollection GetSublayers()
        {
            return new SublayerCollection(this);
        }

        public FilterCollection GetFilters(bool getFilterConditions)
        {
            return new FilterCollection(this, getFilterConditions);
        }

        public Guid RegisterProvider(ref Interop.FWPM_PROVIDER0 provider)
        {
            if (0 == provider.providerKey.CompareTo(Guid.Empty))
                provider.providerKey = Guid.NewGuid();

            uint error = NativeMethods.FwpmProviderAdd0(_nativeEngineHandle, ref provider, IntPtr.Zero);
            if (0 != error)
                throw new WfpException(error, "FwpmProviderAdd0");

            return provider.providerKey;
        }

        public Guid RegisterSublayer(ref Interop.FWPM_SUBLAYER0 sublayer)
        {
            if (0 == sublayer.subLayerKey.CompareTo(Guid.Empty))
                sublayer.subLayerKey = Guid.NewGuid();

            uint error = NativeMethods.FwpmSubLayerAdd0(_nativeEngineHandle, ref sublayer, IntPtr.Zero);
            if (0 != error)
                throw new WfpException(error, "FwpmProviderAdd0");

            return sublayer.subLayerKey;
        }

        public void RegisterFilter(Filter filter)
        {
            if (0 == filter.FilterKey.CompareTo(Guid.Empty))
                filter.FilterKey = Guid.NewGuid();

            ulong id;
            WFPdotNet.Interop.FWPM_FILTER0 nf = filter.Marshal();
            uint err = NativeMethods.FwpmFilterAdd0(_nativeEngineHandle, ref nf, IntPtr.Zero, out id);
            if (0 != err)
                throw new WfpException(err, "FwpmFilterAdd0");

            filter.FilterId = id;
        }

        public void UnregisterProvider(Guid providerKey)
        {
            uint error = NativeMethods.FwpmProviderDeleteByKey0(_nativeEngineHandle, ref providerKey);
            if (0 != error)
                throw new WfpException(error, "FwpmProviderDeleteByKey0");
        }

        public void UnregisterSublayer(Guid subLayerKey)
        {
            uint error = NativeMethods.FwpmSubLayerDeleteByKey0(_nativeEngineHandle, ref subLayerKey);
            if (0 != error)
                throw new WfpException(error, "FwpmProviderDeleteByKey0");
        }

        public void UnregisterFilter(Guid filterKey)
        {
            uint error = NativeMethods.FwpmFilterDeleteByKey0(_nativeEngineHandle, ref filterKey);
            if (0 != error)
                throw new WfpException(error, "FwpmFilterDeleteByKey0");
        }

        public Transaction BeginTransaction(bool readOnly = false)
        {
            return new Transaction(this, readOnly);
        }

        public FilterSubscription SubscribeFilterChange(FilterChangeCallback callback, object context)
        {
            return new FilterSubscription(this, callback, context);
        }

        public FilterSubscription SubscribeFilterChange(FilterChangeCallback callback, object context, Guid providerKey, Guid layerKey)
        {
            return new FilterSubscription(this, callback, context, providerKey, layerKey);
        }

        public NetEventSubscription SubscribeNetEvent(NetEventCallback callback, object context)
        {
            return new NetEventSubscription(this, callback, context);
        }

        public Filter GetFilter(Guid guid, bool getConditions)
        {
            FwpmMemorySafeHandle nativeMem = null;
            try
            {
                uint err = NativeMethods.FwpmFilterGetByKey0(this._nativeEngineHandle, ref guid, out nativeMem);
                if (err != 0)
                    throw new WfpException(err, "FwpmFilterGetByKey0");

                Interop.FWPM_FILTER0 nativeFilter = (Interop.FWPM_FILTER0)Marshal.PtrToStructure(nativeMem.DangerousGetHandle(), typeof(Interop.FWPM_FILTER0));
                return new Filter(nativeFilter, getConditions);
            }
            finally
            {
                nativeMem?.Dispose();
            }
        }

        public Filter GetFilter(ulong id, bool getConditions)
        {
            FwpmMemorySafeHandle nativeMem = null;
            try
            {
                uint err = NativeMethods.FwpmFilterGetById0(this._nativeEngineHandle, id, out nativeMem);
                if (err != 0)
                    throw new WfpException(err, "FwpmFilterGetById0");

                Interop.FWPM_FILTER0 nativeFilter = (Interop.FWPM_FILTER0)Marshal.PtrToStructure(nativeMem.DangerousGetHandle(), typeof(Interop.FWPM_FILTER0));
                return new Filter(nativeFilter, getConditions);
            }
            finally
            {
                nativeMem?.Dispose();
            }
        }

        public void Dispose()
        {
            _nativeEngineHandle.Dispose();
        }
    }
}
