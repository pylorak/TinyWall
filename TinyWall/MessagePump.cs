using System;
using System.Windows.Forms;
using System.Threading;

namespace PKSoft
{
    internal class MessagePump : DisposableObject
    {
        internal class WindowMessageEventArgs : EventArgs
        {
            internal WindowMessageEventArgs(System.Windows.Forms.Message msg)
            {
                _Message = msg;
            }

            private System.Windows.Forms.Message _Message;
            internal System.Windows.Forms.Message Message
            {
                get
                {
                    return _Message;
                }
            }
        }

        private class MessagePumpWindow : NativeWindow, IDisposable
        {
            internal event EventHandler<WindowMessageEventArgs> MessageReceived;

            internal MessagePumpWindow()
            {
                CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref System.Windows.Forms.Message msg)
            {
                if (MessageReceived != null)
                    MessageReceived(this, new WindowMessageEventArgs(msg));

                if (msg.Msg == _WM_EXIT_PUMP)
                    System.Windows.Forms.Application.ExitThread();

                base.WndProc(ref msg);
            }

            ~MessagePumpWindow()
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

                this.DestroyHandle();
            }
        }

        private Thread messagePump;

        private static uint _WM_EXIT_PUMP = 0;
        private static uint _WM_INIT_PUMP = 0;
        internal static int WM_INIT_PUMP
        {
            get
            {
                return (int)_WM_INIT_PUMP;
            }
        }

        
        internal MessagePump(string pumpName, EventHandler<WindowMessageEventArgs> msgRcvCallback)
        {
            if (_WM_INIT_PUMP == 0)
                _WM_INIT_PUMP = NativeMethods.RegisterWindowMessage("MessagePump_WM_INIT_PUMP");
            if (_WM_EXIT_PUMP == 0)
                _WM_EXIT_PUMP = NativeMethods.RegisterWindowMessage("MessagePump_WM_EXIT_PUMP");

            // start message pump in its own thread
            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                messagePump = new Thread((ThreadStart)delegate
                    {
                        using (MessagePumpWindow window = new MessagePumpWindow())
                        {
                            _hWnd = window.Handle;
                            window.MessageReceived += msgRcvCallback;
                            NativeMethods.PostMessage(window.Handle, _WM_INIT_PUMP, IntPtr.Zero, IntPtr.Zero);
                            mre.Set();
                            System.Windows.Forms.Application.Run();
                            _hWnd = IntPtr.Zero;
                        }
                    });
                messagePump.Name = pumpName;
                messagePump.IsBackground = true;
                messagePump.Start();

                mre.WaitOne();
            }
        }

        private IntPtr _hWnd = IntPtr.Zero;
        internal IntPtr hWnd
        {
            get
            {
                return _hWnd;
            }
        }

        ~MessagePump()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release managed resources
            }

            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.

            NativeMethods.PostMessage(this.hWnd, _WM_EXIT_PUMP, IntPtr.Zero, IntPtr.Zero);

            messagePump = null;

            base.Dispose(disposing);
        }
    }
}
