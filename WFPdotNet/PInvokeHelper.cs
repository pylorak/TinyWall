using System;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Principal;

namespace WFPdotNet
{
    public static class PInvokeHelper
    {
        public static T[] PtrToStructureArray<T>(IntPtr start, uint numElem, uint stride)
        {
            T[] ret = new T[numElem];
            long ptr = start.ToInt64();
            for (int i = 0; i < numElem; i++, ptr += stride)
            {
                ret[i] = Marshal.PtrToStructure<T>(new IntPtr(ptr));
            }
            return ret;
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
        
        internal static string ConvertSidToStringSid(IntPtr pSid)
        {
            if (!ConvertSidToStringSid(pSid, out AllocHLocalSafeHandle ptrStrSid))
                return null;

            string strSid = Marshal.PtrToStringUni(ptrStrSid.DangerousGetHandle());
            ptrStrSid.Dispose();
            return strSid;
        }
    }
}
