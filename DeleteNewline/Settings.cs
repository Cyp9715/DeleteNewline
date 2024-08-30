using GlobalHook;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Windows.Storage;

namespace DeleteNewline
{
    public static class Genesis
    {
        private static Mutex singleProcess = new Mutex(true, "260bf0b2-4dae-4146-9c0b-f794ad868790");

        public static void Initialize()
        {
            PreventMultipleRun();
            SetSettingFile();
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
        private static void SetSettingFile()
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
                    string jsonContent = File.ReadAllText(Settings.settingFilePath);

                    Settings? loadedSettings = JsonConvert.DeserializeObject<Settings>(jsonContent, new JsonSerializerSettings
                    {
                        // ObjectCreationHandling.Replace를 사용하여 기존 객체를 새로운 객체로 대체
                        ObjectCreationHandling = ObjectCreationHandling.Replace,
                    });

                    if (loadedSettings != null)
                    {
                        loadedSettings.AdditionalRegexes ??= new List<AdditionalRegex>();
                        Settings.DeepCopy(loadedSettings);
                    }
                    else
                    {
                        throw new JsonSerializationException("Failed to deserialize settings.");
                    }
                }
                catch (JsonSerializationException ex)
                {
                    MessageBox.Show($"The saved JSON file is corrupted: {ex.Message}\nPlease delete the file or modify it, then run DeleteNewline again",
                        "Delete Newline");
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while loading settings: {ex.Message}\nPlease check the settings file and try again.",
                        "Delete Newline");
                    Application.Current.Shutdown();
                }
            }

            var setting = Settings.GetInstance();
            Hook.SetKeys(setting.bindKey_1, setting.bindKey_2);
        }
    }

    public class AdditionalRegex
    {
        public string RegexExpression { get; set; } = string.Empty;
        public string RegexReplace { get; set; } = string.Empty;
    }

    class Settings
    {
        private static Settings? instance;
        public static readonly string settingFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "setting.json");
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
        public static void DeepCopy(Settings source)
        {
            instance ??= new Settings();

            instance.mainWindowSize_width = source.mainWindowSize_width;
            instance.mainWindowSize_height = source.mainWindowSize_height;
            instance.topMost = source.topMost;
            instance.notification = source.notification;
            instance.bindKey_1 = source.bindKey_1;
            instance.bindKey_2 = source.bindKey_2;
            instance.regexExpression = source.regexExpression;
            instance.regexReplace = source.regexReplace;

            // List DeepCopy
            instance.AdditionalRegexes = source.AdditionalRegexes?.Select(ar => new AdditionalRegex
            {
                RegexExpression = ar.RegexExpression,
                RegexReplace = ar.RegexReplace
            }).ToList() ?? new List<AdditionalRegex>();

            instance.inputTestRegex = source.inputTestRegex;
        }

        // fixed file path.
        public static void Save()
        {
            instance ??= new Settings();
            File.WriteAllText(settingFilePath, JsonConvert.SerializeObject(instance, Formatting.Indented));
        }


        public double mainWindowSize_width { get; set; }
        public double mainWindowSize_height { get; set; }

        public bool topMost { get; set; } = false;
        public bool notification { get; set; } = true;

        public Key bindKey_1 { get; set; } = Key.LeftAlt;
        public Key bindKey_2 { get; set; } = Key.F1;

        public string regexExpression { get; set; } = String.Empty;
        public string regexReplace { get; set; } = String.Empty;

        public List<AdditionalRegex>? AdditionalRegexes { get; set; }

        public string inputTestRegex { get; set; } = String.Empty;
    }
}
