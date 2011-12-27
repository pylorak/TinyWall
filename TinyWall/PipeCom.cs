using System;
using System.Threading;
using System.Security.Principal;
using System.Globalization;
using System.IO.Pipes;
using System.IO;
using System.Text;

namespace PKSoft
{
    internal delegate Message PipeDataReceived(Message req);

    internal class PipeCom : DisposableObject
    {
        private const string CLIENT_PUBLIC_KEY = "A036E6F1E41F224B33F498535F7DF9B9382BA82AB3E028ABEDC4A6C13D701B73";
        private const string SERVER_PUBLIC_KEY = "9B046814B7CF7EF8CA7D9142C09E9F4943F458F67C1598CEE6A9BB473828DA2A";

        private bool m_RunThreads = true;
        private PipeStream m_Pipe;
        private Thread m_PipeWorkerThread;
        private RequestQueue m_ReqQueue;
        private PipeDataReceived m_RcvCallback;

        protected override void DisposeManaged()
        {
            m_RunThreads = false;
            m_ReqQueue.Dispose();
            m_Pipe.Dispose();
            base.DisposeManaged();
        }

        internal PipeCom(string pipeName, PipeDataReceived recvCallback)
        {
            // Allow authenticated users access to the pipe
            SecurityIdentifier AuthenticatedSID = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            PipeAccessRule par = new PipeAccessRule(AuthenticatedSID, PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow);
            PipeSecurity ps = new PipeSecurity();
            ps.AddAccessRule(par);            

            // Create pipe server
            m_Pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 2048, 2048, ps);
            m_RcvCallback = recvCallback;

            // Start thread that is going to do the actual communication
            m_PipeWorkerThread = new Thread(new ThreadStart(PipeServerWorker));
            m_PipeWorkerThread.IsBackground = true;
            m_PipeWorkerThread.Start();
        }

        internal PipeCom(string pipeName)
        {
            // Create pipe client
            m_Pipe = new NamedPipeClientStream(pipeName);

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
            NamedPipeClientStream PipeClient = m_Pipe as NamedPipeClientStream;
            try
            {
                if (!PipeClient.IsConnected)
                {
                    PipeClient.Connect(50);
                    if (!PipeClient.IsConnected)
                        return new Message(TinyWallCommands.COM_ERROR);
                    else if (!AuthAsClient())
                        return new Message(TinyWallCommands.COM_ERROR);
                }

                // Send command
                WriteMsg(msg);

                // Get response
                return ReadMsg();
            }
            catch
            {
                return new Message(TinyWallCommands.COM_ERROR);
            }
        }

        private void PipeClientWorker()
        {
            while (m_RunThreads)
            {
                ReqResp req = m_ReqQueue.Dequeue();
                req.Response = SenderProcessor(req.Request);
                req.SignalResponse();
            }
        }

        internal ReqResp QueueMessage(Message msg)
        {
            ReqResp resp = new ReqResp(msg);
            m_ReqQueue.Enqueue(resp);
            return resp;
        }

        internal Message QueueMessageSimple(TinyWallCommands cmd)
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
            if ((cli.Command != TinyWallCommands.VERIFY_KEYS) || ((string)cli.Arguments[0] != CLIENT_PUBLIC_KEY))
                return false;

            WriteMsg(new Message(TinyWallCommands.VERIFY_KEYS, SERVER_PUBLIC_KEY));
            return true;
        }

        private bool AuthAsClient()
        {
            WriteMsg(new Message(TinyWallCommands.VERIFY_KEYS, CLIENT_PUBLIC_KEY));

            Message srv = ReadMsg();
            if ((srv.Command != TinyWallCommands.VERIFY_KEYS) || ((string)srv.Arguments[0] != SERVER_PUBLIC_KEY))
                return false;

            return true;
        }

    }
}
