using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TinyWall.Interface;

namespace PKSoft
{
    class TrafficRateMonitor : TinyWall.Interface.Internal.Disposable
    {
        private readonly PerformanceCounterCategory Category;
        private readonly string BytesSentName;
        private readonly string BytesReceivedName;

        private PerformanceCounter[] TxCounters = new PerformanceCounter[0];
        private PerformanceCounter[] RxCounters = new PerformanceCounter[0];
        private bool disposed;

        public TrafficRateMonitor()
        {
            var mapper = new PerfMapper();
            string CategoryName = mapper.EnglishToLocalized("Network Interface");
            BytesSentName = mapper.EnglishToLocalized("Bytes Sent/Sec");
            BytesReceivedName = mapper.EnglishToLocalized("Bytes Received/Sec");

            Category = new PerformanceCounterCategory(CategoryName);
            ReInitInstances();
        }

        private void ReInitInstances()
        {
            for (int i = 0; i < TxCounters.Length; ++i)
            {
                TxCounters[i].Dispose();
                RxCounters[i].Dispose();
            }

            string[] interfaces = Category.GetInstanceNames();
            TxCounters = new PerformanceCounter[interfaces.Length];
            RxCounters = new PerformanceCounter[interfaces.Length];
            for (int i = 0; i < interfaces.Length; ++i)
            {
                TxCounters[i] = new PerformanceCounter(Category.CategoryName, BytesSentName, interfaces[i]);
                RxCounters[i] = new PerformanceCounter(Category.CategoryName, BytesReceivedName, interfaces[i]);

                TxCounters[i].NextValue();
                RxCounters[i].NextValue();
            }
        }

        private bool InterfacesChanged()
        {
            string[] interfaces = Category.GetInstanceNames();
            if (interfaces.Length != TxCounters.Length)
                return true;

            for (int i = 0; i < interfaces.Length; ++i)
            {
                if (interfaces[i] != TxCounters[i].InstanceName)
                    return true;
            }

            return false;
        }

        public void Update()
        {
            int rxTotal = 0;
            int txTotal = 0;

            try
            {
                for (int i = 0; i < TxCounters.Length; ++i)
                {
                    txTotal += (int)TxCounters[i].NextValue();
                    rxTotal += (int)RxCounters[i].NextValue();
                }

                BytesSentPerSec = txTotal;
                BytesReceivedPerSec = rxTotal;
            }
            finally
            {
                if (InterfacesChanged())
                    ReInitInstances();
            }
        }

        public int BytesSentPerSec { get; private set; }
        public int BytesReceivedPerSec { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Release managed resources
                for (int i = 0; i < TxCounters.Length; ++i)
                {
                    TxCounters[i].Dispose();
                    RxCounters[i].Dispose();
                }
            }

            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.
            disposed = true;
            base.Dispose(disposing);
        }

        private class PerfMapper
        {
            private Dictionary<string, int> English;
            private Dictionary<int, string> Localized;

            public PerfMapper()
            {
                string[] english;
                string[] local;

                english = Registry.ReadRegMultiString(Registry.HKEY_LOCAL_MACHINE, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib\009", "Counter", RegWow64Options.KEY_WOW64_64KEY);
                local = Registry.ReadRegMultiString(Registry.HKEY_LOCAL_MACHINE, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib\CurrentLanguage", "Counter", RegWow64Options.KEY_WOW64_64KEY);

                /*
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    using (var key = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib\009"))
                    {
                        english = (string[])key.GetValue("Counter");
                    }
                    using (var key = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib\CurrentLanguage"))
                    {
                        local = (string[])key.GetValue("Counter");
                    }
                }
                */

                // Create English lookup table
                English = new Dictionary<string, int>(english.Length / 2, StringComparer.InvariantCultureIgnoreCase);
                for (int ix = 0; ix < english.Length - 1; ix += 2)
                {
                    int index = int.Parse(english[ix]);
                    if (!English.ContainsKey(english[ix + 1])) English.Add(english[ix + 1], index);
                }
                // Create localized lookup table
                Localized = new Dictionary<int, string>(local.Length / 2);
                for (int ix = 0; ix < local.Length - 1; ix += 2)
                {
                    int index = int.Parse(local[ix]);
                    Localized.Add(index, local[ix + 1]);
                }
            }

            public bool HasName(string name)
            {
                if (!English.ContainsKey(name)) return false;
                var index = English[name];
                return !Localized.ContainsKey(index);
            }

            public string EnglishToLocalized(string text)
            {
                if (HasName(text)) return Localized[English[text]];
                else return text;
            }

            public static string IndexToLocalized(int index)
            {
                int size = 0;
                uint ret = PdhLookupPerfNameByIndex(null, index, null, ref size);
                if (ret == 0x800007D2)
                {
                    var buffer = new StringBuilder(size);
                    ret = PdhLookupPerfNameByIndex(null, index, buffer, ref size);
                    if (ret == 0) return buffer.ToString();
                }
                throw new System.ComponentModel.Win32Exception((int)ret, "PDH lookup failed");
            }

            [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
            private static extern uint PdhLookupPerfNameByIndex(string machine, int index, [Out] StringBuilder buffer, ref int bufsize);
        }
    }
}
