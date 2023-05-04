using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DeleteNewline
{
    class ClipboardManager
    {
        public StringBuilder DeleteClipboardNewline(ref IDataObject idata, bool splitPeriod = false)
        {
            StringBuilder stringBuilder;

            string clipboardText = (string)idata.GetData(DataFormats.Text);
            stringBuilder = new StringBuilder(clipboardText);

            if (splitPeriod == false)
            {
                stringBuilder.Replace("\r\n", " ");
            }
            else
            {
                stringBuilder.Replace("\r\n", " ");
                stringBuilder.Replace(". ", ".\r\n");
            }

            return stringBuilder;
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
