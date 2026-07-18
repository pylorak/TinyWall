using pylorak.Utilities;
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Security;

namespace pylorak.Windows
{
    public class AttachedParentConsole : Disposable
    {
        [SuppressUnmanagedCodeSecurity]
        private static class SafeNativeMethods
        {
            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool AttachConsole(int dwProcessId);

            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool FreeConsole();
        }

        public bool HasAttached { get; init; }

        public AttachedParentConsole()
        {
            const int ATTACH_PARENT_PROCESS = -1;

            if (Environment.UserInteractive)
                HasAttached = SafeNativeMethods.AttachConsole(ATTACH_PARENT_PROCESS);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (HasAttached)
                SafeNativeMethods.FreeConsole();

            base.Dispose(disposing);
        }
    }
}
