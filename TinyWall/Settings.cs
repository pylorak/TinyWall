using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace PKSoft
{
    [Serializable]
    public abstract class SettingsBase : ICloneable
    {
        public object Clone()
        {
            return Utils.DeepClone(this);
        }
    }

    // Zone settings are specific to network zones (public, domain, private..)
    // Applied by: Service
    // Controlled by: Controller
    // Visible by: Service, Controller
    [Serializable]
    public class ZoneSettings : SettingsBase
    {
        private const string ENC_SALT = "O?2E/)YFq~e:w@a,";
        private const string ENC_IV = "X0@!H93!Y=8&/M/T";   // must be 16/24/32 bytes

        public string ZoneName = "Unknown";
        public string[] SpecialExceptions = new string[0];
        public bool BlockMalwarePorts = true;
        public bool EnableDefaultWindowsRules = true;
        public bool AllowLocalSubnet = false;
        public AppExceptionSettings[] AppExceptions = new AppExceptionSettings[0];


        internal ZoneSettings()
        {
            // Add recommended profiles as standard
            ApplicationCollection allKnownApps = GlobalInstances.ProfileMan.KnownApplications;
            foreach (Application app in allKnownApps)
            {
                if (app.Recommended && app.Special)
                    SpecialExceptions = Utils.ArrayAddItem(SpecialExceptions, app.Name);
            }
        }

        internal void Save()
        {
            // Construct file path
            string SettingsFile = Path.Combine(SettingsManager.AppDataPath, "Zone"+ZoneName);

            // Construct key
            string key = ENC_SALT + MachineFingerprint.Fingerprint();
            key = Hasher.HashString(key).Substring(0, 16);

            SerializationHelper.SaveToEncryptedXMLFile<ZoneSettings>(this, SettingsFile, key, ENC_IV);
        }

        internal static ZoneSettings Load(string zoneName)
        {
            try
            {
                // Construct file path
                string SettingsFile = Path.Combine(SettingsManager.AppDataPath, "Zone" + zoneName);

                // Construct key
                string key = ENC_SALT + MachineFingerprint.Fingerprint();
                key = Hasher.HashString(key).Substring(0, 16);

                return SerializationHelper.LoadFromEncryptedXMLFile<ZoneSettings>(SettingsFile, key, ENC_IV);
            }
            catch
            {
                ZoneSettings settings = new ZoneSettings();
                settings.ZoneName = zoneName;
                return settings;
            }
        }

        internal void Normalize()
        {
            for (int i = 0; i < AppExceptions.Length; ++i)
            {
                AppExceptionSettings app1 = AppExceptions[i];

                for (int j = AppExceptions.Length-1; j > i; --j)
                {
                    AppExceptionSettings app2 = AppExceptions[j];

                    if (app1.AppID == app2.AppID)
                    {
                        AppExceptionSettings older = app1;
                        AppExceptionSettings newer = app2;
                        if (app1.CreationDate > app2.CreationDate)
                        {
                            older = app2;
                            newer = app1;
                        }
                        AppExceptions = Utils.ArrayRemoveItem(AppExceptions, older);
                        newer.RegenerateID();
                    }
                    else if (AppExceptionSettings.ExecutableNameEquals(app1, app2) &&
                        (app1.Timer == AppExceptionTimer.Permanent) && (app2.Timer == AppExceptionTimer.Permanent))
                    {
                        List<string> profs = new List<string>();
                        profs.AddRange(app1.Profiles);
                        profs.AddRange(app2.Profiles);
                        app1.RegenerateID();
                        app1.Profiles = profs.Distinct().ToArray();
                        AppExceptions = Utils.ArrayRemoveItem(AppExceptions, app2);
                    }
                }

                // If "Blind trust" is enabled, then all other profiles are redundant
                if (Utils.ArrayContains(app1.Profiles, "Blind trust"))
                {
                    app1.RegenerateID();
                    app1.Profiles = new string[] { "Blind trust" };
                }
            }
        }
    }

    // Machine settings are global for the current computer
    // Applied by: Service
    // Controlled by: Controller
    // Visible by: Service, Controller
    [Serializable]
    public class MachineSettings : SettingsBase
    {
        private const string ENC_SALT = "O?2E/)YFq~e:w@a,";
        private const string ENC_IV = "X0@!H93!Y=8&/M/T";   // must be 16/24/32 bytes
        private readonly object locker = new object();

        public bool LockHostsFile = false;
        public bool HostsBlocklist = false;
        public DateTime LastUpdateCheck;
        public bool AutoUpdateCheck = true;
        public FirewallMode StartupMode = FirewallMode.Normal;

        internal void Save()
        {
            // Construct file path
            string SettingsFile = Path.Combine(SettingsManager.AppDataPath, "MachineConfig");

            // Construct key
            string key = ENC_SALT + MachineFingerprint.Fingerprint();
            key = Hasher.HashString(key).Substring(0, 16);

            lock (locker)
            {
                SerializationHelper.SaveToEncryptedXMLFile<MachineSettings>(this, SettingsFile, key, ENC_IV);
            }
        }

        internal static MachineSettings Load()
        {
            try
            {
                // Construct file path
                string SettingsFile = Path.Combine(SettingsManager.AppDataPath, "MachineConfig");

                // Construct key
                string key = ENC_SALT + MachineFingerprint.Fingerprint();
                key = Hasher.HashString(key).Substring(0, 16);

                return SerializationHelper.LoadFromEncryptedXMLFile<MachineSettings>(SettingsFile, key, ENC_IV);
            }
            catch
            {
                return new MachineSettings();
            }
        }
    }

    // Operational settings for the service
    // Applied by: Service
    // Controlled by: Service
    // Visible by: Service
    [Serializable]
    public class ServiceSettings : SettingsBase
    {
        private const string ENC_SALT = "O?2E/)YFq~e:w@a,";
        private const string ENC_IV = "X0@!H93!Y=8&/M/T";   // must be 16/24/32 bytes

        internal static string PasswordFilePath
        {
            get { return Path.Combine(SettingsManager.AppDataPath, "pwd"); }

        }
            
        private bool _Locked;

        internal bool Locked
        {
            get { return _Locked && HasPassword; }
            set
            {
                if (value && HasPassword)
                    _Locked = true;
            }
        }

        internal bool Unlock(string passHash)
        {
            if (!HasPassword)
                return true;

            try
            {
                // Construct file path
                string SettingsFile = PasswordFilePath;

                // Construct key
                string key = ENC_SALT + passHash;
                key = Hasher.HashString(key).Substring(0, 16);
                string hash = SerializationHelper.LoadFromEncryptedXMLFile<string>(SettingsFile, key, ENC_IV);
                if (hash == passHash)
                    _Locked = false;
            }
            catch
            {
                return false;
            }

            return !_Locked;
        }

        internal bool HasPassword
        {
            get
            {
                if (!File.Exists(PasswordFilePath))
                    return false;

                FileInfo fi = new FileInfo(PasswordFilePath);
                return (fi.Length != 0);
            }
        }

        internal void SetPass(string passHash)
        {
            // Construct file path
            string SettingsFile = PasswordFilePath;

            if (passHash == Hasher.HashString(string.Empty))
                // If we have no password, delete password explicitly
                File.Delete(SettingsFile);
            else
            {
                // Construct key
                string key = ENC_SALT + passHash;
                key = Hasher.HashString(key).Substring(0, 16);

                SerializationHelper.SaveToEncryptedXMLFile<string>(passHash, SettingsFile, key, ENC_IV);
            }
        }

        /* Keep this around in case we need it in the future
        internal void Save()
        {
            // Construct file path
            string SettingsFile = FilePath;

            // Construct key
            string key = ENC_SALT + MachineFingerprint.Fingerprint();
            key = Hasher.HashString(key).Substring(0, 16);

            SerializationHelper.SaveToEncryptedXMLFile<ServiceSettings>(this, SettingsFile, key, ENC_IV);
        }

        internal static ServiceSettings Load()
        {
            try
            {
                // Construct file path
                string SettingsFile = FilePath;

                // Construct key
                string key = ENC_SALT + MachineFingerprint.Fingerprint();
                key = Hasher.HashString(key).Substring(0, 16);

                return SerializationHelper.LoadFromEncryptedXMLFile<ServiceSettings>(SettingsFile, key, ENC_IV);
            }
            catch
            {
                // TODO: handle this in caller
                return new ServiceSettings();
            }
        }

        internal static string FilePath
        {
            get
            {
                const string fn = "SrvConfig";
                return Path.Combine(SettingsManager.AppDataPath, fn);
            }
        }
        */
    }

    // Operational settings for the controller
    // Applied by: Controller
    // Controlled by: Controller
    // Visible by: Controller
    [Serializable]
    public class ControllerSettings : SettingsBase
    {
        // Connections window
        public System.Windows.Forms.FormWindowState ConnFormWindowState = System.Windows.Forms.FormWindowState.Normal;
        public System.Drawing.Point ConnFormWindowLoc = new System.Drawing.Point(150, 150);
        public System.Drawing.Size ConnFormWindowSize = new System.Drawing.Size(827, 386);
        public bool ConnFormShowConnections = true;
        public bool ConnFormShowOpenPorts = false;
        public bool ConnFormShowBlocked = false;

        // Manage window
        public bool AskForExceptionDetails = false;
        public int ManageTabIndex;

        public static string UserDataPath
        {
            get
            {
                string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                dir = System.IO.Path.Combine(dir, SettingsManager.APP_NAME);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public void Save()
        {
            // Construct file path
            string SettingsFile = Path.Combine(UserDataPath, "ControllerConfig");

            SerializationHelper.SaveToXMLFile(this, SettingsFile);
        }

        public static ControllerSettings Load()
        {
            try
            {
                // Construct file path
                string SettingsFile = Path.Combine(UserDataPath, "ControllerConfig");

                return SerializationHelper.LoadFromXMLFile<ControllerSettings>(SettingsFile);
            }
            catch
            {
                return new ControllerSettings();
            }
        }
    }
    
    [Serializable]
    internal static class SettingsManager
    {
        public const string APP_NAME = "TinyWall";

        internal static ZoneSettings CurrentZone;
        internal static MachineSettings GlobalConfig;
        internal static ServiceSettings ServiceConfig;
        internal static ControllerSettings ControllerConfig;
        internal static int Changeset;

        internal static string AppDataPath
        {
            get
            {
#if DEBUG
                return Path.GetDirectoryName(Utils.ExecutablePath);
#else
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), SettingsManager.APP_NAME);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
#endif
            }
        }
    }

    public class SettingsContainer
    {
        public ZoneSettings CurrentZone;
        public MachineSettings GlobalConfig;
        public ServiceSettings ServiceConfig;
        public ControllerSettings ControllerConfig;
    }
}


/*
private static bool GetRegistryValueBool(string path, string value, bool standard)
{
    try
    {
        using (RegistryKey key = Registry.LocalMachine.CreateSubKey(path, RegistryKeyPermissionCheck.ReadSubTree))
        {
            return ((int)key.GetValue(value, standard ? 1 : 0)) != 0;
        }
    }
    catch
    {
        return standard;
    }
}

private void SaveRegistryValueBool(string path, string valueName, bool value)
{
    using (RegistryKey key = Registry.LocalMachine.CreateSubKey(path, RegistryKeyPermissionCheck.ReadWriteSubTree))
    {
        key.SetValue(valueName, value ? 1 : 0);
    }
}*/
