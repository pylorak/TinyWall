using System;
using System.Collections.Generic;
using System.Text;

namespace pylorak.TinyWall.Parser
{
    public static class RecursiveParser
    {
        public static string ResolveString(string input)
        {
            var stack = new Stack<ParserVariable>();

            for (int i = 0; i < input.Length; ++i)
            {
                if (ParserRegistryVariable.IsStartTag(input, i))
                {
                    var var = new ParserRegistryVariable(i);
                    stack.Push(var);
                    i += var.GetOpeningTagLength() - 1;
                }
                else if (ParserNoTrailingSlashVariable.IsStartTag(input, i))
                {
                    var var = new ParserNoTrailingSlashVariable(i);
                    stack.Push(var);
                    i += var.GetOpeningTagLength() - 1;
                }
                else if (ParserFolderVariable.IsStartTag(input, i))
                {
                    var var = new ParserFolderVariable(i);
                    stack.Push(var);
                    i += var.GetOpeningTagLength() - 1;
                }
                else if (ParserParentVariable.IsStartTag(input, i))
                {
                    var var = new ParserParentVariable(i);
                    stack.Push(var);
                    i += var.GetOpeningTagLength() - 1;
                }
                else if (input[i] == '}')
                {
                    if (stack.Count < 1)
                        return input;

                    var var = stack.Pop();
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
