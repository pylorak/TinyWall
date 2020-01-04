using System;
using System.IO.Pipes;
using System.Threading;
using System.IO;

namespace TinyWall.Interface.Internal
{
    public class PipeClientEndpoint : Disposable
    {
        private static readonly string PipeName = "TinyWallController";

        private bool disposed = false;
        private Thread m_PipeWorkerThread;
        private BoundedMessageQueue m_Queue = new BoundedMessageQueue();
        private bool m_Run = true;

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            m_Run = false;

            if (disposing)
            {
                // Release managed resources
                m_PipeWorkerThread.Join(TimeSpan.FromMilliseconds(2000));
                m_Queue.Dispose();
            }

            m_PipeWorkerThread = null;
            m_Queue = null;
            disposed = true;
            base.Dispose(disposing);
        }

        public PipeClientEndpoint(string clientPipeName)
        {
            // Start thread that is going to do the actual communication
            m_PipeWorkerThread = new Thread(new ThreadStart(PipeClientWorker));
            m_PipeWorkerThread.IsBackground = true;
            m_PipeWorkerThread.Start();
        }

        private void PipeClientWorker()
        {
            while (m_Run)
            {
                TwMessage msg;
                Future<TwMessage> future;
                m_Queue.Dequeue(out msg, out future);

                // In case of a communication error,
                // retry a small number of times.
                TwMessage response = new TwMessage();
                for (int i = 0; i < 3; ++i)
                {
                    response = SenderProcessor(msg);
                    if (response.Type != MessageType.COM_ERROR)
                        break;

                    Thread.Sleep(200);
                }

                future.Value = response;
            }
        }

        private TwMessage SenderProcessor(TwMessage msg)
        {
            try
            {
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.WriteThrough))
                {
                    pipeClient.Connect(500);

                    // Send command
                    SerializationHelper.SerializeToPipe(pipeClient, msg);

                    // Get response
                    return SerializationHelper.DeserializeFromPipe<TwMessage>(pipeClient);
                }
            }
            catch
            {
                return new TwMessage(MessageType.COM_ERROR);
            }
        }

        public Future<TwMessage> QueueMessage(TwMessage msg)
        {
            Future<TwMessage> future = new Future<TwMessage>();
            m_Queue.Enqueue(msg, future);
            return future;
        }

        public TwMessage QueueMessageSimple(MessageType cmd)
        {
            using (Future<TwMessage> f = QueueMessage(new TwMessage(cmd)))
            {
                return f.Value;
            }
        }

        public TwMessage QueueMessageSimple(MessageType cmd, object arg0)
        {
            using (Future<TwMessage> f = QueueMessage(new TwMessage(cmd, arg0)))
            {
                return f.Value;
            }
        }

        public TwMessage QueueMessageSimple(MessageType cmd, object arg0, object arg1)
        {
            using (Future<TwMessage> f = QueueMessage(new TwMessage(cmd, arg0, arg1)))
            {
                return f.Value;
            }
        }
    }
}
