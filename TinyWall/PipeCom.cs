using System;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Diagnostics;

namespace PKSoft
{
    internal delegate Message PipeDataReceived(Message req);

    internal class PipeCom : DisposableObject
    {
        private static readonly string PipeName = "TinyWallController";

        private PipeStream m_Pipe;
        private Thread m_PipeWorkerThread;
        private RequestQueue m_ReqQueue;
        private PipeDataReceived m_RcvCallback;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release managed resources
                m_PipeWorkerThread.Abort();
                m_PipeWorkerThread.Join(TimeSpan.FromMilliseconds(500));
                if (m_ReqQueue != null)
                    m_ReqQueue.Dispose();
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

        internal PipeCom(PipeDataReceived recvCallback)
        {
            m_RcvCallback = recvCallback;

            // Start thread that is going to do the actual communication
            m_PipeWorkerThread = new Thread(new ThreadStart(PipeServerWorker));
            m_PipeWorkerThread.IsBackground = true;
            m_PipeWorkerThread.Start();
        }

        internal PipeCom(string clientPipeName)
        {
            m_ReqQueue = new RequestQueue();

            // Start thread that is going to do the actual communication
            m_PipeWorkerThread = new Thread(new ThreadStart(PipeClientWorker));
            m_PipeWorkerThread.IsBackground = true;
            m_PipeWorkerThread.Start();
        }

        private void PipeServerWorker()
        {
            // Allow authenticated users access to the pipe
            SecurityIdentifier AuthenticatedSID = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            PipeAccessRule par = new PipeAccessRule(AuthenticatedSID, PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow);
            PipeSecurity ps = new PipeSecurity();
            ps.AddAccessRule(par);

            bool abort = false;
            while (!abort)
            {
                try
                {
                    // Create pipe server
                    using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 2048, 2048, ps))
                    {
                        m_Pipe = pipeServer;

                        if (!pipeServer.IsConnected)
                        {
                            pipeServer.WaitForConnection();
                            if (!AuthAsServer())
                                throw new InvalidOperationException("Client authentication failed.");
                        }

                        while (true)
                        {
                            // Read msg
                            Message msg = ReadMsg();
                            Message resp = m_RcvCallback(msg);
                            WriteMsg(resp);
                        }
                    } //using
                }
                catch (ThreadAbortException)
                {
                    abort = true;
                }
                catch { }
                finally 
                {
                    m_Pipe = null;
                }
            } //while
        }

        private Message SenderProcessor(Message msg)
        {
            try
            {
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(PipeName))
                {
                    m_Pipe = pipeClient;

                    if (!pipeClient.IsConnected)
                    {
                        pipeClient.Connect(500);
                        if (!pipeClient.IsConnected)
                            return new Message(TWControllerMessages.COM_ERROR);
                        else if (!AuthAsClient())
                            return new Message(TWControllerMessages.COM_ERROR);
                    }

                    // Send command
                    WriteMsg(msg);

                    // Get response
                    return ReadMsg();
                }
            }
            catch
            {
                return new Message(TWControllerMessages.COM_ERROR);
            }
            finally
            {
                m_Pipe = null;
            }
        }

        private void PipeClientWorker()
        {
            while (true)
            {
                ReqResp req = m_ReqQueue.Dequeue();

                // In case of a communication error,
                // retry a small number of times.
                for (int i = 0; i < 3; ++i)
                {
                    req.Response = SenderProcessor(req.Request);
                    if (req.Request.Command != TWControllerMessages.COM_ERROR)
                        break;

                    System.Threading.Thread.Sleep(200);
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
#if !DEBUG
            long clientPid;
            if (!NativeMethods.GetNamedPipeClientProcessId(m_Pipe.SafePipeHandle.DangerousGetHandle(), out clientPid))
                return false;

            using (Process client = Process.GetProcessById((int)clientPid))
            {
                using (Process server = Process.GetCurrentProcess())
                {
                    if (client.MainModule.FileName.Equals(server.MainModule.FileName, StringComparison.OrdinalIgnoreCase))
                    {
                        WriteMsg(new Message(TWControllerMessages.RESPONSE_OK));
                        return true;
                    }
                    else
                        return false;
                }
            }
#else
            WriteMsg(new Message(TWControllerMessages.RESPONSE_OK));
            return true;
#endif
        }

        private bool AuthAsClient()
        {
            Message srv = ReadMsg();
            return (srv.Command == TWControllerMessages.RESPONSE_OK);
        }

    }
}
