using System;

namespace PKSoft
{
    public abstract class DisposableObject : IDisposable
    {
        private bool m_IsDisposed = false;

        [System.Xml.Serialization.XmlIgnore]
        public bool IsDisposed
        {
            get { return m_IsDisposed; }
        }

        ~DisposableObject()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        protected void Dispose(bool disposing)
        {
            if (m_IsDisposed)
                return;

            try
            {
                if (disposing)
                {
                    DisposeManaged();
                }

                DisposeNative();
            }
            finally
            {
                m_IsDisposed = true;
            }
        }

        protected virtual void DisposeManaged() { }
        protected virtual void DisposeNative() { }
    }
}
