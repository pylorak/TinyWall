using System;
using System.Text;
using System.Runtime.InteropServices;

namespace PKSoft
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll",CharSet=CharSet.Auto, CallingConvention=CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, IntPtr dwExtraInfo);
        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const uint MOUSEEVENTF_RIGHTUP = 0x10;
        internal static void DoMouseRightClick()
        {
            //Call the imported function with the cursor's current position  
            uint X = (uint)System.Windows.Forms.Cursor.Position.X;
            uint Y = (uint)System.Windows.Forms.Cursor.Position.Y;
            mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, X, Y, 0, IntPtr.Zero);
        }

        [DllImport("Wer.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        internal static extern void WerAddExcludedApplication(
            [MarshalAs(UnmanagedType.LPWStr)]
            string pwzExeName,
            [MarshalAs(UnmanagedType.Bool)]
            bool bAllUsers);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetNamedPipeClientProcessId(IntPtr Pipe, out long ClientProcessId);
    }
}
