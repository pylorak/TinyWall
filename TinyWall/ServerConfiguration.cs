using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace pylorak.TinyWall
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
    public sealed class BlockListSettings
    {
        [DataMember(EmitDefaultValue = false)]
        public bool EnableBlocklists = false;

        [DataMember(EmitDefaultValue = false)]
        public bool EnablePortBlocklist = false;

        [DataMember(EmitDefaultValue = false)]
        public bool EnableHostsBlocklist = false;
    }

    [DataContract(Namespace = "TinyWall")]
    public sealed class ServerProfileConfiguration
    {
        [DataMember(EmitDefaultValue = false)]
        public string ProfileName { get; set; } = string.Empty;

        [DataMember(EmitDefaultValue = false)]
        public List<string> SpecialExceptions { get; set; } = new List<string>();

        [DataMember(EmitDefaultValue = false)]
        public bool AllowLocalSubnet { get; set; } = false;

        [DataMember(EmitDefaultValue = false)]
        public List<FirewallExceptionV3> AppExceptions { get; set; } = new List<FirewallExceptionV3>();

        [DataMember(EmitDefaultValue = false)]
        public bool DisplayOffBlock { get; set; } = false;

        public ServerProfileConfiguration()
        { }

        public ServerProfileConfiguration(string name)
        {
            ProfileName = name;
        }

        public bool HasSpecialException(string name)
        {
            foreach (string appName in SpecialExceptions)
            {
                if (string.Equals(name, appName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void TryUnify(FirewallExceptionV3 olderEx, FirewallExceptionV3 newerEx, out bool removeOlder)
        {
            removeOlder = false;

            if (olderEx.Id == newerEx.Id)
            {
                // Two exceptions can have the same IDs if the user just edited
                // an exception, in which case the newer (edited) version
                // is added using the same ID as the unedited one.
                // The newer one then completely supersedes the older version.
                removeOlder = true;
            }
            else if (olderEx.Subject.Equals(newerEx.Subject)
                && (olderEx.Timer == AppExceptionTimer.Permanent)
                && (newerEx.Timer == AppExceptionTimer.Permanent)
            )
            {
                // Merge the older exception's rules into the newer one,
                // then keep only the newer.
                ExceptionPolicy targetPolicy = newerEx.Policy;
                if (olderEx.Policy.MergeRulesTo(ref targetPolicy))
                {
                    newerEx.Policy = targetPolicy;
                    newerEx.RegenerateId();
                    removeOlder = true;
                }
            }
        }

        public void AddExceptions(IList<FirewallExceptionV3> newList)
        {
            var dstList = AppExceptions;
            var srcList = new List<FirewallExceptionV3>(newList);

            // Dedup the list of new exceptions within itself,
            // before merging into the target list.
            DedupExceptions(srcList);

            var dstRemoved = new HashSet<int>();
            var srcRemoved = new HashSet<int>();

            for (int i = 0; i < dstList.Count; ++i)
            {
                if (dstRemoved.Contains(i))
                    continue;

                FirewallExceptionV3 app1 = dstList[i];

                for (int j = 0; j < srcList.Count; ++j)
                {
                    // app1 may have been removed by an earlier iteration of this
                    // inner loop. It must not take part in any further comparisons.
                    if (dstRemoved.Contains(i))
                        break;

                    if (srcRemoved.Contains(j))
                        continue;

                    FirewallExceptionV3 app2 = srcList[j];
                    var older = app1.CreationDate > app2.CreationDate ? app2 : app1;
                    bool olderIsApp1 = ReferenceEquals(older, app1);
                    var newer = olderIsApp1 ? app2 : app1;

                    TryUnify(older, newer, out bool doRemoveOlder);
                    if (doRemoveOlder)
                    {
                        if (olderIsApp1)
                            dstRemoved.Add(i);
                        else
                            srcRemoved.Add(j);
                    }
                } // for j
            } // for i

            // Remove entries that were marked for deletion
            for (int i = dstList.Count - 1; i >= 0; --i)
            {
                if (dstRemoved.Contains(i))
                    dstList.RemoveAt(i);
            }
            for (int j = srcList.Count - 1; j >= 0; --j)
            {
                if (srcRemoved.Contains(j))
                    srcList.RemoveAt(j);
            }
            dstList.AddRange(srcList);
        } // method

        public void Normalize()
        {
            DedupExceptions(AppExceptions);
        } // method

        private static void DedupExceptions(List<FirewallExceptionV3> exceptions)
        {
            // Set of indices we have removed thus we must not compare against
            var removed = new HashSet<int>();

            for (int i = 0; i < exceptions.Count; ++i)
            {
                if (removed.Contains(i))
                    continue;

                FirewallExceptionV3 app1 = exceptions[i];

                for (int j = i + 1; j < exceptions.Count; ++j)
                {
                    if (removed.Contains(i))
                        break;

                    if (removed.Contains(j))
                        continue;

                    FirewallExceptionV3 app2 = exceptions[j];
                    var older = app1.CreationDate > app2.CreationDate ? app2 : app1;
                    bool olderIsApp1 = ReferenceEquals(older, app1);
                    var newer = olderIsApp1 ? app2 : app1;

                    TryUnify(older, newer, out bool doRemoveOlder);
                    if (doRemoveOlder)
                    {
                        removed.Add(olderIsApp1 ? i : j);
                    }
                } // for j
            } // for i

            // Remove entries that were marked for deletion
            for (int i = exceptions.Count - 1; i >= 0; --i)
            {
                if (removed.Contains(i))
                    exceptions.RemoveAt(i);
            }
        } // method
    }

    [DataContract(Namespace = "TinyWall")]
    public sealed class ServerConfiguration : ISerializable<ServerConfiguration>
    {
        public int ConfigVersion { get; set; } = 1;

        // Machine settings
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

        private string _ActiveProfileName = string.Empty;

        [DataMember(EmitDefaultValue = false)]
        public string ActiveProfileName
        {
            get => _ActiveProfileName;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException($"Argument {nameof(value)} may not be null or empty.");

                _ActiveProfileName = value;
                _ActiveProfile = null;
            }
        }


        private ServerProfileConfiguration? _ActiveProfile;
        public ServerProfileConfiguration ActiveProfile
        {
            get
            {
                if (string.IsNullOrEmpty(ActiveProfileName))
                    throw new InvalidOperationException();

                if (_ActiveProfile is null)
                {
                    foreach (ServerProfileConfiguration profile in Profiles)
                    {
                        if (profile.ProfileName.Equals(ActiveProfileName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            _ActiveProfile = profile;
                            break;
                        }
                    }

                    if (_ActiveProfile is null)
                    {
                        _ActiveProfile = new ServerProfileConfiguration(ActiveProfileName);
                        Profiles.Add(_ActiveProfile);
                    }
                }

                return _ActiveProfile;
            }
        }

        // These keys are okay to be public. Only meant for obfuscation to prevent manual editing of config file by users.
        // Actual protection against unauthorized modification is provided by filesystem permissions.
        private const string ENC_SALT = @";n~3+i=wV;eg6Q@f";
        private const string ENC_IV = @"0!.&3x=GGu%>$G&5";   // must be 16/24/32 bytes

        public void Save(string filePath)
        {
            string key = Hasher.HashString(ENC_SALT).Substring(0, 16);
            SerializationHelper.SerializeToEncryptedFile(this, filePath, key, ENC_IV);
        }

        public static ServerConfiguration Load(string filePath)
        {
            string key = Hasher.HashString(ENC_SALT).Substring(0, 16);
            return SerializationHelper.DeserializeFromEncryptedFile(filePath, key, ENC_IV, new ServerConfiguration());
        }

        public void Normalize()
        {
            foreach (ServerProfileConfiguration profile in Profiles)
            {
                profile.Normalize();
            }
        }

        public JsonTypeInfo<ServerConfiguration> GetJsonTypeInfo()
        {
            return SourceGenerationContext.Default.ServerConfiguration;
        }

        internal static string AppDataPath
        {
            get
            {
#if DEBUG
                return Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
#else
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TinyWall");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
#endif
            }
        }

        public ServerConfiguration() { }

    } // class
}
