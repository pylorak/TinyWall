using System;

using NetFwTypeLib;

namespace PKSoft.WindowsFirewall
{
    /// <summary>
    /// This class enables a read-only access to most of properties of firewall policy under Windows Vista
    /// </summary>
    /// <remarks>
    /// The Windows Firewall/Internet Connection Sharing service must be running to access this class.
    /// This class requires Windows Vista.
    /// </remarks>
    internal sealed class Policy
    {
        /// <summary>
        /// Contains an instance of the "HNetCfg.FwPolicy2" object.
        /// </summary>
        private INetFwPolicy2 fwPolicy2;

        /// <summary>
        /// Contains currently active profile type. All properies will be read for this profile types.
        /// </summary>
        private NET_FW_PROFILE_TYPE2_ fwCurrentProfileTypes;

        /// <summary>
        /// Is <b>true</b> if running on Windows Vista or higher, <b>false</b> in case of any other OS.
        /// </summary>
        private static bool IsVista = (Environment.OSVersion.Version >= new Version("6.0.0")) && (System.Environment.OSVersion.Platform == PlatformID.Win32NT);

        /// <summary>
        /// Creates an instance of <see cref="Policy"/> object.
        /// </summary>
        internal Policy()
        {
            //if running on non Vista system then throw an Exception
            if (!IsVista) 
                throw new InvalidOperationException("This class is designed only for Windows Vista and higher.");

            //Create an instance of "HNetCfg.FwPolicy2"
            Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);
            //read Current Profile Types (only to increase Performace)
            //avoids access on CurrentProfileTypes from each Property
            fwCurrentProfileTypes = (NET_FW_PROFILE_TYPE2_)fwPolicy2.CurrentProfileTypes;
        }

        /// <summary>
        /// Indicates whether the firewall is enabled.
        /// </summary>
        /// <exception cref=""></exception>
        internal bool Enabled
        {
            get {return fwPolicy2.get_FirewallEnabled(fwCurrentProfileTypes);}
            set { ForAllProfiles((p, v) => fwPolicy2.set_FirewallEnabled(p, v), fwCurrentProfileTypes, value); }
        }

        /// <summary>
        /// The read only list of Rules.
        /// </summary>
        internal Rules GetRules(bool ignoreExistingRules)
        {
            return new Rules(fwPolicy2.Rules, ignoreExistingRules);
        }

        /// <summary>
        /// Indicates that inbound traffic should be blocked by the firewall. 
        /// </summary>
        internal bool BlockAllInboundTraffic
        {
            get { return fwPolicy2.get_BlockAllInboundTraffic(fwCurrentProfileTypes); }
            set { ForAllProfiles((p, v) => fwPolicy2.set_BlockAllInboundTraffic(p, v), fwCurrentProfileTypes, value); }
        }
        /// <summary>
        /// Retrieves currently active profiles. 
        /// </summary>
        internal ProfileType CurrentProfileTypes
        {
            get { return (ProfileType)fwPolicy2.CurrentProfileTypes; }
        }

        /// <summary>
        /// Specifies the default action for inbound traffic. 
        /// </summary>
        internal PacketAction DefaultInboundAction
        {
            get { return (PacketAction)fwPolicy2.get_DefaultInboundAction(fwCurrentProfileTypes); }
            set { ForAllProfiles((p, v) => fwPolicy2.set_DefaultInboundAction(p, v), fwCurrentProfileTypes, (NET_FW_ACTION_)value); }
        }

        /// <summary>
        /// Specifies the default action for outbound. 
        /// </summary>
        internal PacketAction DefaultOutboundAction
        {
            get { return (PacketAction)fwPolicy2.get_DefaultOutboundAction(fwCurrentProfileTypes); }
            set { ForAllProfiles((p, v) => fwPolicy2.set_DefaultOutboundAction(p, v), fwCurrentProfileTypes, (NET_FW_ACTION_)value); }
        }

        /// <summary>
        /// A list of interfaces on which firewall settings are excluded. 
        /// </summary>
        internal string[] ExcludedInterfaces
        {
            get {return (string[])fwPolicy2.get_ExcludedInterfaces(fwCurrentProfileTypes);}
        }

        /// <summary>
        /// Indicates whether interactive firewall notifications are disabled. 
        /// </summary>
        internal bool NotificationsDisabled
        {
            get { return fwPolicy2.get_NotificationsDisabled(fwCurrentProfileTypes); }
            set { ForAllProfiles((p, v) => fwPolicy2.set_NotificationsDisabled(p, v), fwCurrentProfileTypes, value); }
        }

        /// <summary>
        /// Access to the Windows Service Hardening (WSH) store. 
        /// </summary>
        internal ServiceRestriction ServiceRestriction
        {
            get { return new ServiceRestriction(fwPolicy2.ServiceRestriction); }
        }

        /// <summary>
        /// Indicates whether unicast incoming responses to outgoing multicast and broadcast traffic are disabled. 
        /// </summary>
        internal bool UnicastResponsesToMulticastBroadcastDisabled
        {
            get { return fwPolicy2.get_UnicastResponsesToMulticastBroadcastDisabled(fwCurrentProfileTypes); }
        }

        internal LocalPolicyState LocalPolicyModifyState
        {
            get
            {
                return (LocalPolicyState)fwPolicy2.LocalPolicyModifyState;
            }
        }

        internal void ResetFirewall()
        {
            fwPolicy2.RestoreLocalFirewallDefaults();
        }

        private static void ForAllProfiles<T>(Action<NET_FW_PROFILE_TYPE2_, T> f, NET_FW_PROFILE_TYPE2_ prof, T param)
        {
            if ((prof & NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN) == NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN)
                f(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN, param);
            if ((prof & NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC) == NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC)
                f(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC, param);
            if ((prof & NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE) == NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE)
                f(NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE, param);
        }
    }
}