using System.Collections.Generic;
using System.Threading;

namespace TinyWall.Interface.Internal
{
    public sealed class BoundedMessageQueue : Disposable
    {
        private List<TwMessage> MsgQueue = new List<TwMessage>();
        private List<Future<TwMessage>> FutureQueue = new List<Future<TwMessage>>();
        private Semaphore BoundSema = new Semaphore(0, 64);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                BoundSema.Close();
            }

            MsgQueue = null;
            FutureQueue = null;
            BoundSema = null;

            base.Dispose(disposing);
        }

        public void Enqueue(TwMessage msg, Future<TwMessage> future)
        {
            lock (BoundSema)
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
                Thread.Sleep(10);
                goto RetrySema;
            }
        }

        public void Dequeue(out TwMessage msg, out Future<TwMessage> future)
        {
            BoundSema.WaitOne();
            lock (BoundSema)
            {
                msg = MsgQueue[0];
                future = FutureQueue[0];
                MsgQueue.RemoveAt(0);
                FutureQueue.RemoveAt(0);
            }
        }

        public bool HasMessageType(MessageType type)
        {
            lock (BoundSema)
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
