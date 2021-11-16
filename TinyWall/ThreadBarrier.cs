using System.Threading;
using TinyWall.Interface.Internal;

namespace PKSoft
{
    internal class ThreadBarrier : Disposable
    {
        private readonly ManualResetEvent BarrierEvent;
        private int Count;

        internal ThreadBarrier(int count)
        {
            BarrierEvent = new ManualResetEvent(false);
            Count = count;
        }

        internal void Wait()
        {
            Interlocked.Decrement(ref Count);
            if (Count > 0)
                BarrierEvent.WaitOne();
            else
                BarrierEvent.Set();
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                // Release managed resources

                BarrierEvent.Close();
            }

            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.

            base.Dispose(disposing);
        }
    }
}
