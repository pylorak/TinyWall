using System.Collections.Generic;
using System.Threading;
using pylorak.Utilities;

namespace pylorak.TinyWall
{
    public sealed class BoundedMessageQueue : Disposable
    {
        private readonly List<TwMessage> MsgQueue = new();
        private readonly List<Future<TwMessage>> FutureQueue = new();
        private readonly Semaphore BoundSema = new(0, 64);
        private readonly object locker = new();

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                BoundSema.Close();
            }

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
