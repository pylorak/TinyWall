using System;
using System.IO;
using System.Threading;
using System.Diagnostics.Eventing.Reader;
using NetFwTypeLib;

namespace PKSoft
{
    class WindowsFirewall : IDisposable
    {
        private EventLogWatcher WFEventWatcher;

        // This is a list of apps that are allowed to change firewall rules
        private static readonly string[] WhitelistedApps = new string[]
        {
#if DEBUG
            Path.Combine(Path.GetDirectoryName(TinyWall.Interface.Internal.Utils.ExecutablePath), "TinyWall.vshost.exe"),
#endif
            TinyWall.Interface.Internal.Utils.ExecutablePath,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "dllhost.exe")
        };

        public WindowsFirewall()
        {
            DisableMpsSvc();

            try
            {
                WFEventWatcher = new EventLogWatcher("Microsoft-Windows-Windows Firewall With Advanced Security/Firewall");
                WFEventWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(WFEventWatcher_EventRecordWritten);
                WFEventWatcher.Enabled = true;
            }
            catch(Exception e)
            {
                Utils.Log("Cannot monitor Windows Firewall. Is the 'eventlog' service running? For details see next log entry.", Utils.LOG_ID_SERVICE);
                Utils.LogException(e, Utils.LOG_ID_SERVICE);
            }
        }

        private static void WFEventWatcher_EventRecordWritten(object sender, EventRecordWrittenEventArgs e)
        {
            try
            {
                int propidx = -1;
                switch (e.EventRecord.Id)
                {
                    case 2003:     // firewall setting changed
                        {
                            propidx = 7;
                            break;
                        }
                    case 2005:     // rule changed
                        {
                            propidx = 22;
                            break;
                        }
                    case 2006:     // rule deleted
                        {
                            propidx = 3;
                            break;
                        }
                    case 2032:     // firewall has been reset
                        {
                            propidx = 1;
                            break;
                        }
                    default:
                        // Nothing to do
                        return;
                }

                System.Diagnostics.Debug.Assert(propidx != -1);

                // If the rules were changed by us, do nothing
                string EVpath = (string)e.EventRecord.Properties[propidx].Value;
                for (int i = 0; i < WhitelistedApps.Length; ++i)
                {
                    if (string.Compare(WhitelistedApps[i], EVpath, StringComparison.OrdinalIgnoreCase) == 0)
                        return;
                }
            }
            catch { }

            DisableMpsSvc();
        }

        public void Dispose()
        {
            WFEventWatcher?.Dispose();
            RestoreMpsSvc();
        }

        private static INetFwPolicy2 GetFwPolicy2()
        {
            Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            return (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);
        }

        private static INetFwRule CreateFwRule(string name, NET_FW_ACTION_ action, NET_FW_RULE_DIRECTION_ dir)
        {
            Type tNetFwRule = Type.GetTypeFromProgID("HNetCfg.FwRule");
            INetFwRule rule = (INetFwRule)Activator.CreateInstance(tNetFwRule);

            rule.Name = name;
            rule.Action = action;
            rule.Direction = dir;
            rule.Grouping = "TinyWall";
            rule.Profiles = (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE | (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC | (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN;
            rule.Enabled = true;
            if ((NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN == dir) && (NET_FW_ACTION_.NET_FW_ACTION_ALLOW == action))
                rule.EdgeTraversal = true;

            return rule;
        }

        private static void MpsNotificationsDisable(INetFwPolicy2 pol, bool disable)
        {
            if (pol.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] != disable)
                pol.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = disable;
            if (pol.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] != disable)
                pol.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = disable;
            if (pol.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] != disable)
                pol.NotificationsDisabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = disable;
        }

        private static void DisableMpsSvc()
        {
            try
            {
                INetFwPolicy2 fwPolicy2 = GetFwPolicy2();

                // Disable Windows Firewall notifications
                MpsNotificationsDisable(fwPolicy2, true);

                // Add new rules
                string newRuleId = $"TinyWall Compat [{Utils.RandomString(6)}]";
                fwPolicy2.Rules.Add(CreateFwRule(newRuleId, NET_FW_ACTION_.NET_FW_ACTION_ALLOW, NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN));
                fwPolicy2.Rules.Add(CreateFwRule(newRuleId, NET_FW_ACTION_.NET_FW_ACTION_ALLOW, NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT));

                // Remove earlier rules
                INetFwRules rules = fwPolicy2.Rules;
                foreach (INetFwRule rule in rules)
                {
                    string ruleName = rule.Name;
                    if (!string.IsNullOrEmpty(ruleName) && ruleName.Contains("TinyWall") && (ruleName != newRuleId))
                        rules.Remove(rule.Name);
                }
            }
            catch { }
        }

        private static void RestoreMpsSvc()
        {
            try
            {
                INetFwPolicy2 fwPolicy2 = GetFwPolicy2();

                // Enable Windows Firewall notifications
                MpsNotificationsDisable(fwPolicy2, false);

                // Remove earlier rules
                INetFwRules rules = fwPolicy2.Rules;
                foreach (INetFwRule rule in rules)
                {
                    if ((rule.Grouping != null) && rule.Grouping.Equals("TinyWall"))
                        rules.Remove(rule.Name);
                }
            }
            catch { }
        }
    }
}
