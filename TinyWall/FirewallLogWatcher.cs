using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;
using PKSoft.WindowsFirewall;

namespace PKSoft
{
    internal enum EventLogEvent
    {
        BLOCKED,
        ALLOWED,
        ALLOWED_LISTEN = 5154,
        ALLOWED_CONNECTION = 5156,
        ALLOWED_LOCAL_BIND = 5158,
        BLOCKED_LISTEN = 5155,
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
        internal RuleDirection Direction;
        internal string SourceIP;
        internal string DestinationIP;
        internal int SourcePort;
        internal int DestinationPort;
        internal string AppPath;

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
                SourcePort.GetHashCode() ^ DestinationPort.GetHashCode() ^
                AppPath.GetHashCode();
        }

        internal bool Equals(FirewallLogEntry obj, bool timestampMustMatch)
        {
            // Return true if the fields match.
            bool eventMatch =
                this.DestinationIP.Equals(obj.DestinationIP) &&
                (this.DestinationPort == obj.DestinationPort) &&
                (this.Event == obj.Event) &&
                (this.ProcessID == obj.ProcessID) &&
                (this.Protocol == obj.Protocol) &&
                (this.Direction == obj.Direction) &&
                this.SourceIP.Equals(obj.SourceIP) &&
                this.AppPath.Equals(obj.AppPath) &&
                (this.SourcePort == obj.SourcePort);

            if (timestampMustMatch)
                return this.Timestamp.Equals(obj.Timestamp) && eventMatch;
            else
                return eventMatch;
        }
    }

    internal class FirewallLogWatcher : DisposableObject
    {
        //private readonly string FIREWALLLOG_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"LogFiles\Firewall\pfirewall.log");
        private EventLogWatcher LogWatcher = null;
        private List<FirewallLogEntry> NewEntries = new List<FirewallLogEntry>();

        internal event EventHandler NewLogEntry;

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
            EventLogQuery evquery = new EventLogQuery("Security", PathType.LogName, "*[System[(EventID=5154 or EventID=5155 or EventID=5157 or EventID=5152 or EventID=5159 or EventID=5156 or EventID=5158)]]");
            LogWatcher = new EventLogWatcher(evquery);
            LogWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(LogWatcher_EventRecordWritten);
            LogWatcher.Enabled = true;

            EnableLogging();    // Enable logging
        }

        private FirewallLogEntry ParseLogEntry(EventRecordWrittenEventArgs e)
        {
            FirewallLogEntry entry = new FirewallLogEntry();
            entry.Timestamp = DateTime.Now;
            entry.Event = (EventLogEvent)e.EventRecord.Id;

            switch (e.EventRecord.Id)
            {
                case 5154:
                case 5155:
                case 5158:
                case 5159:
                    entry.ProcessID = (UInt64)e.EventRecord.Properties[0].Value;
                    entry.AppPath = Utils.GetPathOfProcess((int)entry.ProcessID);
                    entry.SourceIP = (string)e.EventRecord.Properties[2].Value;
                    entry.SourcePort = int.Parse((string)e.EventRecord.Properties[3].Value);
                    entry.Protocol = (Protocol)(UInt32)e.EventRecord.Properties[4].Value;
                    entry.DestinationIP = string.Empty;
                    entry.DestinationPort = 0;
                    break;
                case 5152:
                case 5156:
                case 5157:
                default:
                    entry.ProcessID = (UInt64)e.EventRecord.Properties[0].Value;
                    entry.AppPath = Utils.GetPathOfProcess((int)entry.ProcessID);
                    entry.SourceIP = (string)e.EventRecord.Properties[3].Value;
                    entry.SourcePort = int.Parse((string)e.EventRecord.Properties[4].Value);
                    entry.DestinationIP = (string)e.EventRecord.Properties[5].Value;
                    entry.DestinationPort = int.Parse((string)e.EventRecord.Properties[6].Value);
                    entry.Protocol = (Protocol)(UInt32)e.EventRecord.Properties[7].Value;
                    switch ((string)e.EventRecord.Properties[2].Value)
                    {
                        case "%%14592":
                            entry.Direction = RuleDirection.In;
                            break;
                        case "%%14593":
                            entry.Direction = RuleDirection.Out;
                            break;
                        default:
                            entry.Direction = RuleDirection.Invalid;
                            break;
                    }
                    break;
            }

            // Replace invalid IP strings with the "unspecified address" IPv6 specifier
            if (string.IsNullOrEmpty(entry.DestinationIP))
                entry.DestinationIP = "::";
            if (string.IsNullOrEmpty(entry.SourceIP))
                entry.SourceIP = "::";

            return entry;
        }

        void LogWatcher_EventRecordWritten(object sender, EventRecordWrittenEventArgs e)
        {
            // Maximum number of allowed entries
            const int MAX_ENTRIES = 1000;

            FirewallLogEntry entry;
            try
            {
                entry = ParseLogEntry(e);
            }
            catch
            {
                return;
            }
            switch (entry.Event)
            {
                case EventLogEvent.BLOCKED_LISTEN:
                    {
                        break;
                    }
                case EventLogEvent.BLOCKED_PACKET:
                    {
                        break;
                    }
                case EventLogEvent.BLOCKED_CONNECTION:
                    {
                        break;
                    }
                case EventLogEvent.BLOCKED_LOCAL_BIND:
                    {
                        // TODO: Figure out when and if at all this case can happen
                        break;
                    }
                case EventLogEvent.ALLOWED_LISTEN:
                    {
                        break;
                    }
                case EventLogEvent.ALLOWED_CONNECTION:
                    {
                        break;
                    }
                case EventLogEvent.ALLOWED_LOCAL_BIND:
                    {
                        break;
                    }
                default:
#if DEBUG
                    throw new InvalidOperationException();
#else
                    return;
#endif
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

            if (NewLogEntry != null)
                NewLogEntry(this, null);
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

        private static class NativeMethods
        {
            [Flags]
            internal enum AuditingInformationEnum : uint
            {
                POLICY_AUDIT_EVENT_SUCCESS = 1,
                POLICY_AUDIT_EVENT_FAILURE = 2,
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct AUDIT_POLICY_INFORMATION
            {
                internal Guid AuditSubCategoryGuid;
                internal AuditingInformationEnum AuditingInformation;
                internal Guid AuditCategoryGuid;
            }

            [DllImport("advapi32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.U1)]
            internal static extern bool AuditSetSystemPolicy([In] ref AUDIT_POLICY_INFORMATION pAuditPolicy, uint policyCount);
        }

        private static readonly Guid PACKET_LOGGING_AUDIT_SUBCAT = new Guid("{0CCE9225-69AE-11D9-BED3-505054503030}");
        private static readonly Guid CONNECTION_LOGGING_AUDIT_SUBCAT = new Guid("{0CCE9226-69AE-11D9-BED3-505054503030}");

        private static void AuditSetSystemPolicy(Guid guid, bool success, bool failure)
        {
            NativeMethods.AUDIT_POLICY_INFORMATION pol = new NativeMethods.AUDIT_POLICY_INFORMATION();
            pol.AuditCategoryGuid = guid;
            pol.AuditSubCategoryGuid = guid;
            if (success)
                pol.AuditingInformation |= NativeMethods.AuditingInformationEnum.POLICY_AUDIT_EVENT_SUCCESS;
            if (failure)
                pol.AuditingInformation |= NativeMethods.AuditingInformationEnum.POLICY_AUDIT_EVENT_FAILURE;

            if (!NativeMethods.AuditSetSystemPolicy(ref pol, 1))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        private void EnableLogging()
        {
            try
            {
                Privilege.RunWithPrivilege(Privilege.Security, true, delegate (object state)
                {
                    AuditSetSystemPolicy(PACKET_LOGGING_AUDIT_SUBCAT, true, true);
                    AuditSetSystemPolicy(CONNECTION_LOGGING_AUDIT_SUBCAT, true, true);
                }, null);
            }
            catch { }
        }

        private void DisableLogging()
        {
            try
            {
                Privilege.RunWithPrivilege(Privilege.Security, true, delegate (object state)
                {
                    AuditSetSystemPolicy(PACKET_LOGGING_AUDIT_SUBCAT, false, false);
                    AuditSetSystemPolicy(CONNECTION_LOGGING_AUDIT_SUBCAT, false, false);
                }, null);
            }
            catch { }
        }
}
}
