using System;
using System.Collections.Generic;
using TinyWall.Interface.Internal;

namespace TinyWall.Interface
{
    public sealed class Controller : Disposable
    {
        private bool disposed = false;
        private PipeClientEndpoint Endpoint;

        public Controller(string serverEndpoint)
        {
            Endpoint = new PipeClientEndpoint(serverEndpoint);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                Endpoint.Dispose();
            }

            Endpoint = null;
            disposed = true;
            base.Dispose(disposing);
        }

        public MessageType GetServerConfig(out ServerConfiguration serverConfig, out ServerState serverState, ref Guid changeset)
        {
            // Detect if server settings have changed in comparison to ours and download
            // settings only if we need them. Settings are "version numbered" using the "changeset"
            // property. We send our changeset number to the service and if it differs from his,
            // the service will send back the settings.

            serverConfig = null;
            serverState = null;

            Guid clientChangeset = changeset;
            Guid serverChangeset = Guid.Empty;

            TwMessage resp = Endpoint.QueueMessageSimple(MessageType.GET_SETTINGS, clientChangeset);
            if (resp.Type == MessageType.RESPONSE_OK)
            {
                serverChangeset = (Guid)resp.Arguments[0];
                changeset = serverChangeset;
                if (serverChangeset != clientChangeset)
                {
                    serverConfig = (ServerConfiguration)resp.Arguments[1];
                    serverState = (ServerState)resp.Arguments[2];
                }
            }

            return resp.Type;
        }

        public MessageType SetServerConfig(ref ServerConfiguration serverConfig, ref Guid changeset, out ServerState serverState)
        {
            serverState = null;

            TwMessage resp = Endpoint.QueueMessageSimple(MessageType.PUT_SETTINGS, serverConfig, changeset);

            if (resp.Arguments[0] is ServerConfiguration tmp)
            {
                serverConfig = tmp;
                changeset = (Guid)resp.Arguments[1];
                serverState = (ServerState)resp.Arguments[2];
            }

            return resp.Type;
        }

        public Future<TwMessage> BeginReadFwLog()
        {
            return Endpoint.QueueMessage(new TwMessage(MessageType.READ_FW_LOG));
        }

        public List<FirewallLogEntry> EndReadFwLog(Future<TwMessage> f)
        {
            try
            {
                if ((f.Value.Arguments != null) && (f.Value.Arguments.Length > 0) && (f.Value.Arguments[0] is List<FirewallLogEntry> ret))
                    return ret;
                else
                    // TODO: Do we want to show an error to the user?
                    return new List<FirewallLogEntry>();
            }
            finally
            {
                f.Dispose();
            }
        }

        public MessageType SwitchFirewallMode(FirewallMode mode)
        {
            return Endpoint.QueueMessageSimple(MessageType.MODE_SWITCH, mode).Type;
        }

        public MessageType RequestServerStop()
        {
            return Endpoint.QueueMessageSimple(MessageType.STOP_SERVICE).Type;
        }

        public bool IsServerLocked
        {
            get
            {
                TwMessage resp = Endpoint.QueueMessageSimple(MessageType.IS_LOCKED);
                if (MessageType.RESPONSE_OK == resp.Type)
                    return (bool)resp.Arguments[0];
                else
                    return false;
            }
        }

        public MessageType SetPassphrase(string pwd)
        {
            return Endpoint.QueueMessageSimple(MessageType.SET_PASSPHRASE, pwd).Type;
        }

        public MessageType TryUnlockServer(string pwd)
        {
            return Endpoint.QueueMessageSimple(MessageType.UNLOCK, pwd).Type;
        }

        public MessageType LockServer()
        {
            return Endpoint.QueueMessageSimple(MessageType.LOCK).Type;
        }

        public string TryGetProcessPath(int pid)
        {
            TwMessage resp = Endpoint.QueueMessageSimple(MessageType.GET_PROCESS_PATH, pid);
            if (resp.Type == MessageType.RESPONSE_OK)
                return resp.Arguments[0] as string;
            else
                return string.Empty;
        }
    }
}
