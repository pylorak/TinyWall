using System;
using System.Xml.Serialization;

namespace PKSoft.Obsolete
{
    [Serializable]  // Needed for ICloneable implementation 
    [Obsolete]
    public sealed class Application
    {

        internal PKSoft.DatabaseClasses.Application ToNewFormat()
        {
            PKSoft.DatabaseClasses.Application ret = new DatabaseClasses.Application();
            ret.Name = this.Name;

            if (this.Special)
                ret.SetFlag("TWUI:Special");
            if (this.Recommended)
                ret.SetFlag("TWUI:Recommended");

            foreach (AppExceptionAssoc appex in this.FileTemplates)
                ret.Components.Add(appex.ToNewFormat());

            return ret;
        }

        // Application name
        [XmlAttributeAttribute()]
        public string Name;
        public string LocalizedName
        {
            get
            {
                try
                {
                    string ret = PKSoft.Resources.Exceptions.ResourceManager.GetString(Name);
                    return string.IsNullOrEmpty(ret) ? Name : ret;
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
