using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Threading;
using pylorak.Utilities;

namespace pylorak.TinyWall
{
    public class PipeClientEndpoint
    {
        private readonly object SenderSyncRoot = new();
        private readonly string m_PipeName;

        public PipeClientEndpoint(string clientPipeName)
        {
            m_PipeName = clientPipeName;
        }

        private void SendRequest(TwRequest req)
        {
            TwMessage ret = TwMessageComError.Instance;
            lock (SenderSyncRoot)
            {
                // In case of a communication error,
                // retry a small number of times.
                for (int i = 0; i < 2; ++i)
                {
                    var resp = SendRequest(req.Request);
                    if (resp.Type != MessageType.COM_ERROR)
                    {
                        ret = resp;
                        break;
                    }

                    Thread.Sleep(200);
                }
            }

            req.Response = ret;
        }

        private TwMessage SendRequest(TwMessage msg)
        {
            try
            {
                using var pipeClient = new NamedPipeClientStream (".", m_PipeName, PipeDirection.InOut, PipeOptions.WriteThrough);
                pipeClient.Connect(1000);
                pipeClient.ReadMode = PipeTransmissionMode.Message;

                // Send command
                SerializationHelper.SerializeToPipe<TwMessage>(pipeClient, msg);

                // Get response
                return SerializationHelper.DeserializeFromPipe<TwMessage>(pipeClient, 20000, TwMessageComError.Instance);
            }
            catch
            {
                return TwMessageComError.Instance;
            }
        }

        public TwRequest QueueMessage(TwMessage msg)
        {
            var req = new TwRequest(msg);
            SendRequest(req);
            return req;
        }
    }
}
