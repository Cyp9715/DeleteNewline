using Delete_Newline.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Globalization;
using Windows.Media.Ocr;

namespace Delete_Newline.Views;

public sealed partial class OCRPage : Page
{
    public OCRViewModel ViewModel { get; }
    
    // Track the single OCR capture window
    private OcrCaptureWindow? activeOcrWindow;

    public OCRPage()
    {
        ViewModel = App.GetService<OCRViewModel>();
        InitializeComponent();
        DataContext = ViewModel;
        
        // Initialize ViewModel to sync with OCRService
        ViewModel.Initialize();
        
        LoadOcrLanguages();
        
        // Set dummy focus button for hotkey registration
        ViewModel.SetDummyFocusButton(DummyFocusButton);
    }

    private void LoadOcrLanguages()
    {
        System.Diagnostics.Debug.WriteLine("=== Loading OCR Languages ===");
        
        var availableLanguages = OcrEngine.AvailableRecognizerLanguages;
        
        System.Diagnostics.Debug.WriteLine($"Total available OCR languages: {availableLanguages.Count}");
        
        foreach (var language in availableLanguages)
        {
            LanguageComboBox.Items.Add(language);
            System.Diagnostics.Debug.WriteLine($"  - {language.DisplayName} ({language.LanguageTag})");
        }
        
        // Sync ComboBox selection with ViewModel's loaded language
        if (ViewModel.SelectedLanguage != null)
        {
            // Find the matching language in ComboBox items by LanguageTag
            var matchingLanguage = availableLanguages.FirstOrDefault(l => l.LanguageTag == ViewModel.SelectedLanguage.LanguageTag);
            if (matchingLanguage != null)
            {
                LanguageComboBox.SelectedItem = matchingLanguage;
                // Update ViewModel to use the same instance as ComboBox for consistency
                ViewModel.SelectedLanguage = matchingLanguage;
                System.Diagnostics.Debug.WriteLine($"Synced ComboBox with saved language: {matchingLanguage.DisplayName} ({matchingLanguage.LanguageTag})");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Saved language '{ViewModel.SelectedLanguage.LanguageTag}' not found in available languages");
                // Fall back to default
                SetDefaultLanguage(availableLanguages);
            }
        }
        else
        {
            // Fallback: ViewModel doesn't have a language set, set default
            SetDefaultLanguage(availableLanguages);
        }
    }

    private void SetDefaultLanguage(IReadOnlyList<Language> availableLanguages)
    {
        var englishLang = availableLanguages.FirstOrDefault(l => l.LanguageTag.StartsWith("en"));
        var defaultLanguage = englishLang ?? availableLanguages.FirstOrDefault();
        
        if (defaultLanguage != null)
        {
            LanguageComboBox.SelectedItem = defaultLanguage;
            ViewModel.SelectedLanguage = defaultLanguage;
            System.Diagnostics.Debug.WriteLine($"Set default language: {defaultLanguage.DisplayName} ({defaultLanguage.LanguageTag})");
        }
    }

    // Hotkey TextBox focus events
    private void TextBox_OcrHotkey_GotFocus(object sender, RoutedEventArgs e)
    {
        ViewModel.StartHotkeyRegistration();
    }

    private void TextBox_OcrHotkey_LostFocus(object sender, RoutedEventArgs e)
    {
        ViewModel.EndHotkeyRegistration();
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is Language newSelectedLanguage)
        {
            ViewModel.SelectedLanguage = newSelectedLanguage;
            System.Diagnostics.Debug.WriteLine($"=== Language Changed ===");
            System.Diagnostics.Debug.WriteLine($"New language: {newSelectedLanguage.DisplayName} ({newSelectedLanguage.LanguageTag})");
            
            // Test OCR engine availability for selected language
            var testEngine = OcrEngine.TryCreateFromLanguage(newSelectedLanguage);
            if (testEngine != null)
            {
                System.Diagnostics.Debug.WriteLine($"✅ OCR engine available for {newSelectedLanguage.DisplayName}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ OCR engine NOT available for {newSelectedLanguage.DisplayName}");
            }
            
            // Language is automatically saved via ViewModel's OnSelectedLanguageChanged
        }
    }

    // Method to close OCR window from any instance
    public void CloseOcrCaptureWindow()
    {
        if (activeOcrWindow != null)
        {
            try
            {
                activeOcrWindow.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing OCR window: {ex.Message}");
            }
            finally
            {
                activeOcrWindow = null;
            }
        }
    }
} 