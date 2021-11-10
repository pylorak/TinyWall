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

        private Guid? _providerKey;
        private SafeHGlobalHandle _providerKeyHandle;

        private SafeHGlobalHandle _weightHandle;

        private List<FilterCondition> _conditions;
        private SafeHGlobalHandle _conditionsHandle;

        private SafeHGlobalHandle _displayDataHandle;
        private string _DisplayName;
        private string _DisplayDescription;
        private DisplaySyncMode _DisplaySynchNeeded;

        private Filter()
        {
            _nativeStruct.providerKey = IntPtr.Zero;
            _providerKeyHandle = null;

            _weightHandle = SafeHGlobalHandle.Alloc(sizeof(ulong));
            _nativeStruct.weight.type = Interop.FWP_DATA_TYPE.FWP_UINT64;
            _nativeStruct.weight.value.uint64 = _weightHandle.DangerousGetHandle();

            _conditions = new List<FilterCondition>();
            _conditionsHandle = null;
        }

        public Filter(string name, string desc, Guid providerKey, FilterActions action, ulong weight) : this()
        {
            this.DisplayName = name;
            this.DisplayDescription = desc;
            this.ProviderKey = providerKey;
            this.Action = action;
            this.Weight = weight;
        }

        internal Filter(in Interop.FWPM_FILTER0_NoStrings filt0, bool getConditions)
        {
            _nativeStruct = filt0;
            _DisplaySynchNeeded = DisplaySyncMode.ToManaged;

            // TODO: Do we really not need to own these SafeHandles ???
            //_weightHandle = new AllocHGlobalSafeHandle(_nativeStruct.weight.value.uint64, false);
            //_conditionsHandle = new AllocHGlobalSafeHandle(_nativeStruct.filterConditions, false);

            if (_nativeStruct.providerKey != IntPtr.Zero)
            {
                // TODO: Do we really not need to own these SafeHandles ???
                //_providerKeyHandle = new AllocHGlobalSafeHandle(_nativeStruct.providerKey, false);
                _providerKey = PInvokeHelper.PtrToStructure<Guid>(_nativeStruct.providerKey);
            }

            if (getConditions)
            {
                int condSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Interop.FWPM_FILTER_CONDITION0));
                _conditions = new List<FilterCondition>((int)_nativeStruct.numFilterConditions);
                for (int i = 0; i < (int)_nativeStruct.numFilterConditions; ++i)
                {
                    IntPtr ptr = new IntPtr(_nativeStruct.filterConditions.ToInt64() + i * condSize);
                    FilterCondition cond = new FilterCondition(PInvokeHelper.PtrToStructure<Interop.FWPM_FILTER_CONDITION0>(ptr));
                    _conditions.Add(cond);
                }
            }
        }

        public Interop.FWPM_FILTER0_NoStrings Marshal()
        {
            SynchronizeDisplayData();

            if (_conditionsHandle == null)
            {
                int condSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Interop.FWPM_FILTER_CONDITION0));
                _conditionsHandle?.Dispose();
                _conditionsHandle = SafeHGlobalHandle.Alloc(_conditions.Count * condSize);
                _nativeStruct.filterConditions = _conditionsHandle.DangerousGetHandle();
                _nativeStruct.numFilterConditions = (uint)_conditions.Count;

                unsafe
                {
                    PInvokeHelper.AssertUnmanagedType<Interop.FWPM_FILTER_CONDITION0>();
                    IntPtr dst = _conditionsHandle.DangerousGetHandle();
                    int size = System.Runtime.InteropServices.Marshal.SizeOf<Interop.FWPM_FILTER_CONDITION0>();
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
            switch (_DisplaySynchNeeded)
            {
                case DisplaySyncMode.None:
                    break;
                case DisplaySyncMode.ToManaged:
                    _DisplayName = System.Runtime.InteropServices.Marshal.PtrToStringUni(_nativeStruct.displayData.name);
                    _DisplayDescription = System.Runtime.InteropServices.Marshal.PtrToStringUni(_nativeStruct.displayData.description);
                    break;
                case DisplaySyncMode.ToNative:
                    int nameSize = _DisplayName.Length * 2;
                    int descriptionSize = _DisplayDescription.Length * 2;
                    int unmanagedSize = nameSize + descriptionSize + (2 * 2);
                    _displayDataHandle?.Dispose();
                    _displayDataHandle = SafeHGlobalHandle.Alloc(unmanagedSize);
                    IntPtr namePtr = _displayDataHandle.DangerousGetHandle();
                    IntPtr descriptionPtr = namePtr + nameSize + 2;
                    unsafe
                    {
                        var dst = (char*)namePtr;
                        dst[_DisplayName.Length] = (char)0;
                        fixed (char* src = _DisplayName)
                            Buffer.MemoryCopy(src, dst, nameSize, nameSize);

                        dst = (char*)descriptionPtr;
                        dst[_DisplayDescription.Length] = (char)0;
                        fixed (char* src = _DisplayDescription)
                            Buffer.MemoryCopy(src, dst, descriptionSize, descriptionSize);
                    }
                    _nativeStruct.displayData.name = namePtr;
                    _nativeStruct.displayData.description = descriptionPtr;
                    break;
                default:
                    throw new InvalidOperationException();
            }
            _DisplaySynchNeeded = DisplaySyncMode.None;
        }

        public string DisplayName
        {
            get
            {
                SynchronizeDisplayData();
                return _DisplayName;
            }
            set
            {
                _DisplayName = value;
                _DisplaySynchNeeded = DisplaySyncMode.ToNative;
            }
        }

        public string DisplayDescription
        {
            get
            {
                SynchronizeDisplayData();
                return _DisplayDescription;
            }
            set
            {
                _DisplayDescription = value;
                _DisplaySynchNeeded = DisplaySyncMode.ToNative;
            }
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
                    _providerKeyHandle = SafeHGlobalHandle.FromStruct(value.Value);
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
            get { return PInvokeHelper.PtrToStructure<ulong>(_nativeStruct.weight.value.uint64); }
            set { PInvokeHelper.StructureToPtr(value, _nativeStruct.weight.value.uint64); }
        }
        public List<FilterCondition> Conditions
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
            _providerKeyHandle?.Dispose();
            _weightHandle?.Dispose();
            _conditionsHandle?.Dispose();
            _displayDataHandle?.Dispose();

            _providerKeyHandle = null;
            _weightHandle = null;
            _conditionsHandle = null;
        }
    }
}
