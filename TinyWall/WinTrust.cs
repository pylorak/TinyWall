using System;
using System.Runtime.InteropServices;

namespace PKSoft
{
    internal sealed class WinTrust
    {
        private enum WinTrustDataUIChoice : uint
        {
            All = 1,
            None = 2,
            NoBad = 3,
            NoGood = 4
        }


        public enum WinTrustDataRevocationChecks : uint
        {
            None = 0x00000000,
            WholeChain = 0x00000001
        }

        private enum WinTrustDataChoice : uint
        {
            File = 1,
            Catalog = 2,
            Blob = 3,
            Signer = 4,
            Certificate = 5
        }

        private enum WinTrustDataStateAction : uint
        {
            Ignore = 0x00000000,
            Verify = 0x00000001,
            Close = 0x00000002,
            AutoCache = 0x00000003,
            AutoCacheFlush = 0x00000004
        }

        [FlagsAttribute]
        private enum WinTrustDataProvFlags : uint
        {
            UseIe4TrustFlag = 0x00000001,
            NoIe4ChainFlag = 0x00000002,
            NoPolicyUsageFlag = 0x00000004,
            RevocationCheckNone = 0x00000010,
            RevocationCheckEndCert = 0x00000020,
            RevocationCheckChain = 0x00000040,
            RevocationCheckChainExcludeRoot = 0x00000080,
            SaferFlag = 0x00000100,
            HashOnlyFlag = 0x00000200,
            UseDefaultOsverCheck = 0x00000400,
            LifetimeSigningFlag = 0x00000800,
            CacheOnlyUrlRetrieval = 0x00001000 // affects CRL retrieval and AIA retrieval
        }
        private enum WinTrustDataUIContext : uint
        {
            Execute = 0,
            Install = 1
        }
        private enum WinVerifyTrustResult : uint
        {
            Success = 0,
            ProviderUnknown = 0x800b0001, // The trust provider is not recognized on this system
            ActionUnknown = 0x800b0002, // The trust provider does not support the specified action
            SubjectFormUnknown = 0x800b0003, // The trust provider does not support the form specified for the subject
            SubjectNotTrusted = 0x800b0004 // The subject failed the specified verification action
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        class WinTrustFileInfo
        {
            UInt32 StructSize = (UInt32)Marshal.SizeOf(typeof(WinTrustFileInfo));
            IntPtr pszFilePath; // required, file name to be verified
            IntPtr hFile = IntPtr.Zero; // optional, open handle to FilePath
            IntPtr pgKnownSubject = IntPtr.Zero; // optional, subject type if it is known


            public WinTrustFileInfo(String _filePath)
            {
                pszFilePath = Marshal.StringToCoTaskMemAuto(_filePath);
            }
            ~WinTrustFileInfo()
            {
                Marshal.FreeCoTaskMem(pszFilePath);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        class WinTrustData
        {
            UInt32 StructSize = (UInt32)Marshal.SizeOf(typeof(WinTrustData));
            IntPtr PolicyCallbackData = IntPtr.Zero;
            IntPtr SIPClientData = IntPtr.Zero;
            // required: UI choice
            WinTrustDataUIChoice UIChoice = WinTrustDataUIChoice.None;
            // required: certificate revocation check options
            WinTrustDataRevocationChecks RevocationChecks = WinTrustDataRevocationChecks.WholeChain;
            // required: which structure is being passed in?
            WinTrustDataChoice UnionChoice = WinTrustDataChoice.File;
            // individual file
            IntPtr FileInfoPtr;
            WinTrustDataStateAction StateAction = WinTrustDataStateAction.Ignore;
            IntPtr StateData = IntPtr.Zero;
            String URLReference = null;
            WinTrustDataProvFlags ProvFlags = WinTrustDataProvFlags.CacheOnlyUrlRetrieval | WinTrustDataProvFlags.RevocationCheckChain;
            WinTrustDataUIContext UIContext = WinTrustDataUIContext.Execute;
            // constructor for silent WinTrustDataChoice.File check
            public WinTrustData(String _fileName, WinTrustDataRevocationChecks revocationChecks)
            {
                RevocationChecks = revocationChecks;
                WinTrustFileInfo wtfiData = new WinTrustFileInfo(_fileName);
                FileInfoPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(WinTrustFileInfo)));
                Marshal.StructureToPtr(wtfiData, FileInfoPtr, false);
            }
            ~WinTrustData()
            {
                Marshal.FreeCoTaskMem(FileInfoPtr);
            }
        }

        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        // GUID of the action to perform
        public static readonly Guid DRIVER_ACTION_VERIFY = new Guid("{F750E6C3-38EE-11d1-85E5-00C04FC295EE}");
        public static readonly Guid HTTPSPROV_ACTION = new Guid("{573E31F8-AABA-11d0-8CCB-00C04FC295EE}");
        public static readonly Guid OFFICESIGN_ACTION_VERIFY = new Guid("{5555C2CD-17FB-11d1-85C4-00C04FC295EE}");
        public static readonly Guid WINTRUST_ACTION_GENERIC_CERT_VERIFY = new Guid("{189A3842-3041-11d1-85E1-00C04FC295EE}");
        public static readonly Guid WINTRUST_ACTION_GENERIC_CHAIN_VERIFY = new Guid("{fc451c16-ac75-11d1-b4b8-00c04fb66ea0}");
        public static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 = new Guid("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}");
        public static readonly Guid WINTRUST_ACTION_TRUSTPROVIDER_TEST = new Guid("{573E31F8-DDBA-11d0-8CCB-00C04FC295EE}");

        [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern WinVerifyTrustResult WinVerifyTrust(
        [In] IntPtr hwnd,
        [In] [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID,
        [In] WinTrustData pWVTData
        );

        private WinTrust() { }

        ///
        /// Calls WinTrust.WinVerifyTrust() to check embedded file signature
        ///
        /// absolute path and file name
        /// validation to perform
        /// enumeration
        /// true if the signature is valid, otherwise false
        internal static bool VerifyEmbeddedSignature(string fileName, Guid guidAction, WinTrustDataRevocationChecks revocationChecks)
        {
            WinTrustData wtd = new WinTrustData(fileName, revocationChecks);
            WinVerifyTrustResult result = WinVerifyTrust(INVALID_HANDLE_VALUE, guidAction, wtd);
            return (result == WinVerifyTrustResult.Success);
        }

        internal static bool VerifyFileAuthenticode(string filePath)
        {
            return VerifyEmbeddedSignature(filePath, WINTRUST_ACTION_GENERIC_VERIFY_V2, WinTrustDataRevocationChecks.WholeChain);
        }
    }
}
