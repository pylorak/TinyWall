using System;
using System.Collections.Generic;
using pylorak.Utilities;

namespace pylorak.TinyWall
{
    public sealed class Controller : Disposable
    {
        private readonly PipeClientEndpoint Endpoint;

        public Controller(string serverEndpoint)
        {
            Endpoint = new PipeClientEndpoint(serverEndpoint);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                Endpoint.Dispose();
            }

            base.Dispose(disposing);
        }

        public MessageType GetServerConfig(out ServerConfiguration? serverConfig, out ServerState? serverState, ref Guid clientChangeset)
        {
            // Detect if server settings have changed in comparison to ours and download
            // settings only if we need them. Settings are "version numbered" using the "changeset"
            // property. We send our changeset number to the service and if it differs from his,
            // the service will send back the settings.

            serverConfig = null;
            serverState = null;

            var resp = Endpoint.QueueMessage(MessageType.GET_SETTINGS, clientChangeset).Response;
            if (resp.Type == MessageType.RESPONSE_OK)
            {
                var serverChangeset = (Guid)resp.Arguments[0];
                if (serverChangeset != clientChangeset)
                {
                    clientChangeset = serverChangeset;
                    serverConfig = (ServerConfiguration)resp.Arguments[1];
                    serverState = (ServerState)resp.Arguments[2];
                }
            }

            return resp.Type;
        }

        public MessageType SetServerConfig(ref ServerConfiguration serverConfig, ref Guid clientChangeset, out ServerState? serverState)
        {
            serverState = null;

            var resp = Endpoint.QueueMessage(MessageType.PUT_SETTINGS, serverConfig, clientChangeset).Response;

            if ((resp.Arguments != null) && (resp.Arguments.Length > 0) && (resp.Arguments[0] is ServerConfiguration tmp))
            {
                serverConfig = tmp;
                clientChangeset = (Guid)resp.Arguments[1];
                serverState = (ServerState)resp.Arguments[2];
            }

            if ((serverState == null) && (resp.Type == MessageType.RESPONSE_OK))
                resp.Type = MessageType.RESPONSE_ERROR;

            return resp.Type;
        }

        public TwRequest BeginReadFwLog()
        {
            return Endpoint.QueueMessage(MessageType.READ_FW_LOG);
        }

        public static List<FirewallLogEntry> EndReadFwLog(TwMessage twResp)
        {
            if ((twResp.Arguments != null) && (twResp.Arguments.Length > 0) && (twResp.Arguments[0] is List<FirewallLogEntry> ret))
                return ret;
            else
                // TODO: Do we want to show an error to the user?
                return new List<FirewallLogEntry>();
        }

        public MessageType SwitchFirewallMode(FirewallMode mode)
        {
            return Endpoint.QueueMessage(MessageType.MODE_SWITCH, mode).Response.Type;
        }

        public MessageType RequestServerStop()
        {
            return Endpoint.QueueMessage(MessageType.STOP_SERVICE).Response.Type;
        }

        public bool IsServerLocked
        {
            get
            {
                var resp = Endpoint.QueueMessage(MessageType.IS_LOCKED).Response;
                if (MessageType.RESPONSE_OK == resp.Type)
                    return (bool)resp.Arguments[0];
                else
                    return false;
            }
        }

        public MessageType SetPassphrase(string pwd)
        {
            return Endpoint.QueueMessage(MessageType.SET_PASSPHRASE, pwd).Response.Type;
        }

        public MessageType TryUnlockServer(string pwd)
        {
            return Endpoint.QueueMessage(MessageType.UNLOCK, pwd).Response.Type;
        }

        public MessageType LockServer()
        {
            return Endpoint.QueueMessage(MessageType.LOCK).Response.Type;
        }

        public string TryGetProcessPath(uint pid)
        {
            var resp = Endpoint.QueueMessage(MessageType.GET_PROCESS_PATH, pid).Response;
            if (resp.Type == MessageType.RESPONSE_OK)
            {
                return (resp.Arguments[0] as string)!;
            }
            else
                return string.Empty;
        }
    }
}
