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

        protected override void Dispose(bool disposing)
        {
            m_Run = true;

            if (disposing)
            {
                // Release managed resources
                m_PipeWorkerThread.Join(TimeSpan.FromMilliseconds(5000));
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
                    using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough, 2048, 2048, ps))
                    {
                        if (!pipeServer.IsConnected)
                        {
                            pipeServer.WaitForConnection();
                            if (!AuthAsServer(pipeServer))
                                throw new InvalidOperationException("Client authentication failed.");
                        }

                        // Read msg
                        TwMessage msg = SerializationHelper.Deserialize<TwMessage>(pipeServer);

                        // Write response
                        TwMessage resp = m_RcvCallback(msg);
                        SerializationHelper.Serialize(pipeServer, resp);
                    } //using
                }
                catch { }
            } //while
        }

        private bool AuthAsServer(PipeStream stream)
        {
#if !DEBUG
            long clientPid;
            if (!NativeMethods.GetNamedPipeClientProcessId(stream.SafePipeHandle.DangerousGetHandle(), out clientPid))
                return false;

            using (Process client = Process.GetProcessById((int)clientPid))
            using (Process server = Process.GetCurrentProcess())
            {
                if (client.MainModule.FileName.Equals(server.MainModule.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                else
                    return false;
            }
#else
            return true;
#endif
        }
    }
}
