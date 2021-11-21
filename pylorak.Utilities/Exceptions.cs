using System;


namespace pylorak.Utilities
{
    public sealed class UnexpectedResultExceptions : Exception
    {
        public UnexpectedResultExceptions(string methodName)
            : base($"The method {methodName}() returned an expected result.")
        { }
    }
}
