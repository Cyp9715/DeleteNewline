﻿using System;
using System.Threading;
using GlobalHook;
using System.Windows;


namespace DeleteNewline
{
    // Application InitialSetting.
    class InitialSetting
    {
        System.Threading.Mutex singleton = new Mutex(true, "260bf0b2-4dae-4146-9c0b-f794ad868790");

        public InitialSetting()
        {
            PreventMultipleRun();

            // Init GlobalHook
            Implement.InstallGlobalHook();
        }

        private void PreventMultipleRun()
        {
            if (!singleton.WaitOne(TimeSpan.Zero, true))
            {
                string alertMsg = "Already run " + Global.programName + "!";
                MessageBox.Show(alertMsg);
                Application.Current.Shutdown();
            }
        }

    }
}
