using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Services;

namespace Delete_Newline.ViewModels;

public partial class HotkeyCollectViewModel : ObservableRecipient
{
    private readonly HotkeyCollectManagerService _HotkeyCollectManagerService;
    public INavigationService NavigationService { get; }

    [ObservableProperty]
    public ObservableCollection<HotkeyPageConfiguration> _HotkeyConfigs;

    public HotkeyCollectViewModel(HotkeyCollectManagerService HotkeyCollectManagerService,
        INavigationService navigationService)
    {
        _HotkeyCollectManagerService = HotkeyCollectManagerService;
        NavigationService = navigationService;
        HotkeyConfigs = _HotkeyCollectManagerService.HotkeyConfigs;
    }

    [RelayCommand]
    private void AddHotkey()
    {
        _HotkeyCollectManagerService.AddHotkeyConfig();
    }

    [RelayCommand]
    private void RemoveHotkey(HotkeyPageConfiguration HotkeyConfig)
    {
        if (HotkeyConfig is not null)
        {
            _HotkeyCollectManagerService.RemoveHotkeyConfig(HotkeyConfig);
        }
    }

    [RelayCommand]
    private void NavigateToHotkeyPage(HotkeyPageConfiguration HotkeyConfig)
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
