using System.Drawing;
using Delete_Newline.Helpers;
using Delete_Newline.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Globalization;
using Windows.Media.Ocr;
using Windows.System;
using WinUIEx;
using WinRT.Interop;

namespace Delete_Newline.Views;

public sealed partial class OcrCaptureWindow : WindowEx
{
    private Windows.Foundation.Point startPoint = new();
    private Windows.Foundation.Point currentPoint = new();
    private bool isSelecting = false;
    private Language currentLanguage;
    private OCRPage.MonitorInfo? currentMonitor;
    private Microsoft.UI.Xaml.Media.Imaging.BitmapImage? precapturedBackground;
    private double dpiScale = 1.0;

    public OcrCaptureWindow()
    {
        InitializeComponent();
        
        currentLanguage = new Language("en");
    }

    public void SetupForMonitor(OCRPage.MonitorInfo monitor, Microsoft.UI.Xaml.Media.Imaging.BitmapImage backgroundImage)
    {
        currentMonitor = monitor;
        precapturedBackground = backgroundImage;
        
        System.Diagnostics.Debug.WriteLine($"Setting up for monitor: {monitor.Bounds}");
        
        // Hide title bar completely
        this.SetTitleBar(null);
        this.IsMinimizable = false;
        this.IsMaximizable = false;
        this.IsResizable = false;
        
        // Set window state to normal first
        this.WindowState = WindowState.Normal;
        
        // Set exact window size to match monitor bounds
        this.Width = monitor.Bounds.Width;
        this.Height = monitor.Bounds.Height;
        
        // Get window handle for positioning
        var hwnd = WindowNative.GetWindowHandle(this);
        
        // Get DPI scaling for this monitor
        dpiScale = GetDpiScale(hwnd);
        
        // Remove window decorations for true fullscreen
        RemoveWindowDecorations(hwnd);
        
        // Position window exactly on the target monitor
        SetWindowPos(hwnd, new IntPtr(-1), // HWND_TOPMOST
            monitor.Bounds.Left, monitor.Bounds.Top, 
            monitor.Bounds.Width, monitor.Bounds.Height, 
            SWP_SHOWWINDOW | SWP_NOACTIVATE);
        
        System.Diagnostics.Debug.WriteLine($"Window positioned: {monitor.Bounds.Left},{monitor.Bounds.Top} Size: {monitor.Bounds.Width}x{monitor.Bounds.Height}");
        System.Diagnostics.Debug.WriteLine($"DPI Scale: {dpiScale}");
        System.Diagnostics.Debug.WriteLine($"Background image size: {backgroundImage.PixelWidth}x{backgroundImage.PixelHeight}");
        
        // Initialize after positioning
        Initialize();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, 
        int X, int Y, int cx, int cy, uint uFlags);
    
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);
    
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const int GWL_STYLE = -16;
    private const int WS_CAPTION = 0x00C00000;
    private const int WS_THICKFRAME = 0x00040000;
    private const int WS_MINIMIZE = 0x20000000;
    private const int WS_MAXIMIZE = 0x01000000;
    private const int WS_SYSMENU = 0x00080000;

    private void RemoveWindowDecorations(IntPtr hwnd)
    {
        try
        {
            int style = GetWindowLong(hwnd, GWL_STYLE);
            style &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZE | WS_MAXIMIZE | WS_SYSMENU);
            SetWindowLong(hwnd, GWL_STYLE, style);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to remove window decorations: {ex.Message}");
        }
    }

    private double GetDpiScale(IntPtr hwnd)
    {
        try
        {
            uint dpi = GetDpiForWindow(hwnd);
            return dpi / 96.0; // 96 DPI is 100% scale
        }
        catch
        {
            return 1.0; // Fallback to no scaling
        }
    }

    private void Initialize()
    {
        if (currentMonitor == null) return;
        
        System.Diagnostics.Debug.WriteLine($"Initializing for monitor: {currentMonitor.Bounds}");
        
        // Ensure window fills the entire monitor
        this.Width = currentMonitor.Bounds.Width;
        this.Height = currentMonitor.Bounds.Height;
        
        // Use pre-captured background image
        SetPreCapturedBackground();
        
        // Setup overlay rectangles
        SetupOverlayRectangles();
        
        // Load OCR languages
        LoadOcrLanguages();
        
        // Setup canvas events
        SetupCanvasEvents();
        
        // Setup window-level key handling for ESC
        SetupKeyHandling();
    }

    private void SetupKeyHandling()
    {
        // Handle key events on the main grid (WindowEx may not support KeyDown directly)
        MainGrid.KeyDown += OcrCaptureWindow_KeyDown;
        
        // Make sure the window can receive focus when activated
        this.Activated += (s, e) => 
        {
            RegionClickCanvas.Focus(FocusState.Programmatic);
            MainGrid.Focus(FocusState.Programmatic);
        };
        
        // Also set focus when canvas is loaded
        RegionClickCanvas.Loaded += (s, e) =>
        {
            RegionClickCanvas.Focus(FocusState.Programmatic);
            MainGrid.Focus(FocusState.Programmatic);
        };
    }

    private void OcrCaptureWindow_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Key pressed: {e.Key}");
        
        if (e.Key == VirtualKey.Escape)
        {
            System.Diagnostics.Debug.WriteLine("ESC key detected - closing all OCR windows");
            OCRPage.CloseAllOcrCaptureWindows();
            e.Handled = true;
        }
    }

    private void SetPreCapturedBackground()
    {
        if (precapturedBackground != null && currentMonitor != null)
        {
            BackgroundImage.Source = precapturedBackground;
            
            // Ensure the background image fills exactly the monitor area
            BackgroundImage.Width = currentMonitor.Bounds.Width;
            BackgroundImage.Height = currentMonitor.Bounds.Height;
            
            // Also ensure the main grid fills the window
            MainGrid.Width = currentMonitor.Bounds.Width;
            MainGrid.Height = currentMonitor.Bounds.Height;
            
            System.Diagnostics.Debug.WriteLine($"Background image configured for monitor: {currentMonitor.Bounds}");
            System.Diagnostics.Debug.WriteLine($"Image display size: {BackgroundImage.Width}x{BackgroundImage.Height}");
        }
    }

    private void SetMonitorBackground()
    {
        // This method is now obsolete - we use pre-captured background
        // Keeping for backward compatibility but not using
    }

    private void SetFullscreenWindow()
    {
        // This method is no longer needed since SetupForMonitor handles positioning
    }

    private void SetupOverlayRectangles()
    {
        if (currentMonitor == null) return;
        
        // Initially cover the entire monitor with overlay
        TopOverlay.Width = currentMonitor.Bounds.Width;
        TopOverlay.Height = currentMonitor.Bounds.Height;
        Canvas.SetLeft(TopOverlay, 0);
        Canvas.SetTop(TopOverlay, 0);

        // Hide other overlays initially
        LeftOverlay.Visibility = Visibility.Collapsed;
        RightOverlay.Visibility = Visibility.Collapsed;
        BottomOverlay.Visibility = Visibility.Collapsed;
        
        System.Diagnostics.Debug.WriteLine($"Overlay setup for monitor: {currentMonitor.Bounds.Width}x{currentMonitor.Bounds.Height}");
    }

    private void UpdateOverlayRectangles(Windows.Foundation.Rect selectionRect)
    {
        if (currentMonitor == null) return;
        
        double monitorWidth = currentMonitor.Bounds.Width;
        double monitorHeight = currentMonitor.Bounds.Height;

        // Show all overlay rectangles
        TopOverlay.Visibility = Visibility.Visible;
        LeftOverlay.Visibility = Visibility.Visible;
        RightOverlay.Visibility = Visibility.Visible;
        BottomOverlay.Visibility = Visibility.Visible;

        // Top overlay (above selection)
        TopOverlay.Width = monitorWidth;
        TopOverlay.Height = Math.Max(0, selectionRect.Y);
        Canvas.SetLeft(TopOverlay, 0);
        Canvas.SetTop(TopOverlay, 0);

        // Left overlay (left of selection)
        LeftOverlay.Width = Math.Max(0, selectionRect.X);
        LeftOverlay.Height = selectionRect.Height;
        Canvas.SetLeft(LeftOverlay, 0);
        Canvas.SetTop(LeftOverlay, selectionRect.Y);

        // Right overlay (right of selection)
        RightOverlay.Width = Math.Max(0, monitorWidth - (selectionRect.X + selectionRect.Width));
        RightOverlay.Height = selectionRect.Height;
        Canvas.SetLeft(RightOverlay, selectionRect.X + selectionRect.Width);
        Canvas.SetTop(RightOverlay, selectionRect.Y);

        // Bottom overlay (below selection)
        BottomOverlay.Width = monitorWidth;
        BottomOverlay.Height = Math.Max(0, monitorHeight - (selectionRect.Y + selectionRect.Height));
        Canvas.SetLeft(BottomOverlay, 0);
        Canvas.SetTop(BottomOverlay, selectionRect.Y + selectionRect.Height);
    }

    private void SetupCanvasEvents()
    {
        RegionClickCanvas.PointerPressed += Canvas_PointerPressed;
        RegionClickCanvas.PointerMoved += Canvas_PointerMoved;
        RegionClickCanvas.PointerReleased += Canvas_PointerReleased;
        
        // Make canvas focusable for key events (handled by window now)
        RegionClickCanvas.IsTabStop = true;
        RegionClickCanvas.Focus(FocusState.Programmatic);
    }

    private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        isSelecting = true;
        RegionClickCanvas.CapturePointer(e.Pointer);
        
        startPoint = e.GetCurrentPoint(RegionClickCanvas).Position;
        currentPoint = startPoint;
        
        // Show selection border
        SelectionBorder.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionBorder, startPoint.X);
        Canvas.SetTop(SelectionBorder, startPoint.Y);
        SelectionBorder.Width = 0;
        SelectionBorder.Height = 0;
    }

    private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!isSelecting)
            return;

        currentPoint = e.GetCurrentPoint(RegionClickCanvas).Position;

        // Calculate selection rectangle
        double left = Math.Min(startPoint.X, currentPoint.X);
        double top = Math.Min(startPoint.Y, currentPoint.Y);
        double width = Math.Abs(currentPoint.X - startPoint.X);
        double height = Math.Abs(currentPoint.Y - startPoint.Y);

        // Update selection border
        Canvas.SetLeft(SelectionBorder, left);
        Canvas.SetTop(SelectionBorder, top);
        SelectionBorder.Width = width;
        SelectionBorder.Height = height;

        // Update overlay rectangles to highlight selection
        var selectionRect = new Windows.Foundation.Rect(left, top, width, height);
        UpdateOverlayRectangles(selectionRect);
    }

    private async void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!isSelecting || currentMonitor == null)
            return;

        isSelecting = false;
        RegionClickCanvas.ReleasePointerCapture(e.Pointer);

        // Calculate final selection rectangle in canvas coordinates
        double canvasLeft = Math.Min(startPoint.X, currentPoint.X);
        double canvasTop = Math.Min(startPoint.Y, currentPoint.Y);
        double canvasWidth = Math.Abs(currentPoint.X - startPoint.X);
        double canvasHeight = Math.Abs(currentPoint.Y - startPoint.Y);

        System.Diagnostics.Debug.WriteLine($"Canvas selection: {canvasWidth}x{canvasHeight} at ({canvasLeft}, {canvasTop})");
        System.Diagnostics.Debug.WriteLine($"DPI Scale: {dpiScale}");

        // Check if selection is large enough
        if (canvasWidth < 10 || canvasHeight < 10)
        {
            System.Diagnostics.Debug.WriteLine("Selection too small, ignoring");
            SetupOverlayRectangles();
            SelectionBorder.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            // Convert canvas coordinates to actual screen coordinates with DPI scaling
            int screenLeft = currentMonitor.Bounds.Left + (int)(canvasLeft * dpiScale);
            int screenTop = currentMonitor.Bounds.Top + (int)(canvasTop * dpiScale);
            int screenWidth = (int)(canvasWidth * dpiScale);
            int screenHeight = (int)(canvasHeight * dpiScale);

            var screenRect = new System.Drawing.Rectangle(screenLeft, screenTop, screenWidth, screenHeight);

            System.Diagnostics.Debug.WriteLine($"Canvas coords: {canvasLeft}, {canvasTop}, {canvasWidth}, {canvasHeight}");
            System.Diagnostics.Debug.WriteLine($"Monitor bounds: {currentMonitor.Bounds}");
            System.Diagnostics.Debug.WriteLine($"Final screen rect: {screenRect}");

            // Capture the selected region directly from screen
            var regionBitmap = ImageHelper.GetRegionOfScreenAsBitmap(screenRect);
            
            System.Diagnostics.Debug.WriteLine($"Captured bitmap: {regionBitmap.Width}x{regionBitmap.Height}");
            System.Diagnostics.Debug.WriteLine($"OCR Language: {currentLanguage.DisplayName}");
            
            // Perform OCR
            string ocrText = await OcrHelper.GetTextFromBitmapAsync(regionBitmap, currentLanguage);

            System.Diagnostics.Debug.WriteLine($"OCR Result: '{ocrText}' (Length: {ocrText?.Length ?? 0})");

            if (!string.IsNullOrWhiteSpace(ocrText))
            {
                // Copy to clipboard
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(ocrText);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                
                System.Diagnostics.Debug.WriteLine("Text copied to clipboard successfully");
                
                // Close all OCR windows
                OCRPage.CloseAllOcrCaptureWindows();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No text found in OCR result");
                // Reset overlays if no text found
                SetupOverlayRectangles();
                SelectionBorder.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during OCR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            SetupOverlayRectangles();
            SelectionBorder.Visibility = Visibility.Collapsed;
        }
    }

    private void LoadOcrLanguages()
    {
        var availableLanguages = OcrEngine.AvailableRecognizerLanguages;
        
        // Set default language to English or first available
        var englishLang = availableLanguages.FirstOrDefault(l => l.LanguageTag.StartsWith("en"));
        if (englishLang != null)
        {
            currentLanguage = englishLang;
        }
        else if (availableLanguages.Any())
        {
            currentLanguage = availableLanguages.First();
        }
    }
} 