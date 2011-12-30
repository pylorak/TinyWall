using System;
using System.Globalization;
using System.Threading;

namespace PKSoft
{
    // Possible message types between service and controller
    internal enum TinyWallCommands
    {
        // General responses
        INVALID_COMMAND,
        PING,
        RESPONSE_OK,
        RESPONSE_WARNING,
        RESPONSE_ERROR,
        RESPONSE_LOCKED,
        COM_ERROR,

        // Read commands (>31)
        GET_PROFILE = 32,
        GET_MODE,
        GET_SETTINGS,
        GET_LOCK_STATE,
        GET_UPDATE_DESCRIPTOR,
        VERIFY_KEYS,

        // Unprivileged write commands (>1023)
        UNLOCK = 1024,

        // Privileged write commands (>2047)
        MODE_SWITCH = 2048,
        REINIT,
        RELOAD,
        PUT_SETTINGS,
        LOCK,
        SET_PASSPHRASE,
        STOP_DISABLE,
        CHECK_SCHEDULED_RULES
    }

    // Encapsulates a message tye and its parameters
    [Serializable]
    internal struct Message
    {
        internal TinyWallCommands Command;
        internal object[] Arguments;

        internal Message(TinyWallCommands cmd)
        {
            Command = cmd;
            Arguments = null;
        }
        internal Message(TinyWallCommands cmd, object arg0)
        {
            Command = cmd;
            Arguments = new object[] { arg0 };
        }
        internal Message(TinyWallCommands cmd, object arg0, object arg1)
        {
            Command = cmd;
            Arguments = new object[] { arg0, arg1 };
        }
        internal Message(TinyWallCommands cmd, object arg0, object arg1, object arg2)
        {
            Command = cmd;
            Arguments = new object[] { arg0, arg1, arg2 };
        }
    }

    // Represent a request, and the response to that request
    internal class ReqResp
    {
        internal Message Request;
        internal Message Response;

        private object locker = new object();
        private bool _ResponseReady = false;

        internal ReqResp()
        {
        }

        internal ReqResp(Message msg)
            : this()
        {
            Request = msg;
        }

        internal Message GetResponse()
        {
            // Block until reponse arrives
            lock (locker)
            {
                while (!_ResponseReady)
                    Monitor.Wait(locker);
            }
            return this.Response;
        }

        // When the request has been processed and the response has been completely written,
        // the request consumer needs to call this method.
        internal void SignalResponse()
        {
            lock (locker)
            {
                _ResponseReady = true;
                Monitor.Pulse(locker);
            }
        }

    }
}
