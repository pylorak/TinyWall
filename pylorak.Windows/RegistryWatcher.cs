using System;
using System.Collections.Generic;
using System.Threading;
using System.Security;
using System.Runtime.InteropServices;
using pylorak.Utilities;

namespace pylorak.Windows
{
    [Flags]
    public enum RegNotifyFilter
    {
        NameChange = 0x1,
        AttributeChange = 0x2,
        ValueChange = 0x4,
        SecurityDescriptorChange = 0x8,
    }

    public class RegistryWatcher : Disposable
    {
        private readonly bool WatchSubTree;
        private readonly RegNotifyFilter NotifyFilter;
        private readonly ManualResetEvent StopEvent;
        private readonly EventWaitHandle[] EventHandles;
        private readonly SafeRegistryHandle[] WatchedKeys;
        private readonly Thread WatcherThread;

        public event EventHandler? RegistryChanged;

        private void WatcherProc()
        {
            for (int i = 0; i < WatchedKeys.Length; ++i)
                _ = NativeMethods.RegNotifyChangeKeyValue(WatchedKeys[i].DangerousGetHandle(), WatchSubTree, NotifyFilter, EventHandles[i].SafeWaitHandle.DangerousGetHandle(), true);

            while (true)
            {
                int evIdx = WaitHandle.WaitAny(EventHandles);

                if (evIdx == WatchedKeys.Length)    // the StopEvent got signaled
                {
                    // terminate loop, 'coz we are ending the thread
                    break;
                }
                else
                {
                    _ = NativeMethods.RegNotifyChangeKeyValue(WatchedKeys[evIdx].DangerousGetHandle(), WatchSubTree, NotifyFilter, EventHandles[evIdx].SafeWaitHandle.DangerousGetHandle(), true);
                    if (Enabled) 
                        RegistryChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public RegistryWatcher(string key, bool watchSubTree, RegNotifyFilter notifyFilter = RegNotifyFilter.NameChange | RegNotifyFilter.ValueChange) :
            this(new string[] { key }, watchSubTree, notifyFilter)
        { }

        public RegistryWatcher(IEnumerable<string> keys, bool watchSubTree, RegNotifyFilter notifyFilter = RegNotifyFilter.NameChange | RegNotifyFilter.ValueChange)
        {
            WatchSubTree = watchSubTree;
            NotifyFilter = notifyFilter;
            StopEvent = new ManualResetEvent(false);

            // Find out how many keys we have, and at the same time try to open them
            var tmpHandles = new List<SafeRegistryHandle>();
            foreach (var key in keys)
                tmpHandles.Add(SafeRegistryHandle.Open(key, SafeRegistryHandle.RegistryRights.KEY_READ | SafeRegistryHandle.RegistryRights.KEY_WOW64_64KEY));

            if (tmpHandles.Count == 0)
                throw new ArgumentException("There must be at least one registry key to be monitored.");

            WatchedKeys = new SafeRegistryHandle[tmpHandles.Count];
            EventHandles = new EventWaitHandle[WatchedKeys.Length + 1]; // The last element is for the stop event

            int i = 0;
            foreach (var hndl in tmpHandles)
            {
                WatchedKeys[i] = tmpHandles[i];
                EventHandles[i] = new AutoResetEvent(false);
                ++i;
            }
            EventHandles[i] = StopEvent;

            WatcherThread = new Thread(WatcherProc);
            WatcherThread.Name = "RegistryWatcher";
            WatcherThread.IsBackground = true;
            WatcherThread.Start();
        }

        private volatile bool _Enabled;
        public bool Enabled
        {
            get
            {
                return _Enabled;
            }

            set
            {
                _Enabled = value;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                // Release managed resources
                StopEvent.Set();
                WatcherThread.Join();
                for (int i = 0; i < WatchedKeys.Length; ++i)
                {
                    WatchedKeys[i].Dispose();
                    EventHandles[i].Close();
                }
                StopEvent.Close();
            }

            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.

            base.Dispose(disposing);
        }

        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("advapi32")]
            internal static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool watchSubtree, RegNotifyFilter notifyFilter, IntPtr hEvent, bool asynchronous);
        }
    }
}
