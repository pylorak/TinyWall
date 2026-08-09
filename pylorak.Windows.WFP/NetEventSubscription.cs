using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;

namespace pylorak.Windows.WFP
{
    public struct NetEventData
    {
        public Interop.FWPM_NET_EVENT_TYPE EventType;
        public ulong? filterId;

        public DateTime timeStamp;
        public Interop.NetEventHeaderValidField flags;
        public IpProtocol? ipProtocol;
        public ushort? localPort;
        public ushort? remotePort;
        public string? appId;
        public SecurityIdentifier? userId;
        public Interop.FwpmDirection? direction;
        public string? packageId;
        public byte[]? localAddr;
        public byte[]? remoteAddr;

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
                localAddr = nativeEvent.header.localAddr.ToByteArray(nativeEvent.header.ipVersion == Interop.FWP_IP_VERSION.FWP_IP_VERSION_V6);
            }
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_LOCAL_PORT_SET) != 0)
            {
                localPort = nativeEvent.header.localPort;
            }
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_REMOTE_ADDR_SET) != 0)
            {
                remoteAddr = nativeEvent.header.remoteAddr.ToByteArray(nativeEvent.header.ipVersion == Interop.FWP_IP_VERSION.FWP_IP_VERSION_V6);
            }
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_REMOTE_PORT_SET) != 0)
            {
                remotePort = nativeEvent.header.remotePort;
            }
#if false   // This works, but needs a lot of resources and is currently not needed. Enable when needed.
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_USER_ID_SET) != 0)
            {
                userId = PInvokeHelper.ConvertSidPtrToManaged(nativeEvent.header.userId);
            }
#endif

            if (nativeEvent.type == Interop.FWPM_NET_EVENT_TYPE.FWPM_NET_EVENT_TYPE_CLASSIFY_DROP)
            {
                Interop.FWPM_NET_EVENT_CLASSIFY_DROP1 classify = PInvokeHelper.PtrToStructure<Interop.FWPM_NET_EVENT_CLASSIFY_DROP1>(nativeEvent.EventInfo);
                filterId = classify.filterId;
                direction = classify.msFwpDirection;
            }
        }

        public NetEventData(Interop.FWPM_NET_EVENT2 nativeEvent) : this()
        {
            this.EventType = nativeEvent.type;
            this.timeStamp = nativeEvent.header.timeStamp.Local;
            this.flags = nativeEvent.header.flags;

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
                localAddr = nativeEvent.header.localAddr.ToByteArray(nativeEvent.header.ipVersion == Interop.FWP_IP_VERSION.FWP_IP_VERSION_V6);
            }
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_LOCAL_PORT_SET) != 0)
            {
                localPort = nativeEvent.header.localPort;
            }
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_REMOTE_ADDR_SET) != 0)
            {
                remoteAddr = nativeEvent.header.remoteAddr.ToByteArray(nativeEvent.header.ipVersion == Interop.FWP_IP_VERSION.FWP_IP_VERSION_V6);
            }
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_REMOTE_PORT_SET) != 0)
            {
                remotePort = nativeEvent.header.remotePort;
            }
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_PACKAGE_ID_SET) != 0)
            {
                string? sid = PInvokeHelper.ConvertSidToStringSid(nativeEvent.header.packageSid);
                if ((sid != null) && sid.StartsWith("S-1-15-2-"))
                    packageId = sid;
            }
#if false   // This works, but needs a lot of resources and is currently not needed. Enable when needed.
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_USER_ID_SET) != 0)
            {
                userId = PInvokeHelper.ConvertSidPtrToManaged(nativeEvent.header.userId);
            }
#endif

            if (nativeEvent.type == Interop.FWPM_NET_EVENT_TYPE.FWPM_NET_EVENT_TYPE_CLASSIFY_DROP)
            {
                Interop.FWPM_NET_EVENT_CLASSIFY_DROP1 classify = PInvokeHelper.PtrToStructure<Interop.FWPM_NET_EVENT_CLASSIFY_DROP1>(nativeEvent.EventInfo);
                filterId = classify.filterId;
                direction = classify.msFwpDirection;
            }
            if (nativeEvent.type == Interop.FWPM_NET_EVENT_TYPE.FWPM_NET_EVENT_TYPE_CLASSIFY_ALLOW)
            {
                Interop.FWPM_NET_EVENT_CLASSIFY_ALLOW0 classify = PInvokeHelper.PtrToStructure<Interop.FWPM_NET_EVENT_CLASSIFY_ALLOW0>(nativeEvent.EventInfo);
                filterId = classify.filterId;
                direction = classify.msFwpDirection;
            }
        }

    }

    public delegate void NetEventCallback(NetEventData data);

    public abstract class NetEventSubscription : IDisposable
    {
        protected readonly NetEventCallback _callback;

        public bool IsDisposed { get; private set; }

        protected NetEventSubscription(NetEventCallback callback)
        {
            _callback = callback;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                { }

                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public sealed class NetEventSubscription0 : NetEventSubscription
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class NativeMethods
        {
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate void FWPM_NET_EVENT_CALLBACK0(IntPtr context, IntPtr netEvent1);

            [DllImport("FWPUClnt.dll")]
            internal static extern uint FwpmNetEventSubscribe0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] ref Interop.FWPM_NET_EVENT_SUBSCRIPTION0 subscription,
                [In] FWPM_NET_EVENT_CALLBACK0 callback,
                IntPtr context,
                out IntPtr changeHandle);
        }

        private readonly FwpmNetEventSubscriptionSafeHandle _changeHandle;
        private readonly NativeMethods.FWPM_NET_EVENT_CALLBACK0 _nativeCallbackDelegate0;

        internal NetEventSubscription0(Engine engine, NetEventCallback callback) : base(callback)
        {
            _nativeCallbackDelegate0 = new NativeMethods.FWPM_NET_EVENT_CALLBACK0(NativeCallbackHandler0);

            var subs0 = new Interop.FWPM_NET_EVENT_SUBSCRIPTION0
            {
                sessionKey = engine.SessionKey,
                enumTemplate = IntPtr.Zero
            };

            var err = NativeMethods.FwpmNetEventSubscribe0(engine.NativePtr, ref subs0, _nativeCallbackDelegate0, IntPtr.Zero, out IntPtr outHndl);
            if (0 == err)
                _changeHandle = new FwpmNetEventSubscriptionSafeHandle(outHndl, engine.NativePtr);
            else
                throw new WfpException(err, "FwpmNetEventSubscribe0");
        }

        private void NativeCallbackHandler0(IntPtr context, IntPtr netEvent1)
        {
            Interop.FWPM_NET_EVENT1 ev = PInvokeHelper.PtrToStructure<Interop.FWPM_NET_EVENT1>(netEvent1);
            _callback(new NetEventData(ev));
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _changeHandle.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }

    public sealed class NetEventSubscription1 : NetEventSubscription
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class NativeMethods
        {
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate void FWPM_NET_EVENT_CALLBACK1(IntPtr context, IntPtr netEvent2);

            [DllImport("FWPUClnt.dll")]
            internal static extern uint FwpmNetEventSubscribe1(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] ref Interop.FWPM_NET_EVENT_SUBSCRIPTION0 subscription,
                [In] FWPM_NET_EVENT_CALLBACK1 callback,
                IntPtr context,
                out IntPtr changeHandle);

        }

        private readonly FwpmNetEventSubscriptionSafeHandle _changeHandle;
        private readonly NativeMethods.FWPM_NET_EVENT_CALLBACK1 _nativeCallbackDelegate1;

        internal NetEventSubscription1(Engine engine, NetEventCallback callback) : base(callback)
        {
            _nativeCallbackDelegate1 = new NativeMethods.FWPM_NET_EVENT_CALLBACK1(NativeCallbackHandler1);

            var subs0 = new Interop.FWPM_NET_EVENT_SUBSCRIPTION0
            {
                sessionKey = engine.SessionKey,
                enumTemplate = IntPtr.Zero
            };

            var err = NativeMethods.FwpmNetEventSubscribe1(engine.NativePtr, ref subs0, _nativeCallbackDelegate1, IntPtr.Zero, out IntPtr outHndl);
            if (0 == err)
                _changeHandle = new FwpmNetEventSubscriptionSafeHandle(outHndl, engine.NativePtr);
            else
                throw new WfpException(err, "FwpmNetEventSubscribe1");
        }

        private void NativeCallbackHandler1(IntPtr context, IntPtr netEvent1)
        {
            var ev = PInvokeHelper.PtrToStructure<Interop.FWPM_NET_EVENT2>(netEvent1);
            _callback(new NetEventData(ev));
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _changeHandle.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }

}
