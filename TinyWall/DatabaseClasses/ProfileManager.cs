using System.Collections.Generic;
using System.IO;

namespace PKSoft
{
    public class ProfileManager
    {
        private ProfileCollection m_Profiles;
        private ProfileCollection m_GenericProfiles;
        private ApplicationCollection m_Applications;

        public static string DBPath
        {
            get { return Path.Combine(SettingsManager.AppDataPath, "profiles.xml"); }
        }

        public static ProfileManager Load(string filePath)
        {
            ProfileManager newInstance = SerializationHelper.LoadFromXMLFile<ProfileManager>(filePath);
            newInstance.FinishLoading();
            return newInstance;
        }

        public void Save(string filePath)
        {
            SerializationHelper.SaveToXMLFile(this, filePath);

            FinishLoading();
        }

        public ProfileManager()
        {
            m_Profiles = new ProfileCollection();
            m_GenericProfiles = new ProfileCollection();
            m_Applications = new ApplicationCollection();
            FinishLoading();
        }

        private void FinishLoading()
        {
            // Create built-in profiles
            {
                Profile p;

                if (!m_GenericProfiles.Contains("Blind trust"))
                {
                    // "Blind trust"
                    p = new Profile();
                    p.Name = "Blind trust";
                    p.Rules = new RuleDef[] { new RuleDef(AppExceptionSettings.GenerateID(), "Blind trust", WindowsFirewall.PacketAction.Allow, WindowsFirewall.RuleDirection.InOut, WindowsFirewall.Protocol.Any) };
                    m_GenericProfiles.Add(p);
                }

                if (!m_GenericProfiles.Contains("Outbound"))
                {
                    // "Outbound"
                    p = new Profile();
                    p.Name = "Outbound";
                    p.Rules = new RuleDef[] { new RuleDef(AppExceptionSettings.GenerateID(), "Allow outbound", WindowsFirewall.PacketAction.Allow, WindowsFirewall.RuleDirection.Out, WindowsFirewall.Protocol.TcpUdp) };
                    m_GenericProfiles.Add(p);
                }

                if (!m_GenericProfiles.Contains("Block"))
                {
                    // "Block"
                    p = new Profile();
                    p.Name = "Block";
                    p.Rules = new RuleDef[] { new RuleDef(AppExceptionSettings.GenerateID(), "Block", WindowsFirewall.PacketAction.Block, WindowsFirewall.RuleDirection.InOut, WindowsFirewall.Protocol.Any) };
                    m_GenericProfiles.Add(p);
                }
            }
        }

        public ProfileCollection AvailableProfiles
        {
            get { return m_GenericProfiles; }
        }

        public Profile GetProfile(string name)
        {
            for (int i = 0; i < this.m_GenericProfiles.Count; ++i)
            {
                if (string.Compare(name, m_GenericProfiles[i].Name, System.StringComparison.InvariantCultureIgnoreCase) == 0)
                    return Utils.DeepClone(m_GenericProfiles[i]);
            }
            for (int i = 0; i < m_Profiles.Count; ++i)
            {
                if (string.Compare(name, m_Profiles[i].Name, System.StringComparison.InvariantCultureIgnoreCase) == 0)
                    return Utils.DeepClone(m_Profiles[i]);
            }
            return null;
        }

        public ApplicationCollection KnownApplications
        {
            get { return m_Applications; }
        }

        public Application GetApplicationByName(string name)
        {
            for (int i = 0; i < m_Applications.Count; ++i)
            {
                if (string.Compare(name, m_Applications[i].Name, System.StringComparison.InvariantCultureIgnoreCase) == 0)
                    return m_Applications[i];
            }
            return null;
        }

        public Application TryGetRecognizedApp(string executablePath, string service, out ProfileAssoc file)
        {
            ProfileAssoc exe = ProfileAssoc.FromExecutable(executablePath, service);

            for (int i = 0; i < m_Applications.Count; ++i)
            {
                for (int j = 0; j < m_Applications[i].Files.Count; ++j)
                {
                    ProfileAssoc assoc = m_Applications[i].Files[j];
                    if (assoc.DoesExecutableSatisfy(exe))
                    {
                        file = assoc.Clone() as ProfileAssoc;
                        file.Executable = executablePath;
                        return m_Applications[i];
                    }
                }
            }

            file = null;
            return null;
        }

        public ProfileCollection GetProfilesFor(ProfileAssoc app)
        {
            ProfileCollection ret = new ProfileCollection();
            for (int j = 0; j < app.Profiles.Length; ++j)
            {
                Profile p = GetProfile(app.Profiles[j]);
                if (p != null)
                    ret.Add(p);
            }
            return ret;
        }
    }
}
