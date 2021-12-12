using System.Threading;
using pylorak.Utilities;

namespace pylorak.TinyWall
{
    public sealed class Future<T> : Disposable where T : struct
    {
        private readonly ManualResetEvent Event = new(false);
        
        private T _Value;

        public T Value
        {
            set
            {
                _Value = value;
                Event.Set();
            }
            get
            {
                Event.WaitOne();
                return _Value;
            }
        }

        public void WaitValue()
        {
            Event.WaitOne();
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                Event.Close();
            }

            base.Dispose(disposing);
        }
    }
}
