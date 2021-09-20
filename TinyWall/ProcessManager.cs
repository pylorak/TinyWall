
using System;
using System.Security;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using TinyWall.Interface;
using System.ComponentModel;

namespace PKSoft
{
    public static class ProcessManager
    {
        [SuppressUnmanagedCodeSecurity]
        protected static class SafeNativeMethods
        {
            [DllImport("kernel32", SetLastError = true)]
            internal static extern SafeObjectHandle OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern bool QueryFullProcessImageName(SafeObjectHandle hProcess, QueryFullProcessImageNameFlags dwFlags, [Out] StringBuilder lpExeName, ref int size);

            [DllImport("ntdll")]
            internal static extern int NtQueryInformationProcess(SafeObjectHandle hProcess, int processInformationClass, [Out] out PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

            [DllImport("kernel32", SetLastError = true)]
            internal static extern SafeObjectHandle CreateToolhelp32Snapshot(SnapshotFlags flags, int id);
            [DllImport("kernel32", SetLastError = true)]
            internal static extern bool Process32First(SafeObjectHandle hSnapshot, [In, Out] ref PROCESSENTRY32 lppe);
            [DllImport("kernel32", SetLastError = true)]
            internal static extern bool Process32Next(SafeObjectHandle hSnapshot, [In, Out] ref PROCESSENTRY32 lppe);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool PostThreadMessage(int threadId, uint msg, UIntPtr wParam, IntPtr lParam);

            [DllImport("kernel32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetProcessTimes(SafeObjectHandle hProcess, out long lpCreationTime, out long lpExitTime, out long lpKernelTime, out long lpUserTime);

            [DllImport("advapi32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool OpenProcessToken(
                SafeObjectHandle ProcessToken,
                TokenAccessLevels DesiredAccess,
                out SafeObjectHandle TokenHandle);

            [DllImport("advapi32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetTokenInformation(
                SafeObjectHandle TokenHandle,
                TokenInformationClass TokenInformationClass,
                HeapSafeHandle TokenInformation,
                int TokenInformationLength,
                out int ReturnLength);
        }

        protected enum TokenInformationClass
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            TokenIsAppContainer,
            TokenCapabilities,
            TokenAppContainerSid,
            TokenAppContainerNumber,
            TokenUserClaimAttributes,
            TokenDeviceClaimAttributes,
            TokenRestrictedUserClaimAttributes,
            TokenRestrictedDeviceClaimAttributes,
            TokenDeviceGroups,
            TokenRestrictedDeviceGroups,
            TokenSecurityAttributes,
            TokenIsRestricted,
            TokenProcessTrustLevel,
            TokenPrivateNameSpace,
            TokenSingletonAttributes,
            TokenBnoIsolation,
            TokenChildProcessFlags,
            TokenIsLessPrivilegedAppContainer,
            TokenIsSandboxed,
            TokenOriginatingProcessTrustLevel,
            MaxTokenInfoClass
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct TOKEN_APPCONTAINER_INFORMATION
        {
            public IntPtr TokenAppContainer;
        };

        [Flags]
        internal enum TokenAccessLevels
        {
            AssignPrimary = 0x00000001,
            Duplicate = 0x00000002,
            Impersonate = 0x00000004,
            TokenQuery = 0x00000008,
            QuerySource = 0x00000010,
            AdjustPrivileges = 0x00000020,
            AdjustGroups = 0x00000040,
            AdjustDefault = 0x00000080,
            AdjustSessionId = 0x00000100,

            Read = 0x00020000 | TokenQuery,

            Write = 0x00020000 | AdjustPrivileges | AdjustGroups | AdjustDefault,

            AllAccess = 0x000F0000 |
                AssignPrimary |
                Duplicate |
                Impersonate |
                TokenQuery |
                QuerySource |
                AdjustPrivileges |
                AdjustGroups |
                AdjustDefault |
                AdjustSessionId,

            MaximumAllowed = 0x02000000
        }

        [StructLayout(LayoutKind.Sequential)]
        protected ref struct PROCESS_BASIC_INFORMATION
        {
            // Fore more info, see docs for NtQueryInformationProcess()
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szExeFile;
        };

        public struct ExtendedProcessEntry
        {
            public PROCESSENTRY32 BaseEntry;
            public long CreationTime;
            public string ImagePath;
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

        private const int MIN_PATH_BUFF_SIZE = 130;
        private const int MAX_PATH_BUFF_SIZE = 1040;

        public static string ExecutablePath { get; } = GetCurrentExecutablePath();
        private static string GetCurrentExecutablePath()
        {
            using (var proc = Process.GetCurrentProcess())
            {
                uint pid = unchecked((uint)proc.Id);
                return ProcessManager.GetProcessPath(pid);
            }
        }
        public static string GetProcessPath(uint processId)
        {
            var buffer = new StringBuilder(MIN_PATH_BUFF_SIZE);
            return GetProcessPath(processId, buffer);
        }

        public static string GetProcessPath(uint processId, StringBuilder buffer)
        {
            using (var hProcess = SafeNativeMethods.OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, processId))
            {
                return GetProcessPath(hProcess, buffer);
            }
        }

        public static string GetProcessPath(SafeObjectHandle hProcess, StringBuilder buffer)
        {
            // This method needs Windows Vista or newer OS
            System.Diagnostics.Debug.Assert(Environment.OSVersion.Version.Major >= 6);

            if (hProcess.IsInvalid)
                return null;

            buffer.Length = 0;
            buffer.Capacity = 130;
            while (true)
            {
                int size = buffer.Capacity;
                if (SafeNativeMethods.QueryFullProcessImageName(hProcess, QueryFullProcessImageNameFlags.NativeFormat, buffer, ref size))
                {
                    if (buffer.Length == 0)
                        return string.Empty;

                    return PathMapper.Instance.ConvertPathIgnoreErrors(buffer.ToString(), PathFormat.Win32);
                }
                else
                {
                    const int ERROR_INSUFFICIENT_BUFFER = 122;
                    int error = Marshal.GetLastWin32Error();
                    if ((ERROR_INSUFFICIENT_BUFFER == error) && (buffer.Capacity < MAX_PATH_BUFF_SIZE))
                    {
                        buffer.Length = 0;
                        buffer.Capacity = MAX_PATH_BUFF_SIZE;
                        continue;
                    }
                    else
                        break;
                }
            }

            return null;
        }

        public static bool GetParentProcess(uint processId, ref uint parentPid)
        {
            using (var hProcess = SafeNativeMethods.OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, processId))
            {
                if (hProcess.IsInvalid)
                    return false;
                    //throw new Exception($"Cannot open process Id {processId}.");

                if (VersionInfo.IsWow64Process)
                {
                    return false;
                    //throw new NotSupportedException("This method is not supported in 32-bit process on a 64-bit OS.");
                }
                else
                {
                    PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
                    int status = SafeNativeMethods.NtQueryInformationProcess(hProcess, 0, out pbi, Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION)), out int returnLength);
                    if (status < 0)
                        throw new Exception($"NTSTATUS: {status}");

                    parentPid = unchecked((uint)pbi.InheritedFromUniqueProcessId.ToInt32());

                    // parentPid might have been reused and thus might not be the actual parent.
                    // Check process creation times to figure it out.
                    using (var hParentProcess = SafeNativeMethods.OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, parentPid))
                    {
                        if (GetProcessCreationTime(hParentProcess, out long parentCreation) && GetProcessCreationTime(hProcess, out long childCreation))
                        {
                            return parentCreation <= childCreation;
                        }
                        return false;
                    }
                }
            }
        }

        private static bool GetProcessCreationTime(SafeObjectHandle hProcess, out long creationTime)
        {
            return SafeNativeMethods.GetProcessTimes(hProcess, out creationTime, out _, out _, out _);
        }

        public static IEnumerable<PROCESSENTRY32> CreateToolhelp32Snapshot()
        {
            const int ERROR_NO_MORE_FILES = 18;

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

        public static IEnumerable<ExtendedProcessEntry> CreateToolhelp32SnapshotExtended()
        {
            StringBuilder sbuilder = new StringBuilder(MIN_PATH_BUFF_SIZE);
            foreach (var p in CreateToolhelp32Snapshot())
            {
                using (var hProcess = SafeNativeMethods.OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, p.th32ProcessID))
                {
                    ExtendedProcessEntry ret;
                    ret.BaseEntry = p;
                    ret.ImagePath = GetProcessPath(hProcess, sbuilder);
                    GetProcessCreationTime(hProcess, out ret.CreationTime);
                    yield return ret;
                }
            }
        }

        public static void WakeMessageQueues(Process p)
        {
            foreach (ProcessThread thread in p.Threads)
            {
                const uint WM_NULL = 0;
                SafeNativeMethods.PostThreadMessage(thread.Id, WM_NULL, UIntPtr.Zero, IntPtr.Zero);
            }
        }

        public static void TerminateProcess(Process p, int timeoutMs)
        {
            if (p.MainWindowHandle == IntPtr.Zero)
            {
                foreach (ProcessThread thread in p.Threads)
                {
                    const uint WM_QUIT = 0x0012;
                    SafeNativeMethods.PostThreadMessage(thread.Id, WM_QUIT, UIntPtr.Zero, IntPtr.Zero);
                }
            }
            else
            {
                p.CloseMainWindow();
            }
            if (!p.WaitForExit(timeoutMs))
            {
                p.Kill();
                p.WaitForExit(1000);
            }
        }

        public static string GetAppContainerSid(uint pid)
        {
            if (!UwpPackage.PlatformSupport)
                return null;

            using (var hProcess = SafeNativeMethods.OpenProcess(ProcessAccessFlags.QueryInformation, false, pid))
            {
                if (!SafeNativeMethods.OpenProcessToken(hProcess, TokenAccessLevels.TokenQuery, out SafeObjectHandle token))
                    return null;

                const int hTokenInfoMemSize = 128;

                using (var hToken = token)
                using (var hTokenInfo = new HeapSafeHandle(hTokenInfoMemSize))
                {
                    if (!SafeNativeMethods.GetTokenInformation(hToken, TokenInformationClass.TokenAppContainerSid, hTokenInfo, hTokenInfoMemSize, out _))
                        return null;

                    var tokenAppContainerInfo = (TOKEN_APPCONTAINER_INFORMATION)Marshal.PtrToStructure(hTokenInfo.DangerousGetHandle(), typeof(TOKEN_APPCONTAINER_INFORMATION));
                    if (tokenAppContainerInfo.TokenAppContainer == IntPtr.Zero)
                        return null;

                    return Utils.ConvertSidToStringSid(tokenAppContainerInfo.TokenAppContainer);
                }
            }
        }
    }
}
