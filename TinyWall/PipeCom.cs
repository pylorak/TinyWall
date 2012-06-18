using System;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;

namespace PKSoft
{
    internal delegate Message PipeDataReceived(Message req);

    internal class PipeCom : DisposableObject
    {
        private const string CLIENT_PUBLIC_KEY = "A036E6F1E41F224B33F498535F7DF9B9382BA82AB3E028ABEDC4A6C13D701B73";
        private const string SERVER_PUBLIC_KEY = "9B046814B7CF7EF8CA7D9142C09E9F4943F458F67C1598CEE6A9BB473828DA2A";
        private readonly string PipeName;

        private bool m_RunThreads = true;
        private PipeStream m_Pipe;
        private Thread m_PipeWorkerThread;
        private RequestQueue m_ReqQueue;
        private PipeDataReceived m_RcvCallback;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release managed resources

                if (m_ReqQueue != null)
                    m_ReqQueue.Dispose();
                if (m_Pipe != null)
                    m_Pipe.Dispose();
            }

            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.

            m_Pipe = null;
            m_PipeWorkerThread = null;
            m_ReqQueue = null;
            m_RcvCallback = null;
            base.Dispose(disposing);
        }

        internal PipeCom(string serverPipeName, PipeDataReceived recvCallback)
        {
            PipeName = serverPipeName;

            // Allow authenticated users access to the pipe
            SecurityIdentifier AuthenticatedSID = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            PipeAccessRule par = new PipeAccessRule(AuthenticatedSID, PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow);
            PipeSecurity ps = new PipeSecurity();
            ps.AddAccessRule(par);            

            // Create pipe server
            m_Pipe = new NamedPipeServerStream(serverPipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 2048, 2048, ps);
            m_RcvCallback = recvCallback;

            // Start thread that is going to do the actual communication
            m_PipeWorkerThread = new Thread(new ThreadStart(PipeServerWorker));
            m_PipeWorkerThread.IsBackground = true;
            m_PipeWorkerThread.Start();
        }

        internal PipeCom(string clientPipeName)
        {
            PipeName = clientPipeName;
            m_ReqQueue = new RequestQueue();

            // Start thread that is going to do the actual communication
            m_PipeWorkerThread = new Thread(new ThreadStart(PipeClientWorker));
            m_PipeWorkerThread.IsBackground = true;
            m_PipeWorkerThread.Start();
        }

        private void PipeServerWorker()
        {
            NamedPipeServerStream pipeServer = m_Pipe as NamedPipeServerStream;
            while (m_RunThreads)
            {
                try
                {
                    if (!pipeServer.IsConnected)
                    {
                        pipeServer.WaitForConnection();
                        if (!AuthAsServer())
                            throw new InvalidOperationException("Client authentication failed.");
                    }

                    // Read msg
                    Message msg = ReadMsg();
                    Message resp = m_RcvCallback(msg);
                    WriteMsg(resp);
                }
                catch
                {
                    pipeServer.Disconnect();
                }
            } // while
        }

        private Message SenderProcessor(Message msg)
        {
            try
            {
                if (m_Pipe == null)
                {
                    // Create pipe client
                    m_Pipe = new NamedPipeClientStream(PipeName);
                }

                NamedPipeClientStream PipeClient = m_Pipe as NamedPipeClientStream;

                if (!PipeClient.IsConnected)
                {
                    PipeClient.Connect(200);
                    if (!PipeClient.IsConnected)
                        return new Message(TWControllerMessages.COM_ERROR);
                    else if (!AuthAsClient())
                        return new Message(TWControllerMessages.COM_ERROR);
                }

                // Send command
                WriteMsg(msg);

                // Get response
                return ReadMsg();
            }
            catch
            {
                if (m_Pipe != null)
                {
                    m_Pipe.Dispose();
                    m_Pipe = null;
                }
                return new Message(TWControllerMessages.COM_ERROR);
            }
        }

        private void PipeClientWorker()
        {
            while (m_RunThreads)
            {
                ReqResp req = m_ReqQueue.Dequeue();

                // In case of a communication error,
                // retry a small number of times.
                for (int i = 0; i < 2; ++i)
                {
                    req.Response = SenderProcessor(req.Request);
                    if (req.Request.Command != TWControllerMessages.COM_ERROR)
                        break;
                }

                req.SignalResponse();
            }
        }

        internal ReqResp QueueMessage(Message msg)
        {
            ReqResp resp = new ReqResp(msg);
            m_ReqQueue.Enqueue(resp);
            return resp;
        }

        internal Message QueueMessageSimple(TWControllerMessages cmd)
        {
            return QueueMessage(new Message(cmd)).GetResponse();
        }

        private Message ReadMsg()
        {
            return SerializationHelper.Deserialize<Message>(m_Pipe);
        }

        private void WriteMsg(Message msg)
        {
            SerializationHelper.Serialize(m_Pipe, msg);
            m_Pipe.Flush();
        }

        private bool AuthAsServer()
        {
            Message cli = ReadMsg();
            if ((cli.Command != TWControllerMessages.VERIFY_KEYS) || ((string)cli.Arguments[0] != CLIENT_PUBLIC_KEY))
                return false;

            WriteMsg(new Message(TWControllerMessages.VERIFY_KEYS, SERVER_PUBLIC_KEY));
            return true;
        }

        private bool AuthAsClient()
        {
            WriteMsg(new Message(TWControllerMessages.VERIFY_KEYS, CLIENT_PUBLIC_KEY));

            Message srv = ReadMsg();
            if ((srv.Command != TWControllerMessages.VERIFY_KEYS) || ((string)srv.Arguments[0] != SERVER_PUBLIC_KEY))
                return false;

            return true;
        }

    }
}
