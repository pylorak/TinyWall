using System;
using System.Xml.Serialization;

namespace PKSoft
{
    [Serializable]
    public class Profile
    {
        [XmlAttributeAttribute()]
        public string Name;

        // If AppSpecific is set to true, this profile will only be shown to a user
        // if a specified executable matches the profile's conditions.
        [XmlAttributeAttribute()]
        public bool AppSpecific;

        public RuleDef[] Rules;

        public override string ToString()
        {
            return this.Name;
        }

        public bool ShouldSerializeAppSpecific()
        {
            return AppSpecific;
        }
    }
}
