using System;
using System.Globalization;
using System.Threading;

namespace PKSoft
{
    // Possible message types from controller to service
    internal enum TWControllerMessages
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
        GET_SETTINGS = 32,
        GET_PROCESS_PATH,
        VERIFY_KEYS,
        READ_FW_LOG,

        // Unprivileged write commands (>1023)
        UNLOCK = 1024,

        // Privileged write commands (>2047)
        MODE_SWITCH = 2048,
        REINIT,
        PUT_SETTINGS,
        LOCK,
        SET_PASSPHRASE,
        STOP_DISABLE,
        MINUTE_TIMER
    }

    // Possible message types from service to controller
    internal enum TWServiceMessages
    {
        DATABASE_UPDATED
    }

    // Encapsulates a message tye and its parameters
    [Serializable]
    internal struct Message
    {
        internal TWControllerMessages Command;
        internal object[] Arguments;

        internal Message(TWControllerMessages cmd)
        {
            Command = cmd;
            Arguments = null;
        }
        internal Message(TWControllerMessages cmd, object arg0)
        {
            Command = cmd;
            Arguments = new object[] { arg0 };
        }
        internal Message(TWControllerMessages cmd, object arg0, object arg1)
        {
            Command = cmd;
            Arguments = new object[] { arg0, arg1 };
        }
        internal Message(TWControllerMessages cmd, object arg0, object arg1, object arg2)
        {
            Command = cmd;
            Arguments = new object[] { arg0, arg1, arg2 };
        }
        internal Message(TWControllerMessages cmd, object arg0, object arg1, object arg2, object arg3)
        {
            Command = cmd;
            Arguments = new object[] { arg0, arg1, arg2, arg3 };
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
