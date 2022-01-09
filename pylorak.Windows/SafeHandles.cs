using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Security;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using Microsoft.Win32.SafeHandles;

namespace pylorak.Windows
{
    public sealed class SafeHGlobalHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("kernel32")]
            public static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

            [DllImport("kernel32")]
            public static extern IntPtr GlobalFree(IntPtr hMem);
        }

        private Type? MarshalDestroyType;

        private bool NeedsMarshalDestroy => (MarshalDestroyType != null);

        private static IntPtr AllocNativeMem(uint nBytes, bool zeroInit = false)
        {
            const uint GMEM_ZEROINIT = 0x0040;
            return NativeMethods.GlobalAlloc(zeroInit ? GMEM_ZEROINIT : 0, new UIntPtr(nBytes));
        }

        public static SafeHGlobalHandle Alloc(uint nBytes, bool zeroInit = false)
        {
            return new SafeHGlobalHandle(AllocNativeMem(nBytes, zeroInit));
        }

        public static SafeHGlobalHandle Alloc(int nBytes, bool zeroInit = false)
        {
            return Alloc((uint)nBytes, zeroInit);
        }

        public static SafeHGlobalHandle FromString(string? str)
        {
            return (str == null)
                ? new SafeHGlobalHandle()
                : new SafeHGlobalHandle(Marshal.StringToHGlobalUni(str));
        }

        public static SafeHGlobalHandle FromStruct<T>(T obj) where T : unmanaged
        {
            var size = Marshal.SizeOf(typeof(T));
            var ret = Alloc(size, true);
            ret.MarshalFromStruct(obj);
            return ret;
        }

        public static SafeHGlobalHandle FromManagedStruct<T>(T obj)
        {
            var size = Marshal.SizeOf(typeof(T));
            var ret = Alloc(size, true);
            ret.MarshalFromManagedStruct(obj);
            return ret;
        }

        public void MarshalFromStruct<T>(T obj, int offset = 0) where T : unmanaged
        {
            if (NeedsMarshalDestroy)
                Marshal.DestroyStructure(this.handle, MarshalDestroyType);

            int size = Marshal.SizeOf(typeof(T));
            unsafe
            {
                Buffer.MemoryCopy(&obj, (byte*)this.handle.ToPointer() + offset, size, size);
            }
            MarshalDestroyType = null;
        }

        public void MarshalFromManagedStruct<T>(T obj)
        {
            Marshal.StructureToPtr(obj, this.handle, NeedsMarshalDestroy);
            MarshalDestroyType = typeof(T);
        }

        public T ToStruct<T>() where T : unmanaged
        {
            T ret = default;
            var size = Marshal.SizeOf(typeof(T));
            unsafe
            {
                Buffer.MemoryCopy(handle.ToPointer(), &ret, size, size);
            }
            return ret;
        }

        public void ForgetAndResize(uint newSize, bool zeroInit = false)
        {
            if (this.IsClosed)
                throw new InvalidOperationException("The SafeHandle is already closed.");

            var newHndl = AllocNativeMem(newSize, zeroInit);
            if (newHndl == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception();

            if (!this.IsInvalid)
            {
                if (NeedsMarshalDestroy)
                {
                    Marshal.DestroyStructure(this.handle, MarshalDestroyType);
                    MarshalDestroyType = null;
                }
                NativeMethods.GlobalFree(this.handle);
            }

            SetHandle(newHndl);
        }

        public SafeHGlobalHandle()
            : this(IntPtr.Zero)
        { }

        public SafeHGlobalHandle(IntPtr ptr)
            : base(true)
        {
            SetHandle(ptr);
        }

        protected override bool ReleaseHandle()
        {
            if (NeedsMarshalDestroy)
            {
                Marshal.DestroyStructure(this.handle, MarshalDestroyType);
                MarshalDestroyType = null;
            }
            bool ret = (IntPtr.Zero == NativeMethods.GlobalFree(handle));
            SetHandle(IntPtr.Zero);
            return ret;
        }
    }

    public sealed class SafeObjectHandle : SafeHandleZeroOrMinusOneIsInvalid   // OpenProcess returns 0 on failure
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("kernel32", SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            public static extern bool CloseHandle(IntPtr hHandle);
        }

        public SafeObjectHandle() : base(true) { }

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

        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("advapi32"),
             ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            public static extern int RegCloseKey(IntPtr hKey);

            [DllImport("advapi32", CharSet = CharSet.Unicode)]
            public static extern int RegOpenKeyEx(IntPtr hKey, string subKey, uint ulOptions, uint samDesired, out SafeRegistryHandle hkResult);
        }

        public SafeRegistryHandle() : base(true) { }

        internal SafeRegistryHandle(IntPtr hndl, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(hndl);
        }

        public static SafeRegistryHandle Open(RegistryBaseKey baseKey, string subKey, RegistryRights access)
        {
            int err = NativeMethods.RegOpenKeyEx(new IntPtr((int)baseKey), subKey, 0, (uint)access, out SafeRegistryHandle safeHandle);
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
            return (0 == NativeMethods.RegCloseKey(handle));
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
            var sb = new StringBuilder(64);

            using var safeHandle = FindFirstVolume(sb);
            if (safeHandle.IsInvalid)
                throw new Win32Exception();

            yield return sb.ToString();

            while (safeHandle.FindNextVolume(sb))
            {
                yield return sb.ToString();
            }

            int errno = Marshal.GetLastWin32Error();
            if (errno == ERROR_NO_MORE_FILES)
                yield break;
            else
                throw new Win32Exception(errno);
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

    public sealed class SafeSidHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("advapi32")]
            public static extern IntPtr FreeSid(IntPtr pSid);

            [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool ConvertSidToStringSid(IntPtr Sid, out AllocHLocalSafeHandle StringSid);
        }

        public SafeSidHandle(IntPtr ptr, bool ownsHandle)
            : base(ownsHandle)
        {
            this.handle = ptr;
        }

        public SafeSidHandle()
            : base(true)
        {
            this.handle = IntPtr.Zero;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        protected override bool ReleaseHandle()
        {
            return (IntPtr.Zero == NativeMethods.FreeSid(handle));
        }

        public static string? ToStringSid(IntPtr pSid)
        {
            AllocHLocalSafeHandle? ptrStrSid = null;
            try
            {
                if (!NativeMethods.ConvertSidToStringSid(pSid, out ptrStrSid))
                    return null;

                return Marshal.PtrToStringUni(ptrStrSid.DangerousGetHandle());
            }
            finally
            {
                ptrStrSid?.Dispose();
            }
        }

        public string? GetStringSid()
        {
            return ToStringSid(handle);
        }
    }

}
