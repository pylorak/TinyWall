using System;

using NetFwTypeLib;

namespace PKSoft.WindowsFirewall
{
    /// <summary>
    /// This enumerated type specifies which direction of traffic a rule applies to
    /// </summary>
    public enum RuleDirection
    {
        Invalid,
        In = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
        Out = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT,

        [Obsolete("This value is used for boundary checking only and is not valid for application programming.")]
        Max = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_MAX,

        // Virtual values (>1024)
        InOut = 1024
    }
}
