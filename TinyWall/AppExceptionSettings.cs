using System;
using System.Collections.Generic;
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
    public class AppExceptionSettings : ICloneable
    {
        public bool? Recognized = null;
        public string ServiceName;

        public string[] Profiles;
        public AppExceptionTimer Timer;
        public DateTime CreationDate = DateTime.Now;
        public string AppID = GenerateID();

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
        public string OpenPortListenLocalTCP = string.Empty;
        public string OpenPortOutboundRemoteUDP = string.Empty;
        public string OpenPortListenLocalUDP = string.Empty;
        
        public AppExceptionSettings()
        {
            ExecutablePath = string.Empty;
            ServiceName = string.Empty;
            Profiles = new string[0];
            Timer = AppExceptionTimer.Permanent;
            CreationDate = DateTime.Now;
        }

        public AppExceptionSettings(string execPath)
        {
            this.ExecutablePath = execPath;
            this.Profiles = new string[0];
        }

        public bool IsService
        {
            get { return !string.IsNullOrEmpty(ServiceName); }
        }

        public object Clone()
        {
            return Utils.DeepClone(this);
        }

        public override string ToString()
        {
            return ExecutablePath;
        }

        public static bool ExecutableNameEquals(AppExceptionSettings app1, AppExceptionSettings app2)
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

        internal static List<AppExceptionSettings> CheckForAppDependencies(System.Windows.Forms.IWin32Window parent, AppExceptionSettings ex)
        {
            List<AppExceptionSettings> exceptions = new List<AppExceptionSettings>();
            exceptions.Add(ex);

            ProfileAssoc appFile = null;
            ApplicationCollection allApps = Utils.DeepClone(GlobalInstances.ProfileMan.KnownApplications);
            Application app = allApps.TryGetRecognizedApp(ex.ExecutablePath, ex.ServiceName, out appFile);
            if ((app != null) && app.ResolveFilePaths())
            {
                if (app.FileRealizations.Count > 1)
                {
                    if (System.Windows.Forms.MessageBox.Show(
                        parent,
                        string.Format("The file you are about to unblock is part of the application \"{0}\". This application requires additional files unblocked to function properly. \r\n\r\n Do you want TinyWall to automatically whitelist these files for you?", app.Name),
                        "Application dependency",
                        System.Windows.Forms.MessageBoxButtons.YesNo,
                        System.Windows.Forms.MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
                    {
                        foreach (ProfileAssoc pa in app.FileRealizations)
                        {
                            exceptions.Add(pa.ToExceptionSetting());
                        }
                    }
                }
            }

            return exceptions;
        }
    };

}
