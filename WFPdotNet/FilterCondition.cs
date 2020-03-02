using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Security.AccessControl;

namespace WFPdotNet
{
    public enum FieldMatchType
    {
        FWP_MATCH_EQUAL,
        FWP_MATCH_GREATER,
        FWP_MATCH_LESS,
        FWP_MATCH_GREATER_OR_EQUAL,
        FWP_MATCH_LESS_OR_EQUAL,
        FWP_MATCH_RANGE,
        FWP_MATCH_FLAGS_ALL_SET,
        FWP_MATCH_FLAGS_ANY_SET,
        FWP_MATCH_FLAGS_NONE_SET,
        FWP_MATCH_EQUAL_CASE_INSENSITIVE,
        FWP_MATCH_NOT_EQUAL,
        FWP_MATCH_TYPE_MAX
    }

    public enum FieldKeyNames
    {
        Unknown,
        FWPM_CONDITION_IP_LOCAL_ADDRESS,
        FWPM_CONDITION_IP_REMOTE_ADDRESS,
        FWPM_CONDITION_IP_LOCAL_PORT,
        FWPM_CONDITION_IP_REMOTE_PORT,
        FWPM_CONDITION_IP_PROTOCOL,
        FWPM_CONDITION_ALE_APP_ID,
        FWPM_CONDITION_ALE_ORIGINAL_APP_ID,
        FWPM_CONDITION_ICMP_TYPE,
        FWPM_CONDITION_ICMP_CODE,
        FWPM_CONDITION_ORIGINAL_ICMP_TYPE,
    }

    public class FilterCondition : IDisposable
    {
        protected Interop.FWPM_FILTER_CONDITION0 _nativeStruct;

        public Guid FieldKey
        {
            get { return _nativeStruct.fieldKey; }
        }

        protected FieldKeyNames? _fieldKeyName;
        public FieldKeyNames FieldKeyName
        {
            get
            {
                if (_fieldKeyName.HasValue)
                    return _fieldKeyName.Value;

                if (0 == _nativeStruct.fieldKey.CompareTo(ConditionKeys.FWPM_CONDITION_IP_LOCAL_ADDRESS))
                    _fieldKeyName = FieldKeyNames.FWPM_CONDITION_IP_LOCAL_ADDRESS;
                else if (0 == _nativeStruct.fieldKey.CompareTo(ConditionKeys.FWPM_CONDITION_IP_REMOTE_ADDRESS))
                    _fieldKeyName = FieldKeyNames.FWPM_CONDITION_IP_REMOTE_ADDRESS;
                else if (0 == _nativeStruct.fieldKey.CompareTo(ConditionKeys.FWPM_CONDITION_IP_LOCAL_PORT))
                    _fieldKeyName = FieldKeyNames.FWPM_CONDITION_IP_LOCAL_PORT;
                else if (0 == _nativeStruct.fieldKey.CompareTo(ConditionKeys.FWPM_CONDITION_IP_REMOTE_PORT))
                    _fieldKeyName = FieldKeyNames.FWPM_CONDITION_IP_REMOTE_PORT;
                else if (0 == _nativeStruct.fieldKey.CompareTo(ConditionKeys.FWPM_CONDITION_IP_PROTOCOL))
                    _fieldKeyName = FieldKeyNames.FWPM_CONDITION_IP_PROTOCOL;
                else if (0 == _nativeStruct.fieldKey.CompareTo(ConditionKeys.FWPM_CONDITION_ALE_APP_ID))
                    _fieldKeyName = FieldKeyNames.FWPM_CONDITION_ALE_APP_ID;
                else if (0 == _nativeStruct.fieldKey.CompareTo(ConditionKeys.FWPM_CONDITION_ALE_ORIGINAL_APP_ID))
                    _fieldKeyName = FieldKeyNames.FWPM_CONDITION_ALE_ORIGINAL_APP_ID;
                else if (0 == _nativeStruct.fieldKey.CompareTo(ConditionKeys.FWPM_CONDITION_ICMP_TYPE))
                    _fieldKeyName = FieldKeyNames.FWPM_CONDITION_ICMP_TYPE;
                else if (0 == _nativeStruct.fieldKey.CompareTo(ConditionKeys.FWPM_CONDITION_ICMP_CODE))
                    _fieldKeyName = FieldKeyNames.FWPM_CONDITION_ICMP_CODE;
                else if (0 == _nativeStruct.fieldKey.CompareTo(ConditionKeys.FWPM_CONDITION_ORIGINAL_ICMP_TYPE))
                    _fieldKeyName = FieldKeyNames.FWPM_CONDITION_ORIGINAL_ICMP_TYPE;
                else
                    _fieldKeyName = FieldKeyNames.Unknown;

                return _fieldKeyName.Value;
            }
        }

        public FieldMatchType MatchType
        {
            get { return _nativeStruct.matchType; }
            set
            {
                if ((FieldMatchType.FWP_MATCH_NOT_EQUAL == value) && !VersionInfo.Win7OrNewer)
                    throw new NotSupportedException("FWP_MATCH_NOT_EQUAL requires Windows 7 or newer.");

                _nativeStruct.matchType = value;
            }
        }

        public Interop.FWP_CONDITION_VALUE0 ConditionValue
        {
            get { return _nativeStruct.conditionValue; }
        }

        protected FilterCondition()
        {
            _nativeStruct = new Interop.FWPM_FILTER_CONDITION0();
        }

        internal FilterCondition(Interop.FWPM_FILTER_CONDITION0 cond0)
        {
            _nativeStruct = cond0;
        }

        public FilterCondition(Guid fieldKey, FieldMatchType matchType, Interop.FWP_CONDITION_VALUE0 conditionValue)
        {
            _nativeStruct.fieldKey = fieldKey;
            _nativeStruct.matchType = matchType;
            _nativeStruct.conditionValue = conditionValue;
        }

        internal Interop.FWPM_FILTER_CONDITION0 Marshal()
        {
            return _nativeStruct;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) { }
    }

    public enum RemoteOrLocal
    {
        Remote,
        Local
    }

    public sealed class IpFilterCondition : FilterCondition
    {
        private static readonly byte[] MaskByteBitsLookup = new byte[]
        { 0x00, 0x80, 0xC0, 0xE0, 0xF0, 0xF8, 0xFC, 0xFE, 0xFF };

        private AllocHGlobalSafeHandle nativeMem;

        public IpFilterCondition(IPAddress addr, RemoteOrLocal peer)
        {
            _nativeStruct.matchType = FieldMatchType.FWP_MATCH_EQUAL;
            _nativeStruct.fieldKey = (peer == RemoteOrLocal.Local) ? ConditionKeys.FWPM_CONDITION_IP_LOCAL_ADDRESS : ConditionKeys.FWPM_CONDITION_IP_REMOTE_ADDRESS;

            byte[] addressBytes = addr.GetAddressBytes();
            Array.Reverse(addressBytes);

            switch (addr.AddressFamily)
            {
                case System.Net.Sockets.AddressFamily.InterNetwork:
                    _nativeStruct.conditionValue.type = Interop.FWP_DATA_TYPE.FWP_UINT32;
                    _nativeStruct.conditionValue.uint32 = BitConverter.ToUInt32(addressBytes, 0);
                    break;
                case System.Net.Sockets.AddressFamily.InterNetworkV6:
                    nativeMem = new AllocHGlobalSafeHandle(16);
                    IntPtr ptr = nativeMem.DangerousGetHandle();
                    _nativeStruct.conditionValue.type = Interop.FWP_DATA_TYPE.FWP_BYTE_ARRAY16_TYPE;
                    _nativeStruct.conditionValue.byteArray16 = ptr;
                    System.Runtime.InteropServices.Marshal.Copy(addressBytes, 0, ptr, 16);
                    break;
                default:
                    throw new NotSupportedException("Only the IPv4 and Ipv6 address families are supported.");
            }
        }

        public IpFilterCondition(IPAddress addr, byte subnetLen, RemoteOrLocal peer)
        {
            if (((addr.AddressFamily == AddressFamily.InterNetwork) && (subnetLen > 32))
             || ((addr.AddressFamily == AddressFamily.InterNetworkV6) && (subnetLen > 128)))
                throw new ArgumentOutOfRangeException("Subnet length out of range for the address family.");

            _nativeStruct.matchType = FieldMatchType.FWP_MATCH_EQUAL;
            _nativeStruct.fieldKey = (peer == RemoteOrLocal.Local) ? ConditionKeys.FWPM_CONDITION_IP_LOCAL_ADDRESS : ConditionKeys.FWPM_CONDITION_IP_REMOTE_ADDRESS;

            byte[] addressBytes = addr.GetAddressBytes();

            switch (addr.AddressFamily)
            {
                case System.Net.Sockets.AddressFamily.InterNetwork:
                    // Convert CIDR subnet length to byte array
                    byte[] maskBytes = new byte[4];
                    int prefix = subnetLen;
                    for (int i = 0; i < maskBytes.Length; ++i)
                    {
                        int s = (prefix < 8) ? prefix : 8;
                        maskBytes[i] = MaskByteBitsLookup[s];
                        prefix -= s;
                    }
                    Array.Reverse(maskBytes);
                    Array.Reverse(addressBytes);

                    Interop.FWP_V4_ADDR_AND_MASK addrAndMask4 = new Interop.FWP_V4_ADDR_AND_MASK();
                    addrAndMask4.addr = BitConverter.ToUInt32(addressBytes, 0);
                    addrAndMask4.mask = BitConverter.ToUInt32(maskBytes, 0);
                    nativeMem = PInvokeHelper.StructToHGlobal<Interop.FWP_V4_ADDR_AND_MASK>(addrAndMask4);

                    _nativeStruct.conditionValue.type = Interop.FWP_DATA_TYPE.FWP_V4_ADDR_MASK;
                    _nativeStruct.conditionValue.v4AddrMask = nativeMem.DangerousGetHandle();
                    break;
                case System.Net.Sockets.AddressFamily.InterNetworkV6:
                    Interop.FWP_V6_ADDR_AND_MASK addrAndMask6 = new Interop.FWP_V6_ADDR_AND_MASK();
                    addrAndMask6.addr = addressBytes;
                    addrAndMask6.prefixLength = subnetLen;
                    nativeMem = PInvokeHelper.StructToHGlobal<Interop.FWP_V6_ADDR_AND_MASK>(addrAndMask6);
                    
                    _nativeStruct.conditionValue.type = Interop.FWP_DATA_TYPE.FWP_V6_ADDR_MASK;
                    _nativeStruct.conditionValue.v6AddrMask = nativeMem.DangerousGetHandle();
                    break;
                default:
                    throw new NotSupportedException("Only the IPv4 and IPv6 address families are supported.");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                nativeMem?.Dispose();
                nativeMem = null;
            }

            base.Dispose(disposing);
        }
    }
    
    public sealed class PortFilterCondition : FilterCondition
    {
        private AllocHGlobalSafeHandle rangeNativeMem;

        private PortFilterCondition(RemoteOrLocal peer)
        {
            _nativeStruct.fieldKey = (peer == RemoteOrLocal.Local) ? ConditionKeys.FWPM_CONDITION_IP_LOCAL_PORT : ConditionKeys.FWPM_CONDITION_IP_REMOTE_PORT;
        }

        public PortFilterCondition(ushort portNumber, RemoteOrLocal peer)
            : this(peer)
        {
            Init(portNumber);
        }

        public PortFilterCondition(ushort minPort, ushort maxPort, RemoteOrLocal peer)
            : this(peer)
        {
            Init(minPort, maxPort);
        }

        public PortFilterCondition(string portOrRange, RemoteOrLocal peer)
            : this(peer)
        {
            bool isRange = (-1 != portOrRange.IndexOf('-'));
            if (isRange)
            {
                string[] minmax = portOrRange.Split('-');
                ushort min = ushort.Parse(minmax[0], System.Globalization.CultureInfo.InvariantCulture);
                ushort max = ushort.Parse(minmax[1], System.Globalization.CultureInfo.InvariantCulture);
                Init(min, max);
            }
            else
            {
                ushort portNumber = ushort.Parse(portOrRange, System.Globalization.CultureInfo.InvariantCulture);
                Init(portNumber);
            }
        }

        private void Init(ushort portNumber)
        {
            _nativeStruct.matchType = FieldMatchType.FWP_MATCH_EQUAL;
            _nativeStruct.conditionValue.type = Interop.FWP_DATA_TYPE.FWP_UINT16;
            _nativeStruct.conditionValue.uint16 = portNumber;
        }

        private void Init(ushort minPort, ushort maxPort)
        {
            _nativeStruct.matchType = FieldMatchType.FWP_MATCH_RANGE;

            Interop.FWP_RANGE0 range = new Interop.FWP_RANGE0();
            range.valueLow.type = Interop.FWP_DATA_TYPE.FWP_UINT16;
            range.valueLow.uint16 = minPort;
            range.valueHigh.type = Interop.FWP_DATA_TYPE.FWP_UINT16;
            range.valueHigh.uint16 = maxPort;

            rangeNativeMem = PInvokeHelper.StructToHGlobal<Interop.FWP_RANGE0>(range);
            _nativeStruct.conditionValue.type = Interop.FWP_DATA_TYPE.FWP_RANGE_TYPE;
            _nativeStruct.conditionValue.rangeValue = rangeNativeMem.DangerousGetHandle();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                rangeNativeMem?.Dispose();
                rangeNativeMem = null;
            }

            base.Dispose(disposing);
        }
    }

    // RFC 1700
    public enum IpProtocol : byte
    {
        HOPOPT = 0,
        ICMPv4 = 1,
        IGMP = 2,
        TCP = 6,
        UDP = 17,
        GRE = 47,
        ESP = 50,
        AH = 51,
        ICMPv6 = 58
    }

    public sealed class ProtocolFilterCondition : FilterCondition
    {
        public ProtocolFilterCondition(byte proto)
        {
            Init(proto);
        }

        public ProtocolFilterCondition(IpProtocol proto)
        {
            Init((byte)proto);
        }

        private void Init(byte proto)
        {
            _nativeStruct.matchType = FieldMatchType.FWP_MATCH_EQUAL;
            _nativeStruct.fieldKey = ConditionKeys.FWPM_CONDITION_IP_PROTOCOL;
            _nativeStruct.conditionValue.type = Interop.FWP_DATA_TYPE.FWP_UINT8;
            _nativeStruct.conditionValue.uint8 = proto;
        }
    }

    public sealed class AppIdFilterCondition : FilterCondition
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class NativeMethods
        {
            [DllImport("FWPUClnt.dll", EntryPoint = "FwpmGetAppIdFromFileName0")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern uint FwpmGetAppIdFromFileName0(
                [MarshalAs(UnmanagedType.LPWStr), In]  string fileName,
                [Out] out FwpmMemorySafeHandle appId);
        }

        private FwpmMemorySafeHandle appIdNativeMem;

        public AppIdFilterCondition(string filePath, bool bBeforeProxying = false)
        {
            if (bBeforeProxying && !VersionInfo.Win8OrNewer)
                throw new NotSupportedException("FWPM_CONDITION_ALE_ORIGINAL_APP_ID (set by bBeforeProxying) requires Windows 8 or newer.");

            uint err = NativeMethods.FwpmGetAppIdFromFileName0(filePath, out appIdNativeMem);
            if (0 != err)
                throw new WfpException(err, "FwpmGetAppIdFromFileName0");

            _nativeStruct.matchType = FieldMatchType.FWP_MATCH_EQUAL;
            _nativeStruct.fieldKey = bBeforeProxying ? ConditionKeys.FWPM_CONDITION_ALE_ORIGINAL_APP_ID : ConditionKeys.FWPM_CONDITION_ALE_APP_ID;
            _nativeStruct.conditionValue.type = Interop.FWP_DATA_TYPE.FWP_BYTE_BLOB_TYPE;
            _nativeStruct.conditionValue.byteBlob = appIdNativeMem.DangerousGetHandle();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                appIdNativeMem?.Dispose();
                appIdNativeMem = null;
            }

            base.Dispose(disposing);
        }
    }

    public abstract class SecurityDescriptorFilterCondition : FilterCondition
    {
        private AllocHGlobalSafeHandle byteBlobNativeMem;
        private AllocHGlobalSafeHandle sdNativeMem;

        protected SecurityDescriptorFilterCondition() { }

        protected void Init(Guid fieldKey, RawSecurityDescriptor sd, FieldMatchType matchType)
        {
            // Get the SD in SDDL self-related form into an unmanaged pointer
            byte[] sdBinaryForm = new byte[sd.BinaryLength];
            sd.GetBinaryForm(sdBinaryForm, 0);
            sdNativeMem = new AllocHGlobalSafeHandle(sd.BinaryLength);
            System.Runtime.InteropServices.Marshal.Copy(sdBinaryForm, 0, sdNativeMem.DangerousGetHandle(), sd.BinaryLength);

            //  Create FWP_BYTE_BLOB for the SD
            Interop.FWP_BYTE_BLOB blob = new Interop.FWP_BYTE_BLOB();
            blob.size = (uint)sd.BinaryLength;
            blob.data = sdNativeMem.DangerousGetHandle();
            byteBlobNativeMem = PInvokeHelper.StructToHGlobal<Interop.FWP_BYTE_BLOB>(blob);

            _nativeStruct.matchType = matchType;
            _nativeStruct.fieldKey = fieldKey;
            _nativeStruct.conditionValue.type = Interop.FWP_DATA_TYPE.FWP_SECURITY_DESCRIPTOR_TYPE;
            _nativeStruct.conditionValue.sd = byteBlobNativeMem.DangerousGetHandle();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                byteBlobNativeMem?.Dispose();
                sdNativeMem?.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    public sealed class ServiceNameFilterCondition : SecurityDescriptorFilterCondition
    {
        public ServiceNameFilterCondition(string serviceName, FieldMatchType matchType)
        {
            // Get service SID
            string serviceSid = GetServiceSidFromName(serviceName);

            // Put service SID into SDDL form
            string sddl = string.Format("O:SYG:SYD:(A;;CCRC;;;{0})", serviceSid);

            // Construct condition from security descriptor
            Init(ConditionKeys.FWPM_CONDITION_ALE_USER_ID, new RawSecurityDescriptor(sddl), matchType);
        }

        public ServiceNameFilterCondition(string serviceName) : this(serviceName, FieldMatchType.FWP_MATCH_EQUAL)
        {
        }

        private string GetServiceSidFromName(string serviceName)
        {
#if false
            /*
             * This piece of code is the "standard" solution, but it only
             * allows retrieval of service SIDs for already installed services.
             * Also, it is about 8x slower.
             */
            NTAccount f = new NTAccount(@"NT SERVICE\" + serviceName);
            SecurityIdentifier sid = (SecurityIdentifier)f.Translate(typeof(SecurityIdentifier));
            return sid.ToString();
#endif

            // For the steps of converting a service name to a service SID, see:
            // https://pcsxcetrasupport3.wordpress.com/2013/09/08/how-do-you-get-a-service-sid-from-a-service-name/

            // 1: Input service same.
            // haha

            // 2: Convert service name to upper case.
            serviceName = serviceName.ToUpperInvariant();

            // 3: Get the Unicode bytes()  from the upper case service name.
            byte[] unicode = System.Text.UnicodeEncoding.Unicode.GetBytes(serviceName);

            // 4: Run bytes() thru the sha1 hash function.
            byte[] sha1 = null;
            using (System.Security.Cryptography.SHA1Managed hasher = new System.Security.Cryptography.SHA1Managed())
            {
                sha1 = hasher.ComputeHash(unicode);
            }

            // 5: Reverse the byte() string  returned from the SHA1 hash function(on Little Endian systems Not tested on Big Endian systems)
            // Optimized away by reversing array order in steps 7 and 10.

            // 6: Split the reversed string into 5 blocks of 4 bytes each.
            uint[] dec = new uint[5];
            for (int i = 0; i < dec.Length; ++i)
            {
                // 7: Convert each block of hex bytes() to Decimal
                string hexBlock =sha1[i * 4 + 3].ToString("X") +
                    sha1[i * 4 + 2].ToString("X") +
                    sha1[i * 4 + 1].ToString("X") +
                    sha1[i * 4 + 0].ToString("X");

                dec[i] = Convert.ToUInt32(hexBlock, 16);
            }

            // 8: Reverse the Position of the blocks.
            // 9: Create the first part of the SID "S-1-5-80-"
            // 10: Tack on each block of Decimal strings with a "-" in between each block that was converted and reversed.
            // 11: Finally out put the complete SID for the service.
            string serviceSid = string.Format("S-1-5-80-{0}-{1}-{2}-{3}-{4}",
                dec[0],
                dec[1],
                dec[2],
                dec[3],
                dec[4]
            );

            return serviceSid;
        }
    }

    public sealed class UserIdFilterCondition : SecurityDescriptorFilterCondition
    {
        public UserIdFilterCondition(string sid, RemoteOrLocal peer)
        {
            string sddl = string.Format("O:LSD:(A;;CC;;;{0}))", sid);
            Init(
                (RemoteOrLocal.Local == peer) ? ConditionKeys.FWPM_CONDITION_ALE_USER_ID : ConditionKeys.FWPM_CONDITION_ALE_REMOTE_USER_ID,
                new RawSecurityDescriptor(sddl),
                FieldMatchType.FWP_MATCH_EQUAL
            );
        }
    }

    public sealed class PackageIdFilterCondition : FilterCondition
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class NativeMethods
        {
            [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool ConvertStringSidToSid(string stringSid, out AllocHLocalSafeHandle ptrSid);

            [DllImport("userenv", SetLastError = false, CharSet = CharSet.Unicode)]
            internal static extern int DeriveAppContainerSidFromAppContainerName(string appContainerName, out SidSafeHandle sid);
        }

        private SafeHandle sidNativeMem = null;

        public PackageIdFilterCondition(IntPtr sid)
        {
            if (!VersionInfo.Win8OrNewer)
                throw new NotSupportedException("FWPM_CONDITION_ALE_PACKAGE_ID requires Windows 8 or newer.");

            Init(PInvokeHelper.CopyNativeSid(sid));
        }

        public PackageIdFilterCondition(string sid)
        {
            if (!VersionInfo.Win8OrNewer)
                throw new NotSupportedException("FWPM_CONDITION_ALE_PACKAGE_ID requires Windows 8 or newer.");

            if (!NativeMethods.ConvertStringSidToSid(sid, out AllocHLocalSafeHandle tmpHndl))
                throw new Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());

            Init(tmpHndl);
        }

        private PackageIdFilterCondition(SafeHandle sid)
        {
            if (!VersionInfo.Win8OrNewer)
                throw new NotSupportedException("FWPM_CONDITION_ALE_PACKAGE_ID requires Windows 8 or newer.");

            Init(sid);
        }

        private void Init(SafeHandle sidHandle)
        {
            sidNativeMem = sidHandle;

            _nativeStruct.matchType = FieldMatchType.FWP_MATCH_EQUAL;
            _nativeStruct.fieldKey = ConditionKeys.FWPM_CONDITION_ALE_PACKAGE_ID;
            _nativeStruct.conditionValue.type = Interop.FWP_DATA_TYPE.FWP_SID;
            _nativeStruct.conditionValue.sd = sidNativeMem.DangerousGetHandle();
        }

        public static PackageIdFilterCondition FromPackageFamilyName(string packageFamilyName)
        {
            if (!VersionInfo.Win8OrNewer)
                throw new NotSupportedException("FWPM_CONDITION_ALE_PACKAGE_ID requires Windows 8 or newer.");

            if (0 != NativeMethods.DeriveAppContainerSidFromAppContainerName(packageFamilyName, out SidSafeHandle tmpHndl))
                throw new ArgumentException();

            return new PackageIdFilterCondition(tmpHndl);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                sidNativeMem?.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    [Flags]
    public enum ConditionFlags : uint
    {
        FWP_CONDITION_FLAG_IS_LOOPBACK = 0x00000001,
        FWP_CONDITION_FLAG_IS_IPSEC_SECURED = 0x00000002,
        FWP_CONDITION_FLAG_IS_REAUTHORIZE = 0x00000004,
        FWP_CONDITION_FLAG_IS_WILDCARD_BIND = 0x00000008,
        FWP_CONDITION_FLAG_IS_RAW_ENDPOINT = 0x00000010,
        FWP_CONDITION_FLAG_IS_FRAGMENT = 0x00000020,
        FWP_CONDITION_FLAG_IS_FRAGMENT_GROUP = 0x00000040,
        FWP_CONDITION_FLAG_IS_IPSEC_NATT_RECLASSIFY = 0x00000080,
        FWP_CONDITION_FLAG_REQUIRES_ALE_CLASSIFY = 0x00000100,
        FWP_CONDITION_FLAG_IS_IMPLICIT_BIND = 0x00000200,
        FWP_CONDITION_FLAG_IS_REASSEMBLED = 0x00000400,
        FWP_CONDITION_FLAG_IS_NAME_APP_SPECIFIED = 0x00004000,
        FWP_CONDITION_FLAG_IS_PROMISCUOUS = 0x00008000,
        FWP_CONDITION_FLAG_IS_AUTH_FW = 0x00010000,
        FWP_CONDITION_FLAG_IS_RECLASSIFY = 0x00020000,
        FWP_CONDITION_FLAG_IS_OUTBOUND_PASS_THRU = 0x00040000,
        FWP_CONDITION_FLAG_IS_INBOUND_PASS_THRU = 0x00080000,
        FWP_CONDITION_FLAG_IS_CONNECTION_REDIRECTED = 0x00100000,
        FWP_CONDITION_FLAG_IS_PROXY_CONNECTION = 0x00200000,
        FWP_CONDITION_FLAG_IS_APPCONTAINER_LOOPBACK = 0x00400000,
        FWP_CONDITION_FLAG_IS_NON_APPCONTAINER_LOOPBACK = 0x00800000,
        FWP_CONDITION_FLAG_IS_RESERVED = 0x01000000,
        FWP_CONDITION_FLAG_IS_HONORING_POLICY_AUTHORIZE = 0x02000000
    }

    public sealed class FlagsFilterCondition : FilterCondition
    {
        public FlagsFilterCondition(ConditionFlags flags, FieldMatchType matchType)
        {
            _nativeStruct.matchType = matchType;
            _nativeStruct.fieldKey = ConditionKeys.FWPM_CONDITION_FLAGS;
            _nativeStruct.conditionValue.type = Interop.FWP_DATA_TYPE.FWP_UINT32;
            _nativeStruct.conditionValue.uint32 = (uint)flags;
        }
    }

    public enum SioRcvAll : uint
    {
        SIO_RCVALL = (uint)2550136833u,
        SIO_RCVALL_MCAST = (uint)2550136834u,
        SIO_RCVALL_IGMPMCAST = (uint)2550136835u
    }
}