using System.Threading;

namespace pylorak.TinyWall
{
    public class TwRequest
    {
        private TwMessage? _Response;
        private object ResponseSyncRoot { get { return this; } }

        public TwMessage Request { get; init; }
        public TwMessage Response
        {
            get
            {
                lock (ResponseSyncRoot)
                {
                    while (!_Response.HasValue)
                    {
                        Monitor.Wait(ResponseSyncRoot);
                    }
                    return _Response.Value;
                }
            }
            set
            {
                lock (ResponseSyncRoot)
                {
                    _Response = value;
                    Monitor.Pulse(ResponseSyncRoot);
                }
            }
        }

        public void WaitResponse()
        {
            var _ = Response;
        }

        public TwRequest(MessageType type, params object[] args)
        {
            Request = new TwMessage(type, args);
        }

        public TwRequest(TwMessage reqMsg)
        {
            Request = reqMsg;
        }
    }
}
