using CommunityToolkit.Mvvm.ComponentModel;

using Delete_Newline.Contracts.Services;

namespace Delete_Newline.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    [ObservableProperty]
    public object? _selectedItem;

    public INavigationService NavigationService { get; }
    public INavigationViewService NavigationViewService { get; }

    public ShellViewModel(INavigationService navigationService,
        INavigationViewService navigationViewService)
    {
        NavigationService = navigationService;
        NavigationViewService = navigationViewService;
    }
}