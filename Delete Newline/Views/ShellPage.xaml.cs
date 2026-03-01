using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Delete_Newline.Helpers;
using Delete_Newline.ViewModels;
using Delete_Newline.Services;

namespace Delete_Newline.Views;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel { get; }

    public ShellPage()
    {
        ViewModel = App.GetService<ShellViewModel>();
        InitializeComponent();

        ViewModel.NavigationService.Frame = NavigationFrame;
        ViewModel.NavigationService.InitializeNavigationView(NavigationViewControl);

        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
        RefreshLocalizedTexts();

        // Initialize InAppNotificationService
        var inAppNotificationService = App.GetService<InAppNotificationService>();
        inAppNotificationService.Initialize(GlobalInfoBar);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set default selected item only when no page has been navigated yet.
        if (ViewModel.NavigationService.Frame?.Content == null)
        {
            ViewModel.SelectedItem = ViewModel.NavigationService.GetSelectedItem(typeof(RegexCollectPage));
        }
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

    public void RefreshLocalizedTexts()
    {
        AppTitleBarText.Text = LocalizationHelper.GetLocalizedString("AppDisplayName");
        SetNavigationItemContent(HotkeysPageItem, "Shell_RegexExpressions/Content");
        SetNavigationItemContent(OCRPageItem, "Shell_OCR/Content");
        SetNavigationItemContent(SettingsPageItem, "Shell_Settings/Content");
    }

    private static void SetNavigationItemContent(NavigationViewItem item, string resourceKey)
    {
        string localized = LocalizationHelper.GetLocalizedString(resourceKey);

        // Guard against exposing resource keys in the UI when a lookup fails.
        if (string.IsNullOrWhiteSpace(localized) == false && localized != resourceKey)
        {
            item.Content = localized;
        }
    }
}
