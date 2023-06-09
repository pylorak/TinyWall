﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace pylorak.Windows.WFP
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
                out IntPtr enumHandle);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmSessionEnum0")]
            internal static extern uint FwpmSessionEnum0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] FwpmSessionEnumSafeHandle enumHandle,
                uint numEntriesRequested,
                out FwpmMemorySafeHandle entries,
                out uint numEntriesReturned);
        }

        internal SessionCollection(Engine engine)
            : base(new List<Interop.FWPM_SESSION0>())
        {
            FwpmSessionEnumSafeHandle? enumSafeHandle = null;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                var err = NativeMethods.FwpmSessionCreateEnumHandle0(engine.NativePtr, IntPtr.Zero, out IntPtr outHndl);
                if (0 == err)
                    enumSafeHandle = new FwpmSessionEnumSafeHandle(outHndl, engine.NativePtr);
                else
                    throw new WfpException(err, "FwpmSessionCreateEnumHandle0");

                while (true)
                {
                    const uint numEntriesRequested = 10;

                    FwpmMemorySafeHandle? entries = null;
                    try
                    {
                        // FwpmSessionEnum0() returns a list of pointers in batches
                        err = NativeMethods.FwpmSessionEnum0(engine.NativePtr, enumSafeHandle, numEntriesRequested, out entries, out uint numEntriesReturned);
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
