using System;
using System.Text;
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
        public ulong? filterId;

        public DateTime timeStamp;
        public Interop.NetEventHeaderValidField flags;
        public IpProtocol? ipProtocol;
        public ushort? localPort;
        public ushort? remotePort;
        public string appId;
        public SecurityIdentifier userId;
        public Interop.FwpmDirection? direction;

#if true
        public string localAddr;
        public string remoteAddr;
        private static string ToIpAddress(Interop.InternetworkAddr addr, bool isIpV6, StringBuilder sb)
        {
            sb.Length = 0;
            if (isIpV6)
            {
                unsafe
                {
                    for (int i = 15; i >= 0; i -= 2)
                    {
                        byte b1 = addr.AddrV6[i];
                        byte b0 = addr.AddrV6[i - 1];
                        if (
                            (b1 == 0)
                            && (b0 == 0)
                            )
                        {
                            sb.Append('0');
                        }
                        else
                        {
                            ToHex(b1, sb);
                            ToHex(b0, sb);
                        }
                        if (i > 2)
                            sb.Append(':');
                    }
                }
            }
            else
            {
                unsafe
                {
                    byte* b = (byte*)&addr.AddrV4;

                    ToStringBuilder(b[3], sb);
                    sb.Append('.');
                    ToStringBuilder(b[2], sb);
                    sb.Append('.');
                    ToStringBuilder(b[1], sb);
                    sb.Append('.');
                    ToStringBuilder(b[0], sb);
                }
            }

            return sb.ToString();
        }

        private static void ToHex(byte b, StringBuilder sb)
        {
            var n = (byte)(b >> 4);
            sb.Append((char)(n > 9 ? n - 10 + 'a' : n + '0'));
            n = (byte)(b & 0x0F);
            sb.Append((char)(n > 9 ? n - 10 + 'a' : n + '0'));
        }

        private const string IntChars = "0123456789";
        private static void ToStringBuilder(uint a, StringBuilder sb)
        {
            if (a == 0)
            {
                sb.Append('0');
            }
            else
            {
                unsafe
                {
                    char* rev = stackalloc char[16];
                    int i = 15;
                    while (a > 0)
                    {
                        rev[i] = IntChars[(int)(a % 10)];
                        --i;
                        a /= 10;
                    }
                    ++i;
                    for (; i < 16; ++i)
                        sb.Append(rev[i]);
                }
            }
        }
#else
        public IPAddress localAddr;
        public IPAddress remoteAddr;
        private static IPAddress ToIpAddress(Interop.InternetworkAddr addr, bool isIpV6)
        {
            return isIpV6 ? addr.ToIpV6() : addr.ToIpV4();
        }
#endif

        public NetEventData(Interop.FWPM_NET_EVENT1 nativeEvent, StringBuilder sb) : this()
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
                localAddr = ToIpAddress(nativeEvent.header.localAddr, nativeEvent.header.ipVersion == Interop.FWP_IP_VERSION.FWP_IP_VERSION_V6, sb);
            }
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_LOCAL_PORT_SET) != 0)
            {
                localPort = nativeEvent.header.localPort;
            }
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_REMOTE_ADDR_SET) != 0)
            {
                remoteAddr = ToIpAddress(nativeEvent.header.remoteAddr, nativeEvent.header.ipVersion == Interop.FWP_IP_VERSION.FWP_IP_VERSION_V6, sb);
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
                Interop.FWPM_NET_EVENT_CLASSIFY_DROP1 classify = (Interop.FWPM_NET_EVENT_CLASSIFY_DROP1)Marshal.PtrToStructure(nativeEvent.EventInfo, typeof(Interop.FWPM_NET_EVENT_CLASSIFY_DROP1));
                filterId = classify.filterId;
                direction = classify.msFwpDirection;
            }
        }

        public NetEventData(Interop.FWPM_NET_EVENT2 nativeEvent, StringBuilder sb) : this()
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
                localAddr = ToIpAddress(nativeEvent.header.localAddr, nativeEvent.header.ipVersion == Interop.FWP_IP_VERSION.FWP_IP_VERSION_V6, sb);
            }
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_LOCAL_PORT_SET) != 0)
            {
                localPort = nativeEvent.header.localPort;
            }
            if ((flags & Interop.NetEventHeaderValidField.FWPM_NET_EVENT_FLAG_REMOTE_ADDR_SET) != 0)
            {
                remoteAddr = ToIpAddress(nativeEvent.header.remoteAddr, nativeEvent.header.ipVersion == Interop.FWP_IP_VERSION.FWP_IP_VERSION_V6, sb);
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
                Interop.FWPM_NET_EVENT_CLASSIFY_DROP1 classify = (Interop.FWPM_NET_EVENT_CLASSIFY_DROP1)Marshal.PtrToStructure(nativeEvent.EventInfo, typeof(Interop.FWPM_NET_EVENT_CLASSIFY_DROP1));
                filterId = classify.filterId;
                direction = classify.msFwpDirection;
            }
            if (nativeEvent.type == Interop.FWPM_NET_EVENT_TYPE.FWPM_NET_EVENT_TYPE_CLASSIFY_ALLOW)
            {
                Interop.FWPM_NET_EVENT_CLASSIFY_ALLOW0 classify = (Interop.FWPM_NET_EVENT_CLASSIFY_ALLOW0)Marshal.PtrToStructure(nativeEvent.EventInfo, typeof(Interop.FWPM_NET_EVENT_CLASSIFY_ALLOW0));
                filterId = classify.filterId;
                direction = classify.msFwpDirection;
            }
        }

    }

    public delegate void NetEventCallback(object context, NetEventData data);

    public abstract class NetEventSubscription : IDisposable
    {
        protected readonly FwpmNetEventSubscriptionSafeHandle _changeHandle;
        protected readonly NetEventCallback _callback;
        protected readonly StringBuilder SBuilder = new StringBuilder(40);
        protected readonly object _context;

        protected abstract uint CreateSubscription(FwpmEngineSafeHandle engineHandle, ref Interop.FWPM_NET_EVENT_SUBSCRIPTION0 subscription, IntPtr context, out FwpmNetEventSubscriptionSafeHandle changeHandle);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "dummy")]
        protected NetEventSubscription(Engine engine, NetEventCallback callback, object context)
        {
            _callback = callback;
            _context = context;
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
                    templ0.startTime.timestamp = 0;
                    templ0.endTime.timestamp = long.MaxValue;
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
                    err = CreateSubscription(engine.NativePtr, ref subs0, IntPtr.Zero, out _changeHandle);
                    if (0 == err)
                        handleOk = _changeHandle.SetEngineReference(engine.NativePtr);
                }

                // Do error handling after the CER
                if (0 != err)
                    throw new WfpException(err, "FwpmNetEventSubscribe1");
            }
            finally
            {
                templMemHandle?.Dispose();
            }
        }

        public void Dispose()
        {
            _changeHandle.Dispose();
        }
    }

    public sealed class NetEventSubscription0 : NetEventSubscription
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class NativeMethods
        {
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate void FWPM_NET_EVENT_CALLBACK0(IntPtr context, IntPtr netEvent1);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmNetEventSubscribe0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmNetEventSubscribe0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] ref Interop.FWPM_NET_EVENT_SUBSCRIPTION0 subscription,
                [In] FWPM_NET_EVENT_CALLBACK0 callback,
                [In] IntPtr context,
                [Out] out FwpmNetEventSubscriptionSafeHandle changeHandle);
        }

        private NativeMethods.FWPM_NET_EVENT_CALLBACK0 _nativeCallbackDelegate0;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "dummy")]
        internal NetEventSubscription0(Engine engine, NetEventCallback callback, object context) : base(engine, callback, context)
        { }

        protected override uint CreateSubscription(FwpmEngineSafeHandle engineHandle, ref Interop.FWPM_NET_EVENT_SUBSCRIPTION0 subscription, IntPtr context, out FwpmNetEventSubscriptionSafeHandle changeHandle)
        {
            _nativeCallbackDelegate0 = new NativeMethods.FWPM_NET_EVENT_CALLBACK0(NativeCallbackHandler0);
            return NativeMethods.FwpmNetEventSubscribe0(engineHandle, ref subscription, _nativeCallbackDelegate0, IntPtr.Zero, out changeHandle);
        }

        private void NativeCallbackHandler0(IntPtr context, IntPtr netEvent1)
        {
            Interop.FWPM_NET_EVENT1 ev = (Interop.FWPM_NET_EVENT1)Marshal.PtrToStructure(netEvent1, typeof(Interop.FWPM_NET_EVENT1));
            _callback(_context, new NetEventData(ev, SBuilder));
        }
    }

    public sealed class NetEventSubscription1 : NetEventSubscription
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class NativeMethods
        {
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate void FWPM_NET_EVENT_CALLBACK1(IntPtr context, IntPtr netEvent2);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmNetEventSubscribe1")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmNetEventSubscribe1(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] ref Interop.FWPM_NET_EVENT_SUBSCRIPTION0 subscription,
                [In] FWPM_NET_EVENT_CALLBACK1 callback,
                [In] IntPtr context,
                [Out] out FwpmNetEventSubscriptionSafeHandle changeHandle);

        }

        private NativeMethods.FWPM_NET_EVENT_CALLBACK1 _nativeCallbackDelegate0;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "dummy")]
        internal NetEventSubscription1(Engine engine, NetEventCallback callback, object context) : base(engine, callback, context)
        { }

        protected override uint CreateSubscription(FwpmEngineSafeHandle engineHandle, ref Interop.FWPM_NET_EVENT_SUBSCRIPTION0 subscription, IntPtr context, out FwpmNetEventSubscriptionSafeHandle changeHandle)
        {
            _nativeCallbackDelegate0 = new NativeMethods.FWPM_NET_EVENT_CALLBACK1(NativeCallbackHandler1);
            return NativeMethods.FwpmNetEventSubscribe1(engineHandle, ref subscription, _nativeCallbackDelegate0, IntPtr.Zero, out changeHandle);
        }

        private void NativeCallbackHandler1(IntPtr context, IntPtr netEvent1)
        {
            Interop.FWPM_NET_EVENT2 ev = (Interop.FWPM_NET_EVENT2)Marshal.PtrToStructure(netEvent1, typeof(Interop.FWPM_NET_EVENT2));
            _callback(_context, new NetEventData(ev, SBuilder));
        }
    }

}
