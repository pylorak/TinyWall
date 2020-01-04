using System;
using System.Threading;
using TinyWall.Interface.Internal;

namespace PKSoft
{
    public sealed class ThreadThrottler : Disposable
    {
        bool disposed = false;
        private readonly Thread ThreadRef;
        private readonly ThreadPriority OriginalPriority;
        private readonly ThreadPriority RequestedPriority;
        private readonly object SynchRoot = new object();
        private int NumRequests = 0;

        public ThreadThrottler(ThreadPriority newPriority, bool autoRequest = false)
            : this(Thread.CurrentThread, newPriority, autoRequest)
        { }

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
            lock(SynchRoot)
            {
                if (NumRequests == 0)
                    ThreadRef.Priority = RequestedPriority;

                ++NumRequests;
            }
        }

        public void Release()
        {
            lock(SynchRoot)
            {
                --NumRequests;

                if (NumRequests == 0)
                    ThreadRef.Priority = OriginalPriority;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

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
