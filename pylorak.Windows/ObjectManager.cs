using System;
using System.Security;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace pylorak.Windows
{
    public sealed class SafeNtObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("kernel32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr hObject);
        }

        public SafeNtObjectHandle()
            : this(IntPtr.Zero)
        { }

        public SafeNtObjectHandle(IntPtr ptr)
            : base(true)
        {
            SetHandle(ptr);
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(handle);
        }
    }

    public class SafeUnicodeStringHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeUnicodeStringHandle(string path)
            : base(true)
        {
            Initialize(path, (ushort)path.Length);
        }

        public SafeUnicodeStringHandle(ushort charCapacity)
            : base(true)
        {
            Initialize(null, charCapacity);
        }

        public SafeUnicodeStringHandle()
            : base(true)
        {
            SetHandle(IntPtr.Zero);
        }

        protected override bool ReleaseHandle()
        {
            if (base.handle == IntPtr.Zero)
                return true;

            Marshal.FreeHGlobal(base.handle);
            base.handle = IntPtr.Zero;

            return true;
        }

        internal bool StringEquals(in NativeMethods.UNICODE_STRING other, bool caseInSensitive)
        {
            return NativeMethods.RtlEqualUnicodeString(this, in other, caseInSensitive);
        }

        public NativeMethods.UNICODE_STRING ToStruct()
        {
            NativeMethods.UNICODE_STRING ret = new NativeMethods.UNICODE_STRING();
            int size = Marshal.SizeOf(typeof(NativeMethods.UNICODE_STRING));
            unsafe
            {
                Buffer.MemoryCopy(handle.ToPointer(), &ret, size, size);
            }
            return ret;
        }

        private void Initialize(string str, ushort capacityInChars)
        {
            var capacityInBytes = sizeof(char) * capacityInChars;
            var lengthInBytes = (str?.Length ?? 0) * 2;
            var structLen = Marshal.SizeOf(typeof(NativeMethods.UNICODE_STRING));
            Debug.Assert(capacityInBytes >= lengthInBytes);

            if (capacityInBytes > ushort.MaxValue)
                throw new ArgumentException("Requested capacity too big.");
            if (lengthInBytes % 2 != 0)
                throw new ArgumentException("Invalid array length, must be a multiple of two.");

            NativeMethods.UNICODE_STRING objectName;
            objectName.length = (ushort)lengthInBytes;
            objectName.maximumLength = (ushort)capacityInBytes;
            objectName.buffer = IntPtr.Zero;

            var pbBuffer = Marshal.AllocHGlobal(structLen + capacityInBytes);
            if (pbBuffer != IntPtr.Zero)
            {
                SetHandle(pbBuffer);
                objectName.buffer = (IntPtr)((ulong)pbBuffer + (ulong)structLen);
                Marshal.StructureToPtr(objectName, pbBuffer, false);
                if (str != null)
                {
                    unsafe
                    {
                        fixed (char* ch = str)
                        {
                            Buffer.MemoryCopy(ch, objectName.buffer.ToPointer(), lengthInBytes, lengthInBytes);
                        }
                    }
                }
            }
        }
    }

    public class NtStatusException : Exception
    {
        public uint NtStatus { get; private set; }

        public NtStatusException(uint ntStatus)
        {
            NtStatus = ntStatus;
        }
    }

    public static class NativeMethods
    {
        private const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;

        public enum NtStatus : uint
        {
            STATUS_SUCCESS = 0,
            STATUS_INSUFFICIENT_RESOURCES = 0xC000009A,
            STATUS_INVALID_PARAMETER = 0xC00000EF,
            STATUS_OBJECT_NAME_INVALID = 0xC0000033,
            STATUS_OBJECT_NAME_NOT_FOUND = 0xC0000034,
            STATUS_OBJECT_PATH_NOT_FOUND = 0xC000003A,
            STATUS_OBJECT_PATH_SYNTAX_BAD = 0xC000003B,
            STATUS_NO_MORE_ENTRIES = 0x8000001A,
            STATUS_MORE_ENTRIES = 0x00000105,
            STATUS_BUFFER_TOO_SMALL = 0xC0000023,
            STATUS_INVALID_HANDLE = 0xC0000008,
        }

        [Flags]
        public enum AccessMask : uint
        {
            None = 0,
            DELETE = 0x00010000,
            READ_CONTROL = 0x00020000,
            SYNCHRONIZE = 0x00100000,
            WRITE_DAC = 0x00040000,
            WRITE_OWNER = 0x00080000,
            STANDARD_RIGHTS_READ = READ_CONTROL,
            STANDARD_RIGHTS_WRITE = READ_CONTROL,
            STANDARD_RIGHTS_EXECUTE = READ_CONTROL,
            GENERIC_READ = 0x80000000,
            GENERIC_WRITE = 0x40000000,
            GENERIC_EXECUTE = 0x20000000,
            FILE_READ_DATA = 0x0001,
            FILE_READ_ATTRIBUTES = 0x0080,
            FILE_READ_EA = 0x0008,
            FILE_WRITE_DATA = 0x0002,
            FILE_WRITE_ATTRIBUTES = 0x0100,
            FILE_WRITE_EA = 0x0010,
            FILE_APPEND_DATA = 0x0004,
            FILE_EXECUTE = 0x0020,
            FILE_GENERIC_READ = STANDARD_RIGHTS_READ | FILE_READ_DATA | FILE_READ_ATTRIBUTES | FILE_READ_EA | SYNCHRONIZE,
            FILE_GENERIC_WRITE = STANDARD_RIGHTS_WRITE | FILE_WRITE_DATA | FILE_WRITE_ATTRIBUTES | FILE_WRITE_EA | FILE_APPEND_DATA | SYNCHRONIZE,
            FILE_GENERIC_EXECUTE = STANDARD_RIGHTS_EXECUTE | FILE_READ_ATTRIBUTES | FILE_EXECUTE | SYNCHRONIZE,
            FILE_LIST_DIRECTORY = FILE_READ_DATA,
            FILE_TRAVERSE = FILE_EXECUTE,
            FILE_ADD_FILE = FILE_WRITE_DATA,
            FILE_ADD_SUBDIRECTORY = FILE_APPEND_DATA,
            FILE_CREATE_PIPE_INSTANCE = FILE_APPEND_DATA,
            FILE_DELETE_CHILD = 0x0040,
            DIRECTORY_QUERY = 1,
            DIRECTORY_TRAVERSE = 2,
            DIRECTORY_CREATE_OBJECT = 4,
            DIRECTORY_CREATE_SUBDIRECTORY = 8,
            DIRECTORY_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | 0xF,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OBJECT_ATTRIBUTES
        {
            public enum Attributes
            {
                None = 0,
                OBJ_CASE_INSENSITIVE = 0x00000040,
                OBJ_INHERIT = 0x00000002
            }

            public int length;
            public IntPtr rootDirectory;
            public IntPtr objectName;
            public Attributes attributes;
            public IntPtr securityDescriptor;
            public IntPtr securityQualityOfService;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct UNICODE_STRING
        {
            public ushort length;
            public ushort maximumLength;
            public IntPtr buffer;

            public override string ToString()
            {
                return Marshal.PtrToStringUni(buffer, length / 2);
            }

            public bool StartsWith(UNICODE_STRING needle, bool caseInSensitive = false)
            {
                if (needle.length > this.length)
                    return false;

                var thisCopy = this;
                thisCopy.length = needle.length;
                return RtlEqualUnicodeString(in thisCopy, in needle, caseInSensitive);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OBJECT_DIRECTORY_INFORMATION
        {
            public UNICODE_STRING Name;
            public UNICODE_STRING TypeName;
        }

        [DllImport("ntdll")]
        public static extern NtStatus NtOpenDirectoryObject(out SafeNtObjectHandle Handle, AccessMask DesiredAccess, ref OBJECT_ATTRIBUTES ObjectAttributes);

        [DllImport("ntdll")]
        public static extern NtStatus NtQueryDirectoryObject(SafeNtObjectHandle DirectoryHandle, IntPtr Buffer, uint Length, bool ReturnSingleEntry, bool RestartScan, ref uint Context, out uint ReturnLength);

        [DllImport("ntdll")]
        public static extern NtStatus NtOpenSymbolicLinkObject(out SafeNtObjectHandle Handle, AccessMask DesiredAccess, ref OBJECT_ATTRIBUTES ObjectAttributes);

        [DllImport("ntdll")]
        public static extern NtStatus NtQuerySymbolicLinkObject(SafeNtObjectHandle Handle, SafeUnicodeStringHandle LinkTarget, out uint ReturnLength);

        [DllImport("ntdll")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool RtlEqualUnicodeString(SafeUnicodeStringHandle str1, in UNICODE_STRING str2, [MarshalAs(UnmanagedType.U1)] bool caseInSensitive);

        [DllImport("ntdll")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool RtlEqualUnicodeString(in UNICODE_STRING str1, in UNICODE_STRING str2, [MarshalAs(UnmanagedType.U1)] bool caseInSensitive);
    }

    public class ObjectManager
    {
        private static NativeMethods.OBJECT_ATTRIBUTES InitializeObjectAttributes(IntPtr objectName, SafeNtObjectHandle parentObject = null)
        {
            return new NativeMethods.OBJECT_ATTRIBUTES()
            {
                length = Marshal.SizeOf(typeof(NativeMethods.OBJECT_ATTRIBUTES)),
                rootDirectory = parentObject?.DangerousGetHandle() ?? IntPtr.Zero,
                attributes = NativeMethods.OBJECT_ATTRIBUTES.Attributes.OBJ_CASE_INSENSITIVE,
                securityDescriptor = IntPtr.Zero,
                securityQualityOfService = IntPtr.Zero,
                objectName = objectName,
            };
        }
        public static SafeNtObjectHandle OpenDirectoryObjectForRead(string dirName, SafeNtObjectHandle parentObject = null)
        {
            using var objectName = new SafeUnicodeStringHandle(dirName);
            var oa = InitializeObjectAttributes(objectName.DangerousGetHandle(), parentObject);
            var success = NativeMethods.NtOpenDirectoryObject(out SafeNtObjectHandle handle, NativeMethods.AccessMask.DIRECTORY_QUERY | NativeMethods.AccessMask.DIRECTORY_TRAVERSE, ref oa);
            return handle;
        }

        private static SafeNtObjectHandle OpenSymbolicLinkObjectForRead(string linkName, SafeNtObjectHandle parentObject = null)
        {
            using var objectName = new SafeUnicodeStringHandle(linkName);
            var oa = InitializeObjectAttributes(objectName.DangerousGetHandle(), parentObject);
            var success = NativeMethods.NtOpenSymbolicLinkObject(out SafeNtObjectHandle handle, NativeMethods.AccessMask.GENERIC_READ, ref oa);
            return handle;
        }

        private static SafeNtObjectHandle OpenSymbolicLinkObjectForRead(NativeMethods.UNICODE_STRING linkName, SafeNtObjectHandle parentObject = null)
        {
            unsafe
            {
                var oa = InitializeObjectAttributes(new IntPtr(&linkName), parentObject);
                var success = NativeMethods.NtOpenSymbolicLinkObject(out SafeNtObjectHandle handle, NativeMethods.AccessMask.GENERIC_READ, ref oa);
                return handle;
            }
        }

        public static string QueryLinkTarget(ref SafeUnicodeStringHandle buffer, SafeNtObjectHandle linkHandle, SafeNtObjectHandle parentObject)
        {
            while (true)
            {
                var ret = NativeMethods.NtQuerySymbolicLinkObject(linkHandle, buffer, out uint returnLen);
                if (ret == NativeMethods.NtStatus.STATUS_SUCCESS)
                {
                    return buffer.ToStruct().ToString();
                }
                else if (ret == NativeMethods.NtStatus.STATUS_BUFFER_TOO_SMALL)
                {
                    var newBuffer = new SafeUnicodeStringHandle((ushort)returnLen);
                    try
                    {
                        buffer.Dispose();
                        buffer = newBuffer;
                    }
                    catch
                    {
                        newBuffer.Dispose();
                        throw;
                    }
                }
                else
                {
                    throw new NtStatusException((uint)ret);
                }
            }

            throw new InvalidOperationException("Logic error.");
        }

        public static string QueryLinkTarget(ref SafeUnicodeStringHandle buffer, NativeMethods.UNICODE_STRING linkName, SafeNtObjectHandle parentObject)
        {
            using var linkHandle = OpenSymbolicLinkObjectForRead(linkName, parentObject);
            return QueryLinkTarget(ref buffer, linkHandle, parentObject);
        }

        public static string QueryLinkTarget(ref SafeUnicodeStringHandle buffer, string linkName, SafeNtObjectHandle parentObject)
        {
            using var linkHandle = OpenSymbolicLinkObjectForRead(linkName, parentObject);
            return QueryLinkTarget(ref buffer, linkHandle, parentObject);
        }

        public static IEnumerable<NativeMethods.OBJECT_DIRECTORY_INFORMATION> QueryDirectory(SafeNtObjectHandle dirObjHndl)
        {
            DirectoryQueryFilter<NativeMethods.OBJECT_DIRECTORY_INFORMATION> filter = (in NativeMethods.OBJECT_DIRECTORY_INFORMATION di, ref NativeMethods.OBJECT_DIRECTORY_INFORMATION ret) =>
            {
                ret = di;
                return true;
            };

            foreach (var item in QueryDirectory(dirObjHndl, filter))
                yield return item;
        }

        public static IEnumerable<NativeMethods.UNICODE_STRING> QueryDirectoryForType(SafeNtObjectHandle dirObjHndl, string objType)
        {
            using var typeHandle = new SafeUnicodeStringHandle(objType);

            DirectoryQueryFilter<NativeMethods.UNICODE_STRING> filter = (in NativeMethods.OBJECT_DIRECTORY_INFORMATION di, ref NativeMethods.UNICODE_STRING ret) =>
            {
                if (typeHandle.StringEquals(in di.TypeName, true))
                {
                    ret = di.Name;
                    return true;
                }
                return false;
            };

            foreach (var item in QueryDirectory(dirObjHndl, filter))
                yield return item;
        }

        internal delegate bool DirectoryQueryFilter<R>(in NativeMethods.OBJECT_DIRECTORY_INFORMATION di, ref R ret);

        internal static IEnumerable<R> QueryDirectory<R>(SafeNtObjectHandle dirObjHndl, DirectoryQueryFilter<R> filter)
        {
            uint ctx = 0;
            uint bufLen = 512;
            bool restart = true;
            R retVal = default;

            using var buf = SafeHGlobalHandle.Alloc(bufLen);

            while (true)
            {
                while (true)
                {
                    var ret = NativeMethods.NtQueryDirectoryObject(dirObjHndl, buf.DangerousGetHandle(), bufLen, true, restart, ref ctx, out uint returnLen);
                    if (ret == NativeMethods.NtStatus.STATUS_SUCCESS)
                    {
                        break;
                    }
                    else if (ret == NativeMethods.NtStatus.STATUS_BUFFER_TOO_SMALL)
                    {
                        bufLen = 2 * returnLen;
                        buf.ForgetAndResize(bufLen);
                    }
                    else if (ret == NativeMethods.NtStatus.STATUS_NO_MORE_ENTRIES)
                    {
                        yield break;
                    }
                    else
                    {
                        throw new NtStatusException((uint)ret);
                    }
                }
                restart = false;

                var dirInfo = buf.ToStruct<NativeMethods.OBJECT_DIRECTORY_INFORMATION>();

                if (filter(in dirInfo, ref retVal))
                    yield return retVal;
            }
        }
    }
}
