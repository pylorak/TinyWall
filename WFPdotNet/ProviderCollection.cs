using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace WFPdotNet
{
    public class ProviderCollection : System.Collections.ObjectModel.ReadOnlyCollection<Interop.FWPM_PROVIDER0>
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class NativeMethods
        {
            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmProviderCreateEnumHandle0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmProviderCreateEnumHandle0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] IntPtr enumTemplate,
                [Out] out FwpmProviderEnumSafeHandle enumHandle);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmProviderEnum0")]
            internal static extern uint FwpmProviderEnum0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] FwpmProviderEnumSafeHandle enumHandle,
                [In] uint numEntriesRequested,
                [Out] out FwpmMemorySafeHandle entries,
                [Out] out uint numEntriesReturned);
        }

        internal ProviderCollection(Engine engine)
            : base(new List<Interop.FWPM_PROVIDER0>())
        {
            FwpmProviderEnumSafeHandle enumSafeHandle = null;
            FwpmMemorySafeHandle entries = null;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                uint err;
                bool handleOk = false;

                // Atomically construct the native handle
                RuntimeHelpers.PrepareConstrainedRegions();
                try { }
                finally
                {
                    err = NativeMethods.FwpmProviderCreateEnumHandle0(engine.NativePtr, IntPtr.Zero, out enumSafeHandle);
                    if (0 == err)
                        handleOk = enumSafeHandle.SetEngineReference(engine.NativePtr);
                }

                // Do error handling after the CER
                if (!handleOk)
                    throw new Exception("Failed to set handle value.");
                if (0 != err)
                    throw new WfpException(err, "FwpmProviderCreateEnumHandle0");

                while (true)
                {
                    const uint numEntriesRequested = 10;
                    uint numEntriesReturned;

                    // FwpmProviderEnum0() returns a list of pointers in batches
                    err = NativeMethods.FwpmProviderEnum0(engine.NativePtr, enumSafeHandle, numEntriesRequested, out entries, out numEntriesReturned);
                    if (0 != err)
                        throw new WfpException(err, "FwpmProviderEnum0");

                    // Dereference each pointer in the current batch
                    IntPtr[] ptrList = PInvokeHelper.PtrToStructureArray<IntPtr>(entries.DangerousGetHandle(), numEntriesReturned, (uint)Marshal.SizeOf(typeof(IntPtr)));
                    for (int i = 0; i < numEntriesReturned; ++i)
                    {
                        Items.Add((Interop.FWPM_PROVIDER0)Marshal.PtrToStructure(ptrList[i], typeof(Interop.FWPM_PROVIDER0)));
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
