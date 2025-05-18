using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Delete_Newline.Helpers;
using Delete_Newline.ViewModels;
using Delete_Newline.Services;

namespace Delete_Newline.Views;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel { get; }
    private InAppNotificationService _inAppNotificationService;

    public ShellPage()
    {
        ViewModel = App.GetService<ShellViewModel>();
        InitializeComponent();

        ViewModel.NavigationService.Frame = NavigationFrame;
        ViewModel.NavigationViewService.Initialize(NavigationViewControl);

        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
        AppTitleBarText.Text = LocalizationHelper.GetLocalizedString("AppDisplayName");

        // Initialize InAppNotificationService
        _inAppNotificationService = App.GetService<InAppNotificationService>();
        _inAppNotificationService.Initialize(GlobalInfoBar);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set HotkeyCollectPage as the default selected item
        ViewModel.SelectedItem = ViewModel.NavigationViewService.GetSelectedItem(typeof(HotkeyCollectPage));
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

    // These methods are now effectively handled by InAppNotificationService directly
    // public void ShowInAppNotification(string titleKey, string messageKey, InfoBarSeverity severity = InfoBarSeverity.Informational, params object[]? messageArgs)
    // {
    // GlobalInfoBar.Title = LocalizationHelper.GetLocalizedString(titleKey);
    // GlobalInfoBar.Message = LocalizationHelper.GetLocalizedString(messageKey, messageArgs ?? System.Array.Empty<object>());
    // GlobalInfoBar.Severity = severity;
    // GlobalInfoBar.IsOpen = true;
    // }

    // public void HideInAppNotification()
    // {
    // GlobalInfoBar.IsOpen = false;
    // }
}
