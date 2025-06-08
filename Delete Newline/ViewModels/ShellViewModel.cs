using CommunityToolkit.Mvvm.ComponentModel;
using Delete_Newline.Contracts.Services;
using Delete_Newline.Views;
using Microsoft.UI.Xaml.Navigation;

namespace Delete_Newline.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    private readonly IPageService _pageService; // Add IPageService

    [ObservableProperty]
    private object? _selectedItem;

    public INavigationService NavigationService { get; }

    public ShellViewModel(INavigationService navigationService, IPageService pageService)
    {
        NavigationService = navigationService;
        _pageService = pageService;

        NavigationService.Navigated += OnNavigated;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        Type typeForSelectedItemCalculation;

        if (e.SourcePageType == typeof(RegexPage))
        {
            typeForSelectedItemCalculation = typeof(RegexCollectPage);
        }
        else
        {
            typeForSelectedItemCalculation = e.SourcePageType;
        }

        var currentSelectedItem = NavigationService.GetSelectedItem(typeForSelectedItemCalculation);
        if (currentSelectedItem != null)
        {
            SelectedItem = currentSelectedItem;
        }
    }
}