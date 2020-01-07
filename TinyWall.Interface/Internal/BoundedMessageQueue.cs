using System.Collections.Generic;
using System.Threading;

namespace TinyWall.Interface.Internal
{
    public sealed class BoundedMessageQueue : Disposable
    {
        private bool disposed = false;
        private List<TwMessage> MsgQueue = new List<TwMessage>();
        private List<Future<TwMessage>> FutureQueue = new List<Future<TwMessage>>();
        private Semaphore BoundSema = new Semaphore(0, 64);
        private readonly object locker = new object();

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                BoundSema.Close();
            }

            MsgQueue = null;
            FutureQueue = null;
            BoundSema = null;
            disposed = true;
            base.Dispose(disposing);
        }

        public void Enqueue(TwMessage msg, Future<TwMessage> future)
        {
            lock (locker)
            {
                MsgQueue.Add(msg);
                FutureQueue.Add(future);
            }

            RetrySema:
            try
            {
                BoundSema.Release();
            }
            catch (SemaphoreFullException)
            {
                Thread.Sleep(50);
                goto RetrySema;
            }
        }

        public void Dequeue(out TwMessage msg, out Future<TwMessage> future)
        {
            BoundSema.WaitOne();
            lock (locker)
            {
                msg = MsgQueue[0];
                future = FutureQueue[0];
                MsgQueue.RemoveAt(0);
                FutureQueue.RemoveAt(0);
            }
        }

        public bool HasMessageType(MessageType type)
        {
            lock (locker)
            {
                for (int i = 0; i < MsgQueue.Count; ++i)
                {
                    if (MsgQueue[i].Type == type)
                        return true;
                }
            }

            return false;
        }
    }
}
