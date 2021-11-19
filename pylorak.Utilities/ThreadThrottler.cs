using System;
using System.Threading;

namespace pylorak.Utilities
{
    public sealed class ThreadThrottler : Disposable
    {
        private readonly Thread ThreadRef;
        private readonly ThreadPriority OriginalPriority;
        private readonly ThreadPriority RequestedPriority;
        private int NumRequests = 0;
        public object SynchRoot { get; } = new();

        public ThreadThrottler(Thread thread, ThreadPriority newPriority, bool autoRequest = false)
        {
            ThreadRef = thread;
            OriginalPriority = thread.Priority;
            RequestedPriority = newPriority;

            if (autoRequest)
                Request();
        }

        public void Request()
        {
            if (NumRequests == 0)
                ThreadRef.Priority = RequestedPriority;

            ++NumRequests;
        }

        public void Release()
        {
            --NumRequests;

            if (NumRequests == 0)
                ThreadRef.Priority = OriginalPriority;
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            System.Diagnostics.Debug.Assert(NumRequests <= 1);

            if (disposing)
            {
                try { ThreadRef.Priority = OriginalPriority; } catch { }
            }

            base.Dispose(disposing);
        }
    }
}
