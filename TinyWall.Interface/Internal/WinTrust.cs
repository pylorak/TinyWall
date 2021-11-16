using System;
using System.Security;
using System.Runtime.InteropServices;

namespace TinyWall.Interface.Internal
{
    public static class WinTrust
    {
        public enum VerifyResult
        {
            SIGNATURE_MISSING,
            SIGNATURE_VALID,
            SIGNATURE_INVALID
        }

        private enum WinTrustDataUIChoice : uint
        {
            All = 1,
            None = 2,
            NoBad = 3,
            NoGood = 4
        }

        private enum WinTrustDataRevocationChecks : uint
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

        [Flags]
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
            CacheOnlyUrlRetrieval = 0x00001000,
            DisableMD2andMD4 = 0x00002000      // Win7 SP1+: Disallows use of MD2 or MD4 in the chain except for the root 
        }
        private enum WinTrustDataUIContext : uint
        {
            Execute = 0,
            Install = 1
        }
        private enum WinVerifyTrustResult : uint
        {
            TRUST_SUCCESS = 0u,
            TRUST_E_NOSIGNATURE = 0x800B0100u,
            TRUST_E_SUBJECT_NOT_TRUSTED = 0x800B0004u,
            TRUST_E_PROVIDER_UNKNOWN = 0x800B0001u,
            TRUST_E_ACTION_UNKNOWN = 0x800B0002u,
            TRUST_E_SUBJECT_FORM_UNKNOWN = 0x800B0003u,
            CRYPT_E_SECURITY_SETTINGS = 0x80092026u,
            CRYPT_E_FILE_ERROR = 0x80092003u
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private readonly struct WinTrustFileInfo
        {
            readonly uint StructSize;
            [MarshalAs(UnmanagedType.LPWStr)] readonly string pszFilePath;
            readonly IntPtr hFile;
            readonly IntPtr pgKnownSubject;

            internal WinTrustFileInfo(string path)
            {
                StructSize = (uint)Marshal.SizeOf(typeof(WinTrustFileInfo));
                pszFilePath = path;
                hFile = IntPtr.Zero;
                pgKnownSubject = IntPtr.Zero;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class WinTrustData : Disposable
        {
            public uint StructSize = (uint)Marshal.SizeOf(typeof(WinTrustData));
            public IntPtr PolicyCallbackData = IntPtr.Zero;
            public IntPtr SIPClientData = IntPtr.Zero;
            public WinTrustDataUIChoice UIChoice = WinTrustDataUIChoice.None;
            public WinTrustDataRevocationChecks RevocationChecks = WinTrustDataRevocationChecks.WholeChain;
            public WinTrustDataChoice UnionChoice = WinTrustDataChoice.File;
            public IntPtr FileInfoPtr;
            public WinTrustDataStateAction StateAction = WinTrustDataStateAction.Verify;
            public IntPtr StateData = IntPtr.Zero;
            public string? URLReference;
            public WinTrustDataProvFlags ProvFlags = WinTrustDataProvFlags.CacheOnlyUrlRetrieval | WinTrustDataProvFlags.RevocationCheckChain;
            public WinTrustDataUIContext UIContext = WinTrustDataUIContext.Execute;

            public WinTrustData(string fileName, WinTrustDataRevocationChecks revocationChecks)
            {
                // On Win7SP1+, don't allow MD2 or MD4 signatures
                if ((Environment.OSVersion.Version.Major > 6) ||
                    ((Environment.OSVersion.Version.Major == 6) && (Environment.OSVersion.Version.Minor > 1)) ||
                    ((Environment.OSVersion.Version.Major == 6) && (Environment.OSVersion.Version.Minor == 1) && !String.IsNullOrEmpty(Environment.OSVersion.ServicePack)))
                {
                    ProvFlags |= WinTrustDataProvFlags.DisableMD2andMD4;
                }

                RevocationChecks = revocationChecks;

                var wtfiData = new WinTrustFileInfo(fileName);
                FileInfoPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(WinTrustFileInfo)));
                Marshal.StructureToPtr(wtfiData, FileInfoPtr, false);
            }

            protected override void Dispose(bool disposing)
            {
                if (IsDisposed)
                    return;

                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                Marshal.DestroyStructure(FileInfoPtr, typeof(WinTrustFileInfo));
                Marshal.FreeCoTaskMem(FileInfoPtr);

                base.Dispose(disposing);
            }

            ~WinTrustData() => Dispose(false);
        }

        //private static readonly Guid DRIVER_ACTION_VERIFY                   = new(0xf750e6c3, 0x38ee, 0x11d1, 0x85, 0xe5, 0x0, 0xc0, 0x4f, 0xc2, 0x95, 0xee);
        //private static readonly Guid HTTPSPROV_ACTION                       = new(0x573e31f8, 0xaaba, 0x11d0, 0x8c, 0xcb, 0x0, 0xc0, 0x4f, 0xc2, 0x95, 0xee);
        //private static readonly Guid OFFICESIGN_ACTION_VERIFY               = new(0x5555c2cd, 0x17fb, 0x11d1, 0x85, 0xc4, 0x0, 0xc0, 0x4f, 0xc2, 0x95, 0xee);
        //private static readonly Guid WINTRUST_ACTION_GENERIC_CHAIN_VERIFY   = new(0xfc451c16, 0xac75, 0x11d1, 0xb4, 0xb8, 0x0, 0xc0, 0x4f, 0xb6, 0x6e, 0xa0);
        private static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2      = new(0x00aac56b, 0xcd44, 0x11d0, 0x8c, 0xc2, 0x0, 0xc0, 0x4f, 0xc2, 0x95, 0xee);
        //private static readonly Guid WINTRUST_ACTION_TRUSTPROVIDER_TEST     = new(0x573e31f8, 0xddba, 0x11d0, 0x8c, 0xcb, 0x0, 0xc0, 0x4f, 0xc2, 0x95, 0xee);

        [SuppressUnmanagedCodeSecurity]
        private static class SafeNativeMethods
        {
            [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern WinVerifyTrustResult WinVerifyTrust(
                [In] IntPtr hwnd,
                [In] [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID,
                [In] WinTrustData pWVTData
            );
        }

        private static VerifyResult VerifyEmbeddedSignature(string fileName, Guid guidAction, WinTrustDataRevocationChecks revocationChecks)
        {
            using var wtd = new WinTrustData(fileName, revocationChecks);
            WinVerifyTrustResult lStatus = SafeNativeMethods.WinVerifyTrust(IntPtr.Zero, guidAction, wtd);

            // Any hWVTStateData must be released by a call with close.
            wtd.StateAction = WinTrustDataStateAction.Close;
            SafeNativeMethods.WinVerifyTrust(IntPtr.Zero, guidAction, wtd);

            switch (lStatus)
            {
                case WinVerifyTrustResult.TRUST_SUCCESS:
                    return VerifyResult.SIGNATURE_VALID;
                case WinVerifyTrustResult.CRYPT_E_FILE_ERROR:
                    return VerifyResult.SIGNATURE_MISSING;
                default:
                    uint dwLastError;
                    unchecked { dwLastError = (uint)Marshal.GetLastWin32Error(); }
                    if (((uint)WinVerifyTrustResult.TRUST_E_NOSIGNATURE == dwLastError) ||
                            ((uint)WinVerifyTrustResult.TRUST_E_SUBJECT_FORM_UNKNOWN == dwLastError) ||
                            ((uint)WinVerifyTrustResult.TRUST_E_PROVIDER_UNKNOWN == dwLastError))
                    {
                        return VerifyResult.SIGNATURE_MISSING;
                    }
                    else
                    {
                        return VerifyResult.SIGNATURE_INVALID;
                    }
            }
        }

        public static VerifyResult VerifyFileAuthenticode(string filePath)
        {
            return VerifyEmbeddedSignature(filePath, WINTRUST_ACTION_GENERIC_VERIFY_V2, WinTrustDataRevocationChecks.WholeChain);
        }
    }
}
