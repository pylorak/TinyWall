using System;
using System.IO.Pipes;
using System.Threading;

namespace TinyWall.Interface.Internal
{
    public class PipeClientEndpoint : Disposable
    {
        private readonly Thread m_PipeWorkerThread;
        private readonly BoundedMessageQueue m_Queue = new BoundedMessageQueue();
        private readonly string m_PipeName;

        private bool m_Run = true;
        private bool disposed = false;

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            m_Run = false;

            if (disposing)
            {
                // Release managed resources
                QueueMessageSimple(MessageType.WAKE_CLIENT_SENDER_QUEUE);
                m_PipeWorkerThread.Join(TimeSpan.FromMilliseconds(2000));
                m_Queue.Dispose();
            }

            disposed = true;
            base.Dispose(disposing);
        }

        public PipeClientEndpoint(string clientPipeName)
        {
            m_PipeName = clientPipeName;
            m_PipeWorkerThread = new Thread(new ThreadStart(PipeClientWorker));
            m_PipeWorkerThread.IsBackground = true;
            m_PipeWorkerThread.Start();
        }

        private void PipeClientWorker()
        {
            while (m_Run)
            {
                m_Queue.Dequeue(out TwMessage msg, out Future<TwMessage> future);
                if (msg.Type == MessageType.WAKE_CLIENT_SENDER_QUEUE)
                {
                    future.Value = new TwMessage(MessageType.RESPONSE_OK);
                    continue;
                }

                // In case of a communication error,
                // retry a small number of times.
                TwMessage response = new TwMessage();
                for (int i = 0; i < 2; ++i)
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
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", m_PipeName, PipeDirection.InOut, PipeOptions.WriteThrough))
                {
                    pipeClient.Connect(1000);
                    pipeClient.ReadMode = PipeTransmissionMode.Message;

                    // Send command
                    SerializationHelper.SerializeToPipe(pipeClient, msg);

                    // Get response
                    return SerializationHelper.DeserializeFromPipe<TwMessage>(pipeClient, 20000);
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
