using System;

namespace PKSoft
{
    public abstract class DisposableObject : IDisposable
    {
        ~DisposableObject()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release managed resources
            }

            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.
        }
    }
}
