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

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmEngineSetOption0")]
            internal static extern uint FwpmEngineSetOption0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] Interop.FWPM_ENGINE_OPTION option,
                [In] ref Interop.FWP_VALUE0 newValue);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmEngineGetOption0")]
            internal static extern uint FwpmEngineGetOption0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] Interop.FWPM_ENGINE_OPTION option,
                [Out] out FwpmMemorySafeHandle value);

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
            SessionKey = Guid.NewGuid();
            session.sessionKey = SessionKey;
            session.displayData.name = displName;
            session.displayData.description = displDescr;
            session.flags = flags;
            session.txnWaitTimeoutInMSec = txnTimeoutMsec;

            uint error = NativeMethods.FwpmEngineOpen0(null, (uint)Interop.RPC_C_AUTHN.RPC_C_AUTHN_WINNT, IntPtr.Zero, ref session, out _nativeEngineHandle);
            if (0 != error)
                throw new WfpException(error, "FwpmEngineOpen0");
        }

        private uint EngineOptionGetValue(Interop.FWPM_ENGINE_OPTION opt)
        {
            uint err = NativeMethods.FwpmEngineGetOption0(_nativeEngineHandle, opt, out FwpmMemorySafeHandle nativeMem);
            if (0 != err)
                throw new WfpException(err, "FwpmEngineGetOption0");

            try
            {
                Interop.FWP_VALUE0 val = (Interop.FWP_VALUE0)Marshal.PtrToStructure(nativeMem.DangerousGetHandle(), typeof(Interop.FWP_VALUE0));
                System.Diagnostics.Debug.Assert(val.type == Interop.FWP_DATA_TYPE.FWP_UINT32);
                return val.uint32;
            }
            finally
            {
                nativeMem.Dispose();
            }
        }

        private void EngineOptionSetValue(Interop.FWPM_ENGINE_OPTION opt, uint val)
        {
            Interop.FWP_VALUE0 vs = new Interop.FWP_VALUE0();
            vs.type = Interop.FWP_DATA_TYPE.FWP_UINT32;
            vs.uint32 = val;

            uint err = NativeMethods.FwpmEngineSetOption0(_nativeEngineHandle, opt, ref vs);
            if (0 != err)
                throw new WfpException(err, "FwpmEngineSetOption0");
        }

        public bool CollectNetEvents
        {
            get
            {
                uint val = EngineOptionGetValue(Interop.FWPM_ENGINE_OPTION.FWPM_ENGINE_COLLECT_NET_EVENTS);
                return (val != 0);
            }
            set
            {
                EngineOptionSetValue(Interop.FWPM_ENGINE_OPTION.FWPM_ENGINE_COLLECT_NET_EVENTS, value ? 1u : 0u);
            }
        }

        public Interop.InboundEventMatchKeyword EventMatchAnyKeywords
        {
            get
            {
                return (Interop.InboundEventMatchKeyword)EngineOptionGetValue(Interop.FWPM_ENGINE_OPTION.FWPM_ENGINE_NET_EVENT_MATCH_ANY_KEYWORDS);
            }
            set
            {
                EngineOptionSetValue(Interop.FWPM_ENGINE_OPTION.FWPM_ENGINE_NET_EVENT_MATCH_ANY_KEYWORDS, (uint)value);
            }
        }

        public int TxnWatchdogTimeoutMsec
        {
            get
            {
                return (int)EngineOptionGetValue(Interop.FWPM_ENGINE_OPTION.FWPM_ENGINE_TXN_WATCHDOG_TIMEOUT_IN_MSEC);
            }
            set
            {
                EngineOptionSetValue(Interop.FWPM_ENGINE_OPTION.FWPM_ENGINE_TXN_WATCHDOG_TIMEOUT_IN_MSEC, (uint)value);
            }
        }

        public Guid SessionKey
        {
            get; private set;
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
            if (Guid.Empty == provider.providerKey)
                provider.providerKey = Guid.NewGuid();

            uint error = NativeMethods.FwpmProviderAdd0(_nativeEngineHandle, ref provider, IntPtr.Zero);
            if (0 != error)
                throw new WfpException(error, "FwpmProviderAdd0");

            return provider.providerKey;
        }

        public Guid RegisterSublayer(Sublayer sublayer)
        {
            if (Guid.Empty == sublayer.SublayerKey)
                sublayer.SublayerKey = Guid.NewGuid();

            var nativeStruct = sublayer.Marshal();

            uint error = NativeMethods.FwpmSubLayerAdd0(_nativeEngineHandle, ref nativeStruct, IntPtr.Zero);
            if (0 != error)
                throw new WfpException(error, "FwpmProviderAdd0");

            return sublayer.SublayerKey;
        }

        public void RegisterFilter(Filter filter)
        {
            if (Guid.Empty == filter.FilterKey)
                filter.FilterKey = Guid.NewGuid();

            Interop.FWPM_FILTER0 nf = filter.Marshal();
            uint err = NativeMethods.FwpmFilterAdd0(_nativeEngineHandle, ref nf, IntPtr.Zero, out ulong id);
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
            if (VersionInfo.Win8OrNewer)
                return new NetEventSubscription1(this, callback, context);
            else
                return new NetEventSubscription0(this, callback, context);
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
