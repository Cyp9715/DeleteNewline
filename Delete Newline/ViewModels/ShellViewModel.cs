using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.UI.Xaml.Navigation;

using Delete_Newline.Contracts.Services;
using Delete_Newline.Views;
using Delete_Newline.Services;
using System.Collections.ObjectModel;
using Delete_Newline.Contracts.Structures;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Helpers;
using System.Collections.Specialized;

namespace Delete_Newline.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    [ObservableProperty]
    private object? selectedItem;


    public INavigationService NavigationService { get; }
    public INavigationViewService NavigationViewService { get; }

    public ShellViewModel(INavigationService navigationService,
        INavigationViewService navigationViewService)
    {
        NavigationService = navigationService;
        NavigationViewService = navigationViewService;
    }
}
