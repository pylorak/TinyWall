using System;
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
                    while (_Response is null)
                    {
                        Monitor.Wait(ResponseSyncRoot);
                    }
                    return _Response;
                }
            }
            set
            {
                lock (ResponseSyncRoot)
                {
                    if (_Response is not null)
                        throw new InvalidOperationException("Repeated assignment to Response property is not allowed.");
                    _Response = value;
                    Monitor.Pulse(ResponseSyncRoot);
                }
            }
        }

        public void WaitResponse()
        {
            var _ = Response;
        }

        public TwRequest(TwMessage reqMsg)
        {
            Request = reqMsg;
        }
    }
}
