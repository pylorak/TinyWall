using System;
using System.Collections.Generic;
using System.Text;

namespace TinyWall.Interface.Parser
{
    public static class RecursiveParser
    {
        public static string ResolveString(string input)
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
                if (ParserFolderVariable.IsStartTag(input, i))
                {
                    var = new ParserFolderVariable();
                    var.Start = i;
                    stack.Push(var);
                    i += var.GetOpeningTagLength() - 1;
                }
                if (ParserParentVariable.IsStartTag(input, i))
                {
                    var = new ParserParentVariable();
                    var.Start = i;
                    stack.Push(var);
                    i += var.GetOpeningTagLength() - 1;
                }
                else if (input[i] == '}')
                {
                    if (stack.Count < 1)
                        return input;

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
