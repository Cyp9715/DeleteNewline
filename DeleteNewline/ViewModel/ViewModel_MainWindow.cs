using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeleteNewline.ViewModel
{
    class ViewModel_MainWindow
    {
        InitialSetting initialSettings;
        MainWindow mainWindow;

        public ViewModel_MainWindow(MainWindow mainWindow_)
        {
            mainWindow = mainWindow_;
            initialSettings = new InitialSetting(mainWindow_);
        }

    }
}
