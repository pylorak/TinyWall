using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using TinyWall.Interface.Internal;
using TinyWall.Interface;
using TinyWall;

using System.Xml.Serialization;

namespace PKSoft
{
    [Serializable]
    public class ZoneSettings
    {
        public string ZoneName = "Unknown";
        public List<string> SpecialExceptions = new List<string>();
        public bool AllowLocalSubnet = false;
        public List<FirewallExceptionV3> AppExceptions = new List<FirewallExceptionV3>();
    }

    [Serializable]
    public class MachineSettings
    {
        public BlockListSettings Blocklists = new BlockListSettings();
        public bool LockHostsFile = true;
        public DateTime LastUpdateCheck;
        public bool AutoUpdateCheck = true;
        public FirewallMode StartupMode = FirewallMode.Normal;
    }


    // --------------------------------------------------------------------------------------------------------------------------------


    
    [Obsolete]
    public sealed class ServiceSettings21
    {
        internal const string APP_NAME = "TinyWall";
        private const string ENC_SALT = @";U~2+i=wV;eE3Q@f";
        private const string ENC_IV = @"0!#&9x=GGu%>$g&5";   // must be 16/24/32 bytes

        public int SequenceNumber = -1;

        // Machine settings
        public BlockListSettings Blocklists = new BlockListSettings();
        public bool LockHostsFile = true;
        public DateTime LastUpdateCheck;
        public bool AutoUpdateCheck = true;
        public FirewallMode StartupMode = FirewallMode.Normal;

        // Zone settings
        public List<string> SpecialExceptions = new List<string>();
        public bool AllowLocalSubnet = false;
        public List<Obsolete.FirewallException> AppExceptions = new List<Obsolete.FirewallException>();

        internal static string AppDataPath
        {
            get
            {
#if DEBUG
                return Path.GetDirectoryName(TinyWall.Interface.Internal.Utils.ExecutablePath);
#else
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ServiceSettings21.APP_NAME);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
#endif
            }
        }
        
        internal ServiceSettings21() { }

        /*
        internal ServiceSettings21(bool enableRecommendedExceptions)
        {
            if (enableRecommendedExceptions)
            {
                // Add recommended profiles as standard
                Obsolete.ApplicationCollection allKnownApps = GlobalInstances.ProfileMan.KnownApplications;
                foreach (Obsolete.Application app in allKnownApps)
                {
                    if (app.Recommended && app.Special)
                        SpecialExceptions.Add(app.Name);
                }
            }
        }
        */

        internal void Save()
        {
            throw new InvalidOperationException();
        }

        internal ServerConfiguration ToNewFormat()
        {
            ServerConfiguration ret = new ServerConfiguration();
            ret.SetActiveProfile(PKSoft.Resources.Messages.Default);
            ret.AutoUpdateCheck = this.AutoUpdateCheck;
            ret.Blocklists.EnableBlocklists = this.Blocklists.EnableBlocklists;
            ret.Blocklists.EnableHostsBlocklist = this.Blocklists.EnableHostsBlocklist;
            ret.Blocklists.EnablePortBlocklist = this.Blocklists.EnablePortBlocklist;
            ret.LockHostsFile = this.LockHostsFile;
            ret.StartupMode = this.StartupMode;

            ServerProfileConfiguration prof = ret.ActiveProfile;
            prof.AllowLocalSubnet = this.AllowLocalSubnet;
            prof.SpecialExceptions = this.SpecialExceptions;
            foreach (Obsolete.FirewallException ex in this.AppExceptions)
                prof.AppExceptions.Add(ex.ToNewFormat2());

            return ret;
        }

        internal static ServiceSettings21 Load()
        {
            ServiceSettings21 ret = null;

            // Construct file path
            string SettingsFile = Path.Combine(ServiceSettings21.AppDataPath, "config");

            if (File.Exists(SettingsFile))
            {
                try
                {
                    // Construct key
                    string key = ENC_SALT;
                    key = Hasher.HashString(key).Substring(0, 16);

                    ret = Deprecated.SerializationHelper.LoadFromEncryptedXMLFile<ServiceSettings21>(SettingsFile, key, ENC_IV);
                    List<string> distinctSpecialEx = new List<string>();
                    distinctSpecialEx.AddRange(ret.SpecialExceptions.Distinct());
                    ret.SpecialExceptions = distinctSpecialEx;
                }
                catch
                {
                }

                if (ret == null)
                {
                    // Try again by loading config file from older versions, which uses a different encryption key
                    try
                    {
                        // Construct key
                        string key = ENC_SALT + Deprecated.MachineFingerprint.Fingerprint();
                        key = Hasher.HashString(key).Substring(0, 16);

                        ret = Deprecated.SerializationHelper.LoadFromEncryptedXMLFile<ServiceSettings21>(SettingsFile, key, ENC_IV);
                        List<string> distinctSpecialEx = new List<string>();
                        distinctSpecialEx.AddRange(ret.SpecialExceptions.Distinct());
                        ret.SpecialExceptions = distinctSpecialEx;
                    }
                    catch
                    {
                    }
                }
            }

            return ret;
        }

    } // class

    [Serializable]
    public sealed class ControllerSettings
    {
        // UI Localization
        public string Language = "auto";

        // Connections window
        public System.Windows.Forms.FormWindowState ConnFormWindowState = System.Windows.Forms.FormWindowState.Normal;
        public System.Drawing.Point ConnFormWindowLoc = new System.Drawing.Point(150, 150);
        public System.Drawing.Size ConnFormWindowSize = new System.Drawing.Size((int)(830 * Utils.DpiScalingFactor), (int)(400 * Utils.DpiScalingFactor));
        public bool ConnFormShowConnections = true;
        public bool ConnFormShowOpenPorts = false;
        public bool ConnFormShowBlocked = false;

        // Processes window
        public System.Windows.Forms.FormWindowState ProcessesFormWindowState = System.Windows.Forms.FormWindowState.Normal;
        public System.Drawing.Point ProcessesFormWindowLoc = new System.Drawing.Point(150, 150);
        public System.Drawing.Size ProcessesFormWindowSize = new System.Drawing.Size((int)(830 * Utils.DpiScalingFactor), (int)(400 * Utils.DpiScalingFactor));

        // Manage window
        public bool AskForExceptionDetails = false;
        public int SettingsTabIndex;
        public System.Drawing.Point SettingsFormWindowLoc = new System.Drawing.Point(150, 150);
        public System.Drawing.Size SettingsFormWindowSize = new System.Drawing.Size((int)(768 * Utils.DpiScalingFactor), (int)(486 * Utils.DpiScalingFactor));

        // Hotkeys
        public bool EnableGlobalHotkeys = true;

        internal static string UserDataPath
        {
            get
            {
                string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                dir = System.IO.Path.Combine(dir, "TinyWall");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
            }
        }

        internal void Save()
        {
            string SettingsFile = Path.Combine(UserDataPath, "ControllerConfig");
            try
            {
                using (AtomicFileUpdater fileUpdater = new AtomicFileUpdater(SettingsFile))
                {
                    SerializationHelper.SaveToXMLFile(this, fileUpdater.TemporaryFilePath);
                    fileUpdater.Commit();
                }
            }
            catch { }
        }

        internal static ControllerSettings Load()
        {
            ControllerSettings ret = null;

            // Construct file path
            string SettingsFile = Path.Combine(UserDataPath, "ControllerConfig");

            if (File.Exists(SettingsFile))
            {
                try
                {
                    ret = SerializationHelper.LoadFromXMLFile<ControllerSettings>(SettingsFile);
                }
                catch
                {
                    try
                    {
                        ret = Deprecated.SerializationHelper.LoadFromXMLFile<ControllerSettings>(SettingsFile);
                    }
                    catch { }
                }
            }

            if (ret == null)
            {
                ret = new ControllerSettings();
            }

            return ret;
        }
    }

    public sealed class PasswordManager
    {
        internal static string PasswordFilePath { get; } = Path.Combine(Utils.AppDataPath, "pwd");

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

        internal void SetPass(string password)
        {
            // Construct file path
            string SettingsFile = PasswordFilePath;

            if (password == string.Empty)
                // If we have no password, delete password explicitly
                File.Delete(SettingsFile);
            else
            {
                using (AtomicFileUpdater fileUpdater = new AtomicFileUpdater(PasswordFilePath))
                {
                    string salt = Utils.RandomString(8);
                    string hash = TinyWall.Pbkdf2.GetHashForStorage(password, salt, 150000, 16);
                    File.WriteAllText(fileUpdater.TemporaryFilePath, hash, Encoding.UTF8);
                    fileUpdater.Commit();
                }
            }
        }

        internal bool Unlock(string password)
        {
            if (!HasPassword)
                return true;

            try
            {   // TODO: Try to read old password format first.
                // Remove once TW 2.1 is not supported anymore.

                const string ENC_SALT = "O?2E/)YFq~e:w@a,";
                const string ENC_IV = "X0@!H93!Y=8&/M/T";   // must be 16/24/32 bytes

                // Construct key
                string key = ENC_SALT + password;
                key = Hasher.HashString(key).Substring(0, 16);
                string hash = Deprecated.SerializationHelper.LoadFromEncryptedXMLFile<string>(PasswordFilePath, key, ENC_IV);
                if (hash == password)
                {
                    _Locked = false;
                    SetPass(password);  // re-write password using new method
                }
            }
            catch
            {
                try
                {
                    string storedHash = System.IO.File.ReadAllText(PasswordFilePath, System.Text.Encoding.UTF8);
                    _Locked = !Pbkdf2.CompareHash(storedHash, password);
                }
                catch { }
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
    }

    public sealed class ConfigContainer
    {
        public ServerConfiguration Service = null;
        public ControllerSettings Controller = null;
    }

    namespace Obsolete
    {
        [Obsolete, Serializable]
        public sealed class ConfigContainer
        {
            public ServiceSettings21 Service = null;
            public ControllerSettings Controller = null;
        }
    }

    internal static class ActiveConfig
    {
        internal static ServerConfiguration Service = null;
        internal static ControllerSettings Controller = null;

        /*
        internal static ConfigContainer ToContainer()
        {
            ConfigContainer c = new ConfigContainer();
            c.Controller = ActiveConfig.Controller;
            c.Service = ActiveConfig.Service;
            return c;
        }*/
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

