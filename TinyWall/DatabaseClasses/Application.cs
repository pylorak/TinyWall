using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace PKSoft
{
    [Serializable]  // Needed for ICloneable implementation 
    public sealed class Application
    {
        // Application name
        [XmlAttributeAttribute()]
        public string Name;
        public string LocalizedName
        {
            get
            {
                try
                {
                    return PKSoft.Resources.Exceptions.ResourceManager.GetString(Name, PKSoft.Resources.Exceptions.Culture);
                }
                catch
                {
                    return Name;
                }
            }
        }

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
        public AppExceptionAssocCollection FileTemplates = new AppExceptionAssocCollection();

        public bool ShouldSerializeRecommended()
        {
            return Recommended;
        }

        public bool ShouldSerializeSpecial()
        {
            return Special;
        }

        public override string ToString()
        {
            return this.Name;
        }

        internal bool ResolveFilePaths(string pathHint = null)
        {
            bool foundFiles = false;
            for (int i = 0; i < FileTemplates.Count; ++i)
            {
                foundFiles |= FileTemplates[i].SearchForFile(pathHint);
            }
            return foundFiles;
        }
    }
}
