using System;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using GlobalHook;
using System.Windows;
using DeleteNewline.ViewModel;


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

        string settingFilePath = "settings.json";

        private void CheckSettingFile()
        {
            if (File.Exists(settingFilePath) == false)
            {
                var settings = DeleteNewline.ViewModel.Settings.GetSettings();
                JsonConvert.SerializeObject(settings, Formatting.Indented);
            }
            else
            {
                string str_settings = File.ReadAllText(settingFilePath);
                var loadedSettings = JsonConvert.DeserializeObject<DeleteNewline.ViewModel.Settings>(str_settings);

                if (loadedSettings != null)
                {
                    var settingsInstance = DeleteNewline.ViewModel.Settings.GetSettings();
                    CopySettings(loadedSettings, settingsInstance);
                }
            }
        }

        private void CopySettings(Settings source, Settings destination)
        {
            destination.topMost = source.topMost;
            destination.notification = source.notification;
            destination.bindKey_1 = source.bindKey_1;
            destination.bindKey_1 = source.bindKey_1;
            destination.regexExpression = source.regexExpression;
            destination.regexReplace = source.regexReplace;
            destination.inputRegex = source.inputRegex;
            destination.outputRegex = source.outputRegex;
        }
    }
}
