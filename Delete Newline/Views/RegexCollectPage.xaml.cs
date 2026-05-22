using Delete_Newline.Contracts.Structures;
using Delete_Newline.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Delete_Newline.Views;

public sealed partial class RegexCollectPage : Page
{
    private bool _isHidingSearch;

    public RegexCollectViewModel ViewModel
    {
        get;
    }

    public RegexCollectPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<RegexCollectViewModel>();
        DataContext = ViewModel;
    }

    private void RegexCollectPage_Loaded(object sender, RoutedEventArgs e)
    {
        SynchronizeRegexSearchOverlayWithViewModel();
    }

    private void SearchKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.IsSearchVisible)
        {
            HideRegexSearchBox(clearSearchText: false);
            args.Handled = true;
            return;
        }

        ShowRegexSearchBox();
        args.Handled = true;
    }

    private void RegexSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.SearchText = RegexSearchTextBox.Text;
    }

    private void RegexSearchTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isHidingSearch)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(RegexSearchTextBox.Text))
        {
            return;
        }

        HideRegexSearchBox();
    }

    private void ShowRegexSearchBox()
    {
        ViewModel.ShowSearch();
        RegexSearchOverlay.Visibility = Visibility.Visible;
        if (RegexSearchTextBox.Text != ViewModel.SearchText)
        {
            RegexSearchTextBox.Text = ViewModel.SearchText;
        }

        RegexSearchTextBox.Focus(FocusState.Keyboard);
        RegexSearchTextBox.SelectAll();
    }

    private void HideRegexSearchBox(bool clearSearchText = true)
    {
        _isHidingSearch = true;
        try
        {
            ViewModel.HideSearch(clearSearchText);
            if (clearSearchText && RegexSearchTextBox.Text.Length > 0)
            {
                RegexSearchTextBox.Text = string.Empty;
            }

            RegexSearchFocusSink.Focus(FocusState.Programmatic);
            RegexSearchOverlay.Visibility = Visibility.Collapsed;
        }
        finally
        {
            _isHidingSearch = false;
        }
    }

    private void SynchronizeRegexSearchOverlayWithViewModel()
    {
        if (RegexSearchTextBox.Text != ViewModel.SearchText)
        {
            RegexSearchTextBox.Text = ViewModel.SearchText;
        }

        RegexSearchOverlay.Visibility = ViewModel.IsSearchVisible
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void RegexConfigCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: RegexPageStructure regexConfig })
        {
            return;
        }

        if (RegexConfigGridView.SelectedItems.Contains(regexConfig))
        {
            return;
        }

        RegexConfigGridView.SelectedItems.Clear();
        RegexConfigGridView.SelectedItems.Add(regexConfig);
    }

    private void RegexConfigGridView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Escape)
        {
            return;
        }

        if (RegexConfigGridView.SelectedItems.Count == 0)
        {
            return;
        }

        RegexConfigGridView.SelectedItems.Clear();
        e.Handled = true;
    }

    private async void RemoveRegexMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        RegexPageStructure[] selectedRegexConfigs = RegexConfigGridView.SelectedItems
            .OfType<RegexPageStructure>()
            .ToArray();

        if (selectedRegexConfigs.Length > 0)
        {
            await ViewModel.RemoveRegexConfigsAsync(selectedRegexConfigs);
            return;
        }

        if (sender is FrameworkElement { DataContext: RegexPageStructure regexConfig })
        {
            await ViewModel.RemoveRegexConfigsAsync(regexConfig);
        }
    }
}
