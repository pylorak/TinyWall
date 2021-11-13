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
        private enum DisplaySyncMode
        {
            None,
            ToNative,
            ToManaged
        }

        private Interop.FWPM_FILTER0_NoStrings _nativeStruct;

        private ulong _weight;
        private Guid _providerKey;
        private SafeHGlobalHandle _weightAndProviderKeyHandle;

        private string _displayName;
        private string _displayDescription;
        private SafeHGlobalHandle _displayDataHandle;

        private FilterConditionList _conditions;
        private SafeHGlobalHandle _conditionsHandle;

        private Filter()
        {
            _weightAndProviderKeyHandle = SafeHGlobalHandle.Alloc(sizeof(ulong) + Marshal.SizeOf(typeof(Guid)));
            _nativeStruct.weight.type = Interop.FWP_DATA_TYPE.FWP_UINT64;
            _nativeStruct.weight.value.uint64 = _weightAndProviderKeyHandle.DangerousGetHandle();
            _nativeStruct.providerKey = _weightAndProviderKeyHandle.DangerousGetHandle() + sizeof(ulong);
        }

        public Filter(string name, string desc, Guid providerKey, FilterActions action, ulong weight, FilterConditionList conditions = null) : this()
        {
            _conditions = (conditions is null) ? new FilterConditionList() : conditions;

            this.DisplayName = name;
            this.DisplayDescription = desc;
            this.ProviderKey = providerKey;
            this.Action = action;
            this.Weight = weight;
        }

        internal Filter(in Interop.FWPM_FILTER0_NoStrings filt0, bool getConditions) : this()
        {
            _nativeStruct = filt0;

            if (_nativeStruct.providerKey != IntPtr.Zero)
                ProviderKey = PInvokeHelper.PtrToStructure<Guid>(_nativeStruct.providerKey);

            if (_nativeStruct.weight.value.uint64 != IntPtr.Zero)
                Weight = PInvokeHelper.PtrToStructure<ulong>(_nativeStruct.weight.value.uint64);

            if (_nativeStruct.displayData.name != IntPtr.Zero)
                DisplayName = Marshal.PtrToStringUni(_nativeStruct.displayData.name);

            if (_nativeStruct.displayData.description != IntPtr.Zero)
                DisplayDescription = Marshal.PtrToStringUni(_nativeStruct.displayData.description);

            if (getConditions)
            {
                int condSize = Marshal.SizeOf(typeof(Interop.FWPM_FILTER_CONDITION0));
                _conditions = new FilterConditionList((int)_nativeStruct.numFilterConditions);
                for (int i = 0; i < (int)_nativeStruct.numFilterConditions; ++i)
                {
                    IntPtr ptr = new IntPtr(_nativeStruct.filterConditions.ToInt64() + i * condSize);
                    FilterCondition cond = new FilterCondition(PInvokeHelper.PtrToStructure<Interop.FWPM_FILTER_CONDITION0>(ptr));
                    _conditions.Add(cond);
                }
            }
        }

        public Interop.FWPM_FILTER0_NoStrings Prepare()
        {
            SynchronizeDisplayData();

            if (_conditionsHandle == null)
            {
                int condSize = Marshal.SizeOf(typeof(Interop.FWPM_FILTER_CONDITION0));
                _conditionsHandle?.Dispose();
                _conditionsHandle = SafeHGlobalHandle.Alloc(_conditions.Count * condSize);
                _nativeStruct.filterConditions = _conditionsHandle.DangerousGetHandle();
                _nativeStruct.numFilterConditions = (uint)_conditions.Count;

                unsafe
                {
                    PInvokeHelper.AssertUnmanagedType<Interop.FWPM_FILTER_CONDITION0>();
                    IntPtr dst = _conditionsHandle.DangerousGetHandle();
                    int size = Marshal.SizeOf<Interop.FWPM_FILTER_CONDITION0>();
                    for (int i = 0; i < _conditions.Count; ++i)
                    {
                        var cond = _conditions[i].Marshal();
                        Interop.FWPM_FILTER_CONDITION0* src = &cond;
                        Buffer.MemoryCopy(src, dst.ToPointer(), size, size);
                        dst += size;
                    }
                }
            }

            return _nativeStruct;
        }

        public Guid FilterKey
        {
            get { return _nativeStruct.filterKey; }
            set { _nativeStruct.filterKey = value; }
        }

        private void SynchronizeDisplayData()
        {
            if (_displayDataHandle != null)
                // Already synchronized
                return;

            int nameSize = _displayName.Length * 2;
            int descriptionSize = _displayDescription.Length * 2;
            int unmanagedSize = nameSize + descriptionSize + (2 * 2);
            _displayDataHandle = SafeHGlobalHandle.Alloc(unmanagedSize);

            IntPtr namePtr = _displayDataHandle.DangerousGetHandle();
            IntPtr descriptionPtr = namePtr + nameSize + 2;
            unsafe
            {
                var dst = (char*)namePtr;
                dst[_displayName.Length] = (char)0;
                fixed (char* src = _displayName)
                    Buffer.MemoryCopy(src, dst, nameSize, nameSize);

                dst = (char*)descriptionPtr;
                dst[_displayDescription.Length] = (char)0;
                fixed (char* src = _displayDescription)
                    Buffer.MemoryCopy(src, dst, descriptionSize, descriptionSize);
            }

            _nativeStruct.displayData.name = namePtr;
            _nativeStruct.displayData.description = descriptionPtr;
        }

        public string DisplayName
        {
            get
            {
                return _displayName;
            }
            set
            {
                _displayName = value;
                _displayDataHandle?.Dispose();
                _displayDataHandle = null;
            }
        }

        public string DisplayDescription
        {
            get
            {
                return _displayDescription;
            }
            set
            {
                _displayDescription = value;
                _displayDataHandle?.Dispose();
                _displayDataHandle = null;
            }
        }

        public FilterFlags Flags
        {
            get { return _nativeStruct.flags; }
            set { _nativeStruct.flags = value; }
        }

        public Guid ProviderKey
        {
            get { return _providerKey; }
            set
            {
                if (_weightAndProviderKeyHandle is null)
                    throw new InvalidOperationException();

                _providerKey = value;
                PInvokeHelper.StructureToPtr(value, _nativeStruct.providerKey);
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
            get { return _weight; }
            set
            {
                if (_weightAndProviderKeyHandle is null)
                    throw new InvalidOperationException();

                _weight = value;
                PInvokeHelper.StructureToPtr(value, _nativeStruct.weight.value.uint64);
            }
        }
        public FilterConditionList Conditions
        {
            get 
            {
                // Invalidate cache
                _conditionsHandle?.Dispose();
                _conditionsHandle = null;

                return _conditions; 
            }
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
            _weightAndProviderKeyHandle?.Dispose();
            _displayDataHandle?.Dispose();
            _conditionsHandle?.Dispose();
            _conditions?.Dispose();

            _weightAndProviderKeyHandle = null;
            _displayDataHandle = null;
            _conditionsHandle = null;
            _conditions = null;
        }
    }
}
