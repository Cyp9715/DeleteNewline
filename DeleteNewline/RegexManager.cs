using System;
using System.DirectoryServices.ActiveDirectory;
using System.Text.RegularExpressions;

namespace DeleteNewline
{
    public class RegexManager
    {
        public (bool,string) Replace(string text, string regex, string replace)
        {
            try
            {
                text = Regex.Replace(text, @regex, replace);
            }
            catch (Exception)
            {
                return (false, "INVALID REGEX");
            }

            return (true, text);
        }
    }
}
