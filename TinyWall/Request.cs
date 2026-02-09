using System;
using System.Threading;
using System.Threading.Tasks;

namespace pylorak.TinyWall
{
    public class TwRequest
    {
        private TwMessage? _response;
        private readonly TaskCompletionSource<TwMessage> _responseTask = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private object ResponseSyncRoot => this;

        public TwMessage Request { get; init; }

        /// <summary>
        /// Gets a task that completes when the response is available.
        /// Use this for async/await patterns to avoid blocking the calling thread.
        /// </summary>
        public Task<TwMessage> ResponseAsync => _responseTask.Task;

        public TwMessage Response
        {
            get
            {
                lock (ResponseSyncRoot)
                {
                    while (_response is null)
                    {
                        Monitor.Wait(ResponseSyncRoot);
                    }
                    return _response;
                }
            }
            set
            {
                lock (ResponseSyncRoot)
                {
                    if (_response is not null)
                        throw new InvalidOperationException("Repeated assignment to Response property is not allowed.");
                    _response = value;
                    _responseTask.TrySetResult(value);
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

//Bug in .NET/Type doesn't exist in - do not remove if you want 'init' in properties
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}