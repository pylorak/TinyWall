using System;
using System.Security;
using System.Runtime.InteropServices;

namespace pylorak.Windows.WFP
{
    public abstract class FilterEnumeratorBase : IDisposable
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmFilterCreateEnumHandle0")]
            public static extern uint FwpmFilterCreateEnumHandle0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] Interop.FWPM_FILTER_ENUM_TEMPLATE0 enumTemplate,
                [Out] out FwpmFilterEnumSafeHandle enumHandle);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmFilterEnum0")]
            public static extern uint FwpmFilterEnum0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] FwpmFilterEnumSafeHandle enumHandle,
                [In] int numEntriesRequested,
                [Out] out FwpmMemorySafeHandle entries,
                [Out] out int numEntriesReturned);
        }

        private const int NUM_ENTRY_REQUEST_SIZE = 16;
        private readonly Engine _engine;
        private readonly FwpmFilterEnumSafeHandle _enumSafeHandle;
        private readonly int FWPM_FILTER0_SIZE;

        private FwpmMemorySafeHandle _entries;
        private IntPtr _entryListItemPtr;
        private int _entriesRemain;
        private bool _disposed;

        protected FilterEnumeratorBase(Engine engine, Interop.FWPM_FILTER_ENUM_TEMPLATE0 template)
        {
            _engine = engine;

            var err = NativeMethods.FwpmFilterCreateEnumHandle0(engine.NativePtr, template, out _enumSafeHandle);
            if (0 != err)
                throw new WfpException(err, "FwpmFilterCreateEnumHandle0");

            if (!_enumSafeHandle.SetEngineReference(engine.NativePtr))
                throw new Exception("Failed to set handle value.");

            FWPM_FILTER0_SIZE = Marshal.SizeOf(typeof(Interop.FWPM_FILTER0_NoStrings));
        }

        public bool MoveNext()
        {
            if (0 == _entriesRemain)
            {
                _entries?.Dispose();

                var err = NativeMethods.FwpmFilterEnum0(_engine.NativePtr, _enumSafeHandle, NUM_ENTRY_REQUEST_SIZE, out _entries, out _entriesRemain);
                if (0 != err)
                    throw new WfpException(err, "FwpmFilterEnum0");
                if (0 == _entriesRemain)
                    return false;

                _entryListItemPtr = _entries.DangerousGetHandle();
            }

            PInvokeHelper.AssertUnmanagedType<Interop.FWPM_FILTER0_NoStrings>();
            unsafe
            {
                IntPtr* ptrListPtr = (IntPtr*)_entryListItemPtr;
                Interop.FWPM_FILTER0_NoStrings* filtPtr = (Interop.FWPM_FILTER0_NoStrings*)ptrListPtr->ToPointer();
                SetCurrentItem(filtPtr);
                _entryListItemPtr = new IntPtr(++ptrListPtr);
            }

            --_entriesRemain;
            return true;
        }

        protected unsafe abstract void SetCurrentItem(Interop.FWPM_FILTER0_NoStrings* native);

        public void Reset()
        {
            throw new NotSupportedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _enumSafeHandle.Dispose();
                    _entries?.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class FilterEnumerator : FilterEnumeratorBase
    {
        private readonly bool _getFilterConditions;

        public FilterEnumerator GetEnumerator() => this;

        public Filter Current { get; private set; }

        public FilterEnumerator(Engine engine, Interop.FWPM_FILTER_ENUM_TEMPLATE0 template, bool getFilterConditions)
            : base(engine, template)
        {
            _getFilterConditions = getFilterConditions;
        }

        protected override unsafe void SetCurrentItem(Interop.FWPM_FILTER0_NoStrings* native)
        {
            Current = new Filter(in *native, _getFilterConditions);
        }
    }

    public class FilterKeyEnumerator : FilterEnumeratorBase
    {
        public FilterKeyEnumerator GetEnumerator() => this;

        public Guid Current { get; private set; }

        public FilterKeyEnumerator(Engine engine, Interop.FWPM_FILTER_ENUM_TEMPLATE0 template)
            : base(engine, template)
        { }

        protected override unsafe void SetCurrentItem(Interop.FWPM_FILTER0_NoStrings* native)
        {
            Current = native->filterKey;
        }
    }
}
