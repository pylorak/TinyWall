using System;

namespace TinyWall.Interface.Internal
{
    public abstract class Disposable : IDisposable
    {
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            //if (disposing)
            //{
            // Release managed resources
            //}

            // Release unmanaged resources.
            // Set large fields to null.

            //base.Dispose(disposing);
        }

        /* Only if owning unmanaged resources without SafeHandles
        ~DerivedClass()
        {
            Dispose(false);
        }
        */
    }
}
