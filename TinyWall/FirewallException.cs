using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Samples;

namespace PKSoft
{
    // The default value must have value Zero.
    public enum AppExceptionTimer
    {
        Permanent = 0,
        Until_Reboot = -1,
        For_5_Minutes = 5,
        For_30_Minutes = 30,
        For_1_Hour = 60,
        For_4_Hours = 240,
        For_9_Hours = 540,
        For_24_Hours = 1140,
        Invalid
    }

    [Serializable]
    public class FirewallException
    {
        [XmlAttributeAttribute]
        public bool Template = false;
        public bool ShouldSerializeTemplate()
        {
            return Template;
        }

        public string ServiceName = null;
        public bool ShouldSerializeServiceName()
        {
            return !string.IsNullOrEmpty(ServiceName);
        }

        public List<string> Profiles = new List<string>();
        public bool ShouldSerializeProfiles()
        {
            return (Profiles != null) && (Profiles.Count > 0);
        }

        public AppExceptionTimer Timer;
        public bool ShouldSerializeTimer()
        {
            return !Template;
        }

        public DateTime CreationDate = DateTime.Now;
        public bool ShouldSerializeCreationDate()
        {
            return !Template;
        }

        public string AppID = GenerateID();
        public bool ShouldSerializeAppID()
        {
            return !Template;
        }

        public bool LocalNetworkOnly;
        public bool ShouldSerializeLocalNetworkOnly()
        {
            return LocalNetworkOnly;
        }

        public bool AlwaysBlockTraffic;
        public bool ShouldSerializeAlwaysBlockTraffic()
        {
            return AlwaysBlockTraffic;
        }

        public bool UnrestricedTraffic;
        public bool ShouldSerializeUnrestricedTraffic()
        {
            return UnrestricedTraffic;
        }

        internal void RegenerateID()
        {
            AppID = GenerateID();
        }

        [XmlIgnore]
        public string ExecutableName
        {
            get { return System.IO.Path.GetFileName(ExecutablePath); }
        }

        private string _ExecutablePath;
        public string ExecutablePath
        {
            get { return _ExecutablePath; }
            set
            {
                _ExecutablePath = PKSoft.Parser.RecursiveParser.ResolveString(value);
            }
        }
        public bool ShouldSerializeExecutablePath()
        {
            return !string.IsNullOrEmpty(_ExecutablePath);
        }

        public string OpenPortOutboundRemoteTCP = string.Empty;
        public bool ShouldSerializeOpenPortOutboundRemoteTCP()
        {
            return !string.IsNullOrEmpty(OpenPortOutboundRemoteTCP);
        }

        public string OpenPortListenLocalTCP = string.Empty;
        public bool ShouldSerializeOpenPortListenLocalTCP()
        {
            return !string.IsNullOrEmpty(OpenPortListenLocalTCP);
        }

        public string OpenPortOutboundRemoteUDP = string.Empty;
        public bool ShouldSerializeOpenPortOutboundRemoteUDP()
        {
            return !string.IsNullOrEmpty(OpenPortOutboundRemoteUDP);
        }

        public string OpenPortListenLocalUDP = string.Empty;
        public bool ShouldSerializeOpenPortListenLocalUDP()
        {
            return !string.IsNullOrEmpty(OpenPortListenLocalUDP);
        }

        public FirewallException()
        {
            Timer = AppExceptionTimer.Permanent;
            CreationDate = DateTime.Now;
        }

        public FirewallException(string execPath, string service)
        {
            this.ExecutablePath = execPath;
            this.ServiceName = service;
        }

        public bool IsService
        {
            get { return !string.IsNullOrEmpty(ServiceName); }
        }

        public override string ToString()
        {
            return ExecutablePath;
        }

        public static bool ExecutableNameEquals(FirewallException app1, FirewallException app2)
        {
            // File path must match
            if (!string.Equals(app1.ExecutablePath, app2.ExecutablePath, StringComparison.OrdinalIgnoreCase))
                return false;

            // Service name must match
            if (app1.IsService || app2.IsService)
            {
                if (!string.Equals(app1.ServiceName, app2.ServiceName, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        internal void TryRecognizeApp(bool allowModify, out Application app, out AppExceptionAssoc appFile)
        {
            app = null;
            appFile = null;

            if (File.Exists(ExecutablePath))
                app = GlobalInstances.ProfileMan.KnownApplications.TryGetRecognizedApp(ExecutablePath, ServiceName, out appFile);

            if ((app != null) && (app.Special))
            {   // We do not want to recognize special apps
                app = null;
                appFile = null;
            }

            if (allowModify)
            {
                // Apply default settings
                MakeUnrestrictTcpUdp();

                // Apply recognized settings, if available
                if ((app != null) && (!app.Special))
                {
                    ProfileCollection profiles = GlobalInstances.ProfileMan.GetProfilesFor(appFile);
                    appFile.ExceptionTemplate.CopyRulesTo(this);
                }
            }
        }

        internal void CopyRulesTo(FirewallException o)
        {
            o.AlwaysBlockTraffic = this.AlwaysBlockTraffic;
            o.LocalNetworkOnly = this.LocalNetworkOnly;
            o.OpenPortListenLocalTCP = this.OpenPortListenLocalTCP;
            o.OpenPortListenLocalUDP = this.OpenPortListenLocalUDP;
            o.OpenPortOutboundRemoteTCP = this.OpenPortOutboundRemoteTCP;
            o.OpenPortOutboundRemoteUDP = this.OpenPortOutboundRemoteUDP;
            o.ServiceName = this.ServiceName;
            o.UnrestricedTraffic = this.UnrestricedTraffic;
            if (this.Profiles != null)
            {
                o.Profiles = new List<string>();
                o.Profiles.AddRange(this.Profiles);
            }
        }

        internal void MergeRulesTo(FirewallException o)
        {
            List<string> mergedProfiles = new List<string>();
            if (this.Profiles != null)
                mergedProfiles.AddRange(this.Profiles);
            if (o.Profiles != null)
                mergedProfiles.AddRange(o.Profiles);
            o.Profiles = new List<string>();
            o.Profiles.AddRange(mergedProfiles.Distinct());

            if (this.AlwaysBlockTraffic != o.AlwaysBlockTraffic)
                o.AlwaysBlockTraffic = false;
            if (this.LocalNetworkOnly != o.LocalNetworkOnly)
                o.LocalNetworkOnly = true;
            if (this.UnrestricedTraffic != o.UnrestricedTraffic)
                o.UnrestricedTraffic = false;

            o.OpenPortListenLocalTCP = MergeStringList(this.OpenPortListenLocalTCP, o.OpenPortListenLocalTCP);
            o.OpenPortListenLocalUDP = MergeStringList(this.OpenPortListenLocalUDP, o.OpenPortListenLocalUDP);
            o.OpenPortOutboundRemoteTCP = MergeStringList(this.OpenPortOutboundRemoteTCP, o.OpenPortOutboundRemoteTCP);
            o.OpenPortOutboundRemoteUDP = MergeStringList(this.OpenPortOutboundRemoteUDP, o.OpenPortOutboundRemoteUDP);
        }

        private static string MergeStringList(string str1, string str2)
        {
            if (str1 == null)
                return str2;
            if (str2 == null)
                return str1;

            string[] list1 = str1.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string[] list2 = str2.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> mergedList = new List<string>();
            mergedList.AddRange(list1);
            mergedList.AddRange(list2);
            return string.Join(",", mergedList.Distinct().ToArray());
        }

        internal void MakeUnrestrictTcpUdp()
        {
            Profiles = null;
            OpenPortOutboundRemoteTCP = "*";
            OpenPortOutboundRemoteUDP = "*";
            OpenPortListenLocalTCP = "*";
            OpenPortListenLocalUDP = "*";
            AlwaysBlockTraffic = false;
            UnrestricedTraffic = false;
            LocalNetworkOnly = false;
        }

        static internal string GenerateID()
        {
            return "[TW" + Utils.RandomString(12) + "]";
        }

        internal static List<FirewallException> CheckForAppDependencies(FirewallException ex, bool useExOnRecognized, bool specialAllowed, bool promptUI, ApplicationCollection allApps = null)
        {
            List<FirewallException> exceptions = new List<FirewallException>();

            AppExceptionAssoc appFile = null;
            if (allApps == null)
                allApps = Utils.DeepClone(GlobalInstances.ProfileMan.KnownApplications);

            Application app = allApps.TryGetRecognizedApp(ex.ExecutablePath, ex.ServiceName, out appFile);
            if ((app != null) && (!appFile.IsSigned || appFile.IsSignatureValid))
            {
                if (!specialAllowed && app.Special)
                    return exceptions;

                app.ResolveFilePaths(Path.GetDirectoryName(ex.ExecutablePath));

                if (useExOnRecognized)
                    exceptions.Add(ex);
                else
                    exceptions.Add(appFile.CreateException(ex.ExecutablePath));

                foreach (AppExceptionAssoc template in app.FileTemplates)
                {
                    foreach (string execPath in template.ExecutableRealizations)
                    {
                        if (!ex.ExecutablePath.Equals(execPath, StringComparison.OrdinalIgnoreCase))
                            exceptions.Add(template.CreateException(execPath));
                    }
                }
            }
            else
            {
                exceptions.Add(ex);
            }

            if ((exceptions.Count > 1) && promptUI)
            {
                string firstLine, contentLines;
                Utils.SplitFirstLine(string.Format(CultureInfo.InvariantCulture, PKSoft.Resources.Messages.UnblockApp, app.LocalizedName), out firstLine, out contentLines);

                TaskDialog dialog = new TaskDialog();
                dialog.CustomMainIcon = PKSoft.Resources.Icons.firewall;
                dialog.WindowTitle = PKSoft.Resources.Messages.TinyWall;
                dialog.MainInstruction = firstLine;
                dialog.Content = contentLines;
                dialog.DefaultButton = 1;
                dialog.ExpandedControlText = PKSoft.Resources.Messages.UnblockAppShowRelated;
                dialog.ExpandFooterArea = true;
                dialog.AllowDialogCancellation = false;
                dialog.UseCommandLinks = true;

                TaskDialogButton button1 = new TaskDialogButton(101, PKSoft.Resources.Messages.UnblockAppUnblockAllRecommended);
                TaskDialogButton button2 = new TaskDialogButton(102, PKSoft.Resources.Messages.UnblockAppUnblockOnlySelected);
                TaskDialogButton button3 = new TaskDialogButton(103, PKSoft.Resources.Messages.UnblockAppCancel);
                dialog.Buttons = new TaskDialogButton[] { button1, button2, button3 };

                string fileListStr = string.Empty;
                foreach (FirewallException filePath in exceptions)
                    fileListStr += filePath.ExecutablePath + Environment.NewLine;
                dialog.ExpandedInformation = fileListStr.Trim();

                bool success;
                if (Utils.IsMetroActive(out success))
                {
                    Utils.ShowToastNotif(Resources.Messages.ToastInputNeeded);
                }

                switch (dialog.Show())
                {
                    case 101:
                        break;
                    case 102:
                        exceptions.RemoveRange(1, exceptions.Count - 1);
                        break;
                    case 103:
                        exceptions.Clear();
                        break;
                }
            }

            return exceptions;
        }
    }

}
