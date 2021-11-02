using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace WFPdotNet
{
    public class SessionCollection : System.Collections.ObjectModel.ReadOnlyCollection<Interop.FWPM_SESSION0>
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class NativeMethods
        {
            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmSessionCreateEnumHandle0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmSessionCreateEnumHandle0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] IntPtr enumTemplate,
                [Out] out FwpmSessionEnumSafeHandle enumHandle);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmSessionEnum0")]
            internal static extern uint FwpmSessionEnum0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] FwpmSessionEnumSafeHandle enumHandle,
                [In] uint numEntriesRequested,
                [Out] out FwpmMemorySafeHandle entries,
                [Out] out uint numEntriesReturned);
        }

        internal SessionCollection(Engine engine)
            : base(new List<Interop.FWPM_SESSION0>())
        {
            FwpmSessionEnumSafeHandle enumSafeHandle = null;
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
                    err = NativeMethods.FwpmSessionCreateEnumHandle0(engine.NativePtr, IntPtr.Zero, out enumSafeHandle);
                    if (0 == err)
                        handleOk = enumSafeHandle.SetEngineReference(engine.NativePtr);
                }

                // Do error handling after the CER
                if (!handleOk)
                    throw new Exception("Failed to set handle value.");
                if (0 != err)
                    throw new WfpException(err, "FwpmSessionCreateEnumHandle0");

                while (true)
                {
                    const uint numEntriesRequested = 10;
                    uint numEntriesReturned;

                    // FwpmSessionEnum0() returns a list of pointers in batches
                    err = NativeMethods.FwpmSessionEnum0(engine.NativePtr, enumSafeHandle, numEntriesRequested, out entries, out numEntriesReturned);
                    if (0 != err)
                        throw new WfpException(err, "FwpmSessionEnum0");

                    // Dereference each pointer in the current batch
                    IntPtr[] ptrList = PInvokeHelper.PtrToStructureArray<IntPtr>(entries.DangerousGetHandle(), numEntriesReturned, (uint)IntPtr.Size);
                    for (int i = 0; i < numEntriesReturned; ++i)
                    {
                        Items.Add(Marshal.PtrToStructure<Interop.FWPM_SESSION0>(ptrList[i]));
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
