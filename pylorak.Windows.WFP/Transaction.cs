using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace pylorak.Windows.WFP
{
    public sealed class Transaction : IDisposable
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class NativeMethods
        {
            [Flags]
            internal enum TransactionFlags : uint
            {
                FWPM_TXN_READ_WRITE = 0,
                FWPM_TXN_READ_ONLY = 0x00000001
            }

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmTransactionBegin0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmTransactionBegin0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] TransactionFlags flags);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmTransactionCommit0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmTransactionCommit0(
                [In] FwpmEngineSafeHandle engineHandle);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmTransactionAbort0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmTransactionAbort0(
                [In] FwpmEngineSafeHandle engineHandle);
        }

        private readonly FwpmEngineSafeHandle _safeEngineHandle;
        private bool _transactionClosed;

        internal Transaction(Engine engine, bool readOnly)
        {
            NativeMethods.TransactionFlags flags = readOnly ? NativeMethods.TransactionFlags.FWPM_TXN_READ_ONLY : NativeMethods.TransactionFlags.FWPM_TXN_READ_WRITE;
            _safeEngineHandle = engine.NativePtr;

            uint err;
            bool success = false;

            // Atomically start transaction and increase reference count
            RuntimeHelpers.PrepareConstrainedRegions();
            try { }
            finally
            {
                err = NativeMethods.FwpmTransactionBegin0(engine.NativePtr, flags);
                if (0 == err)
                    _safeEngineHandle.DangerousAddRef(ref success);
            }

            // Do error handling after the CER
            if (0 != err)
                throw new WfpException(err, "FwpmTransactionBegin0");
            if (!success)
                throw new Exception("Failed to set handle value.");
        }

        public void Commit()
        {
            if (_transactionClosed)
                throw new InvalidOperationException("Transaction is already closed.");

            uint err;

            // Atomically close transaction and decrease reference count
            RuntimeHelpers.PrepareConstrainedRegions();
            try { }
            finally
            {
                err = NativeMethods.FwpmTransactionCommit0(_safeEngineHandle);
                if (0 == err)
                    _safeEngineHandle.DangerousRelease();
            }

            // Do error handling after the CER
            if (0 != err)
                throw new WfpException(err, "FwpmTransactionCommit0");

            _transactionClosed = true;
        }

        public void Abort()
        {
            if (_transactionClosed)
                throw new InvalidOperationException("Transaction is already closed.");

            uint err;

            // Atomically close transaction and decrease reference count
            RuntimeHelpers.PrepareConstrainedRegions();
            try { }
            finally
            {
                err = NativeMethods.FwpmTransactionAbort0(_safeEngineHandle);
                if (0 == err)
                    _safeEngineHandle.DangerousRelease();
            }

            // Do error handling after the CER
            if (0 != err)
                throw new WfpException(err, "FwpmTransactionAbort0");

            _transactionClosed = true;
        }

        public void Dispose()
        {
            if (!_transactionClosed)
                Abort();
        }
    }
}
