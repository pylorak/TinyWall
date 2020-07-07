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
