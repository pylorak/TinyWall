
using System;
using System.Security;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;
using System.Text;
using TinyWall.Interface;
using System.ComponentModel;

namespace PKSoft
{
    public class ProcessManager
    {
        public sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid   // OpenProcess returns 0 on failure
        {
            internal SafeProcessHandle() : base(true) { }

            internal SafeProcessHandle(IntPtr handle) : base(true)
            {
                SetHandle(handle);
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            override protected bool ReleaseHandle()
            {
                return SafeNativeMethods.CloseHandle(handle);
            }
        }

        public sealed class SafeSnapshotHandle : SafeHandleMinusOneIsInvalid   // CreateToolhelp32Snapshot  returns -1 on failure
        {
            internal SafeSnapshotHandle() : base(true) { }

            internal SafeSnapshotHandle(IntPtr handle) : base(true)
            {
                SetHandle(handle);
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            override protected bool ReleaseHandle()
            {
                return SafeNativeMethods.CloseHandle(handle);
            }
        }

        [SuppressUnmanagedCodeSecurity]
        public static class SafeNativeMethods
        {
            [DllImport("kernel32", SetLastError = true)]
            internal static extern SafeProcessHandle OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);

            [DllImport("kernel32", SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static extern bool CloseHandle(IntPtr hHandle);

            [DllImport("kernel32", SetLastError = true)]
            internal static extern bool QueryFullProcessImageName(SafeProcessHandle hProcess, QueryFullProcessImageNameFlags dwFlags, StringBuilder lpExeName, out int size);

            [DllImport("ntdll")]
            internal static extern int NtQueryInformationProcess(SafeProcessHandle hProcess, int processInformationClass, [Out] out PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

            [DllImport("kernel32", SetLastError = true)]
            internal static extern SafeSnapshotHandle CreateToolhelp32Snapshot(SnapshotFlags flags, int id);
            [DllImport("kernel32", SetLastError = true)]
            internal static extern bool Process32First(SafeSnapshotHandle hSnapshot, ref PROCESSENTRY32 lppe);
            [DllImport("kernel32", SetLastError = true)]
            internal static extern bool Process32Next(SafeSnapshotHandle hSnapshot, ref PROCESSENTRY32 lppe);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_BASIC_INFORMATION
        {
            // Fore more info, see docs for NtQueryInformationProcess()
            internal IntPtr Reserved1;
            internal IntPtr PebBaseAddress;
            internal IntPtr Reserved2_0;
            internal IntPtr Reserved2_1;
            internal IntPtr UniqueProcessId;
            internal IntPtr InheritedFromUniqueProcessId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public int th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public int th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szExeFile;
        };

        [Flags]
        internal enum SnapshotFlags : uint
        {
            HeapList = 0x00000001,
            Process = 0x00000002,
            Thread = 0x00000004,
            Module = 0x00000008,
            Module32 = 0x00000010,
            All = (HeapList | Process | Thread | Module),
            Inherit = 0x80000000,
            NoHeaps = 0x40000000
        }

        [Flags]
        public enum ProcessAccessFlags
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            Synchronize = 0x00100000,
            ReadControl = 0x00020000,
            QueryLimitedInformation = 0x00001000,
        }

        [Flags]
        public enum QueryFullProcessImageNameFlags
        {
            Win32Format = 0,
            NativeFormat = 1
        }

        public static string GetProcessPath(int processId)
        {
            // This method needs Windows Vista or newer OS
            System.Diagnostics.Debug.Assert(Environment.OSVersion.Version.Major >= 6);

            var buffer = new StringBuilder(1024);
            using (SafeProcessHandle hProcess = SafeNativeMethods.OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, processId))
            {
                if (hProcess.IsInvalid)
                    return null;

                int size = buffer.Capacity;
                if (SafeNativeMethods.QueryFullProcessImageName(hProcess, QueryFullProcessImageNameFlags.Win32Format, buffer, out size))
                    return buffer.ToString();
                else
                    return null;
            }
        }

        public static int GetParentProcess(int processId)
        {
            using (SafeProcessHandle hProcess = SafeNativeMethods.OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, processId))
            {
                if (hProcess.IsInvalid)
                    throw new Exception($"Cannot open process Id {processId}.");

                if (VersionInfo.IsWow64Process)
                {
                    throw new NotSupportedException("This method is not supported in 32-bit process on a 64-bit OS.");
                }
                else
                {
                    PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
                    int status = SafeNativeMethods.NtQueryInformationProcess(hProcess, 0, out pbi, Marshal.SizeOf(pbi), out int returnLength);
                    if (status < 0)
                        throw new Exception($"NTSTATUS: {status}");

                    return pbi.InheritedFromUniqueProcessId.ToInt32();
                }
            }
        }

        public static IEnumerable<PROCESSENTRY32> CreateToolhelp32Snapshot()
        {
            const int ERROR_NO_MORE_FILES = 0x12;

            PROCESSENTRY32 pe32 = new PROCESSENTRY32 { };
            pe32.dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32));
            using (var hSnapshot = SafeNativeMethods.CreateToolhelp32Snapshot(SnapshotFlags.Process, 0))
            {
                if (hSnapshot.IsInvalid)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if (!SafeNativeMethods.Process32First(hSnapshot, ref pe32))
                {
                    int errno = Marshal.GetLastWin32Error();
                    if (errno == ERROR_NO_MORE_FILES)
                        yield break;
                    throw new Win32Exception(errno);
                }
                do
                {
                    yield return pe32;
                } while (SafeNativeMethods.Process32Next(hSnapshot, ref pe32));
            }
        }
    }
}
