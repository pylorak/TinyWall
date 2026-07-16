using pylorak.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization.Metadata;

namespace pylorak.TinyWall
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/PKSoft")]
    public sealed class ControllerSettings : ISerializable<ControllerSettings>
    {
        // UI Customization
        [DataMember(EmitDefaultValue = false)]
        public string Language = "auto";
        [DataMember(EmitDefaultValue = false)]
        public string UiTheme = "auto";

        // Connections window
        [DataMember(EmitDefaultValue = false)]
        public System.Windows.Forms.FormWindowState ConnFormWindowState = System.Windows.Forms.FormWindowState.Normal;
        [DataMember(EmitDefaultValue = false)]
        public System.Drawing.Point ConnFormWindowLoc = new(0, 0);
        [DataMember(EmitDefaultValue = false)]
        public System.Drawing.Size ConnFormWindowSize = new(0, 0);
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, int> ConnFormColumnWidths = new();
        [DataMember(EmitDefaultValue = false)]
        public bool ConnFormShowConnections = true;
        [DataMember(EmitDefaultValue = false)]
        public bool ConnFormShowOpenPorts = false;
        [DataMember(EmitDefaultValue = false)]
        public bool ConnFormShowBlocked = false;

        // Processes window
        [DataMember(EmitDefaultValue = false)]
        public System.Windows.Forms.FormWindowState ProcessesFormWindowState = System.Windows.Forms.FormWindowState.Normal;
        [DataMember(EmitDefaultValue = false)]
        public System.Drawing.Point ProcessesFormWindowLoc = new(0, 0);
        [DataMember(EmitDefaultValue = false)]
        public System.Drawing.Size ProcessesFormWindowSize = new(0, 0);
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, int> ProcessesFormColumnWidths = new();

        // Services window
        [DataMember(EmitDefaultValue = false)]
        public System.Windows.Forms.FormWindowState ServicesFormWindowState = System.Windows.Forms.FormWindowState.Normal;
        [DataMember(EmitDefaultValue = false)]
        public System.Drawing.Point ServicesFormWindowLoc = new(0, 0);
        [DataMember(EmitDefaultValue = false)]
        public System.Drawing.Size ServicesFormWindowSize = new(0, 0);
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, int> ServicesFormColumnWidths = new();

        // UwpPackages window
        [DataMember(EmitDefaultValue = false)]
        public System.Windows.Forms.FormWindowState UwpPackagesFormWindowState = System.Windows.Forms.FormWindowState.Normal;
        [DataMember(EmitDefaultValue = false)]
        public System.Drawing.Point UwpPackagesFormWindowLoc = new(0, 0);
        [DataMember(EmitDefaultValue = false)]
        public System.Drawing.Size UwpPackagesFormWindowSize = new(0, 0);
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, int> UwpPackagesFormColumnWidths = new();

        // Manage window
        [DataMember(EmitDefaultValue = false)]
        public bool AskForExceptionDetails = false;
        [DataMember(EmitDefaultValue = false)]
        public int SettingsTabIndex;
        [DataMember(EmitDefaultValue = false)]
        public System.Drawing.Point SettingsFormWindowLoc = new(0, 0);
        [DataMember(EmitDefaultValue = false)]
        public System.Drawing.Size SettingsFormWindowSize = new(0, 0);
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, int> SettingsFormAppListColumnWidths = new();

        // Hotkeys
        [DataMember(EmitDefaultValue = false)]
        public bool EnableGlobalHotkeys = true;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc)
        {
            ConnFormColumnWidths ??= new Dictionary<string, int>();
            ProcessesFormColumnWidths ??= new Dictionary<string, int>();
            ServicesFormColumnWidths ??= new Dictionary<string, int>();
            UwpPackagesFormColumnWidths ??= new Dictionary<string, int>();
            SettingsFormAppListColumnWidths ??= new Dictionary<string, int>();
        }

        internal static string UserDataPath
        {
            get
            {
#if DEBUG
                return Path.GetDirectoryName(Utils.ExecutablePath);
#else
                string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                dir = System.IO.Path.Combine(dir, "TinyWall");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
#endif
            }
        }

        internal static string FilePath => Path.Combine(UserDataPath, "ControllerConfig");

        internal void Save()
        {
            try
            {
                SerializationHelper.SerializeToFile(this, FilePath);
            }
            catch { }
        }

        internal static ControllerSettings Load()
        {
            try
            {
                return SerializationHelper.DeserializeFromFile(FilePath, new ControllerSettings());
            }
            catch { }

            return new ControllerSettings();
        }

        public JsonTypeInfo<ControllerSettings> GetJsonTypeInfo()
        {
            return SourceGenerationContext.Default.ControllerSettings;
        }
    }

    public static class PasswordLock
    {
        private const Pbkdf2.HashFunction PBKDF2_ALGO = Pbkdf2.HashFunction.SHA_256;
        private const int PBKDF2_SALT_LEN = 16;
        private const int PBKDF2_ITERATIONS = 200_000;

        internal static string PasswordFilePath { get; } = Path.Combine(Utils.AppDataPath, "pwd");

        private static bool _Locked;

        internal static bool Locked
        {
            get { return _Locked && HasPassword; }
            set
            {
                if (value && HasPassword)
                    _Locked = true;
            }
        }

        internal static void SetPass(string password)
        {
            // Construct file path
            string SettingsFile = PasswordFilePath;

            if (password == string.Empty)
                // If we have no password, delete password explicitly
                File.Delete(SettingsFile);
            else
            {
                using var rng = RandomNumberGenerator.Create();
                byte[] salt = new byte[PBKDF2_SALT_LEN];
                rng.GetBytes(salt);

                using var fileUpdater = new AtomicFileUpdater(PasswordFilePath);
                var hasher = new Pbkdf2(PBKDF2_ALGO, PBKDF2_ITERATIONS, salt, password);
                File.WriteAllText(fileUpdater.TemporaryFilePath, hasher.ToString(Pbkdf2.StorageFormat.Tw352), Encoding.UTF8);
                fileUpdater.Commit();
            }
        }

        internal static bool Unlock(string password, FileLocker fileLocker)
        {
            if (!HasPassword)
                return true;

            try
            {
                var storedHash = File.ReadAllText(PasswordFilePath, System.Text.Encoding.UTF8);
                var hashNeedsUpgrade = false;
                Pbkdf2 hasher;
                try
                {
                    hasher = Pbkdf2.Parse(storedHash, Pbkdf2.StorageFormat.Tw352);
                }
                catch   // Try loading hash in older format
                {
                    hashNeedsUpgrade = true;
                    hasher = Pbkdf2.Parse(storedHash, Pbkdf2.StorageFormat.Legacy);
                }
                _Locked = !hasher.IsHashOf(password);

                if (!_Locked)
                {
                    // Update stored hash if stored with older parameters
                    hashNeedsUpgrade = hashNeedsUpgrade ||
                        (PBKDF2_ITERATIONS != hasher.Iterations) ||
                        (PBKDF2_ALGO != hasher.Algorithm);

                    if (hashNeedsUpgrade)
                    {
                        fileLocker.Unlock(PasswordFilePath);
                        try
                        {
                            SetPass(password);
                        }
                        finally
                        {
                            fileLocker.Lock(PasswordFilePath, FileAccess.Read, FileShare.Read);
                        }
                    }
                }
            }
            catch { }

            return !_Locked;
        }

        internal static bool HasPassword
        {
            get
            {
                if (!File.Exists(PasswordFilePath))
                    return false;

                var fi = new FileInfo(PasswordFilePath);
                return (fi.Length != 0);
            }
        }
    }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/PKSoft")]
    public sealed class ConfigContainer : ISerializable<ConfigContainer>
    {
        [DataMember(EmitDefaultValue = false)]
        public ServerConfiguration Service;
        [DataMember(EmitDefaultValue = false)]
        public ControllerSettings Controller;

        public ConfigContainer()
        {
            Service = new ServerConfiguration();
            Controller = new ControllerSettings();
        }

        public ConfigContainer(ServerConfiguration server, ControllerSettings client)
        {
            Service = server;
            Controller = client;
        }

        public JsonTypeInfo<ConfigContainer> GetJsonTypeInfo()
        {
            return SourceGenerationContext.Default.ConfigContainer;
        }
    }

    internal static class ActiveConfig
    {
        [AllowNull]
        internal static ServerConfiguration Service = null;
        [AllowNull]
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

