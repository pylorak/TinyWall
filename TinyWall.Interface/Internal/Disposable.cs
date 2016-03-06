using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyWall.Interface.Internal
{
    public abstract class Disposable : IDisposable
    {
        ~Disposable()
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
            //if (disposing)
            //{
            // Release managed resources
            //}

            // Release unmanaged resources.
            // Set large fields to null.

            //base.Dispose(disposing);
        }
    }
}
