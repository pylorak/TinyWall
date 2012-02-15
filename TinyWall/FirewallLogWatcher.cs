using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using PKSoft.WindowsFirewall;

namespace PKSoft
{
    internal enum EventLogEvent
    {
        BLOCKED_CONNECTION = 5157,
        BLOCKED_PACKET = 5152,
        BLOCKED_LOCAL_BIND = 5159
    }

    [Serializable]
    internal class FirewallLogEntry
    {
        internal DateTime Timestamp;
        internal EventLogEvent Event;
        internal UInt64 ProcessID;
        internal Protocol Protocol;
        internal string SourceIP;
        internal string DestinationIP;
        internal int SourcePort;
        internal int DestinationPort;

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
                return false;

            return this.Equals(obj as FirewallLogEntry, true);
        }

        public override int GetHashCode()
        {
            return Timestamp.GetHashCode() ^ Event.GetHashCode() ^ 
                ProcessID.GetHashCode() ^ Protocol.GetHashCode() ^ 
                SourceIP.GetHashCode() ^ DestinationIP.GetHashCode() ^ 
                SourcePort.GetHashCode() ^ DestinationPort.GetHashCode();
        }

        public bool Equals(FirewallLogEntry obj, bool timestampMustMatch)
        {
            // If parameter cannot be cast to Point return false.
            if ((object)obj == null)
                return false;

            // Return true if the fields match.
            bool eventMatch =
                this.DestinationIP.Equals(obj.DestinationIP) &&
                (this.DestinationPort == obj.DestinationPort) &&
                (this.Event == obj.Event) &&
                (this.ProcessID == obj.ProcessID) &&
                (this.Protocol == obj.Protocol) &&
                this.SourceIP.Equals(obj.SourceIP) &&
                (this.SourcePort == obj.SourcePort);

            if (timestampMustMatch)
                return this.Timestamp.Equals(obj.Timestamp) && eventMatch;
            else
                return eventMatch;
        }
    }

    internal class FirewallLogWatcher : DisposableObject
    {
        private readonly string FIREWALLLOG_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"LogFiles\Firewall\pfirewall.log");
        EventLogWatcher LogWatcher = null;
        List<FirewallLogEntry> NewEntries = new List<FirewallLogEntry>();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release managed resources

                LogWatcher.Dispose();
            }

            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.

            DisableLogging();
            LogWatcher = null;
            NewEntries = null;
            base.Dispose(disposing);
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
            // Maximum number of allowed entries
            const int MAX_ENTRIES = 1000;

            FirewallLogEntry entry = new FirewallLogEntry();
            entry.Timestamp = DateTime.Now;
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
                        // TODO: Figure out when and if at all this case can happen
                        break;
                    }

            }

            lock (NewEntries)
            {
                // Safe guard against using up all memory
                if (NewEntries.Count >= MAX_ENTRIES)
                {
                    // Keep the latest MAX_ENTRIES entries
                    int overLimit = NewEntries.Count - MAX_ENTRIES;
                    NewEntries.RemoveRange(0, overLimit);
                }

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
