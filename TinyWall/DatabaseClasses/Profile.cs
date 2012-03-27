using System;
using System.Xml.Serialization;

namespace PKSoft
{
    [Serializable]
    public class Profile
    {
        [XmlAttributeAttribute()]
        public string Name;

        public RuleDef[] Rules;

        public override string ToString()
        {
            return this.Name;
        }
    }
}
