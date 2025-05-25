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
        return HotkeyHelper.GetDisplayText(config.Hotkey.Modifiers, config.Hotkey.Key);
    }

    [RelayCommand]
    private void AddRegex()
    {
        _regexCollectSaveService.AddRegexConfig();
    }

    [RelayCommand]
    private void RemoveRegex(RegexPageStructure regexConfig)
    {
        if (regexConfig is not null)
        {
            _regexCollectSaveService.RemoveRegexConfig(regexConfig);
        }
    }

    [RelayCommand]
    private void NavigateToRegexPage(RegexPageStructure regexConfig)
    {
        App.GetService<RegexViewModel>().CurrentRegexConfig = regexConfig;
        NavigationService.NavigateTo(typeof(RegexViewModel).FullName!);
    }

    public static bool isDragEnded = true;

    [RelayCommand]
    private void DragItemsStarting()
    {
        isDragEnded = false;
    }

    [RelayCommand]
    private void DragItemsCompleted()
    {
        isDragEnded = true;
    }
}
