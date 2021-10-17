using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WFPdotNet
{
    [Flags]
    public enum FilterFlags : uint
    {
        FWPM_FILTER_FLAG_PERSISTENT = 0x00000001,
        FWPM_FILTER_FLAG_BOOTTIME = 0x00000002,
        FWPM_FILTER_FLAG_HAS_PROVIDER_CONTEXT = 0x00000004,
        FWPM_FILTER_FLAG_CLEAR_ACTION_RIGHT = 0x00000008,
        FWPM_FILTER_FLAG_PERMIT_IF_CALLOUT_UNREGISTERED = 0x00000010,
        FWPM_FILTER_FLAG_DISABLED = 0x00000020,
        FWPM_FILTER_FLAG_INDEXED = 0x00000040
    }

    public enum FilterActions : uint
    {
        FWP_ACTION_BLOCK = Interop.FWP_ACTION_TYPE.FWP_ACTION_BLOCK,
        FWP_ACTION_PERMIT = Interop.FWP_ACTION_TYPE.FWP_ACTION_PERMIT,
        FWP_ACTION_CALLOUT_TERMINATING = Interop.FWP_ACTION_TYPE.FWP_ACTION_CALLOUT_TERMINATING,
    }

    public sealed class Filter : IDisposable
    {
        private Interop.FWPM_FILTER0 _nativeStruct;

        private Guid? _providerKey;
        AllocHGlobalSafeHandle _providerKeyHandle;

        AllocHGlobalSafeHandle _weightHandle;

        private List<FilterCondition> _conditions;
        AllocHGlobalSafeHandle _conditionsHandle;

        public Filter()
        {
            _nativeStruct.providerKey = IntPtr.Zero;
            _providerKeyHandle = null;

            _weightHandle = new AllocHGlobalSafeHandle(sizeof(ulong));
            _nativeStruct.weight.type = Interop.FWP_DATA_TYPE.FWP_UINT64;
            _nativeStruct.weight.value.uint64 = _weightHandle.DangerousGetHandle();

            _conditions = new List<FilterCondition>();
            _conditionsHandle = new AllocHGlobalSafeHandle();
        }

        public Filter(string name, string desc, Guid providerKey, FilterActions action, ulong weight) : this()
        {
            this.DisplayName = name;
            this.DisplayDescription = desc;
            this.ProviderKey = providerKey;
            this.Action = action;
            this.Weight = weight;
        }

        internal Filter(Interop.FWPM_FILTER0 filt0, bool getConditions)
        {
            _nativeStruct = filt0;

            // TODO: Do we really not need to own these SafeHandles ???
            _weightHandle = new AllocHGlobalSafeHandle(_nativeStruct.weight.value.uint64, false);
            _conditionsHandle = new AllocHGlobalSafeHandle(_nativeStruct.filterConditions, false);

            if (_nativeStruct.providerKey != IntPtr.Zero)
            {
                // TODO: Do we really not need to own these SafeHandles ???
                _providerKeyHandle = new AllocHGlobalSafeHandle(_nativeStruct.providerKey, false);
                _providerKey = (Guid)System.Runtime.InteropServices.Marshal.PtrToStructure(_providerKeyHandle.DangerousGetHandle(), typeof(Guid));
            }

            if (getConditions)
            {
                int condSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Interop.FWPM_FILTER_CONDITION0));
                _conditions = new List<FilterCondition>((int)_nativeStruct.numFilterConditions);
                for (int i = 0; i < (int)_nativeStruct.numFilterConditions; ++i)
                {
                    IntPtr ptr = new IntPtr(_nativeStruct.filterConditions.ToInt64() + i * condSize);
                    FilterCondition cond = new FilterCondition((Interop.FWPM_FILTER_CONDITION0)System.Runtime.InteropServices.Marshal.PtrToStructure(ptr, typeof(Interop.FWPM_FILTER_CONDITION0)));
                    _conditions.Add(cond);
                }
            }
        }

        public Interop.FWPM_FILTER0 Marshal()
        {
            _conditionsHandle.Dispose();

            _nativeStruct.numFilterConditions = (uint)_conditions.Count;

            int condSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Interop.FWPM_FILTER_CONDITION0));
            _conditionsHandle = new AllocHGlobalSafeHandle(_conditions.Count * condSize);
            _nativeStruct.filterConditions = _conditionsHandle.DangerousGetHandle();
            for (int i = 0; i < _conditions.Count; ++i )
            {
                _conditionsHandle.MarshalFromStruct(_conditions[i].Marshal(), i * condSize);
            }

            return _nativeStruct;
        }


        public Guid FilterKey
        {
            get { return _nativeStruct.filterKey; }
            set { _nativeStruct.filterKey = value; }
        }

        public string DisplayName
        {
            get { return _nativeStruct.displayData.name; }
            set { _nativeStruct.displayData.name = value; }
        }

        public string DisplayDescription
        {
            get { return _nativeStruct.displayData.description; }
            set { _nativeStruct.displayData.description = value; }
        }

        public FilterFlags Flags
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
                    _providerKeyHandle = PInvokeHelper.StructToHGlobal<Guid>(value.Value);
                    _nativeStruct.providerKey = _providerKeyHandle.DangerousGetHandle();
                }
                else
                {
                    _nativeStruct.providerKey = IntPtr.Zero;
                }
            }

        }

        private ulong? _FilterId;
        public ulong FilterId
        {
            internal set { _FilterId = value; }
            get
            {
                if (!_FilterId.HasValue)
                    throw new InvalidOperationException("This filter has not yet been registered with the BFE.");

                return _FilterId.Value;
            }
        }

        public Guid LayerKey
        {
            get { return _nativeStruct.layerKey; }
            set { _nativeStruct.layerKey = value; }
        }
        public Guid SublayerKey
        {
            get { return _nativeStruct.subLayerKey; }
            set { _nativeStruct.subLayerKey = value; }
        }
        public ulong Weight
        {
            get { return (ulong)System.Runtime.InteropServices.Marshal.PtrToStructure(_nativeStruct.weight.value.uint64, typeof(ulong)); }
            set { System.Runtime.InteropServices.Marshal.StructureToPtr(value, _nativeStruct.weight.value.uint64, false); }
        }
        public List<FilterCondition> Conditions
        {
            get { return _conditions; }
        }
        public FilterActions Action
        {
            get { return (FilterActions)_nativeStruct.action.type; }
            set { _nativeStruct.action.type = (Interop.FWP_ACTION_TYPE)value; }
        }
        public Guid CalloutKey
        {
            get { return _nativeStruct.action.calloutKey; }
            set { _nativeStruct.action.calloutKey = value; }
        }

        public void Dispose()
        {
            _providerKeyHandle?.Dispose();
            _weightHandle?.Dispose();
            _conditionsHandle?.Dispose();

            _providerKeyHandle = null;
            _weightHandle = null;
            _conditionsHandle = null;
        }
    }
}
