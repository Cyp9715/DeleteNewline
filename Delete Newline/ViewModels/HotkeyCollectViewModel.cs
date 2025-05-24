using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;
using Delete_Newline.Services;

namespace Delete_Newline.ViewModels;

public partial class HotkeyCollectViewModel : ObservableRecipient
{
    private readonly HotkeyCollectSaveService _HotkeyCollectManagerService;
    public INavigationService NavigationService { get; }

    [ObservableProperty]
    private ObservableCollection<HotkeyPageStructure> _HotkeyConfigs;

    public HotkeyCollectViewModel(HotkeyCollectSaveService HotkeyCollectManagerService,
        INavigationService navigationService)
    {
        _HotkeyCollectManagerService = HotkeyCollectManagerService;
        NavigationService = navigationService;
        HotkeyConfigs = _HotkeyCollectManagerService.HotkeyConfigs;
    }

    public string GetHotkeyDisplayText(HotkeyPageStructure config)
    {
        return HotkeyHelper.GetDisplayText(config.Hotkey.Modifiers, config.Hotkey.Key);
    }

    [RelayCommand]
    private void AddHotkey()
    {
        _HotkeyCollectManagerService.AddHotkeyConfig();
    }

    [RelayCommand]
    private void RemoveHotkey(HotkeyPageStructure HotkeyConfig)
    {
        if (HotkeyConfig is not null)
        {
            _HotkeyCollectManagerService.RemoveHotkeyConfig(HotkeyConfig);
        }
    }

    [RelayCommand]
    private void NavigateToHotkeyPage(HotkeyPageStructure HotkeyConfig)
    {
        App.GetService<HotkeyViewModel>().CurrentHotkeyConfig = HotkeyConfig;
        NavigationService.NavigateTo(typeof(HotkeyViewModel).FullName!);
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
