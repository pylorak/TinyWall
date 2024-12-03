using System;


namespace pylorak.Utilities
{
    public class UnexpectedResultExceptions : Exception
    {
        public UnexpectedResultExceptions(string methodName)
            : base($"The method {methodName}() returned an unexpected result.")
        { }
    }

    public class NullResultExceptions : UnexpectedResultExceptions
    {
        public NullResultExceptions(string methodName)
            : base($"The method {methodName}() returned a null reference.")
        { }
    }
}
