using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;
using Delete_Newline.Services;

namespace Delete_Newline.ViewModels;

public partial class RegexCollectViewModel : ObservableRecipient
{
    private readonly RegexCollectSaveService _regexCollectSaveService;
    public INavigationService NavigationService { get; }

    [ObservableProperty]
    private ObservableCollection<RegexPageStructure> _regexConfigs;

    public RegexCollectViewModel(RegexCollectSaveService regexCollectSaveService,
        INavigationService navigationService)
    {
        _regexCollectSaveService = regexCollectSaveService;
        NavigationService = navigationService;
        RegexConfigs = _regexCollectSaveService.RegexConfigs;
    }

    public string GetHotkeyDisplayText(RegexPageStructure config)
    {
        return HotkeyFormatter.GetDisplayText(config.Hotkey.Modifiers, config.Hotkey.Key);
    }

    [RelayCommand]
    private void AddRegex()
    {
        _regexCollectSaveService.AddRegexConfig();
    }

    [RelayCommand]
    private async Task RemoveRegexAsync(object? parameter)
    {
        IReadOnlyList<RegexPageStructure> targets = GetRemovalTargets(parameter);
        if (targets.Count > 0)
        {
            await _regexCollectSaveService.RemoveRegexConfigsAsync(targets);
        }
    }

    private static IReadOnlyList<RegexPageStructure> GetRemovalTargets(object? parameter)
    {
        if (parameter is RegexPageStructure regexConfig)
        {
            return [regexConfig];
        }

        if (parameter is System.Collections.IEnumerable selectedItems)
        {
            return selectedItems
                .OfType<RegexPageStructure>()
                .Distinct()
                .ToArray();
        }

        return [];
    }

    [RelayCommand]
    private void NavigateToRegexPage(RegexPageStructure regexConfig)
    {
        App.GetService<RegexViewModel>().CurrentRegexConfig = regexConfig;
        NavigationService.NavigateTo(typeof(RegexViewModel).FullName!);
    }
}
