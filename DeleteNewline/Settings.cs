using GlobalHook;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace DeleteNewline
{
    public static class Genesis
    {
        private static Mutex singleProcess = new Mutex(true, "260bf0b2-4dae-4146-9c0b-f794ad868790");

        public static void Initialize()
        {
            PreventMultipleRun();
            ApplySettingFile();
            Hook.Install();
        }

        private static void PreventMultipleRun()
        {
            if (!singleProcess.WaitOne(TimeSpan.Zero, true))
            {
                string alertMsg = "Already run " + GlobalVariables.programName + "!";
                MessageBox.Show(alertMsg);
                Application.Current.Shutdown();
            }
        }

        /* 
         * Setting 파일의 존재여부를 확인하여
         * 존재하지 않는다면 기본 Setting 파일을 생성하며
         * 존재한다면 셋팅사항을 프로그램에 적용.
         */
        private static void ApplySettingFile()
        {
            if (File.Exists(Settings.settingFilePath) == false)
            {
                // 기본 Setting 파일 저장.
                Settings.Save();
            }
            else
            {
                try
                {
                    Settings? loadedSettings = JsonConvert.DeserializeObject<Settings>(
                        File.ReadAllText(Settings.settingFilePath));

                    if (loadedSettings != null)
                    {
                        Settings.CopySetting(loadedSettings);
                    }
                }
                catch(Newtonsoft.Json.JsonSerializationException)
                {
                    MessageBox.Show("The saved JSON file is corrupted. Please delete the file or modify it, then run DeleteNewline again",
                        "Delete Newline");

                    Application.Current.Shutdown();
                }
            }

            var setting = Settings.GetInstance();
            Hook.SetKeys(setting.bindKey_1, setting.bindKey_2);
        }
    }


    class Settings
    {
        private static Settings? instance;

        /* 
         * Setting 파일의 존재여부를 확인하여
         * 존재하지 않는다면 기본 Setting 파일을 생성하며
         * 존재한다면 셋팅사항을 프로그램에 적용.
         */
        private Settings() { }

        public static Settings GetInstance()
        {
            instance ??= new Settings();

            return instance;
        }

        // Loaded Setting 호출시 깊은복사가 필요.
        public static void CopySetting(Settings source)
        {
            if (instance == null)
                GetInstance();

            instance.mainWindowSize_width = source.mainWindowSize_width;
            instance.mainWindowSize_height = source.mainWindowSize_height;
            instance.topMost = source.topMost;
            instance.notification = source.notification;
            instance.bindKey_1 = source.bindKey_1;
            instance.bindKey_2 = source.bindKey_2;
            instance.regexExpression = source.regexExpression;
            instance.regexReplace = source.regexReplace;
            instance.regexExpression_additional = source.regexExpression_additional;
            instance.regexReplace_additional = source.regexReplace_additional;
            instance.inputRegex = source.inputRegex;
            instance.outputRegex = source.outputRegex;
        }

        public const string settingFilePath = "setting.json";

        public static void Save()
        {
            if(instance == null)
                GetInstance();

            string serialized = JsonConvert.SerializeObject(instance, Formatting.Indented);
            File.WriteAllText(settingFilePath, serialized);
        }

        public double mainWindowSize_width { get; set; }
        public double mainWindowSize_height { get; set; }

        public bool topMost { get; set; } = false;
        public bool notification { get; set; } = true;

        public Key bindKey_1 { get; set; } = Key.LeftAlt;
        public Key bindKey_2 { get; set; } = Key.F1;

        public string regexExpression { get; set; } = String.Empty;
        public string regexReplace { get; set; } = String.Empty;

        public List<string> regexExpression_additional = new List<string>();
        public List<string> regexReplace_additional = new List<string>();

        public string inputRegex { get; set; } = String.Empty;
        public string outputRegex { get; set; } = String.Empty;
    }
}
