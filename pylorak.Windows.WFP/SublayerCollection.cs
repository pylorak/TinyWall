using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace pylorak.Windows.WFP
{
    public class SublayerCollection : System.Collections.ObjectModel.ReadOnlyCollection<Sublayer>
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class NativeMethods
        {
            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmSubLayerCreateEnumHandle0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmSubLayerCreateEnumHandle0(
                [In] FwpmEngineSafeHandle engineHandle,
                IntPtr enumTemplate,
                out IntPtr enumHandle);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmSubLayerEnum0")]
            internal static extern uint FwpmSubLayerEnum0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] FwpmSublayerEnumSafeHandle enumHandle,
                uint numEntriesRequested,
                out FwpmMemorySafeHandle entries,
                out uint numEntriesReturned);
        }

        internal SublayerCollection(Engine engine)
            : base(new List<Sublayer>())
        {
            FwpmSublayerEnumSafeHandle? enumSafeHandle = null;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                var err = NativeMethods.FwpmSubLayerCreateEnumHandle0(engine.NativePtr, IntPtr.Zero, out IntPtr outHndl);
                if (0 == err)
                    enumSafeHandle = new FwpmSublayerEnumSafeHandle(outHndl, engine.NativePtr);
                else
                    throw new WfpException(err, "FwpmSessionCreateEnumHandle0");

                while (true)
                {
                    const uint numEntriesRequested = 10;

                    FwpmMemorySafeHandle? entries = null;
                    try
                    {
                        // FwpmSubLayerEnum0() returns a list of pointers in batches
                        err = NativeMethods.FwpmSubLayerEnum0(engine.NativePtr, enumSafeHandle, numEntriesRequested, out entries, out uint numEntriesReturned);
                        if (0 != err)
                            throw new WfpException(err, "FwpmSubLayerEnum0");

                        // Dereference each pointer in the current batch
                        IntPtr[] ptrList = PInvokeHelper.PtrToStructureArray<IntPtr>(entries.DangerousGetHandle(), numEntriesReturned, (uint)IntPtr.Size);
                        for (int i = 0; i < numEntriesReturned; ++i)
                        {
                            Items.Add(new Sublayer(Marshal.PtrToStructure<Interop.FWPM_SUBLAYER0>(ptrList[i])));
                        }

                        // Exit infinite loop if we have exhausted the list
                        if (numEntriesReturned < numEntriesRequested)
                            break;
                    }
                    finally
                    {
                        entries?.Dispose();
                    }
                } // while
            }
            finally
            {
                enumSafeHandle?.Dispose();
            }
        }
    }
}
