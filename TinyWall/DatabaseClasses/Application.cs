using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace PKSoft
{
    [Serializable]  // Needed for ICloneable implementation 
    public class Application : ICloneable
    {
        // Application name
        [XmlAttributeAttribute()]
        public string Name;

        // If Recommended is set to true, this profile will be recommended
        // (and enabled) by default to the user.
        [XmlAttributeAttribute()]
        public bool Recommended;

        // Specifies whether this application should show up
        // in the "special exceptions" list.
        [XmlAttributeAttribute()]
        public bool Special;

        // Executables that belong to this application
        public ProfileAssocCollection Files = new ProfileAssocCollection();

        public bool ShouldSerializeRecommended()
        {
            return Recommended;
        }

        public bool ShouldSerializeSpecial()
        {
            return Special;
        }

        public object Clone()
        {
            return Utils.DeepClone(this);
        }

        public override string ToString()
        {
            return this.Name;
        }

        internal bool ResolveFilePaths()
        {
            bool found = false;
            for (int i = Files.Count - 1; i >= 0; --i)
            {
                ProfileAssoc pa = Files[i].SearchForFile();
                if (pa != null)
                {
                    found = true;
                    Files[i].Executable = pa.Executable;
                }
            }
            return found;
        }
    }
}
