using Delete_Newline.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Globalization;
using Windows.Media.Ocr;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Delete_Newline.Helpers;

namespace Delete_Newline.Views;

public sealed partial class OCRPage : Page
{
    public OCRViewModel ViewModel { get; }
    
    // Track all active OCR capture windows
    private static readonly List<OcrCaptureWindow> activeOcrWindows = new();

    public OCRPage()
    {
        ViewModel = App.GetService<OCRViewModel>();
        InitializeComponent();
        
        LoadOcrLanguages();
    }

    private void LoadOcrLanguages()
    {
        var availableLanguages = OcrEngine.AvailableRecognizerLanguages;
        
        foreach (var language in availableLanguages)
        {
            LanguageComboBox.Items.Add(language);
        }
        
        // Set default to English or first available
        var englishLang = availableLanguages.FirstOrDefault(l => l.LanguageTag.StartsWith("en"));
        if (englishLang != null)
        {
            LanguageComboBox.SelectedItem = englishLang;
        }
        else if (availableLanguages.Any())
        {
            LanguageComboBox.SelectedIndex = 0;
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
        // Close any existing OCR windows first
        CloseAllOcrCaptureWindows();
        
        // Get all available monitors
        var allScreens = GetAllMonitors();
        
        System.Diagnostics.Debug.WriteLine($"Found {allScreens.Count} monitors:");
        foreach (var screen in allScreens)
        {
            System.Diagnostics.Debug.WriteLine($"  Monitor: {screen.Bounds} (W:{screen.Bounds.Width}, H:{screen.Bounds.Height})");
        }
        
        // Pre-capture all monitor screens before showing any windows
        var monitorScreenshots = new Dictionary<MonitorInfo, Microsoft.UI.Xaml.Media.Imaging.BitmapImage>();
        
        foreach (var screen in allScreens)
        {
            System.Diagnostics.Debug.WriteLine($"Capturing screen for monitor: {screen.Bounds}");
            
            // Capture screen BEFORE creating the window - ensure exact bounds
            var screenBitmap = ImageHelper.GetRegionOfScreenAsBitmap(screen.Bounds);
            var imageSource = ImageHelper.BitmapToImageSource(screenBitmap);
            
            System.Diagnostics.Debug.WriteLine($"  Captured bitmap: {screenBitmap.Width}x{screenBitmap.Height}");
            System.Diagnostics.Debug.WriteLine($"  Image source: {imageSource.PixelWidth}x{imageSource.PixelHeight}");
            
            monitorScreenshots[screen] = imageSource;
            
            // Dispose the bitmap to free memory
            screenBitmap.Dispose();
        }
        
        // Now create and show windows with pre-captured backgrounds
        foreach (var screen in allScreens)
        {
            System.Diagnostics.Debug.WriteLine($"Creating OCR window for monitor: {screen.Bounds}");
            
            var ocrWindow = new OcrCaptureWindow();
            ocrWindow.SetupForMonitor(screen, monitorScreenshots[screen]);
            
            // Handle window closed event to remove from tracking list
            ocrWindow.Closed += (sender, args) =>
            {
                if (sender is OcrCaptureWindow closedWindow)
                {
                    activeOcrWindows.Remove(closedWindow);
                }
            };
            
            ocrWindow.Activate();
            
            // Track the window so we can close all of them later
            activeOcrWindows.Add(ocrWindow);
        }
        
        System.Diagnostics.Debug.WriteLine($"Created {activeOcrWindows.Count} OCR windows");
    }

    private List<MonitorInfo> GetAllMonitors()
    {
        detectedMonitors.Clear(); // Clear previous results
        
        // Use Win32 API to enumerate all monitors
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumCallback, IntPtr.Zero);
        
        return new List<MonitorInfo>(detectedMonitors); // Return a copy
    }

    private readonly List<MonitorInfo> detectedMonitors = new();

    private bool MonitorEnumCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
    {
        var monitor = new MonitorInfo
        {
            Bounds = new System.Drawing.Rectangle(
                lprcMonitor.Left,
                lprcMonitor.Top,
                lprcMonitor.Right - lprcMonitor.Left,
                lprcMonitor.Bottom - lprcMonitor.Top),
            Primary = detectedMonitors.Count == 0 // First monitor is typically primary
        };
        
        detectedMonitors.Add(monitor);
        return true;
    }

    // Win32 API declarations
    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

    private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public class MonitorInfo
    {
        public System.Drawing.Rectangle Bounds { get; set; }
        public bool Primary { get; set; }
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is Language selectedLanguage)
        {
            // TODO: Save selected language to settings
            System.Diagnostics.Debug.WriteLine($"Selected OCR language: {selectedLanguage.DisplayName}");
        }
    }

    // Static method to close all OCR windows from any instance
    public static void CloseAllOcrCaptureWindows()
    {
        foreach (var window in activeOcrWindows.ToList()) // ToList to avoid modification during iteration
        {
            try
            {
                window.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing OCR window: {ex.Message}");
            }
        }
        activeOcrWindows.Clear();
    }
} 