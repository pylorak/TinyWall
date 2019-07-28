using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Security.Principal;

namespace WFPdotNet
{
    public struct NetEventData
    {
        public Interop.FWPM_NET_EVENT_TYPE EventType;
        public ulong filterId;

        public DateTime timeStamp;
        public Interop.NetEventHeaderValidField flags;
        public IpProtocol ipProtocol;
        public IPAddress localAddr;
        public IPAddress remoteAddr;
        public ushort localPort;
        public ushort remotePort;
        public string appId;
        public SecurityIdentifier userId;

        public NetEventData(Interop.FWPM_NET_EVENT1 nativeEvent) : this()
        {
            this.EventType = nativeEvent.type;
            this.timeStamp = nativeEvent.header.timeStamp.Local;
            this.flags = nativeEvent.header.flag;

            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_APP_ID_SET) != 0)
            {
                appId = Marshal.PtrToStringAuto(nativeEvent.header.appId.data);
            }
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_IP_PROTOCOL_SET) != 0)
            {
                ipProtocol = (IpProtocol)nativeEvent.header.ipProtocol;
            }
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_LOCAL_ADDR_SET) != 0)
            {
                if (nativeEvent.header.ipVersion == Interop.FWP_IP_VERSION.FWP_IP_VERSION_V4)
                    localAddr = nativeEvent.header.localAddr.ToIpV4();
                else if (nativeEvent.header.ipVersion == Interop.FWP_IP_VERSION.FWP_IP_VERSION_V6)
                    localAddr = nativeEvent.header.localAddr.ToIpV6();
            }
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_LOCAL_PORT_SET) != 0)
            {
                localPort = nativeEvent.header.localPort;
            }
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_REMOTE_ADDR_SET) != 0)
            {
                if (nativeEvent.header.ipVersion == Interop.FWP_IP_VERSION.FWP_IP_VERSION_V4)
                    remoteAddr = nativeEvent.header.remoteAddr.ToIpV4();
                else if (nativeEvent.header.ipVersion == Interop.FWP_IP_VERSION.FWP_IP_VERSION_V6)
                    remoteAddr = nativeEvent.header.remoteAddr.ToIpV6();
            }
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_REMOTE_PORT_SET) != 0)
            {
                remotePort = nativeEvent.header.remotePort;
            }
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_USER_ID_SET) != 0)
            {
                userId = PInvokeHelper.ConvertSidPtrToManaged(nativeEvent.header.userId);
            }

            if (nativeEvent.type == Interop.FWPM_NET_EVENT_TYPE.FWPM_NET_EVENT_TYPE_CLASSIFY_DROP)
            {
                Interop.FWPM_NET_EVENT_CLASSIFY_DROP1 classify = (Interop.FWPM_NET_EVENT_CLASSIFY_DROP1)Marshal.PtrToStructure(nativeEvent.EventInfo, typeof(Interop.FWPM_NET_EVENT_CLASSIFY_DROP1));
                filterId = classify.filterId;
            }
        }
    }

    public delegate void NetEventCallback(object context, NetEventData data);

    public sealed class NetEventSubscription : IDisposable
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class NativeMethods
        {
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate void FWPM_NET_EVENT_CALLBACK0(IntPtr context, IntPtr netEvent);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmNetEventSubscribe0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmNetEventSubscribe0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] ref Interop.FWPM_NET_EVENT_SUBSCRIPTION0 subscription,
                [In] FWPM_NET_EVENT_CALLBACK0 callback,
                [In] IntPtr context,
                [Out] out FwpmNetEventSubscriptionSafeHandle changeHandle);
        }


        private readonly FwpmNetEventSubscriptionSafeHandle _changeHandle;
        private readonly NetEventCallback _callback;
        private readonly object _context;
        private readonly NativeMethods.FWPM_NET_EVENT_CALLBACK0 _nativeCallbackDelegate;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "dummy")]
        internal NetEventSubscription(Engine engine, NetEventCallback callback, object context)
        {
            _callback = callback;
            _context = context;
            _nativeCallbackDelegate = new NativeMethods.FWPM_NET_EVENT_CALLBACK0(NativeCallbackHandler);
            AllocHGlobalSafeHandle templMemHandle = null;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Interop.FWPM_NET_EVENT_SUBSCRIPTION0 subs0 = new Interop.FWPM_NET_EVENT_SUBSCRIPTION0();
                subs0.sessionKey = engine.SessionKey;
                subs0.enumTemplate = IntPtr.Zero;

                if (false)
                {
                    Interop.FWPM_NET_EVENT_ENUM_TEMPLATE0 templ0 = new Interop.FWPM_NET_EVENT_ENUM_TEMPLATE0();
                    templ0.startTime.Local = DateTime.MinValue;
                    templ0.startTime.Local = DateTime.MaxValue;
                    templ0.numFilterConditions = 0;
                    templ0.filterCondition = IntPtr.Zero;

                    templMemHandle = PInvokeHelper.StructToHGlobal<Interop.FWPM_NET_EVENT_ENUM_TEMPLATE0>(templ0);
                    subs0.enumTemplate = templMemHandle.DangerousGetHandle();
                }

                uint err;
                bool handleOk = false;

                // Atomically get the native handle
                RuntimeHelpers.PrepareConstrainedRegions();
                try { }
                finally
                {
                    err = NativeMethods.FwpmNetEventSubscribe0(engine.NativePtr, ref subs0, _nativeCallbackDelegate, IntPtr.Zero, out _changeHandle);
                    if (0 == err)
                        handleOk = _changeHandle.SetEngineReference(engine.NativePtr);
                }

                // Do error handling after the CER
                if (!handleOk)
                    throw new Exception("Failed to set handle value.");
                if (0 != err)
                    throw new WfpException(err, "FwpmFilterSubscribeChanges0");
            }
            finally
            {
                templMemHandle?.Dispose();
            }
        }

        private void NativeCallbackHandler(IntPtr context, IntPtr netEvent)
        {
            Interop.FWPM_NET_EVENT1 ev = (Interop.FWPM_NET_EVENT1)Marshal.PtrToStructure(netEvent, typeof(Interop.FWPM_NET_EVENT1));
            _callback(_context, new NetEventData(ev));
        }

        public void Dispose()
        {
            _changeHandle.Dispose();
        }
    }
}
