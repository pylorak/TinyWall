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
        [XmlArray("Files")]
        public ProfileAssocCollection FileTemplates = new ProfileAssocCollection();

        // Executables that belong to this application
        [XmlIgnore]
        public ProfileAssocCollection FileRealizations = new ProfileAssocCollection();

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
            ProfileAssocCollection foundFiles = new ProfileAssocCollection();
            for (int i = 0; i < FileTemplates.Count; ++i)
            {
                ProfileAssocCollection pac = FileTemplates[i].SearchForFile();
                foreach (ProfileAssoc pa in pac)
                    foundFiles.Add(pa);
            }

            FileRealizations.Clear();
            foreach (ProfileAssoc file in foundFiles)
                FileRealizations.Add(file);

            return foundFiles.Count > 0;
        }
    }
}
