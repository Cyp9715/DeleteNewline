using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static VirtualInput.NativeMethods;

namespace DeleteNewline
{
    public static class RegexManager
    {
        public static (bool,string) Replace(string text, List<string> regexs, List<string> replaces)
        {
            try
            {
                for(int i = 0; i < regexs.Count; ++i)
                {
                    var regex = Regex.Unescape(regexs[i]);
                    var replace = Regex.Unescape(replaces[i]);

                    text = Regex.Replace(text, regex, replace);
                }
            }
            catch (Exception)
            {
                return (false, "[INVALID REGEX]"); 
            }

            return (true, text);
        }

        private static string RemoveMultiBackSlash(in string input)
        {
            return Regex.Replace(input, @"\\{2,}", m => new string('\\', (m.Length + 1) / 2));
        }

    }
}
