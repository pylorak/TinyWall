using System.Collections.Generic;
using System.IO;

namespace PKSoft
{
    public class ProfileManager
    {
        private ProfileCollection m_Profiles;
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

            // TODO: Figure out why we call FinishLoading() here?
            FinishLoading();
        }

        public ProfileManager()
        {
            m_Profiles = new ProfileCollection();
            m_Applications = new ApplicationCollection();
            FinishLoading();
        }

        private void FinishLoading()
        {
            // Create built-in profiles
            {
                Profile p;

                if (!m_Profiles.Contains("Blind trust"))
                {
                    // "Blind trust"
                    p = new Profile();
                    p.Name = "Blind trust";
                    p.Rules = new RuleDef[] { new RuleDef(FirewallException.GenerateID(), "Blind trust", WindowsFirewall.PacketAction.Allow, WindowsFirewall.RuleDirection.InOut, WindowsFirewall.Protocol.Any) };
                    m_Profiles.Add(p);
                }

                if (!m_Profiles.Contains("Outbound"))
                {
                    // "Outbound"
                    p = new Profile();
                    p.Name = "Outbound";
                    p.Rules = new RuleDef[] { new RuleDef(FirewallException.GenerateID(), "Allow outbound", WindowsFirewall.PacketAction.Allow, WindowsFirewall.RuleDirection.Out, WindowsFirewall.Protocol.TcpUdp) };
                    m_Profiles.Add(p);
                }

                if (!m_Profiles.Contains("Block"))
                {
                    // "Block"
                    p = new Profile();
                    p.Name = "Block";
                    p.Rules = new RuleDef[] { new RuleDef(FirewallException.GenerateID(), "Block", WindowsFirewall.PacketAction.Block, WindowsFirewall.RuleDirection.InOut, WindowsFirewall.Protocol.Any) };
                    m_Profiles.Add(p);
                }
            }
        }

        public ProfileCollection AvailableProfiles
        {
            get { return m_Profiles; }
        }

        public Profile GetProfile(string name)
        {
            for (int i = 0; i < this.m_Profiles.Count; ++i)
            {
                if (name.Equals(m_Profiles[i].Name, System.StringComparison.OrdinalIgnoreCase))
                    return Utils.DeepClone(m_Profiles[i]);
            }
            return null;
        }

        public ApplicationCollection KnownApplications
        {
            get { return m_Applications; }
        }

        public ProfileCollection GetProfilesFor(AppExceptionAssoc app)
        {
            ProfileCollection ret = new ProfileCollection();
            FirewallException ex = app.ExceptionTemplate;
            for (int j = 0; j < ex.Profiles.Count; ++j)
            {
                Profile p = GetProfile(ex.Profiles[j]);
                if (p != null)
                    ret.Add(p);
            }
            return ret;
        }
    }
}
