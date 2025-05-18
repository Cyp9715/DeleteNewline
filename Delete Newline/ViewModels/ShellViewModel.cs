using CommunityToolkit.Mvvm.ComponentModel;
using Delete_Newline.Contracts.Services;
using Delete_Newline.Views; // Required for HotkeyPage and HotkeyCollectPage types
using Microsoft.UI.Xaml.Navigation; // For NavigationEventArgs

namespace Delete_Newline.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    private readonly IPageService _pageService; // Add IPageService

    [ObservableProperty]
    private object? _selectedItem;

    public INavigationService NavigationService { get; }
    public INavigationViewService NavigationViewService { get; }

    public ShellViewModel(INavigationService navigationService,
        INavigationViewService navigationViewService,
        IPageService pageService) // Add IPageService to constructor
    {
        NavigationService = navigationService;
        NavigationViewService = navigationViewService;
        _pageService = pageService; // Store IPageService

        NavigationService.Navigated += OnNavigated;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        Type typeForSelectedItemCalculation;

        if (e.SourcePageType == typeof(HotkeyPage))
        {
            typeForSelectedItemCalculation = typeof(HotkeyCollectPage);
        }
        else
        {
            typeForSelectedItemCalculation = e.SourcePageType;
        }

        var currentSelectedItem = NavigationViewService.GetSelectedItem(typeForSelectedItemCalculation);
        if (currentSelectedItem != null)
        {
            SelectedItem = currentSelectedItem;
        }
    }
}