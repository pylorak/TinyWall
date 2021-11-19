using System.Threading;

namespace pylorak.Utilities
{
    public sealed class ThreadBarrier : Disposable
    {
        private readonly ManualResetEvent BarrierEvent;
        private int Count;

        public ThreadBarrier(int count)
        {
            BarrierEvent = new ManualResetEvent(false);
            Count = count;
        }

        public void Wait()
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
                BarrierEvent.Close();
            }

            base.Dispose(disposing);
        }
    }
}
