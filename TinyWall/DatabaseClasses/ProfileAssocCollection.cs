using System;
using System.Collections.ObjectModel;

namespace PKSoft
{
    [Serializable]
    public class ProfileAssocCollection : Collection<ProfileAssoc>
    {
        internal bool ContainsFileRealization(string path)
        {
            for (int i = 0; i < this.Count; ++i)
            {
                if (this[i].Executable.Equals(path, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
