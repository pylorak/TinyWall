using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using TinyWall.Interface.Internal;
using TinyWall.Interface;
using TinyWall;
using System.Runtime.Serialization;

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

    [Serializable]
    public sealed class ControllerSettings
    {
        // UI Localization
        public string Language = "auto";

        // Connections window
        public System.Windows.Forms.FormWindowState ConnFormWindowState = System.Windows.Forms.FormWindowState.Normal;
        public System.Drawing.Point ConnFormWindowLoc = new System.Drawing.Point(0, 0);
        public System.Drawing.Size ConnFormWindowSize = new System.Drawing.Size(0, 0);
        [OptionalField]
        public Dictionary<string, int> ConnFormColumnWidths = new Dictionary<string, int>();
        public bool ConnFormShowConnections = true;
        public bool ConnFormShowOpenPorts = false;
        public bool ConnFormShowBlocked = false;

        // Processes window
        public System.Windows.Forms.FormWindowState ProcessesFormWindowState = System.Windows.Forms.FormWindowState.Normal;
        public System.Drawing.Point ProcessesFormWindowLoc = new System.Drawing.Point(0, 0);
        public System.Drawing.Size ProcessesFormWindowSize = new System.Drawing.Size(0, 0);
        [OptionalField]
        public Dictionary<string, int> ProcessesFormColumnWidths = new Dictionary<string, int>();

        // Services window
        public System.Windows.Forms.FormWindowState ServicesFormWindowState = System.Windows.Forms.FormWindowState.Normal;
        public System.Drawing.Point ServicesFormWindowLoc = new System.Drawing.Point(0, 0);
        public System.Drawing.Size ServicesFormWindowSize = new System.Drawing.Size(0, 0);
        [OptionalField]
        public Dictionary<string, int> ServicesFormColumnWidths = new Dictionary<string, int>();

        // UwpPackages window
        public System.Windows.Forms.FormWindowState UwpPackagesFormWindowState = System.Windows.Forms.FormWindowState.Normal;
        public System.Drawing.Point UwpPackagesFormWindowLoc = new System.Drawing.Point(0, 0);
        public System.Drawing.Size UwpPackagesFormWindowSize = new System.Drawing.Size(0, 0);
        [OptionalField]
        public Dictionary<string, int> UwpPackagesFormColumnWidths = new Dictionary<string, int>();

        // Manage window
        public bool AskForExceptionDetails = false;
        public int SettingsTabIndex;
        public System.Drawing.Point SettingsFormWindowLoc = new System.Drawing.Point(0, 0);
        public System.Drawing.Size SettingsFormWindowSize = new System.Drawing.Size(0, 0);
        [OptionalField]
        public Dictionary<string, int> SettingsFormAppListColumnWidths = new Dictionary<string, int>();

        // Hotkeys
        public bool EnableGlobalHotkeys = true;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc)
        {
            if (ConnFormColumnWidths == null)
                ConnFormColumnWidths = new Dictionary<string, int>();
            if (ProcessesFormColumnWidths == null)
                ProcessesFormColumnWidths = new Dictionary<string, int>();
            if (ServicesFormColumnWidths == null)
                ServicesFormColumnWidths = new Dictionary<string, int>();
            if (UwpPackagesFormColumnWidths == null)
                UwpPackagesFormColumnWidths = new Dictionary<string, int>();
            if (SettingsFormAppListColumnWidths == null)
                SettingsFormAppListColumnWidths = new Dictionary<string, int>();
        }

        internal static string UserDataPath
        {
            get
            {
#if DEBUG
                return Path.GetDirectoryName(TinyWall.Interface.Internal.Utils.ExecutablePath);
#else
                string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                dir = System.IO.Path.Combine(dir, "TinyWall");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
#endif
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
                { }
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
            {
                string storedHash = System.IO.File.ReadAllText(PasswordFilePath, System.Text.Encoding.UTF8);
                _Locked = !Pbkdf2.CompareHash(storedHash, password);
            }
            catch { }

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

