using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Services;

namespace Delete_Newline.ViewModels;

public partial class HotKeyCollectViewModel : ObservableRecipient
{
    private readonly HotKeyCollectSaveService _HotKeyCollectManagerService;
    public INavigationService NavigationService { get; }

    [ObservableProperty]
    public ObservableCollection<HotKeyPageStructure> _HotKeyConfigs;

    public HotKeyCollectViewModel(HotKeyCollectSaveService HotKeyCollectManagerService,
        INavigationService navigationService)
    {
        _HotKeyCollectManagerService = HotKeyCollectManagerService;
        NavigationService = navigationService;
        HotKeyConfigs = _HotKeyCollectManagerService.HotKeyConfigs;
    }

    [RelayCommand]
    private void AddHotKey()
    {
        _HotKeyCollectManagerService.AddHotKeyConfig();
    }

    [RelayCommand]
    private void RemoveHotKey(HotKeyPageStructure HotKeyConfig)
    {
        if (HotKeyConfig is not null)
        {
            _HotKeyCollectManagerService.RemoveHotKeyConfig(HotKeyConfig);
        }
    }

    [RelayCommand]
    private void NavigateToHotKeyPage(HotKeyPageStructure HotKeyConfig)
    {
        App.GetService<HotKeyViewModel>().CurrentHotKeyConfig = HotKeyConfig;
        NavigationService.NavigateTo(typeof(HotKeyViewModel).FullName!);
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
