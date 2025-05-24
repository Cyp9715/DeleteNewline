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
    
    // Currently selected language
    private Language? selectedLanguage;

    public OCRPage()
    {
        ViewModel = App.GetService<OCRViewModel>();
        InitializeComponent();
        
        LoadOcrLanguages();
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
        
        // Set default to English or first available
        Language? defaultLanguage = null;
        var englishLang = availableLanguages.FirstOrDefault(l => l.LanguageTag.StartsWith("en"));
        if (englishLang != null)
        {
            defaultLanguage = englishLang;
            System.Diagnostics.Debug.WriteLine($"Default language set to English: {englishLang.DisplayName}");
        }
        else if (availableLanguages.Any())
        {
            defaultLanguage = availableLanguages.First();
            System.Diagnostics.Debug.WriteLine($"Default language set to first available: {defaultLanguage.DisplayName}");
        }
        
        if (defaultLanguage != null)
        {
            LanguageComboBox.SelectedItem = defaultLanguage;
            selectedLanguage = defaultLanguage;
        }
    }

    private void StartFullScreenOcrButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LaunchFullScreenOcrCapture();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting FullScreen OCR: {ex.Message}");
            // TODO: Show error message to user
        }
    }

    private void LaunchFullScreenOcrCapture()
    {
        // Close any existing OCR window first
        CloseOcrCaptureWindow();
        
        System.Diagnostics.Debug.WriteLine("=== Starting OCR Capture ===");
        System.Diagnostics.Debug.WriteLine($"Selected language: {selectedLanguage?.DisplayName ?? "None"} ({selectedLanguage?.LanguageTag ?? "None"})");
        
        try
        {
            // Pre-capture desktop screenshot for background
            System.Diagnostics.Debug.WriteLine("Capturing desktop screenshot...");
            var backgroundImage = Delete_Newline.Helpers.ImageHelper.GetFullDesktopScreenshotAsImageSource();
            System.Diagnostics.Debug.WriteLine("Desktop screenshot captured successfully");
            
            // Create single OCR window
            var ocrWindow = new OcrCaptureWindow();
            
            // Handle window closed event
            ocrWindow.Closed += (sender, args) =>
            {
                activeOcrWindow = null;
                System.Diagnostics.Debug.WriteLine("OCR window closed");
            };
            
            // Setup fullscreen capture with preloaded background and selected language
            ocrWindow.SetupFullscreen(backgroundImage, selectedLanguage);
            ocrWindow.Activate();
            
            // Track the window
            activeOcrWindow = ocrWindow;
            
            System.Diagnostics.Debug.WriteLine("=== OCR Capture window created and activated ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating OCR window: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is Language newSelectedLanguage)
        {
            selectedLanguage = newSelectedLanguage;
            System.Diagnostics.Debug.WriteLine($"=== Language Changed ===");
            System.Diagnostics.Debug.WriteLine($"New language: {selectedLanguage.DisplayName} ({selectedLanguage.LanguageTag})");
            
            // Test OCR engine availability for selected language
            var testEngine = OcrEngine.TryCreateFromLanguage(selectedLanguage);
            if (testEngine != null)
            {
                System.Diagnostics.Debug.WriteLine($"✅ OCR engine available for {selectedLanguage.DisplayName}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ OCR engine NOT available for {selectedLanguage.DisplayName}");
            }
            
            // TODO: Save selected language to settings
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