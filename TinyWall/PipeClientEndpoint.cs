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
            TwMessage resp = default;

            lock (SenderSyncRoot)
            {
                // In case of a communication error,
                // retry a small number of times.
                for (int i = 0; i < 2; ++i)
                {
                    resp = SendRequest(req.Request);
                    if (resp.Type != MessageType.COM_ERROR)
                        break;

                    Thread.Sleep(200);
                }
            }

            req.Response = resp;
        }

        private TwMessage SendRequest(TwMessage msg)
        {
            try
            {
                using var pipeClient = new NamedPipeClientStream (".", m_PipeName, PipeDirection.InOut, PipeOptions.WriteThrough);
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
            SendRequest(req);
            return req;
        }
    }
}
