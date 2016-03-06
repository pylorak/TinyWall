using System.Threading;

namespace TinyWall.Interface.Internal
{
    public sealed class Future<T> : Disposable where T : struct
    {
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
            if (disposing)
            {
                Event.Close();
            }

            Event = null;

            base.Dispose(disposing);
        }
    }
}
