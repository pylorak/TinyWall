using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

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

        public bool? Recognized = null;
        public bool ShouldSerializeRecognized()
        {
            return (Recognized.HasValue);
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

        internal void TryRecognizeApp(bool allowModify)
        {
            Application app = null;
            AppExceptionAssoc appFile = null;

            if (File.Exists(ExecutablePath))
                app = GlobalInstances.ProfileMan.KnownApplications.TryGetRecognizedApp(ExecutablePath, ServiceName, out appFile);

            this.Recognized = (app != null) && (!app.Special);

            if (allowModify)
            {
                // Apply default settings
                MakeUnrestrictTcpUdp();

                // Apply recognized settings, if available
                if (Recognized.Value)
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
            o.Profiles.Clear();
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

        internal static List<FirewallException> CheckForAppDependencies(System.Windows.Forms.IWin32Window parent, FirewallException ex, bool gui = true, ApplicationCollection allApps = null)
        {
            List<FirewallException> exceptions = new List<FirewallException>();
            exceptions.Add(ex);

            AppExceptionAssoc appFile = null;
            if (allApps == null)
                allApps = Utils.DeepClone(GlobalInstances.ProfileMan.KnownApplications);

            Application app = allApps.TryGetRecognizedApp(ex.ExecutablePath, ex.ServiceName, out appFile);
            if ((app != null) && app.ResolveFilePaths())
            {
                List<FirewallException> exceptions2 = new List<FirewallException>();
                exceptions2.Add(ex);
                foreach (AppExceptionAssoc template in app.FileTemplates)
                {
                    foreach (string execPath in template.ExecutableRealizations)
                    {
                        if (!ex.ExecutablePath.Equals(execPath, StringComparison.OrdinalIgnoreCase))
                            exceptions2.Add(template.CreateException(execPath));
                    }
                }

                if (exceptions2.Count > 1)
                {
                    if (!gui || (System.Windows.Forms.MessageBox.Show(
                        parent,
                        string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.UnblockPartOfApplication, app.Name),
                        PKSoft.Resources.Messages.TinyWall,
                        System.Windows.Forms.MessageBoxButtons.YesNo,
                        System.Windows.Forms.MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes))
                    {
                        exceptions.AddRange(exceptions2);
                    }
                }
            }

            return exceptions;
        }
    };

}
