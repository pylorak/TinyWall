using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Threading;
using pylorak.Utilities;

namespace pylorak.TinyWall
{
    public class PipeClientEndpoint : Disposable
    {
        private readonly Thread m_PipeWorkerThread;
        private readonly BlockingCollection<TwRequest> m_Queue = new(32);
        private readonly CancellationTokenSource Cancellation = new();
        private readonly string m_PipeName;

        private volatile bool m_Run = true;

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            m_Run = false;

            if (disposing)
            {
                // Release managed resources
                Cancellation.Cancel();
                m_PipeWorkerThread.Join(TimeSpan.FromMilliseconds(2000));
                m_Queue.Dispose();
            }

            base.Dispose(disposing);
        }

        public PipeClientEndpoint(string clientPipeName)
        {
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
                TwRequest req;

                try
                {
                    req = m_Queue.Take(Cancellation.Token);
                }
                catch(OperationCanceledException)
                {
                    continue;
                }

                // In case of a communication error,
                // retry a small number of times.
                TwMessage resp = default;
                for (int i = 0; i < 2; ++i)
                {
                    resp = SenderProcessor(req.Request);
                    if (resp.Type != MessageType.COM_ERROR)
                        break;

                    Thread.Sleep(200);
                }

                req.Response = resp;
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

        public TwRequest QueueMessage(MessageType reqType, params object[] args)
        {
            var req = new TwRequest(reqType, args);
            m_Queue.Add(req);
            return req;
        }
    }
}
