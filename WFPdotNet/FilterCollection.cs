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
                [In] IntPtr enumTemplate,
                [Out] out FwpmFilterEnumSafeHandle enumHandle);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmFilterEnum0")]
            internal static extern uint FwpmFilterEnum0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] FwpmFilterEnumSafeHandle enumHandle,
                [In] uint numEntriesRequested,
                [Out] out FwpmMemorySafeHandle entries,
                [Out] out uint numEntriesReturned);
        }

        internal FilterCollection(Engine engine, bool getFilterConditions)
            : base(new List<Filter>())
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
                    err = NativeMethods.FwpmFilterCreateEnumHandle0(engine.NativePtr, IntPtr.Zero, out enumSafeHandle);
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
                    for (int i = 0; i < numEntriesReturned; ++i)
                    {
                        Interop.FWPM_FILTER0 filt0 = Marshal.PtrToStructure<Interop.FWPM_FILTER0>(ptrList[i]);
                        Items.Add(new Filter(filt0, getFilterConditions));
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
    }
}
