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
                ret[i] = (T)Marshal.PtrToStructure(new IntPtr(ptr), typeof(T));
            }
            return ret;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.None)]
        public static AllocHGlobalSafeHandle StructToHGlobal<T>(T obj)
        {
            int size = Marshal.SizeOf(typeof(T));
            AllocHGlobalSafeHandle safeHandle = new AllocHGlobalSafeHandle(size);
            Marshal.StructureToPtr(obj, safeHandle.DangerousGetHandle(), false);
            return safeHandle;
        }

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
    }
}
