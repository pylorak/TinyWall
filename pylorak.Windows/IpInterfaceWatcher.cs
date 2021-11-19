using System;
using System.Security;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Runtime.ConstrainedExecution;
using System.ComponentModel;
using pylorak.Utilities;

namespace pylorak.Windows
{
    public class IpInterfaceWatcher : Disposable
    {
        private sealed class SafeIpHlprNotifyHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            [SuppressUnmanagedCodeSecurity]
            private static class NativeMethods
            {
                [DllImport("Iphlpapi.dll")]
                internal static extern int CancelMibChangeNotify2(IntPtr NotificationHandle);
            }

            public SafeIpHlprNotifyHandle() : base(true) { }

            public SafeIpHlprNotifyHandle(IntPtr handle) : base(true)
            {
                SetHandle(handle);
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            [PrePrepareMethod]
            protected override bool ReleaseHandle()
            {
                return (0 == NativeMethods.CancelMibChangeNotify2(handle));
            }
        }

        private enum ADDRESS_FAMILY
        {
            AF_UNSPEC = 0,
            AF_INET = 2,
            AF_INET6 = 23
        };

        private enum MIB_NOTIFICATION_TYPE
        {
            MibParameterNotification,
            MibAddInstance,
            MibDeleteInstance,
            MibInitialNotification,
        }

        private delegate void NotifyIpInterfaceChangeDelegate(IntPtr CallerContext, IntPtr Row, MIB_NOTIFICATION_TYPE NotificationType);

        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DllImport("Iphlpapi")]
            public static extern int NotifyIpInterfaceChange(ADDRESS_FAMILY Family, NotifyIpInterfaceChangeDelegate Callback, IntPtr CallerContext, [MarshalAs(UnmanagedType.I1)] bool InitialNotification, out SafeIpHlprNotifyHandle NotificationHandle);

            [DllImport("Iphlpapi")]
            public static extern int NotifyUnicastIpAddressChange(ADDRESS_FAMILY Family, NotifyIpInterfaceChangeDelegate Callback, IntPtr CallerContext, [MarshalAs(UnmanagedType.I1)] bool InitialNotification, out SafeIpHlprNotifyHandle NotificationHandle);

            [DllImport("Iphlpapi")]
            public static extern int NotifyRouteChange2(ADDRESS_FAMILY Family, NotifyIpInterfaceChangeDelegate Callback, IntPtr CallerContext, [MarshalAs(UnmanagedType.I1)] bool InitialNotification, out SafeIpHlprNotifyHandle NotificationHandle);
        }

        private readonly NotifyIpInterfaceChangeDelegate NativeCallback;
        private readonly SafeIpHlprNotifyHandle NotifyIpInterfaceChangeHandle;
        private readonly SafeIpHlprNotifyHandle NotifyUnicastIpChangeHandle;
        private readonly SafeIpHlprNotifyHandle NotifyRouteChangeHandle;
        private readonly RegistryWatcher DnsChangeNotifier;
        private readonly EventMerger ChangeEventMerger = new EventMerger(1000);

        public event EventHandler InterfaceChanged;

        public IpInterfaceWatcher()
        {
            NativeCallback = NotifyIpInterfaceChangeCallback;

            int err = NativeMethods.NotifyIpInterfaceChange(ADDRESS_FAMILY.AF_UNSPEC, NativeCallback, IntPtr.Zero, false, out NotifyIpInterfaceChangeHandle);
            if (err != 0)
                throw new Win32Exception(err, "NotifyIpInterfaceChange");

            err = NativeMethods.NotifyUnicastIpAddressChange(ADDRESS_FAMILY.AF_UNSPEC, NativeCallback, IntPtr.Zero, false, out NotifyUnicastIpChangeHandle);
            if (err != 0)
                throw new Win32Exception(err, "NotifyUnicastIpAddressChange");

            err = NativeMethods.NotifyRouteChange2(ADDRESS_FAMILY.AF_UNSPEC, NativeCallback, IntPtr.Zero, false, out NotifyRouteChangeHandle);
            if (err != 0)
                throw new Win32Exception(err, "NotifyRouteChange2");

            try
            {
                DnsChangeNotifier = new RegistryWatcher(
                    new string[] {
                        @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces",
                        @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters\Interfaces"
                    },
                    true);
            }
            catch
            {
                // TODO: This case is implemented just-in-case if Tcpip6 regkey is not avialable when
                //      IPv6 support in OS is not installed, but it might not be necessary. Should be tested
                //      if this can be removed.

                // Try without IPv6 support
                DnsChangeNotifier = new RegistryWatcher(
                    new string[] {
                        @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces",
                    },
                    true);
            }
            DnsChangeNotifier.RegistryChanged += DnsChangeNotifier_RegistryChanged;
            DnsChangeNotifier.Enabled = true;
            ChangeEventMerger.Event += ChangeEventMerger_Event;
        }

        private void ChangeEventMerger_Event(object sender, EventArgs e)
        {
            InterfaceChanged?.Invoke(this, EventArgs.Empty);
        }

        private void DnsChangeNotifier_RegistryChanged(object sender, EventArgs args)
        {
            ChangeEventMerger.Pulse();
        }

        private void NotifyIpInterfaceChangeCallback(IntPtr CallerContext, IntPtr Row, MIB_NOTIFICATION_TYPE NotificationType)
        {
            ChangeEventMerger.Pulse();
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                NotifyIpInterfaceChangeHandle.Dispose();
                NotifyUnicastIpChangeHandle.Dispose();
                NotifyRouteChangeHandle.Dispose();
                DnsChangeNotifier.Dispose();
                ChangeEventMerger.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
