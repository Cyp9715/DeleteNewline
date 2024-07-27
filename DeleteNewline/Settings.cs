using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows.Input;

namespace DeleteNewline
{
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

        public static void Apply(Settings source)
        {
            if (instance == null)
            {
                GetInstance();
            }

            instance.mainWindowSize_width = source.mainWindowSize_width;
            instance.mainWindowSize_height = source.mainWindowSize_height;
            instance.topMost = source.topMost;
            instance.notification = source.notification;
            instance.bindKey_1 = source.bindKey_1;
            instance.bindKey_2 = source.bindKey_2;
            instance.regexExpression = source.regexExpression;
            instance.regexReplace = source.regexReplace;
            instance.inputRegex = source.inputRegex;
            instance.outputRegex = source.outputRegex;
        }

        public const string settingFilePath = "setting.json";

        public static void Save(Settings source)
        {
            string serialized = JsonConvert.SerializeObject(source, Formatting.Indented);
            File.WriteAllText(settingFilePath, serialized);
        }

        public double mainWindowSize_width { get; set; }
        public double mainWindowSize_height { get; set; }

        public bool topMost { get; set; } = false;
        public bool notification { get; set; } = true;

        public Key bindKey_1 { get; set; } = Key.LeftAlt;
        public Key bindKey_2 { get; set; } = Key.F1;

        public string regexInput { get; set; } = String.Empty;

        public string regexExpression { get; set; } = String.Empty;
        public string regexReplace { get; set; } = String.Empty;

        public string inputRegex { get; set; } = String.Empty;
        public string outputRegex { get; set; } = String.Empty;
    }
}
