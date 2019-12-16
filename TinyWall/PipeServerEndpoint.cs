using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using TinyWall.Interface.Internal;


namespace PKSoft
{
    internal delegate TwMessage PipeDataReceived(TwMessage req);

    internal class PipeServerEndpoint : Disposable
    {
        private static readonly string PipeName = "TinyWallController";

        private Thread m_PipeWorkerThread;
        private PipeDataReceived m_RcvCallback;
        private bool m_Run = true;
        private readonly string m_ServerFilePath;

        protected override void Dispose(bool disposing)
        {
            m_Run = false;

            // Create a dummy connection so that worker thread gets out of the infinite WaitForConnection()
            using (NamedPipeClientStream npcs = new NamedPipeClientStream(PipeName))
            {
                npcs.Connect(100);
            }

            if (disposing)
            {
                // Release managed resources
                m_PipeWorkerThread.Join(TimeSpan.FromMilliseconds(1000));
            }

            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.

            m_PipeWorkerThread = null;
            m_RcvCallback = null;
            base.Dispose(disposing);
        }

        internal PipeServerEndpoint(PipeDataReceived recvCallback)
        {
            m_RcvCallback = recvCallback;

            using (Process server = Process.GetCurrentProcess())
            {
                m_ServerFilePath = server.MainModule.FileName;
            }

            // Start thread that is going to do the actual communication
            m_PipeWorkerThread = new Thread(new ThreadStart(PipeServerWorker));
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

            while (m_Run)
            {
                try
                {
                    // Create pipe server
                    using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough, 2048*10, 2048*10, ps))
                    {
                        if (!pipeServer.IsConnected)
                        {
                            pipeServer.WaitForConnection();
                            if (!AuthAsServer(pipeServer))
                                throw new InvalidOperationException("Client authentication failed.");
                        }

                        // Read msg
                        TwMessage msg = SerializationHelper.DeserializeFromPipe<TwMessage>(pipeServer);

                        // Write response
                        TwMessage resp = m_RcvCallback(msg);
                        SerializationHelper.SerializeToPipe(pipeServer, resp);
                    } //using
                }
                catch { }
            } //while
        }

        private bool AuthAsServer(PipeStream stream)
        {
#if !DEBUG
            long clientPid;
            if (!Utils.SafeNativeMethods.GetNamedPipeClientProcessId(stream.SafePipeHandle.DangerousGetHandle(), out clientPid))
                return false;

            string clientFilePath = Utils.GetPathOfProcess((int)clientPid);

            return clientFilePath.Equals(m_ServerFilePath, StringComparison.OrdinalIgnoreCase);
#else
            return true;
#endif
        }
    }
}
