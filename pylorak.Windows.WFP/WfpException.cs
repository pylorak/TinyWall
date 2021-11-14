using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace pylorak.Windows.WFP
{
    [Serializable]
    public class WfpException : Exception
    {
        public readonly uint ErrorCode;

        private static string MakeErrorMsg(uint errCode, string wfpFunction)
        { return $"{wfpFunction} returned error code {errCode}."; }

        public WfpException(uint errCode, string wfpFunction)
            : base(MakeErrorMsg(errCode, wfpFunction))
        {
            ErrorCode = errCode;
        }

        protected WfpException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info != null)
            {
                this.ErrorCode = info.GetUInt32("ErrorCode");
            }
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            if (info != null)
            {
                info.AddValue("ErrorCode", this.ErrorCode);
            }
        }
    }

}
