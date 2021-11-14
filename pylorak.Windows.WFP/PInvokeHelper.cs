using System;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Principal;

namespace pylorak.Windows.WFP
{
    public static class PInvokeHelper
    {
        public static T[] PtrToStructureArray<T>(IntPtr start, uint numElem, uint stride) where T : unmanaged
        {
            T[] ret = new T[numElem];
            long ptr = start.ToInt64();
            for (int i = 0; i < numElem; i++, ptr += stride)
            {
                ret[i] = PInvokeHelper.PtrToStructure<T>(new IntPtr(ptr));
            }
            return ret;
        }

        public static T PtrToStructure<T>(IntPtr src) where T : unmanaged
        {
            var ret = default(T);
            var size = Marshal.SizeOf(typeof(T));
            unsafe
            {
                System.Diagnostics.Debug.Assert(sizeof(T) == size);
                Buffer.MemoryCopy(src.ToPointer(), &ret, size, size);
            }
            return ret;
        }

        public static void StructureToPtr<T>(T src, IntPtr dst) where T : unmanaged
        {
            var size = Marshal.SizeOf(typeof(T));
            unsafe
            {
                System.Diagnostics.Debug.Assert(sizeof(T) == size);
                Buffer.MemoryCopy(&src, dst.ToPointer(), size, size);
            }
        }

        public static SafeHGlobalHandle CreateWfpBlob(IntPtr dataPtr, int dataSize, bool nullTerminateUnicodeData = false)
        {
            // Reserve buffer
            var bufSize = nullTerminateUnicodeData ? dataSize + 2 : dataSize;
            var blobSize = Marshal.SizeOf(typeof(Interop.FWP_BYTE_BLOB));
            var nativeMemHndl = SafeHGlobalHandle.Alloc(blobSize + bufSize);
            var blobPtr = nativeMemHndl.DangerousGetHandle();
            var bufPtr = blobPtr + blobSize;

            // Prepare blob structure
            Interop.FWP_BYTE_BLOB blob;
            blob.data = bufPtr;
            blob.size = (uint)bufSize;

            // Copy all into native memory
            unsafe
            {
                if (nullTerminateUnicodeData)
                {
                    var strBufPtr = (char*)bufPtr.ToPointer();
                    var strLen = dataSize / 2;
                    strBufPtr[strLen] = (char)0;
                }
                Buffer.MemoryCopy(&blob, blobPtr.ToPointer(), blobSize, blobSize);
                Buffer.MemoryCopy(dataPtr.ToPointer(), bufPtr.ToPointer(), dataSize, dataSize);
            }

            return nativeMemHndl;
        }

        public static void AssertUnmanagedType<T>() where T : unmanaged
        { }

        [DllImport("advapi32", CharSet = CharSet.Auto)]
        static extern uint GetLengthSid(IntPtr pSid);

        [DllImport("advapi32", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CopySid(uint nDestinationSidLength, IntPtr pDestinationSid, IntPtr pSourceSid);

        public static SafeHandle CopyNativeSid(IntPtr sid)
        {
            var sidLength = GetLengthSid(sid);
            var ret = new AllocHLocalSafeHandle((int)sidLength);
            CopySid(sidLength, ret.DangerousGetHandle(), sid);
            return ret;
        }

        [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ConvertSidToStringSid(IntPtr Sid, out AllocHLocalSafeHandle StringSid);
        
        internal static string? ConvertSidToStringSid(IntPtr pSid)
        {
            if (!ConvertSidToStringSid(pSid, out AllocHLocalSafeHandle ptrStrSid))
                return null;

            string strSid = Marshal.PtrToStringUni(ptrStrSid.DangerousGetHandle());
            ptrStrSid.Dispose();
            return strSid;
        }
    }
}
