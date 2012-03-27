using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        public bool Template = false;
        public bool ShouldSerializeTemplate()
        {
            return Template;
        }

        public bool? Recognized = null;
        public bool ShouldSerializeRecognized()
        {
            return (Recognized != null);
        }

        public string ServiceName = null;
        public bool ShouldSerializeServiceName()
        {
            return !string.IsNullOrEmpty(ServiceName);
        }

        public string[] Profiles = null;
        public bool ShouldSerializeProfiles()
        {
            return (Profiles != null) && (Profiles.Length > 0);
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
            ExecutablePath = string.Empty;
            ServiceName = string.Empty;
            Profiles = new string[0];
            Timer = AppExceptionTimer.Permanent;
            CreationDate = DateTime.Now;
        }

        public FirewallException(string execPath, string service)
        {
            this.ExecutablePath = execPath;
            this.ServiceName = service;
            this.Profiles = new string[0];
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
            ProfileAssoc appFile = null;

            if (File.Exists(ExecutablePath))
                app = GlobalInstances.ProfileMan.KnownApplications.TryGetRecognizedApp(ExecutablePath, ServiceName, out appFile);

            this.Recognized = (app != null);

            if (allowModify)
            {
                if (Recognized.Value)
                {
                    ProfileCollection profiles = GlobalInstances.ProfileMan.GetProfilesFor(appFile);
                    Profiles = new string[profiles.Count];
                    for (int i = 0; i < profiles.Count; ++i)
                        Profiles[i] = profiles[i].Name;
                }
                else
                {
                    Profiles = new string[1] { "Outbound" };
                }
            }
        }

        static internal string GenerateID()
        {
            return "[TW" + Utils.RandomString(12) + "]";
        }

        internal static List<FirewallException> CheckForAppDependencies(System.Windows.Forms.IWin32Window parent, FirewallException ex, bool gui = true, ApplicationCollection allApps = null)
        {
            List<FirewallException> exceptions = new List<FirewallException>();
            exceptions.Add(ex);

            ProfileAssoc appFile = null;
            if (allApps == null)
                allApps = Utils.DeepClone(GlobalInstances.ProfileMan.KnownApplications);

            Application app = allApps.TryGetRecognizedApp(ex.ExecutablePath, ex.ServiceName, out appFile);
            if ((app != null) && app.ResolveFilePaths())
            {
                List<FirewallException> exceptions2 = new List<FirewallException>();
                exceptions2.Add(ex);
                foreach (ProfileAssoc template in app.FileTemplates)
                {
                    foreach (string execPath in template.ExecutableRealizations)
                    {
                        exceptions2.Add(template.CreateException(execPath));
                    }
                }

                if (exceptions.Count > 1)
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
