using System.Drawing;
using Delete_Newline.Helpers;
using Delete_Newline.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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
    private Language currentLanguage = new Language("en"); // Default language setting
    private Microsoft.UI.Xaml.Media.Imaging.BitmapImage? backgroundImage;
    private bool applyRegexEnabled = false; // Apply regex setting

    // OCRService dependency for notifying completion
    private readonly NotificationService? _notificationService;

    public OcrCaptureWindow()
    {
        InitializeComponent();
        _notificationService = App.GetService<NotificationService>();
    }

    public void SetupFullscreen(Microsoft.UI.Xaml.Media.Imaging.BitmapImage preloadedBackground, Language? selectedLanguage = null, bool applyRegexEnabled = false)
    {
        backgroundImage = preloadedBackground;
        this.applyRegexEnabled = applyRegexEnabled;
        
        // Passed language setting (if any)
        if (selectedLanguage != null)
        {
            currentLanguage = selectedLanguage;
            System.Diagnostics.Debug.WriteLine($"Using selected language: {currentLanguage.DisplayName} ({currentLanguage.LanguageTag})");
        }
        else
        {
            // Default to English if no language is selected
            var availableLanguages = OcrEngine.AvailableRecognizerLanguages;
            var englishLang = availableLanguages.FirstOrDefault(l => l.LanguageTag.StartsWith("en"));
            currentLanguage = englishLang ?? availableLanguages.FirstOrDefault() ?? new Language("en");
            System.Diagnostics.Debug.WriteLine($"Using default language: {currentLanguage.DisplayName} ({currentLanguage.LanguageTag})");
        }
        
        System.Diagnostics.Debug.WriteLine("Setting up fullscreen OCR capture window");
        
        // Remove title bar
        this.SetTitleBar(null);
        
        // Get window handle and remove decorations
        var hwnd = WindowNative.GetWindowHandle(this);
        RemoveWindowDecorations(hwnd);
        
        // Get virtual screen bounds (all monitors)
        var virtualScreen = ImageHelper.GetVirtualScreenBounds();
        
        // Position and size window to cover entire virtual screen
        SetWindowPos(hwnd, 
            new IntPtr(-1), // HWND_TOPMOST
            virtualScreen.X, 
            virtualScreen.Y, 
            virtualScreen.Width, 
            virtualScreen.Height, 
            SWP_NOZORDER | SWP_SHOWWINDOW);
        
        System.Diagnostics.Debug.WriteLine($"Window positioned: {virtualScreen}");
        System.Diagnostics.Debug.WriteLine($"Background image size: {backgroundImage?.PixelWidth}x{backgroundImage?.PixelHeight}");
        
        // Initialize
        Initialize();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, 
        int X, int Y, int cx, int cy, uint uFlags);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    
    private const uint SWP_NOZORDER = 0x0004;
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

    private void Initialize()
    {
        System.Diagnostics.Debug.WriteLine("Initializing OCR capture window");
        
        // Set preloaded background image
        SetBackgroundImage();
        
        // Setup overlay rectangles
        SetupOverlayRectangles();
        
        // Setup canvas events
        SetupCanvasEvents();
        
        // Setup window-level key handling for ESC
        SetupKeyHandling();
    }

    private void SetupKeyHandling()
    {
        // Handle key events on the main grid
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
            System.Diagnostics.Debug.WriteLine("ESC key detected - closing OCR window");
            this.Close();
            e.Handled = true;
        }
    }

    private void SetBackgroundImage()
    {
        try
        {
            if (backgroundImage != null)
            {
                BackgroundImage.Source = backgroundImage;
                System.Diagnostics.Debug.WriteLine("Preloaded background image set successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Warning: No background image provided");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set background image: {ex.Message}");
        }
    }

    private void SetupOverlayRectangles()
    {
        var virtualScreen = ImageHelper.GetVirtualScreenBounds();
        
        // Initially cover the entire virtual screen with overlay
        TopOverlay.Width = virtualScreen.Width;
        TopOverlay.Height = virtualScreen.Height;
        Canvas.SetLeft(TopOverlay, 0);
        Canvas.SetTop(TopOverlay, 0);

        // Hide other overlays initially
        LeftOverlay.Visibility = Visibility.Collapsed;
        RightOverlay.Visibility = Visibility.Collapsed;
        BottomOverlay.Visibility = Visibility.Collapsed;
        
        System.Diagnostics.Debug.WriteLine($"Overlay setup: {virtualScreen.Width}x{virtualScreen.Height}");
    }

    private void UpdateOverlayRectangles(Windows.Foundation.Rect selectionRect)
    {
        var virtualScreen = ImageHelper.GetVirtualScreenBounds();
        double windowWidth = virtualScreen.Width;
        double windowHeight = virtualScreen.Height;

        // Show all overlay rectangles
        TopOverlay.Visibility = Visibility.Visible;
        LeftOverlay.Visibility = Visibility.Visible;
        RightOverlay.Visibility = Visibility.Visible;
        BottomOverlay.Visibility = Visibility.Visible;

        // Top overlay (above selection)
        TopOverlay.Width = windowWidth;
        TopOverlay.Height = Math.Max(0, selectionRect.Y);
        Canvas.SetLeft(TopOverlay, 0);
        Canvas.SetTop(TopOverlay, 0);

        // Left overlay (left of selection)
        LeftOverlay.Width = Math.Max(0, selectionRect.X);
        LeftOverlay.Height = selectionRect.Height;
        Canvas.SetLeft(LeftOverlay, 0);
        Canvas.SetTop(LeftOverlay, selectionRect.Y);

        // Right overlay (right of selection)
        RightOverlay.Width = Math.Max(0, windowWidth - (selectionRect.X + selectionRect.Width));
        RightOverlay.Height = selectionRect.Height;
        Canvas.SetLeft(RightOverlay, selectionRect.X + selectionRect.Width);
        Canvas.SetTop(RightOverlay, selectionRect.Y);

        // Bottom overlay (below selection)
        BottomOverlay.Width = windowWidth;
        BottomOverlay.Height = Math.Max(0, windowHeight - (selectionRect.Y + selectionRect.Height));
        Canvas.SetLeft(BottomOverlay, 0);
        Canvas.SetTop(BottomOverlay, selectionRect.Y + selectionRect.Height);
    }

    private void SetupCanvasEvents()
    {
        RegionClickCanvas.PointerPressed += Canvas_PointerPressed;
        RegionClickCanvas.PointerMoved += Canvas_PointerMoved;
        RegionClickCanvas.PointerReleased += Canvas_PointerReleased;
        
        // Make canvas focusable for key events
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
        if (!isSelecting)
            return;

        isSelecting = false;
        RegionClickCanvas.ReleasePointerCapture(e.Pointer);

        // Calculate final selection rectangle in canvas coordinates
        double canvasLeft = Math.Min(startPoint.X, currentPoint.X);
        double canvasTop = Math.Min(startPoint.Y, currentPoint.Y);
        double canvasWidth = Math.Abs(currentPoint.X - startPoint.X);
        double canvasHeight = Math.Abs(currentPoint.Y - startPoint.Y);

        System.Diagnostics.Debug.WriteLine($"Canvas selection: {canvasWidth}x{canvasHeight} at ({canvasLeft}, {canvasTop})");

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
            // Hide selection border before capturing to prevent it from appearing in OCR image
            SelectionBorder.Visibility = Visibility.Collapsed;
            
            // Small delay to ensure UI update is applied
            await Task.Delay(1);
            
            // Get actual window size for accurate coordinate mapping
            var virtualScreen = ImageHelper.GetVirtualScreenBounds();
            double windowActualWidth = this.Bounds.Width;
            double windowActualHeight = this.Bounds.Height;
            
            // Calculate scale factors between actual window size and virtual screen
            double scaleX = virtualScreen.Width / windowActualWidth;
            double scaleY = virtualScreen.Height / windowActualHeight;
            
            System.Diagnostics.Debug.WriteLine($"=== OCR Coordinate Debug ===");
            System.Diagnostics.Debug.WriteLine($"Virtual screen: {virtualScreen}");
            System.Diagnostics.Debug.WriteLine($"Window actual size: {windowActualWidth}x{windowActualHeight}");
            System.Diagnostics.Debug.WriteLine($"Scale factors: X={scaleX:F3}, Y={scaleY:F3}");
            System.Diagnostics.Debug.WriteLine($"Canvas selection (raw): {canvasLeft}, {canvasTop}, {canvasWidth}, {canvasHeight}");
            
            // Convert canvas coordinates to screen coordinates with proper scaling
            int screenLeft = virtualScreen.X + (int)(canvasLeft * scaleX);
            int screenTop = virtualScreen.Y + (int)(canvasTop * scaleY);
            int screenWidth = (int)(canvasWidth * scaleX);
            int screenHeight = (int)(canvasHeight * scaleY);

            var screenRect = new System.Drawing.Rectangle(screenLeft, screenTop, screenWidth, screenHeight);

            System.Diagnostics.Debug.WriteLine($"Screen capture rect (corrected): {screenRect}");

            // Capture the selected region directly from screen
            var regionBitmap = ImageHelper.GetRegionOfScreenAsBitmap(screenRect);
            
            System.Diagnostics.Debug.WriteLine($"Captured bitmap: {regionBitmap.Width}x{regionBitmap.Height}");
            
            // Close the OCR window immediately to free the screen
            this.Close();
            
            // Process OCR in background without blocking UI
            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessOcrAsync(regionBitmap, screenRect);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Background OCR processing error: {ex.Message}");
                }
                finally
                {
                    regionBitmap?.Dispose();
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during OCR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async Task ProcessOcrAsync(Bitmap regionBitmap, Rectangle screenRect)
    {
        // Save captured image for debugging (only in DEBUG builds)
#if DEBUG
        try
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"ocr_debug_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            regionBitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
            System.Diagnostics.Debug.WriteLine($"DEBUG: Captured image saved to: {tempPath}");
        }
        catch (Exception saveEx)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save debug image: {saveEx.Message}");
        }
#endif
        
        System.Diagnostics.Debug.WriteLine($"OCR Language: {currentLanguage.DisplayName} ({currentLanguage.LanguageTag})");
        
        // Check if OCR engine is available
        var ocrEngine = Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(currentLanguage);
        if (ocrEngine == null)
        {
            System.Diagnostics.Debug.WriteLine($"WARNING: OCR engine not available for {currentLanguage.DisplayName}, trying English...");
            ocrEngine = Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(new Language("en"));
            if (ocrEngine == null)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: No OCR engine available!");
                return;
            }
        }
        System.Diagnostics.Debug.WriteLine($"OCR Engine ready: {ocrEngine != null}");
        
        // Perform OCR
        System.Diagnostics.Debug.WriteLine("Starting OCR process...");
        string ocrText = await OcrHelper.GetTextFromBitmapAsync(regionBitmap, currentLanguage);
        
        System.Diagnostics.Debug.WriteLine($"OCR completed. Raw result: '{ocrText}'");
        System.Diagnostics.Debug.WriteLine($"OCR text length: {ocrText?.Length ?? 0}");
        System.Diagnostics.Debug.WriteLine($"OCR text is null or whitespace: {string.IsNullOrWhiteSpace(ocrText)}");

        // Copy to clipboard if text found
        if (!string.IsNullOrWhiteSpace(ocrText))
        {
            try
            {
                // Switch back to UI thread for clipboard access
                var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                if (dispatcherQueue == null)
                {
                    // Try to get the main window's dispatcher
                    var mainWindow = App.MainWindow;
                    if (mainWindow != null)
                    {
                        dispatcherQueue = mainWindow.DispatcherQueue;
                    }
                }
                
                if (dispatcherQueue != null)
                {
                    dispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                            dataPackage.SetText(ocrText);
                            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                            
                            System.Diagnostics.Debug.WriteLine("✅ Text copied to clipboard successfully!");
                            System.Diagnostics.Debug.WriteLine($"Copied text: '{ocrText.Trim()}'");
                            
                            // Show success notification if notification service is available
                            if (_notificationService != null)
                            {
                                _notificationService.ShowSystemNotification(
                                    "Notification_OCR_Success_Title",
                                    "Notification_OCR_Success_Message",
                                    force: false,
                                    addTag: true,
                                    messageArgs: new object[] { ocrText.Trim().Length }
                                );
                            }
                        }
                        catch (Exception clipboardEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Failed to copy to clipboard: {clipboardEx.Message}");
                            
                            // Show error notification
                            if (_notificationService != null)
                            {
                                _notificationService.ShowSystemNotification(
                                    "Notification_OCR_Error_Title",
                                    "Notification_OCR_Error_Clipboard_Message",
                                    force: false,
                                    addTag: true
                                );
                            }
                        }
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ Could not get dispatcher queue for clipboard access");
                }
            }
            catch (Exception dispatcherEx)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to dispatch to UI thread: {dispatcherEx.Message}");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ No text found in OCR result");
            
            // Show no text found notification
            if (_notificationService != null)
            {
                _notificationService.ShowSystemNotification(
                    "Notification_OCR_NoText_Title",
                    "Notification_OCR_NoText_Message",
                    force: false,
                    addTag: true
                );
            }
            
            // Try alternative OCR approach
            System.Diagnostics.Debug.WriteLine("Trying direct OCR without scaling...");
            try
            {
                var ocrResult = await OcrHelper.GetOcrResultFromBitmapAsync(regionBitmap, currentLanguage);
                System.Diagnostics.Debug.WriteLine($"Direct OCR lines found: {ocrResult.Lines.Count}");
                foreach (var line in ocrResult.Lines)
                {
                    System.Diagnostics.Debug.WriteLine($"  Line: '{line.Text}'");
                }
            }
            catch (Exception directOcrEx)
            {
                System.Diagnostics.Debug.WriteLine($"Direct OCR failed: {directOcrEx.Message}");
            }
        }
    }
} 