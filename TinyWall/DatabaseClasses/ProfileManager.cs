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
            return newInstance;
        }

        public void Save(string filePath)
        {
            SerializationHelper.SaveToXMLFile(this, filePath);
        }

        public ProfileManager()
        {
            m_Profiles = new ProfileCollection();
            m_Applications = new ApplicationCollection();
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
