using System;

using NetFwTypeLib;

namespace PKSoft.WindowsFirewall
{
    /// <summary>
    /// This enumerated type specifies the type of profile. 
    /// The types of Profiles are combinable.
    /// </summary>
    [Flags]
    internal enum ProfileType
    {
        /// <summary>
        /// Profile type is domain. 
        /// </summary>
        Domain = NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN ,

        /// <summary>
        /// Profile type is private. This profile type is used for home and other private network types. 
        /// </summary>
        Private = NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE,

        /// <summary>
        /// Profile type is public. This profile type is used for public internet access points. 
        /// </summary>
        Public = NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC,

        All = NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL 
    }
}
