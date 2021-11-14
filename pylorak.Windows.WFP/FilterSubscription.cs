using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace pylorak.Windows.WFP
{
    public enum FilterChangeType
    {
        Add = Interop.FWPM_CHANGE_TYPE.FWPM_CHANGE_ADD,
        Delete = Interop.FWPM_CHANGE_TYPE.FWPM_CHANGE_DELETE
    }

    public delegate void FilterChangeCallback(object context, FilterChangeType type, Guid filterKey);

    public sealed class FilterSubscription : IDisposable
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class NativeMethods
        {
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            internal delegate void FWPM_FILTER_CHANGE_CALLBACK0(IntPtr context, IntPtr change);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmFilterSubscribeChanges0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmFilterSubscribeChanges0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] ref Interop.FWPM_FILTER_SUBSCRIPTION0 subscription,
                [In] FWPM_FILTER_CHANGE_CALLBACK0 callback,
                [In] IntPtr context,
                out IntPtr changeHandle);
        }


        private readonly FwpmFilterSubscriptionSafeHandle _subscriptionHandle;
        private readonly FilterChangeCallback _callback;
        private readonly object _context;
        private readonly NativeMethods.FWPM_FILTER_CHANGE_CALLBACK0 _nativeCallbackDelegate;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "dummy")]
        private FilterSubscription(Engine engine, FilterChangeCallback callback, object context, Guid? providerKey, Guid? layerKey, bool _)
        {
            _callback = callback;
            _context = context;
            _nativeCallbackDelegate = new NativeMethods.FWPM_FILTER_CHANGE_CALLBACK0(NativeCallbackHandler);
            SafeHGlobalHandle? providerKeyMemHandle = null;

            try
            {
                Interop.FWPM_FILTER_SUBSCRIPTION0 subs0 = new Interop.FWPM_FILTER_SUBSCRIPTION0();
                subs0.flags = Interop.FilterSubscriptionFlags.FWPM_SUBSCRIPTION_FLAG_NOTIFY_ON_ADD | Interop.FilterSubscriptionFlags.FWPM_SUBSCRIPTION_FLAG_NOTIFY_ON_DELETE;
                subs0.enumTemplate = null;

                if (layerKey.HasValue)
                {
                    providerKeyMemHandle = SafeHGlobalHandle.FromStruct(providerKey!.Value);
                    subs0.enumTemplate = new Interop.FWPM_FILTER_ENUM_TEMPLATE0
                    {
                        providerKey = providerKeyMemHandle.DangerousGetHandle(),
                        layerKey = layerKey.Value,
                        numFilterConditions = 0,
                        actionMask = 0xFFFFFFFFu,
                    };
                }

                var err = NativeMethods.FwpmFilterSubscribeChanges0(engine.NativePtr, ref subs0, _nativeCallbackDelegate, IntPtr.Zero, out IntPtr outHndl);
                if (0 == err)
                    _subscriptionHandle = new FwpmFilterSubscriptionSafeHandle(outHndl, engine.NativePtr);
                else
                    throw new WfpException(err, "FwpmFilterSubscribeChanges0");
            }
            finally
            {
                providerKeyMemHandle?.Dispose();
            }
        }

        internal FilterSubscription(Engine engine, FilterChangeCallback callback, object context, Guid providerKey, Guid layerKey)
            : this(engine, callback, context, providerKey, layerKey, false)
        {
        }

        internal FilterSubscription(Engine engine, FilterChangeCallback callback, object context)
            : this(engine, callback, context, null, null, false)
        {
        }

        private void NativeCallbackHandler(IntPtr context, IntPtr change)
        {
            Interop.FWPM_FILTER_CHANGE0 cs = PInvokeHelper.PtrToStructure<Interop.FWPM_FILTER_CHANGE0>(change);
            _callback(_context, (FilterChangeType)cs.changeType, cs.filterKey);
        }

        public void Dispose()
        {
            _subscriptionHandle.Dispose();
        }
    }
}
