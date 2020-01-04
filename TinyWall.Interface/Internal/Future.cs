using System.Threading;

namespace TinyWall.Interface.Internal
{
    public sealed class Future<T> : Disposable where T : struct
    {
        private bool disposed = false;
        private ManualResetEvent Event = new ManualResetEvent(false);
        
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

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                Event.Close();
            }

            Event = null;
            disposed = true;
            base.Dispose(disposing);
        }
    }
}
