using System;
using System.Collections.Generic;
using System.Threading;

namespace PKSoft
{
    internal class ThreadBarrier
    {
        private ManualResetEvent BarrierEvent;
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
    }
}
