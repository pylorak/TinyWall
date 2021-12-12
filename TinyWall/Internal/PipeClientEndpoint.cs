using System;
using System.IO.Pipes;
using System.Threading;
using pylorak.Utilities;

namespace pylorak.TinyWall
{
    public class PipeClientEndpoint : Disposable
    {
        private readonly Thread m_PipeWorkerThread;
        private readonly BoundedMessageQueue m_Queue;
        private readonly string m_PipeName;

        private bool m_Run = true;

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            m_Run = false;

            if (disposing)
            {
                // Release managed resources
                QueueMessageSimple(MessageType.WAKE_CLIENT_SENDER_QUEUE);
                m_PipeWorkerThread.Join(TimeSpan.FromMilliseconds(2000));
                m_Queue.Dispose();
            }

            base.Dispose(disposing);
        }

        public PipeClientEndpoint(string clientPipeName)
        {
            m_Queue = new BoundedMessageQueue();
            m_PipeName = clientPipeName;
            m_PipeWorkerThread = new Thread(new ThreadStart(PipeClientWorker));
            m_PipeWorkerThread.Name = "ClientPipeWorker";
            m_PipeWorkerThread.IsBackground = true;
            m_PipeWorkerThread.Start();
        }

        private void PipeClientWorker()
        {
            while (m_Run)
            {
                m_Queue.Dequeue(out TwMessage msg, out Future<TwMessage>? future);
                if (msg.Type == MessageType.WAKE_CLIENT_SENDER_QUEUE)
                {
                    if (future is not null)
                        future.Value = new TwMessage(MessageType.RESPONSE_OK);
                    continue;
                }

                // In case of a communication error,
                // retry a small number of times.
                var response = new TwMessage();
                for (int i = 0; i < 2; ++i)
                {
                    response = SenderProcessor(msg);
                    if (response.Type != MessageType.COM_ERROR)
                        break;

                    Thread.Sleep(200);
                }

                if (future is not null)
                    future.Value = response;
            }
        }

        private TwMessage SenderProcessor(TwMessage msg)
        {
            try
            {
                using NamedPipeClientStream pipeClient = new(".", m_PipeName, PipeDirection.InOut, PipeOptions.WriteThrough);
                pipeClient.Connect(1000);
                pipeClient.ReadMode = PipeTransmissionMode.Message;

                // Send command
                SerializationHelper.SerializeToPipe(pipeClient, msg);

                // Get response
                var ret = new TwMessage(MessageType.COM_ERROR);
                SerializationHelper.DeserializeFromPipe<TwMessage>(pipeClient, 20000, ref ret);
                return ret;
            }
            catch
            {
                return new TwMessage(MessageType.COM_ERROR);
            }
        }

        public Future<TwMessage> QueueMessage(TwMessage msg)
        {
            var future = new Future<TwMessage>();
            m_Queue.Enqueue(msg, future);
            return future;
        }

        public TwMessage QueueMessageSimple(MessageType cmd)
        {
            using var f = QueueMessage(new TwMessage(cmd));
            return f.Value;
        }

        public TwMessage QueueMessageSimple(MessageType cmd, object arg0)
        {
            using var f = QueueMessage(new TwMessage(cmd, arg0));
            return f.Value;
        }

        public TwMessage QueueMessageSimple(MessageType cmd, object arg0, object arg1)
        {
            using var f = QueueMessage(new TwMessage(cmd, arg0, arg1));
            return f.Value;
        }
    }
}
