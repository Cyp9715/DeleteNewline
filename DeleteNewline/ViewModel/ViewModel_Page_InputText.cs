﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DeleteNewline.ViewModel
{
    class ViewModel_Page_InputText
    {
        public void AddAlertMessage(ref TextBox textBox)
        {
            textBox.AppendText("\r\n");
            textBox.AppendText(" ========================================================\r\n");
            textBox.AppendText(" ================== Only text form can be entered =================\r\n");
            textBox.AppendText(" ========================================================\r\n");
            textBox.ScrollToEnd();
        }
    }
}
