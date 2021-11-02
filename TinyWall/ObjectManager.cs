using System;
using System.Text;
using System.Security;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace pylorak.Windows.ObjectManager
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

        public string GetString()
        {
            var oa = Marshal.PtrToStructure<NativeMethods.UNICODE_STRING>(this.handle);
            return oa.ToString();
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

    public struct ObjectDirectoyInfo
    {
        public string Name;
        public string TypeName;

        public ObjectDirectoyInfo(string name, string type)
        {
            Name = name;
            TypeName = type;
        }

        public override string ToString()
        {
            return $"{Name} | {TypeName}";
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

    internal static class NativeMethods
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
            public SafeHandle objectName;
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
    }

    public class ObjectManager
    {
        private static NativeMethods.OBJECT_ATTRIBUTES InitializeObjectAttributes(SafeUnicodeStringHandle objectName, SafeNtObjectHandle parentObject = null)
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
            var oa = InitializeObjectAttributes(objectName, parentObject);
            var success = NativeMethods.NtOpenDirectoryObject(out SafeNtObjectHandle handle, NativeMethods.AccessMask.DIRECTORY_QUERY | NativeMethods.AccessMask.DIRECTORY_TRAVERSE, ref oa);
            return handle;
        }

        private static SafeNtObjectHandle OpenSymbolicLinkObjectForRead(string linkName, SafeNtObjectHandle parentObject = null)
        {
            using var objectName = new SafeUnicodeStringHandle(linkName);
            var oa = InitializeObjectAttributes(objectName, parentObject);
            var success = NativeMethods.NtOpenSymbolicLinkObject(out SafeNtObjectHandle handle, NativeMethods.AccessMask.GENERIC_READ, ref oa);
            return handle;
        }

        public static string QueryLinkTarget(ref SafeUnicodeStringHandle buffer, string linkName, SafeNtObjectHandle parentObject)
        {
            using var linkHandle = OpenSymbolicLinkObjectForRead(linkName, parentObject);

            while (true)
            {
                var ret = NativeMethods.NtQuerySymbolicLinkObject(linkHandle, buffer, out uint returnLen);
                if (ret == NativeMethods.NtStatus.STATUS_SUCCESS)
                {
                    return buffer.GetString();
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

        public static IEnumerable<ObjectDirectoyInfo> QueryDirectory(SafeNtObjectHandle dirObjHndl)
        {
            uint ctx = 0;
            uint bufLen = 512;
            bool restart = true;

            using (var buf = pylorak.Windows.Services.SafeHGlobalHandle.Alloc(bufLen))
            {
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
                    yield return new ObjectDirectoyInfo(
                        dirInfo.Name.ToString(),
                        dirInfo.TypeName.ToString()
                    );
                }
            }
        }
    }
}
