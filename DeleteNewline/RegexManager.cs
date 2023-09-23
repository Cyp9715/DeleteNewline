using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
                    text = Regex.Replace(text, @regexs[i], @replaces[i]);
                }
            }
            catch (Exception)
            {
                return (false, "[INVALID REGEX]");
            }

            return (true, text);
        }
    }
}
