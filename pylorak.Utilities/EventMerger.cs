using System;
using System.Threading;

namespace pylorak.Utilities
{
    public sealed class EventMerger : Disposable
    {
        private readonly int MaxEventLatencyMs;
        private readonly Timer DelayTimer;
        private readonly object Locker = new();

        public event EventHandler? Event;
        private bool TimerActive;

        public EventMerger(int maxLatencyMs)
        {
            MaxEventLatencyMs = maxLatencyMs;
            DelayTimer = new Timer(DelayExpired);
        }

        public void Pulse()
        {
            if (IsDisposed)
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

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                DelayTimer.Change(Timeout.Infinite, Timeout.Infinite);

                using var wh = new ManualResetEvent(false);
                DelayTimer.Dispose(wh);
                wh.WaitOne();
            }

            base.Dispose(disposing);
        }
    }
}
