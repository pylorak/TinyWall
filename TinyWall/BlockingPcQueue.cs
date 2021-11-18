using System;
using System.Collections.Generic;
using System.Threading;

namespace PKSoft
{
    // TODO: remove if not used
    public class BlockingPcQueue<T>
    {
        private readonly Queue<T> Q = new Queue<T>();

        public void Enqueue(T item)
        {
            lock (SyncRoot)
            {
                Q.Enqueue(item);
                Monitor.Pulse(SyncRoot);
            }
        }

        public bool Dequeue(ref T item, int millisecondsTimeout = Timeout.Infinite)
        {
            lock (SyncRoot)
            {
                while (!IsShutdown && (Q.Count == 0))
                {
                    bool success = Monitor.Wait(SyncRoot, millisecondsTimeout);
                    if (!success)
                        return false;
                }

                if (IsShutdown)
                    return false;

                item = Q.Dequeue();
            }
            return true;
        }

        public void Shutdown()
        {
            lock (SyncRoot)
            {
                IsShutdown = true;
                Monitor.Pulse(SyncRoot);
            }
        }

        public int Count
        {
            get
            {
                lock(SyncRoot)
                {
                    return Q.Count;
                }
            }
        }

        public void Clear()
        {
            lock (SyncRoot)
            {
                Q.Clear();
            }
        }

        public object SyncRoot { get; } = new object();

        public bool IsShutdown { get; private set; }
    }
}
