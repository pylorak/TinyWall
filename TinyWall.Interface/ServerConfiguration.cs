using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace TinyWall.Interface
{
    public enum FirewallMode
    {
        Normal,
        BlockAll,
        AllowOutgoing,
        Disabled,
        Learning,
        Unknown = 100
    }

    [DataContract(Namespace = "TinyWall")]
    [Serializable]
    public sealed class BlockListSettings
    {
        [DataMember(EmitDefaultValue = false)]
        public bool EnableBlocklists = false;

        [DataMember(EmitDefaultValue = false)]
        public bool EnablePortBlocklist = true;

        [DataMember(EmitDefaultValue = false)]
        public bool EnableHostsBlocklist = false;
    }

    [DataContract(Namespace = "TinyWall")]
    [Serializable]
    public sealed class ServerProfileConfiguration
    {
        [DataMember(EmitDefaultValue = false)]
        public string ProfileName { get; set; } = null;

        [DataMember(EmitDefaultValue = false)]
        public List<string> SpecialExceptions { get; set; } = new List<string>();

        [DataMember(EmitDefaultValue = false)]
        public bool AllowLocalSubnet { get; set; } = false;

        [DataMember(EmitDefaultValue = false)]
        public List<FirewallExceptionV3> AppExceptions { get; set; } = new List<FirewallExceptionV3>();

        public ServerProfileConfiguration()
        { }

        public ServerProfileConfiguration(string name)
        {
            ProfileName = name;
        }

        public void Normalize()
        {
            for (int i = 0; i < AppExceptions.Count; ++i)
            {
                FirewallExceptionV3 app1 = AppExceptions[i];

                for (int j = AppExceptions.Count - 1; j > i; --j)
                {
                    FirewallExceptionV3 app2 = AppExceptions[j];

                    if (app1.Id.Equals(app2.Id))
                    {
                        // With equal exception IDs, keep only the newer one
                        // Two exceptions can have the same IDs if the user just edited
                        // an exception, in which case the newer (edited) version
                        // is added using the same ID as the unedited one.

                        FirewallExceptionV3 older = app1;
                        FirewallExceptionV3 newer = app2;
                        if (app1.CreationDate > app2.CreationDate)
                        {
                            older = app2;
                            newer = app1;
                        }
                        AppExceptions.Remove(older);
                        newer.RegenerateId();
                    }
                    else if (app1.Subject.Equals(app2)
                        && (app1.Timer == AppExceptionTimer.Permanent)
                        && (app2.Timer == AppExceptionTimer.Permanent)
                    )
                    {
                        // Merge rules
                        ExceptionPolicy targetPolicy = app1.Policy;
                        if (app2.Policy.MergeRulesTo(ref targetPolicy))
                        {
                            AppExceptions.Remove(app2);
                            app1.Policy = targetPolicy;
                            app1.RegenerateId();
                        }
                    }
                }
            } // for all exceptions
        } // method
    }

    [DataContract(Namespace = "TinyWall")]
    [Serializable]
    public sealed class ServerConfiguration
    {
        private const string APP_NAME = "TinyWall";
        private readonly object locker = new object();

        public int ConfigVersion { get; set; } = 1;

        // Machine settings
        [DataMember(EmitDefaultValue = false)]
        public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;

        [DataMember(EmitDefaultValue = false)]
        public BlockListSettings Blocklists { get; set; } = new BlockListSettings();

        [DataMember(EmitDefaultValue = false)]
        public bool LockHostsFile { get; set; } = true;

        [DataMember(EmitDefaultValue = false)]
        public bool AutoUpdateCheck { get; set; } = true;

        [DataMember(EmitDefaultValue = false)]
        public FirewallMode StartupMode { get; set; } = FirewallMode.Normal;

        [DataMember(EmitDefaultValue = false)]
        public List<ServerProfileConfiguration> Profiles { get; set; } = new List<ServerProfileConfiguration>();

        [DataMember(EmitDefaultValue = false)]
        private string ActiveProfileName = null;

        public void SetActiveProfile(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
                throw new ArgumentException();

            ActiveProfileName = profileName;
            _ActiveProfile = null;
        }
        
        [NonSerialized]
        private ServerProfileConfiguration _ActiveProfile = null;
        public ServerProfileConfiguration ActiveProfile
        {
            get
            {
                if (string.IsNullOrEmpty(ActiveProfileName))
                    throw new InvalidProgramException();

                if (null == _ActiveProfile)
                {
                    foreach (ServerProfileConfiguration profile in Profiles)
                    {
                        if (profile.ProfileName.Equals(ActiveProfileName))
                        {
                            _ActiveProfile = profile;
                            break;
                        }
                    }

                    if (null == _ActiveProfile)
                    {
                        if (Profiles.Count == 0)
                            Profiles.Add(new ServerProfileConfiguration(ActiveProfileName));
                        _ActiveProfile = Profiles[0];
                    }
                }

                return _ActiveProfile;
            }
        }

        private const string ENC_SALT = @";n~3+i=wV;eg6Q@f";
        private const string ENC_IV = @"0!.&3x=GGu%>$G&5";   // must be 16/24/32 bytes

        public void Save(string filePath)
        {
            string key = Internal.Hasher.HashString(ENC_SALT).Substring(0, 16);

            lock (locker)
            {
                Internal.SerializationHelper.SaveToEncryptedXMLFile(this, filePath, key, ENC_IV);
            }
        }

        public static ServerConfiguration Load(string filePath)
        {
            string key = Internal.Hasher.HashString(ENC_SALT).Substring(0, 16);
            return Internal.SerializationHelper.LoadFromEncryptedXMLFile<ServerConfiguration>(filePath, key, ENC_IV);
        }

        public void Normalize()
        {
            foreach (ServerProfileConfiguration profile in Profiles)
            {
                profile.Normalize();
            }
        }


        internal static string AppDataPath
        {
            get
            {
#if DEBUG
                return Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
#else
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TinyWall" /* TODO: was PKSoft.ServiceSettings21.APP_NAME */);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
#endif
            }
        }

        public ServerConfiguration() { }

    } // class
}
