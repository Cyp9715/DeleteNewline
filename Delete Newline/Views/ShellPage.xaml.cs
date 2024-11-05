using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Delete_Newline.Helpers;
using Delete_Newline.ViewModels;
using Microsoft.UI.Xaml.Input;

namespace Delete_Newline.Views;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel
    {
        get;
    }


    public ShellPage(ShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ViewModel.NavigationService.Frame = NavigationFrame;
        ViewModel.NavigationViewService.Initialize(NavigationViewControl);

        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
        AppTitleBarText.Text = "AppDisplayName".GetLocalized();
    }

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);
    }

    private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        AppTitleBar.Margin = new Thickness()
        {
            Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
            Top = AppTitleBar.Margin.Top,
            Right = AppTitleBar.Margin.Right,
            Bottom = AppTitleBar.Margin.Bottom
        };
    }

    private void NavigationViewItem_Tapped(object sender, TappedRoutedEventArgs e)
    {
        // KeybindView로의 직접 내비게이션 코드 추가
        NavigationFrame.Navigate(typeof(KeybindPage));
    }
}
