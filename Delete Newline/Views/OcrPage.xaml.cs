using Delete_Newline.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;

namespace Delete_Newline.Views;

public sealed partial class OCRPage : Page
{
    public OCRViewModel ViewModel { get; }
    

    public OCRPage()
    {
        ViewModel = App.GetService<OCRViewModel>();
        InitializeComponent();
        DataContext = ViewModel;
    }

    private async void DeleteManagedLanguageButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedManageLanguage is not { IsInstalled: true, IsCurrent: false } selectedLanguage)
        {
            return;
        }

        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = GetResourceString("OCRPage_DeleteLanguageDialog.Title", "Delete OCR Language"),
            Content = string.Format(
                GetResourceString("OCRPage_DeleteLanguageDialog.Message", "Remove {0} from Windows OCR languages? You can download it again later."),
                selectedLanguage.DisplayName),
            PrimaryButtonText = GetResourceString("OCRPage_DeleteLanguageDialog.PrimaryButtonText", "Delete"),
            CloseButtonText = GetResourceString("OCRPage_DeleteLanguageDialog.CloseButtonText", "Cancel"),
            DefaultButton = ContentDialogButton.Close
        };

        ContentDialogResult result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteManagedOcrLanguageAsync(selectedLanguage.LanguageTag);
        }
    }

    private static string GetResourceString(string resourceKey, string fallback)
    {
        try
        {
            ResourceManager resourceManager = new();
            ResourceContext resourceContext = resourceManager.CreateResourceContext();
            string languageOverride = Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride;
            if (!string.IsNullOrWhiteSpace(languageOverride))
            {
                resourceContext.QualifierValues["Language"] = languageOverride;
            }

            return resourceManager.MainResourceMap
                .GetSubtree("Resources")
                .GetValue(resourceKey, resourceContext)
                .ValueAsString ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }
}
