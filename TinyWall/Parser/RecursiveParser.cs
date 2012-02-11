using System;
using System.Collections.Generic;
using System.Text;

namespace PKSoft.Parser
{
    internal static class RecursiveParser
    {
        internal static string ResolveRegistry(string input)
        {
            Stack<ParserVariable> stack = new Stack<ParserVariable>();
            ParserVariable var = null;

            for (int i = 0; i < input.Length; ++i)
            {
                if (ParserRegistryVariable.IsStartTag(input, i))
                {
                    var = new ParserRegistryVariable();
                    var.Start = i;
                    stack.Push(var);
                    i += var.GetOpeningTagLength() - 1;
                }
                else if (input[i] == '}')
                {
                    var = stack.Pop();
                    int tagValueStart = var.Start + var.GetOpeningTagLength();
                    int tagValueEnd = i;
                    string varStr = input.Substring(tagValueStart, tagValueEnd - tagValueStart);
                    string newInput = input.Replace(var.GetOpeningTag() + varStr + "}", var.Resolve(varStr));

                    // Adjust i to our new string length
                    i += newInput.Length - input.Length;
                    input = newInput;
                }
            }

            return input;
        }

    }
}
