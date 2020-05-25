using System;
using System.Threading;
using TinyWall.Interface.Internal;

namespace PKSoft
{
    public sealed class ThreadThrottler : Disposable
    {
        private readonly Thread ThreadRef;
        private readonly ThreadPriority OriginalPriority;
        private readonly ThreadPriority RequestedPriority;
        private int NumRequests = 0;
        private bool disposed = false;

        public ThreadThrottler(Thread thread, ThreadPriority newPriority, bool autoRequest = false, bool synchronized = false)
        {
            ThreadRef = thread;
            OriginalPriority = thread.Priority;
            RequestedPriority = newPriority;

            if (synchronized)
                SynchRoot = new object();

            if (autoRequest)
                Request();
        }

        public object SynchRoot { get; }

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
            if (disposed)
                return;

            System.Diagnostics.Debug.Assert(NumRequests <= 1);

            if (disposing)
            {
                try { ThreadRef.Priority = OriginalPriority; }
                catch { }
            }

            disposed = true;
            base.Dispose(disposing);
        }
    }
}
