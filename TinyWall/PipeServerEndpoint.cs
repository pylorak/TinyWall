using System;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using pylorak.Utilities;

namespace pylorak.TinyWall
{
    internal delegate TwMessage PipeDataReceived(TwMessage req);

    internal class PipeServerEndpoint : Disposable
    {
        private readonly Thread m_PipeWorkerThread;
        private readonly PipeDataReceived m_RcvCallback;
        private readonly string m_PipeName;

        private bool m_Run = true;

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            m_Run = false;

            // Create a dummy connection so that worker thread gets out of the infinite WaitForConnection()
            using (var npcs = new NamedPipeClientStream(m_PipeName))
            {
                npcs.Connect(500);
            }

            if (disposing)
            {
                // Release managed resources
                m_PipeWorkerThread.Join(TimeSpan.FromMilliseconds(1000));
            }

            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.
            base.Dispose(disposing);
        }

        internal PipeServerEndpoint(PipeDataReceived recvCallback, string serverPipeName)
        {
            m_RcvCallback = recvCallback;
            m_PipeName = serverPipeName;

            m_PipeWorkerThread = new Thread(new ThreadStart(PipeServerWorker));
            m_PipeWorkerThread.Name = "ServerPipeWorker";
            m_PipeWorkerThread.IsBackground = true;
            m_PipeWorkerThread.Start();
        }

        private void PipeServerWorker()
        {
            // Allow authenticated users access to the pipe
            SecurityIdentifier AuthenticatedSID = new(WellKnownSidType.AuthenticatedUserSid, null);
            PipeAccessRule par = new(AuthenticatedSID, PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow);
            PipeSecurity ps = new();
            ps.AddAccessRule(par);

            while (m_Run)
            {
                try
                {
                    // Create pipe server
                    using var pipeServer = new NamedPipeServerStream(m_PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.WriteThrough, 2048 * 10, 2048 * 10, ps);
                    if (!pipeServer.IsConnected)
                    {
                        pipeServer.WaitForConnection();
                        pipeServer.ReadMode = PipeTransmissionMode.Message;

                        if (!AuthAsServer(pipeServer))
                            throw new InvalidOperationException("Client authentication failed.");
                    }

                    var req = SerialisationHelper.DeserialiseFromPipe<TwMessage>(pipeServer, 3000, TwMessageComError.Instance);
                    var resp = m_RcvCallback(req);
                    SerialisationHelper.SerialiseToPipe(pipeServer, resp);
                }
                catch
                {
                    Thread.Sleep(200);
                }
            } //while
        }

        private static bool AuthAsServer(PipeStream stream)
        {
#if !DEBUG
            if (!Utils.SafeNativeMethods.GetNamedPipeClientProcessId(stream.SafePipeHandle.DangerousGetHandle(), out ulong clientPid))
                return false;

            string clientFilePath = Utils.GetPathOfProcess((uint)clientPid);

            return clientFilePath.Equals(pylorak.Windows.ProcessManager.ExecutablePath, StringComparison.OrdinalIgnoreCase);
#else
            return true;
#endif
        }
    }
}
