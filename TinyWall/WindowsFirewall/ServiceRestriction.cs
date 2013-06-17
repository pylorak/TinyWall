
using NetFwTypeLib;

namespace PKSoft.WindowsFirewall
{
    internal sealed class ServiceRestriction
    {
        INetFwServiceRestriction fwServiceRestriction;

        internal ServiceRestriction(INetFwServiceRestriction serviceRestriction)
        {
            fwServiceRestriction =  serviceRestriction;
        }

        internal Rules GetRules(bool ignoreExistingRules)
        {
            return new Rules(fwServiceRestriction.Rules, ignoreExistingRules);
        }

        internal bool ServiceRestricted(string ServiceName, string AppName)
        {
            return fwServiceRestriction.ServiceRestricted(ServiceName, AppName);
        }
    }
}
