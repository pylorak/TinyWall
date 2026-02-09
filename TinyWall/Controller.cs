using System;
using System.Threading;
using System.Threading.Tasks;

namespace pylorak.TinyWall
{
    public sealed class Controller
    {
        private readonly PipeClientEndpoint Endpoint;

        public Controller(string serverEndpoint)
        {
            Endpoint = new PipeClientEndpoint(serverEndpoint);
        }

        #region Synchronous Methods (Legacy - prefer async versions)

        public MessageType GetServerConfig(out ServerConfiguration? serverConfig, out ServerState? serverState, ref Guid clientChangeset)
        {
            // Detect if server settings have changed in comparison to ours and download
            // settings only if we need them. Settings are "version numbered" using the "changeset"
            // property. We send our changeset number to the service and if it differs from his,
            // the service will send back the settings.

            serverConfig = null;
            serverState = null;

            var resp = Endpoint.QueueMessage(TwMessageGetSettings.CreateRequest(clientChangeset)).Response;

            if (resp.Type == MessageType.GET_SETTINGS)
            {
                var respArgs = (TwMessageGetSettings)resp;
                if (respArgs.Changeset != clientChangeset)
                {
                    clientChangeset = respArgs.Changeset;
                    serverConfig = respArgs.Config;
                    serverState = respArgs.State;
                }
            }

            return resp.Type;
        }

        public TwMessage SetServerConfig(ServerConfiguration serverConfig, Guid clientChangeset)
        {
            return Endpoint.QueueMessage(TwMessagePutSettings.CreateRequest(clientChangeset, serverConfig)).Response;
        }

        public TwRequest BeginReadFwLog()
        {
            return Endpoint.QueueMessage(TwMessageReadFwLog.CreateRequest());
        }

        public static FirewallLogEntry[] EndReadFwLog(TwMessage twResp)
        {
            if (twResp is TwMessageReadFwLog fwLog)
                return fwLog.Entries;
            else
                // TODO: Do we want to show an error to the user?
                return Array.Empty<FirewallLogEntry>();
        }

        public MessageType SwitchFirewallMode(FirewallMode mode)
        {
            return Endpoint.QueueMessage(TwMessageModeSwitch.CreateRequest(mode)).Response.Type;
        }

        public MessageType RequestServerStop()
        {
            return Endpoint.QueueMessage(TwMessageSimple.CreateRequest(MessageType.STOP_SERVICE)).Response.Type;
        }

        public bool IsServerLocked
        {
            get
            {
                var resp = Endpoint.QueueMessage(TwMessageIsLocked.CreateRequest()).Response;
                if (resp is TwMessageIsLocked isLockedResp)
                    return isLockedResp.LockedStatus;
                else
                    return false;
            }
        }

        public MessageType SetPassphrase(string pwd)
        {
            return Endpoint.QueueMessage(TwMessageSetPassword.CreateRequest(pwd)).Response.Type;
        }

        public MessageType TryUnlockServer(string pwd)
        {
            return Endpoint.QueueMessage(TwMessageUnlock.CreateRequest(pwd)).Response.Type;
        }

        public MessageType LockServer()
        {
            return Endpoint.QueueMessage(TwMessageSimple.CreateRequest(MessageType.LOCK)).Response.Type;
        }

        public string TryGetProcessPath(uint pid)
        {
            var resp = Endpoint.QueueMessage(TwMessageGetProcessPath.CreateRequest(pid)).Response;
            if (resp.Type == MessageType.GET_PROCESS_PATH)
            {
                var respArgs = (TwMessageGetProcessPath)resp;
                return respArgs.Path;
            }
            else
                return string.Empty;
        }

        #endregion

        #region Async Methods (Preferred - do not block UI thread)

        /// <summary>
        /// Gets server configuration asynchronously without blocking the calling thread.
        /// </summary>
        public async Task<(MessageType Type, ServerConfiguration? Config, ServerState? State, Guid Changeset)> GetServerConfigAsync(Guid clientChangeset, CancellationToken ct = default)
        {
            var resp = await Endpoint.QueueMessage(TwMessageGetSettings.CreateRequest(clientChangeset)).ResponseAsync.WaitAsync(ct);

            if (resp.Type == MessageType.GET_SETTINGS)
            {
                var respArgs = (TwMessageGetSettings)resp;
                if (respArgs.Changeset != clientChangeset)
                {
                    return (resp.Type, respArgs.Config, respArgs.State, respArgs.Changeset);
                }
            }

            return (resp.Type, null, null, clientChangeset);
        }

        /// <summary>
        /// Sets server configuration asynchronously without blocking the calling thread.
        /// </summary>
        public async Task<TwMessage> SetServerConfigAsync(ServerConfiguration serverConfig, Guid clientChangeset, CancellationToken ct = default)
        {
            return await Endpoint.QueueMessage(TwMessagePutSettings.CreateRequest(clientChangeset, serverConfig)).ResponseAsync.WaitAsync(ct);
        }

        /// <summary>
        /// Reads firewall log asynchronously without blocking the calling thread.
        /// </summary>
        public async Task<FirewallLogEntry[]> ReadFwLogAsync(CancellationToken ct = default)
        {
            var resp = await Endpoint.QueueMessage(TwMessageReadFwLog.CreateRequest()).ResponseAsync.WaitAsync(ct);
            return EndReadFwLog(resp);
        }

        /// <summary>
        /// Switches firewall mode asynchronously without blocking the calling thread.
        /// </summary>
        public async Task<MessageType> SwitchFirewallModeAsync(FirewallMode mode, CancellationToken ct = default)
        {
            var resp = await Endpoint.QueueMessage(TwMessageModeSwitch.CreateRequest(mode)).ResponseAsync.WaitAsync(ct);
            return resp.Type;
        }

        /// <summary>
        /// Requests server stop asynchronously without blocking the calling thread.
        /// </summary>
        public async Task<MessageType> RequestServerStopAsync(CancellationToken ct = default)
        {
            var resp = await Endpoint.QueueMessage(TwMessageSimple.CreateRequest(MessageType.STOP_SERVICE)).ResponseAsync.WaitAsync(ct);
            return resp.Type;
        }

        /// <summary>
        /// Checks if server is locked asynchronously without blocking the calling thread.
        /// </summary>
        public async Task<bool> IsServerLockedAsync(CancellationToken ct = default)
        {
            var resp = await Endpoint.QueueMessage(TwMessageIsLocked.CreateRequest()).ResponseAsync.WaitAsync(ct);
            if (resp is TwMessageIsLocked isLockedResp)
                return isLockedResp.LockedStatus;
            else
                return false;
        }

        /// <summary>
        /// Sets passphrase asynchronously without blocking the calling thread.
        /// </summary>
        public async Task<MessageType> SetPassphraseAsync(string pwd, CancellationToken ct = default)
        {
            var resp = await Endpoint.QueueMessage(TwMessageSetPassword.CreateRequest(pwd)).ResponseAsync.WaitAsync(ct);
            return resp.Type;
        }

        /// <summary>
        /// Tries to unlock server asynchronously without blocking the calling thread.
        /// </summary>
        public async Task<MessageType> TryUnlockServerAsync(string pwd, CancellationToken ct = default)
        {
            var resp = await Endpoint.QueueMessage(TwMessageUnlock.CreateRequest(pwd)).ResponseAsync.WaitAsync(ct);
            return resp.Type;
        }

        /// <summary>
        /// Locks server asynchronously without blocking the calling thread.
        /// </summary>
        public async Task<MessageType> LockServerAsync(CancellationToken ct = default)
        {
            var resp = await Endpoint.QueueMessage(TwMessageSimple.CreateRequest(MessageType.LOCK)).ResponseAsync.WaitAsync(ct);
            return resp.Type;
        }

        /// <summary>
        /// Gets process path asynchronously without blocking the calling thread.
        /// </summary>
        public async Task<string> TryGetProcessPathAsync(uint pid, CancellationToken ct = default)
        {
            var resp = await Endpoint.QueueMessage(TwMessageGetProcessPath.CreateRequest(pid)).ResponseAsync.WaitAsync(ct);
            if (resp.Type == MessageType.GET_PROCESS_PATH)
            {
                var respArgs = (TwMessageGetProcessPath)resp;
                return respArgs.Path;
            }
            else
                return string.Empty;
        }

        #endregion
    }
}
