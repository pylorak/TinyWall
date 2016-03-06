using System;
using System.IO;

namespace PKSoft.Obsolete
{
    [Obsolete]
    public class ProfileManager
    {
        private ProfileCollection m_Profiles;
        private ApplicationCollection m_Applications;

        internal DatabaseClasses.AppDatabase ToNewFormat()
        {
            DatabaseClasses.AppDatabase ret = new DatabaseClasses.AppDatabase();

            foreach (PKSoft.Obsolete.Application oldApp in m_Applications)
            {
                ret.KnownApplications.Add(oldApp.ToNewFormat());
            }

            return ret;
        }

        public static string DBPath
        {
            get { return Path.Combine(ServiceSettings21.AppDataPath, "profiles.xml"); }
        }

        public static ProfileManager Load(string filePath)
        {
            ProfileManager newInstance = Deprecated.SerializationHelper.LoadFromXMLFile<ProfileManager>(filePath);
            return newInstance;
        }

        public void Save(string filePath)
        {
            // TODO: deprecated
            //SerializationHelper.SaveToXMLFile(this, filePath);
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
