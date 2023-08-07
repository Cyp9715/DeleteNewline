using System;
using System.Text.RegularExpressions;

namespace DeleteNewline
{
    public static class RegexManager
    {
        public static (bool,string) Replace(string text, string regex, string replace)
        {
            try
            {
                text = Regex.Replace(text, @regex, @replace);
            }
            catch (Exception)
            {
                return (false, "[INVALID REGEX]");
            }

            return (true, text);
        }
    }
}
