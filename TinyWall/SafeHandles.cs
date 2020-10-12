using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Security;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using Microsoft.Win32.SafeHandles;


namespace PKSoft
{
    public sealed class SafeObjectHandle : SafeHandleZeroOrMinusOneIsInvalid   // OpenProcess returns 0 on failure
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("kernel32", SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            public static extern bool CloseHandle(IntPtr hHandle);
        }

        internal SafeObjectHandle() : base(true) { }

        internal SafeObjectHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        override protected bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(handle);
        }
    }

    public sealed class HeapSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("kernel32")]
            internal static extern IntPtr HeapAlloc(IntPtr heap, uint uFlags, UIntPtr dwBytes);

            [DllImport("kernel32", SetLastError = true)]
            internal static extern IntPtr GetProcessHeap();

            [DllImport("kernel32", SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool HeapFree(IntPtr heap, uint flags, IntPtr mem);
        }

        private static IntPtr ProcessHeap { get; } = NativeMethods.GetProcessHeap();

        public HeapSafeHandle(int nBytes, bool zeroBytes = false)
            : base(true)
        {
            uint flags = zeroBytes ? 0x00000008u : 0u;
            this.handle = NativeMethods.HeapAlloc(ProcessHeap, flags, (UIntPtr)(uint)nBytes);
        }

        public HeapSafeHandle(IntPtr ptr, bool ownsHandle)
            : base(ownsHandle)
        {
            this.handle = ptr;
        }

        public HeapSafeHandle()
            : base(true)
        {
            this.handle = IntPtr.Zero;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        protected override bool ReleaseHandle()
        {
            return NativeMethods.HeapFree(ProcessHeap, 0, handle);
        }
    }

    public enum RegistryBaseKey
    {
        HKEY_CLASSES_ROOT = -2147483648,
        HKEY_CURRENT_USER = -2147483647,
        HKEY_LOCAL_MACHINE = -2147483646,
        HKEY_USERS = -2147483645,
        HKEY_PERFORMANCE_DATA = -2147483644,
        HKEY_CURRENT_CONFIG = -2147483643,
        HKEY_DYN_DATA = -2147483642
    }

    public sealed class SafeRegistryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [Flags]
        public enum RegistryRights : uint
        {
            KEY_READ = 0x20019u,
            KEY_WRITE = 0x20006u,
            KEY_ALL_ACCESS = 0xF003Fu,
            KEY_WOW64_32KEY = 0x0200u,
            KEY_WOW64_64KEY = 0x0100u,
        }
        internal SafeRegistryHandle() : base(true) { }

        internal SafeRegistryHandle(IntPtr hndl, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(hndl);
        }

        [DllImport("advapi32"),
         SuppressUnmanagedCodeSecurity,
         ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static extern int RegCloseKey(IntPtr hKey);

        [DllImport("advapi32", CharSet = CharSet.Unicode),
         SuppressUnmanagedCodeSecurity]
        private static extern int RegOpenKeyEx(IntPtr hKey, string subKey, uint ulOptions, uint samDesired, out SafeRegistryHandle hkResult);

        public static SafeRegistryHandle Open(RegistryBaseKey baseKey, string subKey, RegistryRights access)
        {
            int err = RegOpenKeyEx(new IntPtr((int)baseKey), subKey, 0, (uint)access, out SafeRegistryHandle safeHandle);
            if (0 == err)
                return safeHandle;
            else
                throw new Win32Exception(err, "RegOpenKeyEx");
        }

        private static bool RemoveFromStart(ref string val, string prefix)
        {
            if (val.StartsWith(prefix))
            {
                val = val.Remove(0, prefix.Length);
                return true;
            }

            return false;
        }

        public static SafeRegistryHandle Open(string key, RegistryRights access)
        {
            key = key.Replace('/', '\\');
            if (RemoveFromStart(ref key, @"HKEY_CLASSES_ROOT\"))
                return Open(RegistryBaseKey.HKEY_CLASSES_ROOT, key, access);
            else if (RemoveFromStart(ref key, @"HKEY_CURRENT_USER\"))
                return Open(RegistryBaseKey.HKEY_CURRENT_USER, key, access);
            else if (RemoveFromStart(ref key, @"HKEY_LOCAL_MACHINE\"))
                return Open(RegistryBaseKey.HKEY_LOCAL_MACHINE, key, access);
            else if (RemoveFromStart(ref key, @"HKEY_USERS\"))
                return Open(RegistryBaseKey.HKEY_USERS, key, access);
            else if (RemoveFromStart(ref key, @"HKEY_PERFORMANCE_DATA\"))
                return Open(RegistryBaseKey.HKEY_PERFORMANCE_DATA, key, access);
            else if (RemoveFromStart(ref key, @"HKEY_CURRENT_CONFIG\"))
                return Open(RegistryBaseKey.HKEY_CURRENT_CONFIG, key, access);
            else if (RemoveFromStart(ref key, @"HKEY_DYN_DATA\"))
                return Open(RegistryBaseKey.HKEY_DYN_DATA, key, access);
            else
                throw new ArgumentException("Unrecognized registry base key.");
        }

        override protected bool ReleaseHandle()
        {
            return (0 == RegCloseKey(handle));
        }
    }

    public sealed class AllocHLocalSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("kernel32.dll")]
            internal static extern IntPtr LocalAlloc(uint uFlags, UIntPtr dwBytes);

            [DllImport("kernel32.dll")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern IntPtr LocalFree(IntPtr hMem);
        }

        public AllocHLocalSafeHandle(int nBytes)
            : base(true)
        {
            this.handle = NativeMethods.LocalAlloc(0, new UIntPtr((uint)nBytes));
        }

        public AllocHLocalSafeHandle(IntPtr ptr, bool ownsHandle)
            : base(ownsHandle)
        {
            this.handle = ptr;
        }

        public AllocHLocalSafeHandle()
            : base(true)
        {
            this.handle = IntPtr.Zero;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        protected override bool ReleaseHandle()
        {
            return (IntPtr.Zero == NativeMethods.LocalFree(handle));
        }
    }

    public sealed class FindVolumeSafeHandle : SafeHandleMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern IntPtr FindFirstVolume([Out] StringBuilder lpszVolumeName, int cchBufferLength);

            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FindNextVolume(IntPtr hFindVolume, [Out] StringBuilder lpszVolumeName, int cchBufferLength);

            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FindVolumeClose(IntPtr hFindVolume);
        }

        public FindVolumeSafeHandle()
            : base(true)
        {
            SetHandleAsInvalid();
        }

        private FindVolumeSafeHandle(IntPtr handle)
            : base(true)
        {
            this.handle = handle;
        }

        public static IEnumerable<string> EnumerateVolumes()
        {
            const int ERROR_NO_MORE_FILES = 18;
            StringBuilder sb = new StringBuilder(64);

            using (var safeHandle = FindFirstVolume(sb))
            {
                if (safeHandle.IsInvalid)
                    throw new Win32Exception();

                yield return sb.ToString();

                while(safeHandle.FindNextVolume(sb))
                {
                    yield return sb.ToString();
                }

                int errno = Marshal.GetLastWin32Error();
                if (errno == ERROR_NO_MORE_FILES)
                    yield break;
                else
                    throw new Win32Exception(errno);
            }
        }

        private static FindVolumeSafeHandle FindFirstVolume(StringBuilder dst)
        {
            return new FindVolumeSafeHandle(NativeMethods.FindFirstVolume(dst, dst.Capacity));
        }

        private bool FindNextVolume(StringBuilder dst)
        {
            return NativeMethods.FindNextVolume(handle, dst, dst.Capacity);
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.FindVolumeClose(handle);
        }
    }
}
