using System;

namespace pylorak.TinyWall.Parser
{
    public abstract class ParserVariable
    {
        internal readonly int Start;

        protected ParserVariable(int start)
        {
            Start = start;
        }

        internal abstract string Resolve(string str);
        internal abstract string GetOpeningTag();
        internal abstract int GetOpeningTagLength();
    }
}
