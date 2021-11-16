using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TinyWall.Interface.Internal;

namespace PKSoft
{
    internal class MouseInterceptor : Disposable
    {
        private static class NativeMethods
        {
            internal const int WH_MOUSE_LL = 14;
            internal delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

            internal enum MouseMessages
            {
                WM_LBUTTONDOWN = 0x0201,
                WM_LBUTTONUP = 0x0202,
                WM_MOUSEMOVE = 0x0200,
                WM_MOUSEWHEEL = 0x020A,
                WM_RBUTTONDOWN = 0x0204,
                WM_RBUTTONUP = 0x0205
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct POINT
            {
                internal int x;
                internal int y;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct MSLLHOOKSTRUCT
            {
                internal POINT pt;
                internal uint mouseData;
                internal uint flags;
                internal uint time;
                internal IntPtr dwExtraInfo;
            }

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern IntPtr GetModuleHandle(string lpModuleName);
        }

        internal delegate void MouseHookLButtonDown(int x, int y);
        internal event MouseHookLButtonDown MouseLButtonDown;

        private NativeMethods.LowLevelMouseProc _proc;
        private static IntPtr _hookID = IntPtr.Zero;

        internal MouseInterceptor()
        {
            _proc = HookCallback;

            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookID = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _proc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if ((nCode >= 0) && (NativeMethods.MouseMessages.WM_LBUTTONDOWN == (NativeMethods.MouseMessages)wParam))
            {
                NativeMethods.MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                MouseLButtonDown(hookStruct.pt.x, hookStruct.pt.y);

                //Console.WriteLine(hookStruct.pt.x + ", " + hookStruct.pt.y);
            }
            return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                // Release managed resources
            }

            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.

            if (_hookID != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }

            base.Dispose(disposing);
        }

        ~MouseInterceptor() => Dispose(false);
    }
}
