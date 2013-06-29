using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PKSoft
{
    [Serializable]
    internal class ServiceState
    {
        internal bool HasPassword = false;
        internal bool Locked = false;
        internal UpdateDescriptor Update = null;
        internal FirewallMode Mode = FirewallMode.Unknown;
    }
}
