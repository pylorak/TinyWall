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
        private IntPtr hMonitor;

        public TrafficRateMonitor()
        {
            hMonitor = NativeMethods.TrafficMonitor_Create();
        }

        public void Update()
        {
            NativeMethods.TrafficMonitor_Update(hMonitor, out long txTotal, out long rxTotal);
            BytesSentPerSec = txTotal;
            BytesReceivedPerSec = rxTotal;
        }

        public long BytesSentPerSec { get; private set; }
        public long BytesReceivedPerSec { get; private set; }

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
            NativeMethods.TrafficMonitor_Delete(hMonitor);
            disposed = true;
            base.Dispose(disposing);
        }

        [SuppressUnmanagedCodeSecurity]
        protected static class NativeMethods
        {
            [DllImport("NativeHelper")]
            internal static extern IntPtr TrafficMonitor_Create();

            [DllImport("NativeHelper")]
            internal static extern void TrafficMonitor_Update(IntPtr monitor, out long txBytesPerSec, out long rxBytesPerSec);

            [DllImport("NativeHelper")]
            internal static extern void TrafficMonitor_Delete(IntPtr monitor);
        }
    }
}
