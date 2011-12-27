using System.Collections.Generic;
using System.IO;

namespace PKSoft
{
    public class ProfileManager
    {
        private ProfileCollection m_Profiles;
        private ProfileCollection m_GenericProfiles;
        private ProfileAssocCollection m_Associations;

        public static string DBPath
        {
            get { return Path.Combine(SettingsManager.AppDataPath, "profiles.xml"); }
        }

        public static ProfileManager Load(string fpath)
        {
            ProfileManager newInstance = SerializationHelper.LoadFromXMLFile<ProfileManager>(fpath);
            newInstance.FinishLoading();
            return newInstance;
        }

        public void Save(string fpath)
        {
            SerializationHelper.SaveToXMLFile(this, fpath);

            FinishLoading();
        }

        public ProfileManager()
        {
            m_Profiles = new ProfileCollection();
            m_GenericProfiles = new ProfileCollection();
            m_Associations = new ProfileAssocCollection();
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
                    p.Rules = new RuleDef[] { new RuleDef("Blind trust", WindowsFirewall.PacketAction.Allow, WindowsFirewall.RuleDirection.InOut, WindowsFirewall.Protocol.Any) };
                    m_GenericProfiles.Add(p);
                }

                if (!m_GenericProfiles.Contains("Outbound"))
                {
                    // "Outbound"
                    p = new Profile();
                    p.Name = "Outbound";
                    p.Rules = new RuleDef[] { new RuleDef("Allow outbound", WindowsFirewall.PacketAction.Allow, WindowsFirewall.RuleDirection.Out, WindowsFirewall.Protocol.TcpUdp) };
                    m_GenericProfiles.Add(p);
                }

                if (!m_GenericProfiles.Contains("Block"))
                {
                    // "Block"
                    p = new Profile();
                    p.Name = "Block";
                    p.Rules = new RuleDef[] { new RuleDef("Block", WindowsFirewall.PacketAction.Block, WindowsFirewall.RuleDirection.InOut, WindowsFirewall.Protocol.Any) };
                    m_GenericProfiles.Add(p);
                }
            }
/*
            // Sort generic profiles into GenericProfiles
            for (int i = Profiles.Count - 1; i >= 0; --i)
            {
                if (!Profiles[i].AppSpecific)
                {
                    GenericProfiles.Add(Profiles[i]);
                    Profiles.RemoveAt(i);
                }
            }
*/
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
                    return m_GenericProfiles[i];
            }
            for (int i = 0; i < m_Profiles.Count; ++i)
            {
                if (string.Compare(name, m_Profiles[i].Name, System.StringComparison.InvariantCultureIgnoreCase) == 0)
                    return m_Profiles[i];
            }
            return null;
        }

        public ProfileAssocCollection KnownApplications
        {
            get { return m_Associations; }
        }

        public ProfileAssoc GetApplication(string desc)
        {
            for (int i = 0; i < m_Associations.Count; ++i)
            {
                if (string.Compare(desc, m_Associations[i].Description, System.StringComparison.InvariantCultureIgnoreCase) == 0)
                    return m_Associations[i];
            }
            return null;
        }

        public ProfileAssoc TryGetRecognizedApp(string exepath, string service)
        {
            ProfileAssoc exe = ProfileAssoc.FromExecutable(exepath, service);

            for (int i = 0; i < m_Associations.Count; ++i)
            {
                ProfileAssoc assoc = m_Associations[i];
                if (assoc.DoesExecutableSatisfy(exe))
                {
                    ProfileAssoc ret = assoc.Clone() as ProfileAssoc;
                    ret.Executable = exepath;
                    return ret;
                }
            }

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
