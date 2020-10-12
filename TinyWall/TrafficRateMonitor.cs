using System;
using System.Collections.Generic;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;
using TinyWall.Interface;

namespace PKSoft
{
    class TrafficRateMonitor : TinyWall.Interface.Internal.Disposable
    {
        private bool disposed;

        private readonly IntPtr hQuery;
        private readonly IntPtr hTxCounter;
        private readonly IntPtr hRxCounter;
        private byte[] buffer = new byte[0];

        public TrafficRateMonitor()
        {
            NativeMethods.PdhOpenQuery(null, IntPtr.Zero, out hQuery);
            NativeMethods.PdhAddEnglishCounter(hQuery, "\\Network Interface(*)\\Bytes Sent/Sec", IntPtr.Zero, out hTxCounter);
            NativeMethods.PdhAddEnglishCounter(hQuery, "\\Network Interface(*)\\Bytes Received/Sec", IntPtr.Zero, out hRxCounter);
            NativeMethods.PdhCollectQueryData(hQuery);
        }
        public void Update()
        {
            NativeMethods.PdhCollectQueryData(hQuery);
            BytesSentPerSec = ReadLongCounter(hTxCounter);
            BytesReceivedPerSec = ReadLongCounter(hRxCounter);
        }
        private long ReadLongCounter(IntPtr hCounter)
        {
            const int PDH_CSTATUS_VALID_DATA = 0;
            const int PDH_CSTATUS_NEW_DATA = 1;

            long ret = 0;

            int size = 0;
            int count = 0;
            NativeMethods.PdhGetFormattedCounterArray(hCounter, PDH_FMT.LARGE | PDH_FMT.NOSCALE | PDH_FMT.NOCAP100, ref size, ref count, IntPtr.Zero);

            if (size > buffer.Length)
                buffer = new byte[size];

            unsafe
            {
                fixed (byte* bufferPtr = buffer)
                {
                    NativeMethods.PdhGetFormattedCounterArray(hCounter, PDH_FMT.LARGE | PDH_FMT.NOSCALE | PDH_FMT.NOCAP100, ref size, ref count, (IntPtr)bufferPtr);

                    int stride = (IntPtr.Size == 8) ? 24 : 16;
                    int statusOffset = IntPtr.Size;
                    int largeValueOffset = IntPtr.Size * 2;
                    for (int i = 0; i < count; ++i)
                    {
#if false
                        PDH_FMT_COUNTERVALUE_ITEM item = (PDH_FMT_COUNTERVALUE_ITEM)Marshal.PtrToStructure((IntPtr)(bufferPtr + i * stride), typeof(PDH_FMT_COUNTERVALUE_ITEM));
                        ret += item.FmtValue.largeValue;
#else
                        byte* itemPtr = bufferPtr + i * stride;
                        int CStatus = *(int*)(itemPtr + statusOffset);
                        if ((CStatus == PDH_CSTATUS_NEW_DATA) || (CStatus == PDH_CSTATUS_VALID_DATA))
                            ret += *(long*)(itemPtr + largeValueOffset);
#endif
                    }

                }
            }

            return ret;
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Release managed resources
            }

            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.
            NativeMethods.PdhCloseQuery(hQuery);
            buffer = null;
            disposed = true;
            base.Dispose(disposing);
        }

        public long BytesSentPerSec { get; private set; }
        public long BytesReceivedPerSec { get; private set; }

#if false   // Not used due to inability to compile without platform-dependence
        [StructLayout(LayoutKind.Explicit)]
        private struct PDH_FMT_COUNTERVALUE
        {
            const int PTRSIZE = IntPtr.Size;    // the problematic line

            [FieldOffset(0)]
            public int CStatus;
            [FieldOffset(PTRSIZE)]
            public int longValue;
            [FieldOffset(PTRSIZE)]
            public double doubleValue;
            [FieldOffset(PTRSIZE)]
            public long largeValue;
            [FieldOffset(PTRSIZE)]
            public IntPtr AnsiStringValue;
            [FieldOffset(PTRSIZE)]
            public IntPtr WideStringValue;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PDH_FMT_COUNTERVALUE_ITEM
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string szName;
            public PDH_FMT_COUNTERVALUE FmtValue;
        }
#endif

        [Flags]
        private enum PDH_FMT
        {
            DOUBLE = 0x00000200,
            LARGE = 0x00000400,
            LONG = 0x00000100,
            NOSCALE = 0x00001000,
            NOCAP100 = 0x00008000,
            Scale1000 = 0x00002000
        }

        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("pdh", CharSet = CharSet.Unicode)]
            internal static extern int PdhOpenQuery(string szDataSource, IntPtr dwUserData, [Out] out IntPtr phQuery);

            [DllImport("pdh", CharSet = CharSet.Unicode)]
            internal static extern int PdhAddEnglishCounter(IntPtr hQuery, string szFullCounterPath, IntPtr dwUserData, [Out] out IntPtr phCounter);

            [DllImport("pdh", CharSet = CharSet.Unicode)]
            internal static extern int PdhCollectQueryData(IntPtr hQuery);

            [DllImport("pdh", CharSet = CharSet.Unicode)]
            internal static extern int PdhGetFormattedCounterArray(IntPtr hCounter, PDH_FMT dwFormat, ref int lpdwBufferSize, ref int lpdwItemCount, IntPtr ItemBuffer);

            [DllImport("pdh", CharSet = CharSet.Unicode)]
            internal static extern int PdhCloseQuery(IntPtr hQuery);
        }
    }
}
