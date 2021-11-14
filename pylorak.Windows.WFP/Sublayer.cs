using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace pylorak.Windows.WFP
{
    public sealed class Sublayer : IDisposable
    {
        private Interop.FWPM_SUBLAYER0 _nativeStruct;

        private Guid? _providerKey;
        private SafeHGlobalHandle _providerKeyHandle;

        public Sublayer(string name)
        {
            Name = name;
        }

        internal Sublayer(Interop.FWPM_SUBLAYER0 nativeStruct)
        {
            _nativeStruct = nativeStruct;

            if (_nativeStruct.providerKey != IntPtr.Zero)
            {
                // TODO: Do we really not need to own these SafeHandles ???
                //_providerKeyHandle = new AllocHGlobalSafeHandle(_nativeStruct.providerKey, false);
                _providerKey = PInvokeHelper.PtrToStructure<Guid>(_nativeStruct.providerKey);
            }
        }

        public string Name
        {
            get { return _nativeStruct.displayData.name; }
            set { _nativeStruct.displayData.name = value; }
        }

        public string Description
        {
            get { return _nativeStruct.displayData.description; }
            set { _nativeStruct.displayData.description = value; }
        }

        public Guid SublayerKey
        {
            get { return _nativeStruct.subLayerKey; }
            set { _nativeStruct.subLayerKey = value; }
        }

        public Interop.FWPM_SUBLAYER_FLAGS Flags
        {
            get { return _nativeStruct.flags; }
            set { _nativeStruct.flags = value; }
        }

        public Guid? ProviderKey
        {
            get { return _providerKey; }
            set
            {
                _providerKeyHandle?.Dispose();
                _providerKeyHandle = null;

                _providerKey = value;

                if (value.HasValue)
                {
                    _providerKeyHandle = SafeHGlobalHandle.FromStruct(value.Value);
                    _nativeStruct.providerKey = _providerKeyHandle.DangerousGetHandle();
                }
                else
                {
                    _nativeStruct.providerKey = IntPtr.Zero;
                }
            }
        }

        public ushort Weight
        {
            get { return _nativeStruct.weight; }
            set { _nativeStruct.weight = value; }
        }

        public Interop.FWPM_SUBLAYER0 Marshal()
        {
            return _nativeStruct;
        }

        public void Dispose()
        {
            _providerKeyHandle?.Dispose();
            _providerKeyHandle = null;
        }

        public override string ToString()
        {
            return _nativeStruct.displayData.name;
        }
    }
}
