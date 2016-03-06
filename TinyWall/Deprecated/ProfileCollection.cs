using System;
using System.Collections.ObjectModel;

namespace PKSoft.Obsolete
{
    [Serializable]
    public sealed class ProfileCollection : Collection<Profile>
    {
        public bool Contains(string profileName)
        {
            for (int i = 0; i < this.Count; ++i)
            {
                if (string.Compare(profileName, this[i].Name, StringComparison.Ordinal) == 0)
                    return true;
            }

            return false;
        }
    }
}
