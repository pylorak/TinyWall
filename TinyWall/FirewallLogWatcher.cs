using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PKSoft.WindowsFirewall;
using System.Diagnostics.Eventing.Reader;

namespace PKSoft
{
    internal enum EventLogEvent
    {
        BLOCKED_CONNECTION = 5157,
        BLOCKED_PACKET = 5152,
        BLOCKED_LOCAL_BIND = 5159
    }

    internal class FirewallLogEntry
    {
        internal EventLogEvent Event;
        internal UInt64 ProcessID;
        internal Protocol Protocol;
        internal string SourceIP;
        internal string DestinationIP;
        internal int SourcePort;
        internal int DestinationPort;
    }

    internal class FirewallLogWatcher : DisposableObject
    {
        private readonly string FIREWALLLOG_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"LogFiles\Firewall\pfirewall.log");
        EventLogWatcher LogWatcher = null;
        List<FirewallLogEntry> NewEntries = new List<FirewallLogEntry>();

        protected override void DisposeNative()
        {
            DisableLogging();

            base.DisposeNative();
        }

        protected override void DisposeManaged()
        {
            LogWatcher.Dispose();

            base.DisposeManaged();
        }

        internal FirewallLogWatcher()
        {
            // Create event notifier
            EventLogQuery evquery = new EventLogQuery("Security", PathType.LogName, "*[System[(EventID=5157 or EventID=5152 or EventID=5159)]]");
            LogWatcher = new EventLogWatcher(evquery);
            LogWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(LogWatcher_EventRecordWritten);
            LogWatcher.Enabled = true;

            EnableLogging();    // Enable logging
        }

        void LogWatcher_EventRecordWritten(object sender, EventRecordWrittenEventArgs e)
        {
            // Safe guard against using up all memory
            if (NewEntries.Count >= 1000)
                return;

            FirewallLogEntry entry = new FirewallLogEntry();
            entry.Event = (EventLogEvent)e.EventRecord.Id;

            switch (entry.Event)
            {
                case EventLogEvent.BLOCKED_PACKET:
                    {
                        entry.ProcessID = (UInt64)e.EventRecord.Properties[0].Value;
                        entry.SourceIP = (string)e.EventRecord.Properties[3].Value;
                        int.TryParse((string)e.EventRecord.Properties[4].Value, out entry.SourcePort);
                        entry.DestinationIP = (string)e.EventRecord.Properties[5].Value;
                        int.TryParse((string)e.EventRecord.Properties[6].Value, out entry.DestinationPort);
                        entry.Protocol = (Protocol)(UInt32)e.EventRecord.Properties[7].Value;
                        break;
                    }
                case EventLogEvent.BLOCKED_CONNECTION:
                    {
                        entry.ProcessID = (UInt64)e.EventRecord.Properties[0].Value;
                        entry.SourceIP = (string)e.EventRecord.Properties[3].Value;
                        int.TryParse((string)e.EventRecord.Properties[4].Value, out entry.SourcePort);
                        entry.DestinationIP = (string)e.EventRecord.Properties[5].Value;
                        int.TryParse((string)e.EventRecord.Properties[6].Value, out entry.DestinationPort);
                        entry.Protocol = (Protocol)(UInt32)e.EventRecord.Properties[7].Value;
                        break;
                    }
                case EventLogEvent.BLOCKED_LOCAL_BIND:
                    {
                        //TODO
                        break;
                    }

            }

            lock (NewEntries)
            {
                NewEntries.Add(entry);
            }
        }

        internal List<FirewallLogEntry> QueryNewEntries()
        {
            List<FirewallLogEntry> entries = new List<FirewallLogEntry>();
            lock(NewEntries)
            {
                entries.AddRange(NewEntries);
                NewEntries.Clear();
            }
            return entries;
        }

        private void EnableLogging()
        {
            // Packet logging
            Utils.StartProcess("auditpol.exe", "/set /subcategory:{0CCE9225-69AE-11D9-BED3-505054503030} /success:enable /failure:enable", true, true);
            // Connection logging
            Utils.StartProcess("auditpol.exe", "/set /subcategory:{0CCE9226-69AE-11D9-BED3-505054503030} /success:enable /failure:enable", true, true);
        }

        private void DisableLogging()
        {
            // Packet logging
            Utils.StartProcess("auditpol.exe", "/set /subcategory:{0CCE9225-69AE-11D9-BED3-505054503030} /success:disable /failure:disable", true, true);
            // Connection logging
            Utils.StartProcess("auditpol.exe", "/set /subcategory:{0CCE9226-69AE-11D9-BED3-505054503030} /success:disable /failure:disable", true, true);
        }
    }
}
