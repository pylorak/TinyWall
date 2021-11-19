using System;

namespace pylorak.Utilities
{
    public abstract class Disposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            IsDisposed = true;

            // if (IsDisposed)
            //  return;

            //if (disposing)
            //{
            // Release managed resources
            //}

            // Release unmanaged resources.
            // Set large fields to null.

            //base.Dispose(disposing);
        }

        /* Only if owning unmanaged resources without SafeHandles
        ~DerivedClass() => Dispose(false);
        */
    }
}
