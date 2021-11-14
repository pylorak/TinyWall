using System;

namespace pylorak.Windows.WFP
{
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
    }

}
