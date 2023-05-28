﻿using System.Text.RegularExpressions;
using System.Windows;
using appdata = DeleteNewline.Settings;

namespace DeleteNewline
{
    class ClipboardManager
    {
        public string DeleteClipboardNewline(ref IDataObject idata)
        {
            string clipboardText = (string)idata.GetData(DataFormats.Text);
            
            clipboardText = Regex.Replace(clipboardText, @"\r\n", "");

            if (appdata.Default.deleteMultipleSpace == true)
            {
                clipboardText = Regex.Replace(clipboardText, @"\s+", " ");
            }

            return clipboardText;
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
