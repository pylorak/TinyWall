using System;
using System.Xml.Serialization;

namespace PKSoft.Obsolete
{
    [Serializable]
    public sealed class Profile
    {
        [XmlAttributeAttribute()]
        public string Name;

        public TinyWall.Interface.RuleDef[] Rules;

        public override string ToString()
        {
            return this.Name;
        }
    }
}
