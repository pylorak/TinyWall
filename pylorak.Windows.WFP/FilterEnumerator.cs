using System;
using System.Security;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

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
                [In] Interop.FWPM_FILTER_ENUM_TEMPLATE0? enumTemplate,
                out IntPtr enumHandle);

            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmFilterEnum0")]
            public static extern uint FwpmFilterEnum0(
                [In] FwpmEngineSafeHandle engineHandle,
                [In] FwpmFilterEnumSafeHandle enumHandle,
                [In] int numEntriesRequested,
                [Out] out FwpmMemorySafeHandle entries,
                [Out] out int numEntriesReturned);
        }

        private const int NUM_ENTRY_REQUEST_SIZE = 16;
        private readonly int FWPM_FILTER0_SIZE;
        private readonly Engine _engine;
        private readonly FwpmFilterEnumSafeHandle _enumSafeHandle;
        private readonly SafeHGlobalHandle? _providerGuidHandle;

        private FwpmMemorySafeHandle? _entries;
        private IntPtr _entryListItemPtr;
        private int _entriesRemain;
        private bool _disposed;

        protected FilterEnumeratorBase(Engine engine, Interop.FWPM_FILTER_ENUM_TEMPLATE0? template, Guid? providerKey)
        {
            if ((providerKey.HasValue) && (template is null))
                throw new ArgumentNullException(nameof(template), "Template cannot be null if a providerKey is provided.");

            _engine = engine;

            try
            {

                if (template is not null)
                {
                    // Fill in template.providerKey
                    if (providerKey.HasValue)
                    {
                        _providerGuidHandle = SafeHGlobalHandle.FromStruct(providerKey.Value);
                        template.providerKey = _providerGuidHandle.DangerousGetHandle();
                    }
                    else
                        template.providerKey = IntPtr.Zero;
                }

                var err = NativeMethods.FwpmFilterCreateEnumHandle0(engine.NativePtr, template, out IntPtr outHndl);
                if (0 == err)
                    _enumSafeHandle = new FwpmFilterEnumSafeHandle(outHndl, engine.NativePtr);
                else
                    throw new WfpException(err, "FwpmFilterCreateEnumHandle0");

                FWPM_FILTER0_SIZE = Marshal.SizeOf<Interop.FWPM_FILTER0_NoStrings>();
            }
            catch
            {
                _providerGuidHandle?.Dispose();
                throw;
            }
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
                    _providerGuidHandle?.Dispose();
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

        [DisallowNull]
        public Filter? Current { get; private set; }

        public FilterEnumerator(Engine engine, Interop.FWPM_FILTER_ENUM_TEMPLATE0? template, bool getFilterConditions, Guid? providerKey = null)
            : base(engine, template, providerKey)
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

        public FilterKeyEnumerator(Engine engine, Interop.FWPM_FILTER_ENUM_TEMPLATE0? template, Guid? providerKey = null)
            : base(engine, template, providerKey)
        { }

        protected override unsafe void SetCurrentItem(Interop.FWPM_FILTER0_NoStrings* native)
        {
            Current = native->filterKey;
        }
    }
}
