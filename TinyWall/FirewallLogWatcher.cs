using System;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;
using Microsoft.Samples;
using pylorak.Utilities;
using pylorak.Windows;

namespace pylorak.TinyWall
{
    internal class FirewallLogWatcher : Disposable
    {
        //private readonly string FIREWALLLOG_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"LogFiles\Firewall\pfirewall.log");
        private readonly EventLogWatcher LogWatcher;

        public delegate void NewLogEntryDelegate(FirewallLogWatcher sender, FirewallLogEntry entry);
        public event NewLogEntryDelegate? NewLogEntry;

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return; 
            
            if (disposing)
            {
                // Release managed resources

                LogWatcher.Dispose();
            }

            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.

            DisableLogging();

            base.Dispose(disposing);
        }

        ~FirewallLogWatcher() => Dispose(false);

        internal FirewallLogWatcher()
        {
            // Create event notifier
            EventLogQuery evquery = new("Security", PathType.LogName, "*[System[(EventID=5154 or EventID=5155 or EventID=5157 or EventID=5159 or EventID=5156 or EventID=5158)]]");
            LogWatcher = new EventLogWatcher(evquery);
            LogWatcher.Enabled = false;
            LogWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(LogWatcher_EventRecordWritten);
        }

        internal bool Enabled
        {
            get 
            {
                return LogWatcher.Enabled;
            }

            set
            {
                if (value != LogWatcher.Enabled)
                {
                    if (value)
                        EnableLogging();
                    else
                        DisableLogging();

                    LogWatcher.Enabled = value;
                }
            }
        }

        private static FirewallLogEntry ParseLogEntry(EventRecordWrittenEventArgs e)
        {
            var entry = new FirewallLogEntry();
            entry.Timestamp = DateTime.Now;
            entry.Event = (EventLogEvent)e.EventRecord.Id;

            switch (e.EventRecord.Id)
            {
                case 5154:
                case 5155:
                case 5158:
                case 5159:
                    entry.ProcessId = (uint)(ulong)e.EventRecord.Properties[0].Value;
                    entry.AppPath = (string)e.EventRecord.Properties[1].Value;
                    entry.LocalIp = (string)e.EventRecord.Properties[2].Value;
                    entry.LocalPort = int.Parse((string)e.EventRecord.Properties[3].Value);
                    entry.Protocol = (Protocol)(uint)e.EventRecord.Properties[4].Value;
                    entry.RemoteIp = string.Empty;
                    entry.RemotePort = 0;
                    break;
                case 5156:
                case 5157:
                default:
                    entry.ProcessId = (uint)(ulong)e.EventRecord.Properties[0].Value;
                    entry.AppPath = (string)e.EventRecord.Properties[1].Value;
                    entry.Protocol = (Protocol)(uint)e.EventRecord.Properties[7].Value;
                    switch ((string)e.EventRecord.Properties[2].Value)
                    {
                        case "%%14592":
                            entry.Direction = RuleDirection.In;
                            entry.RemoteIp = (string)e.EventRecord.Properties[3].Value;
                            entry.RemotePort = int.Parse((string)e.EventRecord.Properties[4].Value);
                            entry.LocalIp = (string)e.EventRecord.Properties[5].Value;
                            entry.LocalPort = int.Parse((string)e.EventRecord.Properties[6].Value);
                            break;
                        case "%%14593":
                            entry.Direction = RuleDirection.Out;
                            entry.LocalIp = (string)e.EventRecord.Properties[3].Value;
                            entry.LocalPort = int.Parse((string)e.EventRecord.Properties[4].Value);
                            entry.RemoteIp = (string)e.EventRecord.Properties[5].Value;
                            entry.RemotePort = int.Parse((string)e.EventRecord.Properties[6].Value);
                            break;
                        default:
                            entry.Direction = RuleDirection.Invalid;
                            break;
                    }
                    break;
            }

            // Convert path to Win32 format
            entry.AppPath = PathMapper.Instance.ConvertPathIgnoreErrors(entry.AppPath, PathFormat.Win32);

            // Correct casing of app path
            entry.AppPath = Utils.GetExactPath(entry.AppPath);

            // Replace invalid IP strings with the "unspecified address" IPv6 specifier
            if (string.IsNullOrEmpty(entry.RemoteIp))
                entry.RemoteIp = "::";
            if (string.IsNullOrEmpty(entry.LocalIp))
                entry.LocalIp = "::";

            return entry;
        }

        void LogWatcher_EventRecordWritten(object sender, EventRecordWrittenEventArgs e)
        {
            try
            {
                NewLogEntry?.Invoke(this, ParseLogEntry(e));
            }
            catch { }
        }

        private static class NativeMethods
        {
            [Flags]
            internal enum AuditingInformationEnum : uint
            {
                POLICY_AUDIT_EVENT_UNCHANGED = 0,
                POLICY_AUDIT_EVENT_SUCCESS = 1,
                POLICY_AUDIT_EVENT_FAILURE = 2,
                POLICY_AUDIT_EVENT_NONE = 4,
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

        private static readonly Guid PACKET_LOGGING_AUDIT_SUBCAT = new("{0CCE9225-69AE-11D9-BED3-505054503030}");
        private static readonly Guid CONNECTION_LOGGING_AUDIT_SUBCAT = new("{0CCE9226-69AE-11D9-BED3-505054503030}");

        private static void AuditSetSystemPolicy(Guid guid, bool success, bool failure)
        {
            var pol = new NativeMethods.AUDIT_POLICY_INFORMATION();
            pol.AuditCategoryGuid = guid;
            pol.AuditSubCategoryGuid = guid;
            if (success || failure)
            {
                if (success)
                    pol.AuditingInformation |= NativeMethods.AuditingInformationEnum.POLICY_AUDIT_EVENT_SUCCESS;
                if (failure)
                    pol.AuditingInformation |= NativeMethods.AuditingInformationEnum.POLICY_AUDIT_EVENT_FAILURE;
            }
            else
                pol.AuditingInformation = NativeMethods.AuditingInformationEnum.POLICY_AUDIT_EVENT_NONE;

            if (!NativeMethods.AuditSetSystemPolicy(ref pol, 1))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        private static void EnableLogging()
        {
            try
            {
                Privilege.RunWithPrivilege(Privilege.Security, true, delegate (object? state)
                {
                    AuditSetSystemPolicy(PACKET_LOGGING_AUDIT_SUBCAT, true, true);
                    AuditSetSystemPolicy(CONNECTION_LOGGING_AUDIT_SUBCAT, true, true);
                }, null);
            }
            catch { }
        }

        private static void DisableLogging()
        {
            try
            {
                Privilege.RunWithPrivilege(Privilege.Security, true, delegate (object? state)
                {
                    AuditSetSystemPolicy(PACKET_LOGGING_AUDIT_SUBCAT, false, false);
                    AuditSetSystemPolicy(CONNECTION_LOGGING_AUDIT_SUBCAT, false, false);
                }, null);
            }
            catch { }
        }
    }
}
