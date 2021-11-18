using System;
using System.Threading;

namespace pylorak.Utilities
{
    public class EventMerger : IDisposable
    {
        private readonly int MaxEventLatencyMs;
        private readonly Timer DelayTimer;
        private readonly object Locker = new object();

        public event EventHandler Event;
        private bool TimerActive;
        private bool Disposed;

        public EventMerger(int maxLatencyMs)
        {
            MaxEventLatencyMs = maxLatencyMs;
            DelayTimer = new Timer(DelayExpired);
        }

        public void Pulse()
        {
            if (Disposed)
                throw new ObjectDisposedException(GetType().FullName);

            lock (Locker)
            {
                if (!TimerActive)
                {
                    TimerActive = true;
                    DelayTimer.Change(MaxEventLatencyMs, Timeout.Infinite);
                }
            }
        }

        private void DelayExpired(object args)
        {
            lock(Locker)
            {
                TimerActive = false;
            }

            ThreadPool.QueueUserWorkItem(o => Event?.Invoke(this, EventArgs.Empty));        
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                Disposed = true;
                if (disposing)
                {
                    DelayTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    using (var wh = new ManualResetEvent(false))
                    {
                        DelayTimer.Dispose(wh);
                        wh.WaitOne();
                    }
                }
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
