using Delete_Newline.Contracts.Structures;
using Windows.System;

namespace Delete_Newline.Services;

public static class DefaultRegexProfile
{
    public const string CollectionSettingsKey = "RegexCollection";

    public static async Task EnsureCreatedAsync(SettingsFileService settings, string name, string description)
    {
        // A missing collection means first use. An explicitly empty collection
        // means the user deleted their profiles and must stay empty on restart.
        if (settings.GetAllSettingsSnapshot().ContainsKey(CollectionSettingsKey)) return;

        var profile = new RegexPageStructure
        {
            HotkeyName = name,
            HotkeyComment = description,
            Hotkey = new HotkeyStructure { Modifiers = VirtualKeyModifiers.Menu, Key = VirtualKey.F1 }
        };
        profile.RegexChain.ChainItems.Add(new ChainItem { RegexExpression = @"[\r\n]+", Replace = " " });
        await settings.SaveSettingAsync(CollectionSettingsKey, new[] { profile });
    }
}
