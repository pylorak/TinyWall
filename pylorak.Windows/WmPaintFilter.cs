using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace pylorak.Windows
{
    public class WmPaintFilter : NativeWindow, IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct NMHDR
        {
            public IntPtr hwndFrom;
            public IntPtr idFrom;
            public int code;
        }

        private Control? _target;
        private bool InWmPaint;

        public WmPaintFilter(Control target)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));

            if (target.IsHandleCreated)
                AssignHandle(target.Handle);

            _target.HandleCreated += OnHandleCreated;
            _target.HandleDestroyed += OnHandleDestroyed;
        }

        private void OnHandleCreated(object sender, EventArgs e)
        {
            AssignHandle(_target!.Handle);
        }

        private void OnHandleDestroyed(object sender, EventArgs e)
        {
            ReleaseHandle();
        }

        protected override void WndProc(ref Message msg)
        {
            const int NM_CUSTOMDRAW = -12;
            const int WM_PAINT = 0x0F;
            const int WM_REFLECT_NOTIFY = 0x204E;

            switch (msg.Msg)
            {
                case WM_PAINT:
                    InWmPaint = true;
                    try { base.WndProc(ref msg); }
                    finally { InWmPaint = false; }
                    break;
                case WM_REFLECT_NOTIFY:
                    // Guard against some unexpected messages
                    if (msg.LParam == IntPtr.Zero)
                    {
                        base.WndProc(ref msg);
                        return;
                    }

                    NMHDR nmhdr = Marshal.PtrToStructure<NMHDR>(msg.LParam);
                    if ((nmhdr.code == NM_CUSTOMDRAW) && !InWmPaint)
                    {
                        // Drop customdraw events that don't belong to an active WM_PAINT in progress
                        return;
                    }

                    base.WndProc(ref msg);
                    break;
                default:
                    base.WndProc(ref msg);
                    break;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_target is not null)
                {
                    _target.HandleCreated -= OnHandleCreated;
                    _target.HandleDestroyed -= OnHandleDestroyed;
                    _target = null;
                }
            }
            if (this.Handle != IntPtr.Zero)
                ReleaseHandle();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

}
