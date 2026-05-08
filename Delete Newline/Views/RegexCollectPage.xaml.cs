using Delete_Newline.Contracts.Structures;
using Delete_Newline.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Delete_Newline.Views;

public sealed partial class RegexCollectPage : Page
{
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
}
