using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace WFPdotNet
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
                [Out] out FwpmFilterSubscriptionSafeHandle changeHandle);
        }


        private readonly FwpmFilterSubscriptionSafeHandle _changeHandle;
        private readonly FilterChangeCallback _callback;
        private readonly object _context;
        private readonly NativeMethods.FWPM_FILTER_CHANGE_CALLBACK0 _nativeCallbackDelegate;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "dummy")]
        private FilterSubscription(Engine engine, FilterChangeCallback callback, object context, Guid? providerKey, Guid? layerKey, bool dummy)
        {
            _callback = callback;
            _context = context;
            _nativeCallbackDelegate = new NativeMethods.FWPM_FILTER_CHANGE_CALLBACK0(NativeCallbackHandler);
            AllocHGlobalSafeHandle providerKeyMemHandle = null;
            AllocHGlobalSafeHandle templMemHandle = null;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Interop.FWPM_FILTER_SUBSCRIPTION0 subs0 = new Interop.FWPM_FILTER_SUBSCRIPTION0();
                subs0.flags = Interop.FilterSubscriptionFlags.FWPM_SUBSCRIPTION_FLAG_NOTIFY_ON_ADD | Interop.FilterSubscriptionFlags.FWPM_SUBSCRIPTION_FLAG_NOTIFY_ON_DELETE;
                subs0.enumTemplate = IntPtr.Zero;

                if (layerKey.HasValue)
                {
                    Interop.FWPM_FILTER_ENUM_TEMPLATE0 templ0 = new Interop.FWPM_FILTER_ENUM_TEMPLATE0();
                    providerKeyMemHandle = PInvokeHelper.StructToHGlobal<Guid>(providerKey.Value);
                    templ0.providerKey = providerKeyMemHandle.DangerousGetHandle();
                    templ0.layerKey = layerKey.Value;
                    templ0.enumType = Interop.FWP_FILTER_ENUM_TYPE.FWP_FILTER_ENUM_FULLY_CONTAINED;
                    templ0.flags = 0;
                    templ0.providerContextTemplate = IntPtr.Zero;
                    templ0.numFilterConditions = 0;
                    templ0.filterCondition = IntPtr.Zero;
                    templ0.actionMask = 0xFFFFFFFF;
                    templ0.calloutKey = IntPtr.Zero;

                    templMemHandle = PInvokeHelper.StructToHGlobal<Interop.FWPM_FILTER_ENUM_TEMPLATE0>(templ0);
                    subs0.enumTemplate = templMemHandle.DangerousGetHandle();
                }

                uint err;
                bool handleOk = false;

                // Atomically get the native handle
                RuntimeHelpers.PrepareConstrainedRegions();
                try { }
                finally
                {
                    err = NativeMethods.FwpmFilterSubscribeChanges0(engine.NativePtr, ref subs0, _nativeCallbackDelegate, IntPtr.Zero, out _changeHandle);
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
                providerKeyMemHandle?.Dispose();
                templMemHandle?.Dispose();
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
            Interop.FWPM_FILTER_CHANGE0 cs = (Interop.FWPM_FILTER_CHANGE0)Marshal.PtrToStructure(change, typeof(Interop.FWPM_FILTER_CHANGE0));
            _callback(_context, (FilterChangeType)cs.changeType, cs.filterKey);
        }

        public void Dispose()
        {
            _changeHandle.Dispose();
        }
    }
}
