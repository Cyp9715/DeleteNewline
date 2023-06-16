using System;
using System.Text.RegularExpressions;

namespace DeleteNewline
{
    public class RegexManager
    {
        public string Replace(string text, string regex, string replace)
        {
            try
            {
                text = Regex.Replace(text, @regex, replace);
            }
            catch (Exception)
            {
                return "Invalid Regex";
            }

            return text;
        }
    }
}
