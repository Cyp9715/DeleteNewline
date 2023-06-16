using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace DeleteNewline
{
    class ClipboardManager
    {
        public string DeleteClipboardNewline(ref IDataObject idata, string regex, string replace)
        {
            string clipboardText = (string)idata.GetData(DataFormats.Text);

            try
            {
                clipboardText = Regex.Replace(clipboardText, @regex, replace);
            }
            catch(Exception)
            {
                return "Invalid Regex";
            }

            return clipboardText;
        }

        public string DeleteClipboardNewline(string text, string regex, string replace)
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

        public bool GetClipboardData_Text(ref IDataObject idata)
        {
            idata = Clipboard.GetDataObject();

            if (idata.GetDataPresent(DataFormats.Text) == false)
            {
                return false;
            }

            return true;
        }
    }
}
