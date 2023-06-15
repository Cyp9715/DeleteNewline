using System;
using System.Windows;
using Windows;

namespace DeleteNewline
{
    static class Execute
    {
        static ClipboardManager cbManager = new ClipboardManager();
        static IDataObject? idataObj;

        public static void DeleteNewline_WithNotifier()
        {
            string notifyHeader = String.Empty;
            string notifyContent = String.Empty;
            int limitLen = 100;

            VirtualInput.InputImplement.TypeKeyboard_Copy();

            if (cbManager.GetClipboardData_Text(ref idataObj) == true)
            {
                notifyHeader = "SUCCESS";

                string deletedText = cbManager.DeleteClipboardNewline(ref idataObj, Settings.Default.deleteMultipleSpace).ToString();
                Clipboard.SetDataObject(deletedText);

                if (deletedText.Length > limitLen)
                {
                    notifyContent = deletedText.Substring(0, limitLen) + " ...";
                }
                else
                {
                    notifyContent = deletedText;
                }
            }
            else
            {
                notifyHeader = "ERROR";
                notifyContent = "CLIPBOARD FORM IS NOT TEXT";
            }

            Notification.Send(notifyHeader, notifyContent);
        }

        public static void DeleteNewline_WithoutNotifier()
        {
            VirtualInput.InputImplement.TypeKeyboard_Copy();

            if (cbManager.GetClipboardData_Text(ref idataObj) == true)
            {
                string deletedText = cbManager.DeleteClipboardNewline(ref idataObj, 
                    Settings.Default.deleteMultipleSpace).ToString();
                Clipboard.SetDataObject(deletedText);
            }
        }
    }
}
