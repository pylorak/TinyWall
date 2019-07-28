using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace WFPdotNet
{
    [Serializable]
    public class WfpException : Exception
    {
        public uint ErrorCode;
        public string Origin;

        public WfpException(uint errCode, string origin)
        {
            ErrorCode = errCode;
            Origin = origin;
        }

        public WfpException(uint errCode, string origin, string message)
            : base(message)
        {
            ErrorCode = errCode;
            Origin = origin;
        }

        protected WfpException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info != null)
            {
                this.ErrorCode = info.GetUInt32("ErrorCode");
                this.Origin = info.GetString("Origin");
            }
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            if (info != null)
            {
                info.AddValue("ErrorCode", this.ErrorCode);
                info.AddValue("Origin", this.Origin);
            }
        }
    }

}
