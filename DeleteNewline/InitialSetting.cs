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
            ApplySettingFile();

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

        /* 
         * Setting 파일의 존재여부를 확인하여
         * 존재하지 않는다면 기본 Setting 파일을 생성하며
         * 존재한다면 셋팅사항을 프로그램에 적용.
         */
        private void ApplySettingFile()
        {
            if (File.Exists(Settings.settingFilePath) == false)
            {
                var defaultSetting = Settings.GetInstance();
                Settings.Save(defaultSetting);
            }
            else
            {
                string str_settings = File.ReadAllText(Settings.settingFilePath);
                var loadedSettings = JsonConvert.DeserializeObject<Settings>(str_settings);

                if (loadedSettings != null)
                {
                    Settings.Apply(loadedSettings);
                    Implement.SetHookKeys(loadedSettings);
                }
            }
        }
    }
}
