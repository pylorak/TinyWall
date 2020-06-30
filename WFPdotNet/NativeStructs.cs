using System;
using System.Runtime.InteropServices;

namespace WFPdotNet.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FWPM_DISPLAY_DATA0
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string name;
        [MarshalAs(UnmanagedType.LPWStr)] public string description;
    }

    [Flags]
    public enum FWPM_SESSION_FLAGS : uint
    {
        None = 0,
        FWPM_SESSION_FLAG_DYNAMIC = 0x00000001,
        FWPM_SESSION_FLAG_RESERVED = 0x10000000
    }

    public enum FWPM_ENGINE_OPTION
    {
        FWPM_ENGINE_COLLECT_NET_EVENTS = 0,
        FWPM_ENGINE_NET_EVENT_MATCH_ANY_KEYWORDS,
        FWPM_ENGINE_NAME_CACHE,
        FWPM_ENGINE_MONITOR_IPSEC_CONNECTIONS,
        FWPM_ENGINE_PACKET_QUEUING,
        FWPM_ENGINE_TXN_WATCHDOG_TIMEOUT_IN_MSEC,
        FWPM_ENGINE_OPTION_MAX
    }

    [Flags]
    public enum InboundEventMatchKeyword
    {
        None = 0,
        FWPM_NET_EVENT_KEYWORD_INBOUND_MCAST,
        FWPM_NET_EVENT_KEYWORD_INBOUND_BCAST
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SID_IDENTIFIER_AUTHORITY
    {
        public fixed byte Value[6];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SID
    {
        public byte Revision;
        public byte SubAuthorityCount;
        public SID_IDENTIFIER_AUTHORITY IdentifierAuthority;

        // Note 1: http://stackoverflow.com/questions/6066650/how-to-pinvoke-a-variable-length-array-of-structs-from-gettokeninformation-saf
        // Note 2: http://stackoverflow.com/questions/3939867/passing-a-structure-to-c-api-using-marshal-structuretoptr-in-c-sharp
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)] public uint[] SubAuthority;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FWPM_SESSION0
    {
        public Guid sessionKey;
        public Interop.FWPM_DISPLAY_DATA0 displayData;
        public FWPM_SESSION_FLAGS flags;
        public uint txnWaitTimeoutInMSec;
        public uint processId;
        public IntPtr sid;  // Interop.SID*
        [MarshalAs(UnmanagedType.LPWStr)] public string username;
        [MarshalAs(UnmanagedType.Bool)] public bool kernelMode;
    }

    public enum RPC_C_AUTHN : uint
    {
        RPC_C_AUTHN_WINNT = 10,
        RPC_C_AUTHN_DEFAULT = 0xFFFFFFFF
    }

    [Flags]
    public enum FWPM_PROVIDER_FLAGS : uint
    {
        FWPM_PROVIDER_FLAG_PERSISTENT = 0x00000001,
        FWPM_PROVIDER_FLAG_DISABLED = 0x00000010
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FWP_BYTE_BLOB
    {
        public uint size;
        public IntPtr data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FWPM_PROVIDER0
    {
        public Guid providerKey;
        public Interop.FWPM_DISPLAY_DATA0 displayData;
        public FWPM_PROVIDER_FLAGS flags;
        public FWP_BYTE_BLOB providerData;
        [MarshalAs(UnmanagedType.LPWStr)] public string serviceName;

        public override string ToString()
        {
            return displayData.description;
        }
    }

    [Flags]
    public enum FWPM_SUBLAYER_FLAGS : uint
    {
        FWPM_SUBLAYER_FLAG_PERSISTENT = 0x00000001
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FWPM_SUBLAYER0
    {
        public Guid subLayerKey;
        public Interop.FWPM_DISPLAY_DATA0 displayData;
        public FWPM_SUBLAYER_FLAGS flags;
        public IntPtr providerKey;
        public FWP_BYTE_BLOB providerData;
        public ushort weight;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FWP_V4_ADDR_AND_MASK
    {
        public uint addr;
        public uint mask;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FWP_V6_ADDR_AND_MASK
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = UnmanagedType.U1)]
        public byte[] addr;
        public byte prefixLength;
    }

    public struct FWP_VALUE0
    {
        public FWP_DATA_TYPE type;
        public AnonymousUnion value;

        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct AnonymousUnion
        {
            [FieldOffset(0)]
            public byte uint8;
            [FieldOffset(0)]
            public ushort uint16;
            [FieldOffset(0)]
            public uint uint32;
            [FieldOffset(0)]
            public IntPtr uint64;
            [FieldOffset(0)]
            public sbyte int8;
            [FieldOffset(0)]
            public short int16;
            [FieldOffset(0)]
            public int int32;
            [FieldOffset(0)]
            public IntPtr int64;
            [FieldOffset(0)]
            public float float32;
            [FieldOffset(0)]
            public double* double64;
            [FieldOffset(0)]
            public byte* byteArray16;
            [FieldOffset(0)]
            public FWP_BYTE_BLOB* byteBlob;
            [FieldOffset(0)]
            public IntPtr sid;  // SID*
            [FieldOffset(0)]
            public IntPtr sd;   // FWP_BYTE_BLOB*
            [FieldOffset(0)]
            public IntPtr tokenInformation;
            [FieldOffset(0)]
            public IntPtr tokenAccessInformation;   // FWP_BYTE_BLOB*
            [FieldOffset(0)]
            public IntPtr unicodeString;
            [FieldOffset(0)]
            public IntPtr byteArray6;   // byte*
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FWP_RANGE0
    {
      public FWP_VALUE0 valueLow;
      public FWP_VALUE0 valueHigh;
    }

    public enum FWP_DATA_TYPE : uint
    {
        FWP_EMPTY,
        FWP_UINT8,
        FWP_UINT16,
        FWP_UINT32,
        FWP_UINT64,
        FWP_INT8,
        FWP_INT16,
        FWP_INT32,
        FWP_INT64,
        FWP_FLOAT,
        FWP_DOUBLE,
        FWP_BYTE_ARRAY16_TYPE,
        FWP_BYTE_BLOB_TYPE,
        FWP_SID,
        FWP_SECURITY_DESCRIPTOR_TYPE,
        FWP_TOKEN_INFORMATION_TYPE,
        FWP_TOKEN_ACCESS_INFORMATION_TYPE,
        FWP_UNICODE_STRING_TYPE,
        FWP_BYTE_ARRAY6_TYPE,
        FWP_SINGLE_DATA_TYPE_MAY = 0xFF,
        FWP_V4_ADDR_MASK,
        FWP_V6_ADDR_MASK,
        FWP_RANGE_TYPE,
        FWP_DATA_TYPE_MAX
    }

    public struct FWP_CONDITION_VALUE0
    {
        public FWP_DATA_TYPE type;
        public AnonymousUnion value;

        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct AnonymousUnion
        {
            [FieldOffset(0)]
            public byte uint8;
            [FieldOffset(0)]
            public ushort uint16;
            [FieldOffset(0)]
            public uint uint32;
            [FieldOffset(0)]
            public IntPtr uint64;
            [FieldOffset(0)]
            public sbyte int8;
            [FieldOffset(0)]
            public short int16;
            [FieldOffset(0)]
            public int int32;
            [FieldOffset(0)]
            public long* int64;
            [FieldOffset(0)]
            public float float32;
            [FieldOffset(0)]
            public double* double64;
            [FieldOffset(0)]
            public IntPtr byteArray16;
            [FieldOffset(0)]
            public IntPtr byteBlob;
            [FieldOffset(0)]
            public IntPtr sid;  // SID*
            [FieldOffset(0)]
            public IntPtr sd;   // FWP_BYTE_BLOB*
            [FieldOffset(0)]
            public IntPtr tokenInformation;
            [FieldOffset(0)]
            public FWP_BYTE_BLOB* tokenAccessInformation;
            [FieldOffset(0)]
            public IntPtr unicodeString;
            [FieldOffset(0)]
            public byte* byteArray6;
            [FieldOffset(0)]
            public IntPtr v4AddrMask;   // FWP_V4_ADDR_AND_MASK*
            [FieldOffset(0)]
            public IntPtr v6AddrMask;   // FWP_V6_ADDR_AND_MASK*
            [FieldOffset(0)]
            public IntPtr rangeValue;   // FWP_RANGE0*
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FWPM_FILTER_CONDITION0
    {
        public Guid fieldKey;
        public FieldMatchType matchType;
        public Interop.FWP_CONDITION_VALUE0 conditionValue;
    }

    [Flags]
    public enum FWP_ACTION_FLAG : uint
    {
        FWP_ACTION_FLAG_TERMINATING     = 0x00001000,
        FWP_ACTION_FLAG_NON_TERMINATING = 0x00002000,
        FWP_ACTION_FLAG_CALLOUT         = 0x00004000
    }

    public enum FWP_ACTION_TYPE : uint
    {
        FWP_ACTION_BLOCK = (0x00000001 | FWP_ACTION_FLAG.FWP_ACTION_FLAG_TERMINATING),
        FWP_ACTION_PERMIT = (0x00000002 | FWP_ACTION_FLAG.FWP_ACTION_FLAG_TERMINATING),
        FWP_ACTION_CALLOUT_TERMINATING = (0x00000003 | FWP_ACTION_FLAG.FWP_ACTION_FLAG_CALLOUT | FWP_ACTION_FLAG.FWP_ACTION_FLAG_TERMINATING),
        FWP_ACTION_CALLOUT_INSPECTION = (0x00000004 | FWP_ACTION_FLAG.FWP_ACTION_FLAG_CALLOUT | FWP_ACTION_FLAG.FWP_ACTION_FLAG_NON_TERMINATING),
        FWP_ACTION_CALLOUT_UNKNOWN = (0x00000005 | FWP_ACTION_FLAG.FWP_ACTION_FLAG_CALLOUT)
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct FWPM_ACTION0
    {
        [FieldOffset(0)]
        public FWP_ACTION_TYPE type;
        [FieldOffset(4)]
        public Guid filterType;
        [FieldOffset(4)]
        public Guid calloutKey;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Filter0Context
    {
        [FieldOffset(0)]
        public ulong rawContext;
        [FieldOffset(0)]
        public Guid providerContextKey;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FWPM_FILTER0
    {
        public Guid filterKey;
        public FWPM_DISPLAY_DATA0 displayData;
        public FilterFlags flags;
        public IntPtr providerKey;
        public FWP_BYTE_BLOB providerData;
        public Guid layerKey;
        public Guid subLayerKey;
        public FWP_VALUE0 weight;
        public uint numFilterConditions;
        public IntPtr filterConditions;
        public FWPM_ACTION0 action;
        public Filter0Context filterContext;
        public IntPtr reserved;
        public ulong filterId;
        public FWP_VALUE0 effectiveWeight;
    }

    
    public enum FWP_FILTER_ENUM_TYPE : uint
    {
        FWP_FILTER_ENUM_FULLY_CONTAINED = 0,
        FWP_FILTER_ENUM_OVERLAPPING = 1,
        FWP_FILTER_ENUM_TYPE_MAX = 2
    }

    [Flags]
    public enum FilterEnumTemplateFlags : uint
    {
        FWP_FILTER_ENUM_FLAG_BEST_TERMINATING_MATCH = 0x00000001,
        FWP_FILTER_ENUM_FLAG_SORTED  = 0x00000002,
        FWP_FILTER_ENUM_FLAG_BOOTTIME_ONLY  = 0x00000004,
        FWP_FILTER_ENUM_FLAG_INCLUDE_BOOTTIME  = 0x00000008,
        FWP_FILTER_ENUM_FLAG_INCLUDE_DISABLED = 0x00000010
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FWPM_FILTER_ENUM_TEMPLATE0
    {
        public IntPtr providerKey;
        public Guid layerKey;
        public FWP_FILTER_ENUM_TYPE enumType;
        public FilterEnumTemplateFlags flags;
        public IntPtr providerContextTemplate;
        public uint numFilterConditions;
        public IntPtr filterCondition;
        public uint actionMask;
        public IntPtr calloutKey;
    }

    [Flags]
    public enum FilterSubscriptionFlags : uint
    {
        FWPM_SUBSCRIPTION_FLAG_NOTIFY_ON_ADD = 0x00000001,
        FWPM_SUBSCRIPTION_FLAG_NOTIFY_ON_DELETE = 0x00000002
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FWPM_FILTER_SUBSCRIPTION0
    {
        public IntPtr enumTemplate;
        public FilterSubscriptionFlags flags;
        public Guid sessionKey;
    }

    public enum FWPM_CHANGE_TYPE
    {
        FWPM_CHANGE_ADD = 1,
        FWPM_CHANGE_DELETE = 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FWPM_FILTER_CHANGE0
    {
        public FWPM_CHANGE_TYPE changeType;
        public Guid filterKey;
        public ulong filterId;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct FWPM_NET_EVENT_SUBSCRIPTION0
    {
        public IntPtr enumTemplate;
        public uint flags;
        public Guid sessionKey;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FILETIME
    {
        public long timestamp;
        public DateTime Local
        {
            get { return DateTime.FromFileTime(this.timestamp); }
            set { this.timestamp = value.ToFileTime(); }
        }
        public DateTime Utc
        {
            get { return DateTime.FromFileTimeUtc(this.timestamp); }
            set { this.timestamp = value.ToFileTimeUtc(); }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FWPM_NET_EVENT_ENUM_TEMPLATE0
    {
        public FILETIME startTime;
        public FILETIME endTime;
        public uint numFilterConditions;
        public IntPtr filterCondition;
    }

    public enum FWPM_NET_EVENT_TYPE
    {
        FWPM_NET_EVENT_TYPE_IKEEXT_MM_FAILURE,
        FWPM_NET_EVENT_TYPE_IKEEXT_QM_FAILURE,
        FWPM_NET_EVENT_TYPE_IKEEXT_EM_FAILURE,
        FWPM_NET_EVENT_TYPE_CLASSIFY_DROP,
        FWPM_NET_EVENT_TYPE_IPSEC_KERNEL_DROP,
        FWPM_NET_EVENT_TYPE_IPSEC_DOSP_DROP,
        FWPM_NET_EVENT_TYPE_CLASSIFY_ALLOW,
        FWPM_NET_EVENT_TYPE_CAPABILITY_DROP,
        FWPM_NET_EVENT_TYPE_CAPABILITY_ALLOW,
        FWPM_NET_EVENT_TYPE_CLASSIFY_DROP_MAC,
        FWPM_NET_EVENT_TYPE_MAX
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct InternetworkAddr
    {
        [FieldOffset(0)]
        public uint AddrV4;
        [FieldOffset(0)]
        public fixed byte AddrV6[16];

        public System.Net.IPAddress ToIpV4()
        {
            byte[] b = BitConverter.GetBytes(AddrV4);
            Array.Reverse(b);
            return new System.Net.IPAddress(b);
        }
        public System.Net.IPAddress ToIpV6()
        {
            byte[] b = new byte[16];
            unsafe
            {
                fixed (byte* srcPtr = AddrV6)
                {
                    Marshal.Copy((IntPtr)srcPtr, b, 0, 16);
                }
            }
            Array.Reverse(b);
            return new System.Net.IPAddress(b);
        }
    }

    [Flags]
    public enum NetEventHeaderValidField
    {
        FWPM_NET_EVENT_FLAG_IP_PROTOCOL_SET = 0x00000001,
        FWPM_NET_EVENT_FLAG_LOCAL_ADDR_SET = 0x00000002,
        FWPM_NET_EVENT_FLAG_REMOTE_ADDR_SET = 0x00000004,
        FWPM_NET_EVENT_FLAG_LOCAL_PORT_SET = 0x00000008,
        FWPM_NET_EVENT_FLAG_REMOTE_PORT_SET = 0x00000010,
        FWPM_NET_EVENT_FLAG_APP_ID_SET = 0x00000020,
        FWPM_NET_EVENT_FLAG_USER_ID_SET = 0x00000040,
        FWPM_NET_EVENT_FLAG_SCOPE_ID_SET = 0x00000080,
        FWPM_NET_EVENT_FLAG_IP_VERSION_SET = 0x00000100,
        FWPM_NET_EVENT_FLAG_REAUTH_REASON_SET = 0x00000200,
        FWPM_NET_EVENT_FLAG_PACKAGE_ID_SET = 0x00000400
    }

    public enum FWP_IP_VERSION
    {
        FWP_IP_VERSION_V4 = 0,
        FWP_IP_VERSION_V6 = (FWP_IP_VERSION_V4 + 1),
        FWP_IP_VERSION_NONE = (FWP_IP_VERSION_V6 + 1),
        FWP_IP_VERSION_MAX = (FWP_IP_VERSION_NONE + 1)
    }

    public struct FWPM_NET_EVENT_HEADER1
    {
        public FILETIME timeStamp;
        public NetEventHeaderValidField flag;
        public FWP_IP_VERSION ipVersion;
        public byte ipProtocol;
        public InternetworkAddr localAddr;
        public InternetworkAddr remoteAddr;
        public ushort localPort;
        public ushort remotePort;
        public uint scopeId;
        public FWP_BYTE_BLOB appId;
        public IntPtr userId;
        public AnonymousUnion1 reserved;

        [StructLayout(LayoutKind.Explicit)]
        public struct AnonymousUnion1
        {
            [FieldOffset(0)]
            public AnonymousStruct1 reserved;
        }

        public struct AnonymousStruct1
        {
            public FWP_AF reserved1;
            public AnonymousUnion2 reserved;

            [StructLayout(LayoutKind.Explicit)]
            public struct AnonymousUnion2
            {
                [FieldOffset(0)]
                public AnonymousStruct2 reserved;

                public unsafe struct AnonymousStruct2
                {
                    public fixed byte reserved2[6];
                    public fixed byte reserved3[6];
                    public int reserved4;
                    public int reserved5;
                    public short reserved6;
                    public int reserved7;
                    public int reserved8;
                    public short reserved9;
                    public long reserved10;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FWPM_NET_EVENT1
    {
        public FWPM_NET_EVENT_HEADER1 header;
        public FWPM_NET_EVENT_TYPE type;
        public IntPtr EventInfo;
    };

    public enum FWP_AF
    {
        FWP_AF_INET  = FWP_IP_VERSION.FWP_IP_VERSION_V4,
        FWP_AF_INET6 = FWP_IP_VERSION.FWP_IP_VERSION_V6,
        FWP_AF_ETHER = FWP_IP_VERSION.FWP_IP_VERSION_NONE,
        FWP_AF_NONE  = (FWP_AF_ETHER + 1)
    }

    public struct FWPM_NET_EVENT_HEADER2
    {
        public FILETIME timeStamp;
        public NetEventHeaderValidField flags;
        public FWP_IP_VERSION ipVersion;
        public byte ipProtocol;
        public InternetworkAddr localAddr;
        public InternetworkAddr remoteAddr;
        public ushort localPort;
        public ushort remotePort;
        public uint scopeId;
        public FWP_BYTE_BLOB appId;
        public IntPtr userId;
        public FWP_AF addressFamily;
        public IntPtr packageSid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FWPM_NET_EVENT2
    {
        public FWPM_NET_EVENT_HEADER2 header;
        public FWPM_NET_EVENT_TYPE type;
        public IntPtr EventInfo;
    };

    public enum FwpmDirection : uint
    {
        FWP_DIRECTION_IN = 0x00003900,
        FWP_DIRECTION_OUT = 0x00003901,
        FWP_DIRECTION_FORWARD = 0x00003902
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FWPM_NET_EVENT_CLASSIFY_DROP1
    {
        public ulong filterId;
        public ushort layerId;
        public uint reauthReason;
        public uint originalProfile;
        public uint currentProfile;
        public FwpmDirection msFwpDirection;
        [MarshalAs(UnmanagedType.Bool)]
        public bool isLoopback;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FWPM_NET_EVENT_CLASSIFY_ALLOW0
    {
        public ulong filterId;
        public ushort layerId;
        public uint reauthReason;
        public uint originalProfile;
        public uint currentProfile;
        public FwpmDirection msFwpDirection;
        [MarshalAs(UnmanagedType.Bool)]
        public bool isLoopback;
    }
}