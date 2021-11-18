using System;
using System.Runtime.InteropServices;

public static class WindowFlasher
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

    [DllImport("user32.dll")]
    private static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

    [StructLayout(LayoutKind.Sequential)]
    private struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }

    public const uint FLASHW_STOP = 0;
    public const uint FLASHW_CAPTION = 1;
    public const uint FLASHW_TRAY = 2;
    public const uint FLASHW_ALL = 3;
    public const uint FLASHW_TIMER = 4;
    public const uint FLASHW_TIMERNOFG = 12;

    private static FLASHWINFO Create_FLASHWINFO(IntPtr handle, uint flags, uint count, uint timeout)
    {
        FLASHWINFO fi = new FLASHWINFO();
        fi.cbSize = Convert.ToUInt32(Marshal.SizeOf(fi));
        fi.hwnd = handle;
        fi.dwFlags = flags;
        fi.uCount = count;
        fi.dwTimeout = timeout;
        return fi;
    }

    public static bool FlashOnce(IntPtr formHndl)
    {
        return FlashWindow(formHndl, true);
    }

    public static bool Flash(IntPtr formHndl)
    {
        if (Win2000OrLater)
        {
            FLASHWINFO fi = Create_FLASHWINFO(formHndl, FLASHW_ALL | FLASHW_TIMERNOFG, uint.MaxValue, 0);
            return FlashWindowEx(ref fi);
        }
        return false;
    }

    public static bool Flash(IntPtr formHndl, uint count)
    {
        if (Win2000OrLater)
        {
            FLASHWINFO fi = Create_FLASHWINFO(formHndl, FLASHW_ALL, count, 0);
            return FlashWindowEx(ref fi);
        }
        return false;
    }

    public static bool Start(IntPtr formHndl)
    {
        if (Win2000OrLater)
        {
            FLASHWINFO fi = Create_FLASHWINFO(formHndl, FLASHW_ALL, uint.MaxValue, 0);
            return FlashWindowEx(ref fi);
        }
        return false;
    }

    public static bool Stop(IntPtr formHndl)
    {
        if (Win2000OrLater)
        {
            FLASHWINFO fi = Create_FLASHWINFO(formHndl, FLASHW_STOP, uint.MaxValue, 0);
            return FlashWindowEx(ref fi);
        }
        return false;
    }

    private static bool Win2000OrLater
    {
        get { return System.Environment.OSVersion.Version.Major >= 5; }
    }
}
