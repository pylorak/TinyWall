using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace WFPdotNet
{
    public class FilterCollection : System.Collections.ObjectModel.ReadOnlyCollection<Filter>
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class NativeMethods
        {
            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmFilterCreateEnumHandle0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmFilterCreateEnumHandle0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] Interop.FWPM_FILTER_ENUM_TEMPLATE0 enumTemplate,
                [Out] out FwpmFilterEnumSafeHandle enumHandle);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmFilterEnum0")]
            internal static extern uint FwpmFilterEnum0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] FwpmFilterEnumSafeHandle enumHandle,
                [In] uint numEntriesRequested,
                [Out] out FwpmMemorySafeHandle entries,
                [Out] out uint numEntriesReturned);
        }

        private void Init(Engine engine, bool getFilterConditions, Interop.FWPM_FILTER_ENUM_TEMPLATE0 template)
        {
            FwpmFilterEnumSafeHandle enumSafeHandle = null;
            FwpmMemorySafeHandle entries = null;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                uint err;
                bool handleOk = false;

                // Atomically get the native handle
                RuntimeHelpers.PrepareConstrainedRegions();
                try { }
                finally
                {
                    err = NativeMethods.FwpmFilterCreateEnumHandle0(engine.NativePtr, template, out enumSafeHandle);
                    if (0 == err)
                        handleOk = enumSafeHandle.SetEngineReference(engine.NativePtr);
                }

                // Do error handling after the CER
                if (!handleOk)
                    throw new Exception("Failed to set handle value.");
                if (0 != err)
                    throw new WfpException(err, "FwpmFilterCreateEnumHandle0");

                while (true)
                {
                    const uint numEntriesRequested = 10;
                    uint numEntriesReturned;

                    // FwpmFilterEnum0() returns a list of pointers in batches
                    err = NativeMethods.FwpmFilterEnum0(engine.NativePtr, enumSafeHandle, numEntriesRequested, out entries, out numEntriesReturned);
                    if (0 != err)
                        throw new WfpException(err, "FwpmFilterEnum0");

                    // Dereference each pointer in the current batch
                    IntPtr[] ptrList = PInvokeHelper.PtrToStructureArray<IntPtr>(entries.DangerousGetHandle(), numEntriesReturned, (uint)IntPtr.Size);

                    unsafe
                    {
                        PInvokeHelper.AssertUnmanagedType<Interop.FWPM_FILTER0_NoStrings>();
                        Interop.FWPM_FILTER0_NoStrings filt;
                        int size = System.Runtime.InteropServices.Marshal.SizeOf<Interop.FWPM_FILTER0_NoStrings>();
                        for (int i = 0; i < numEntriesReturned; ++i)
                        {
                            Buffer.MemoryCopy(ptrList[i].ToPointer(), &filt, size, size);
                            Items.Add(new Filter(in filt, getFilterConditions));
                        }
                    }

                    // Exit infinite loop if we have exhausted the list
                    if (numEntriesReturned < numEntriesRequested)
                        break;
                }
            }
            finally
            {
                entries?.Dispose();
                enumSafeHandle?.Dispose();
            }
        }

        internal FilterCollection(Engine engine, bool getFilterConditions, Guid provider, Guid layer)
            : base(new List<Filter>())
        {
            using var providerGuidHandle = SafeHGlobalHandle.FromStruct(provider);
            var template = new Interop.FWPM_FILTER_ENUM_TEMPLATE0
            {
                providerKey = providerGuidHandle.DangerousGetHandle(),
                layerKey = layer,
                flags = Interop.FilterEnumTemplateFlags.FWP_FILTER_ENUM_FLAG_INCLUDE_BOOTTIME | Interop.FilterEnumTemplateFlags.FWP_FILTER_ENUM_FLAG_INCLUDE_DISABLED,
                numFilterConditions = 0,
                actionMask = 0xFFFFFFFFu,
            };
            Init(engine, getFilterConditions, template);
        }

        internal FilterCollection(Engine engine, bool getFilterConditions)
            : base(new List<Filter>())
        {
            Init(engine, getFilterConditions, null);
        }
    }
}
