using System.Drawing;
using Delete_Newline.Helpers;
using Delete_Newline.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Globalization;
using Windows.System;
using WinUIEx;
using WinRT.Interop;
using System.Runtime.InteropServices;

namespace Delete_Newline.Views;

public sealed partial class OcrCaptureWindow : WindowEx
{
    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLongPtr32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        internal delegate IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        internal static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return IntPtr.Size == 8
                ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
                : new IntPtr(SetWindowLongPtr32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        internal const uint SWP_NOZORDER = 0x0004;
        internal const uint SWP_SHOWWINDOW = 0x0040;
        internal const int GWL_STYLE = -16;
        internal const int WS_CAPTION = 0x00C00000;
        internal const int WS_THICKFRAME = 0x00040000;
        internal const int WS_MINIMIZE = 0x20000000;
        internal const int WS_MAXIMIZE = 0x01000000;
        internal const int WS_SYSMENU = 0x00080000;
        internal const uint SWP_FRAMECHANGED = 0x0020;
        internal const uint SWP_NOMOVE = 0x0002;
        internal const uint SWP_NOSIZE = 0x0001;
        internal const uint LWA_ALPHA = 0x00000002;
        internal const int GWL_EXSTYLE = -20;
        internal const int GWLP_WNDPROC = -4;
        internal const uint WM_KEYDOWN = 0x0100;
        internal const uint WM_SYSKEYDOWN = 0x0104;
        internal const int VK_ESCAPE = 0x1B;
        internal const int WS_EX_LAYERED = 0x80000;
        internal const int WS_EX_TOOLWINDOW = 0x00000080;
        internal const int WS_EX_APPWINDOW = 0x00040000;
    }

    private const double OverlayTargetOpacity = 0.6;
    private const double OverlayFadeStep = 0.04;
    private readonly NotificationService? _notificationService;
    private bool _isActivated;
    private bool _isBackgroundImageReady;
    private bool _isOverlayRevealed;
    private Microsoft.UI.Xaml.Media.Imaging.BitmapImage? backgroundImage;
    private Windows.Foundation.Point startPoint = new();
    private Windows.Foundation.Point currentPoint = new();
    private bool isSelecting = false;
    private Language currentLanguage = new Language("en"); // Default language setting
    private Action<Language>? _languageChanged;
    private NativeMethods.WindowProc? _escapeWindowProc;
    private IntPtr _originalWindowProc;
    private IntPtr _hookedHwnd;
    private IntPtr _overlayHwnd;
    private bool _isClosing;

    public OcrCaptureWindow()
    {
        InitializeComponent();
        _notificationService = App.GetService<NotificationService>();
        MainGrid.Loaded += (_, _) => PositionLanguageToolbar();
        MainGrid.SizeChanged += (_, _) => PositionLanguageToolbar();
        this.Activated += OnWindowActivated_FirstTime;
    }

    private void OnWindowActivated_FirstTime(object sender, WindowActivatedEventArgs args)
    {
        _isActivated = true;
        TryRevealPreparedOverlay();

        this.Activated -= OnWindowActivated_FirstTime;
    }

    private void BackgroundImage_ImageOpened(object sender, RoutedEventArgs e)
    {
        BackgroundImage.ImageOpened -= BackgroundImage_ImageOpened;
        BackgroundImage.ImageFailed -= BackgroundImage_ImageFailed;
        _isBackgroundImageReady = true;
        TryRevealPreparedOverlay();
    }

    private void BackgroundImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        BackgroundImage.ImageOpened -= BackgroundImage_ImageOpened;
        BackgroundImage.ImageFailed -= BackgroundImage_ImageFailed;
        _isBackgroundImageReady = true;
        TryRevealPreparedOverlay();
    }

    private void TryRevealPreparedOverlay()
    {
        if (_isOverlayRevealed || !_isActivated || !_isBackgroundImageReady || _overlayHwnd == IntPtr.Zero)
        {
            return;
        }

        _isOverlayRevealed = true;
        NativeMethods.SetLayeredWindowAttributes(_overlayHwnd, 0, 255, NativeMethods.LWA_ALPHA);
        BeginOverlayDarkenFade();
    }

    private void BeginOverlayDarkenFade()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(8) };
        timer.Tick += (s, e) =>
        {
            double opacity = Math.Min(TopOverlay.Opacity + OverlayFadeStep, OverlayTargetOpacity);
            SetOverlayOpacity(opacity);

            if (opacity >= OverlayTargetOpacity)
            {
                timer.Stop();
                SetOverlayOpacity(OverlayTargetOpacity);
            }
        };
        timer.Start();
    }

    private void SetOverlayOpacity(double opacity)
    {
        TopOverlay.Opacity = opacity;
        LeftOverlay.Opacity = opacity;
        RightOverlay.Opacity = opacity;
        BottomOverlay.Opacity = opacity;
    }

    public void SetupFullscreen(
        Microsoft.UI.Xaml.Media.Imaging.BitmapImage preloadedBackground,
        Language? selectedLanguage = null,
        IReadOnlyList<Language>? availableLanguages = null,
        Action<Language>? languageChanged = null)
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        currentLanguage = selectedLanguage ?? new Language("en");
        _languageChanged = languageChanged;
        backgroundImage = preloadedBackground;
        _overlayHwnd = hwnd;
        RemoveWindowFrames(hwnd);
        ApplyOverlayWindowExStyle(hwnd);
        HideOverlayWindowUntilPrepared(hwnd);
        RegisterEscapeMessageHook(hwnd);

        var virtualScreen = ImageHelper.GetVirtualScreenBounds();

        NativeMethods.SetWindowPos(hwnd,
            new IntPtr(-1), // HWND_TOPMOST
            virtualScreen.X,
            virtualScreen.Y,
            virtualScreen.Width,
            virtualScreen.Height,
            NativeMethods.SWP_SHOWWINDOW);

        // TopMost
        NativeMethods.SetForegroundWindow(hwnd);
        
        System.Diagnostics.Debug.WriteLine($"Window positioned: {virtualScreen}");
        System.Diagnostics.Debug.WriteLine($"Background image size: {backgroundImage?.PixelWidth}x{backgroundImage?.PixelHeight}");
        System.Diagnostics.Debug.WriteLine("Initializing OCR capture window");

        BackgroundImage.ImageOpened += BackgroundImage_ImageOpened;
        BackgroundImage.ImageFailed += BackgroundImage_ImageFailed;
        BackgroundImage.Source = backgroundImage;
        SetupOverlayRectangles();
        SetupLanguageSelector(availableLanguages, currentLanguage);
        PositionLanguageToolbar();
        SetupCanvasEvents();
        SetupKeyHandling();
    }

    private void HideOverlayWindowUntilPrepared(IntPtr hwnd)
    {
        SetOverlayOpacity(0);
        NativeMethods.SetLayeredWindowAttributes(hwnd, 0, 0, NativeMethods.LWA_ALPHA);
    }

    private void SetupLanguageSelector(IReadOnlyList<Language>? availableLanguages, Language selectedLanguage)
    {
        IReadOnlyList<Language> languages = availableLanguages is { Count: > 0 }
            ? availableLanguages
            : [selectedLanguage];

        CaptureLanguageComboBox.ItemsSource = languages;
        CaptureLanguageComboBox.SelectedItem = languages.FirstOrDefault(language =>
            language.LanguageTag.Equals(selectedLanguage.LanguageTag, StringComparison.OrdinalIgnoreCase)) ?? languages[0];
    }

    private void PositionLanguageToolbar()
    {
        const double topMargin = 16;
        var virtualScreen = ImageHelper.GetVirtualScreenBounds();
        var primaryScreen = ImageHelper.GetPrimaryScreenBounds();
        double toolbarWidth = LanguageToolbar.Width;
        if (double.IsNaN(toolbarWidth) || toolbarWidth <= 0)
        {
            toolbarWidth = LanguageToolbar.ActualWidth;
        }

        if (double.IsNaN(toolbarWidth) || toolbarWidth <= 0)
        {
            toolbarWidth = LanguageToolbar.MinWidth;
        }

        (double left, double top) = OcrCaptureOverlayLayoutHelper.CalculateTopCenterToolbarPosition(
            virtualScreen,
            primaryScreen,
            Bounds.Width,
            Bounds.Height,
            toolbarWidth,
            topMargin);

        Canvas.SetLeft(LanguageToolbar, left);
        Canvas.SetTop(LanguageToolbar, top);
    }

    private void CaptureLanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CaptureLanguageComboBox.SelectedItem is not Language selectedLanguage)
        {
            return;
        }

        currentLanguage = selectedLanguage;
        _languageChanged?.Invoke(currentLanguage);
        RegionClickCanvas.Focus(FocusState.Programmatic);
    }

    private void RemoveWindowFrames(IntPtr hwnd)
    {
        try
        {
            int style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);
            style &= ~(NativeMethods.WS_CAPTION | NativeMethods.WS_THICKFRAME | NativeMethods.WS_MINIMIZE | NativeMethods.WS_MAXIMIZE | NativeMethods.WS_SYSMENU);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_STYLE, style);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to remove window decorations: {ex.Message}");
        }
    }

    private void SetupKeyHandling()
    {
        var escapeKeyHandler = new KeyEventHandler(OcrCaptureWindow_KeyDown);
        MainGrid.AddHandler(UIElement.KeyDownEvent, escapeKeyHandler, handledEventsToo: true);
        CaptureLanguageComboBox.AddHandler(UIElement.KeyDownEvent, escapeKeyHandler, handledEventsToo: true);
        this.Activated += (s, e) =>
        {
            RegionClickCanvas.Focus(FocusState.Programmatic);
            MainGrid.Focus(FocusState.Programmatic);
        };
        RegionClickCanvas.Loaded += (s, e) =>
        {
            RegionClickCanvas.Focus(FocusState.Programmatic);
            MainGrid.Focus(FocusState.Programmatic);
        };
    }

    private void ApplyOverlayWindowExStyle(IntPtr hwnd)
    {
        try
        {
            int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            exStyle |= (NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TOOLWINDOW);
            exStyle &= ~NativeMethods.WS_EX_APPWINDOW;
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle);

            NativeMethods.SetWindowPos(
                hwnd,
                IntPtr.Zero,
                0,
                0,
                0,
                0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_FRAMECHANGED);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to apply overlay ex style: {ex.Message}");
        }
    }

    private void RegisterEscapeMessageHook(IntPtr hwnd)
    {
        if (_originalWindowProc != IntPtr.Zero)
        {
            return;
        }

        _escapeWindowProc = CaptureWindowProc;
        IntPtr hookPointer = Marshal.GetFunctionPointerForDelegate(_escapeWindowProc);
        IntPtr originalWindowProc = NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWLP_WNDPROC, hookPointer);
        if (originalWindowProc == IntPtr.Zero)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to install OCR capture escape hook: {Marshal.GetLastWin32Error()}");
            _escapeWindowProc = null;
            return;
        }

        _hookedHwnd = hwnd;
        _originalWindowProc = originalWindowProc;
    }

    private void RestoreEscapeMessageHook()
    {
        if (_hookedHwnd == IntPtr.Zero || _originalWindowProc == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.SetWindowLongPtr(_hookedHwnd, NativeMethods.GWLP_WNDPROC, _originalWindowProc);
        _hookedHwnd = IntPtr.Zero;
        _originalWindowProc = IntPtr.Zero;
        _escapeWindowProc = null;
    }

    private IntPtr CaptureWindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if ((msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN)
            && wParam.ToInt64() == NativeMethods.VK_ESCAPE)
        {
            CloseCaptureOverlay();
            return IntPtr.Zero;
        }

        return NativeMethods.CallWindowProc(_originalWindowProc, hwnd, msg, wParam, lParam);
    }

    private void OcrCaptureWindow_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Escape)
        {
            return;
        }

        CloseCaptureOverlay();
        e.Handled = true;
    }

    private void EscapeKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        CloseCaptureOverlay();
        args.Handled = true;
    }

    private void CloseCaptureOverlay()
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        RestoreEscapeMessageHook();
        this.Close();
    }

    private void SetupOverlayRectangles()
    {
        var virtualScreen = ImageHelper.GetVirtualScreenBounds();
        TopOverlay.Width = virtualScreen.Width;
        TopOverlay.Height = virtualScreen.Height;
        Canvas.SetLeft(TopOverlay, 0);
        Canvas.SetTop(TopOverlay, 0);
        LeftOverlay.Visibility = Visibility.Collapsed;
        RightOverlay.Visibility = Visibility.Collapsed;
        BottomOverlay.Visibility = Visibility.Collapsed;
        SelectionBorder.Visibility = Visibility.Collapsed;
        SelectionBorder.Width = 0;
        SelectionBorder.Height = 0;
    }

    private void HighlightSelectionOverlay(Windows.Foundation.Rect selectionRect)
    {
        var virtualScreen = ImageHelper.GetVirtualScreenBounds();
        double windowWidth = virtualScreen.Width;
        double windowHeight = virtualScreen.Height;

        TopOverlay.Visibility = Visibility.Visible;
        LeftOverlay.Visibility = Visibility.Visible;
        RightOverlay.Visibility = Visibility.Visible;
        BottomOverlay.Visibility = Visibility.Visible;

        TopOverlay.Width = windowWidth;
        TopOverlay.Height = Math.Max(0, selectionRect.Y);
        Canvas.SetLeft(TopOverlay, 0);
        Canvas.SetTop(TopOverlay, 0);

        LeftOverlay.Width = Math.Max(0, selectionRect.X);
        LeftOverlay.Height = selectionRect.Height;
        Canvas.SetLeft(LeftOverlay, 0);
        Canvas.SetTop(LeftOverlay, selectionRect.Y);

        RightOverlay.Width = Math.Max(0, windowWidth - (selectionRect.X + selectionRect.Width));
        RightOverlay.Height = selectionRect.Height;
        Canvas.SetLeft(RightOverlay, selectionRect.X + selectionRect.Width);
        Canvas.SetTop(RightOverlay, selectionRect.Y);

        BottomOverlay.Width = windowWidth;
        BottomOverlay.Height = Math.Max(0, windowHeight - (selectionRect.Y + selectionRect.Height));
        Canvas.SetLeft(BottomOverlay, 0);
        Canvas.SetTop(BottomOverlay, selectionRect.Y + selectionRect.Height);

        SelectionBorder.Visibility = Visibility.Visible;
        SelectionBorder.Width = Math.Max(0, selectionRect.Width);
        SelectionBorder.Height = Math.Max(0, selectionRect.Height);
        Canvas.SetLeft(SelectionBorder, selectionRect.X);
        Canvas.SetTop(SelectionBorder, selectionRect.Y);
    }

    private void SetupCanvasEvents()
    {
        RegionClickCanvas.PointerPressed += Canvas_PointerPressed;
        RegionClickCanvas.PointerMoved += Canvas_PointerMoved;
        RegionClickCanvas.PointerReleased += Canvas_PointerReleased;
        RegionClickCanvas.IsTabStop = true;
        RegionClickCanvas.Focus(FocusState.Programmatic);
    }

    private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        isSelecting = true;
        RegionClickCanvas.CapturePointer(e.Pointer);
        startPoint = e.GetCurrentPoint(RegionClickCanvas).Position;
        currentPoint = startPoint;
        SelectionBorder.Visibility = Visibility.Collapsed;
        SelectionBorder.Width = 0;
        SelectionBorder.Height = 0;
    }

    private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!isSelecting) return;

        currentPoint = e.GetCurrentPoint(RegionClickCanvas).Position;
        double left = Math.Min(startPoint.X, currentPoint.X);
        double top = Math.Min(startPoint.Y, currentPoint.Y);
        double width = Math.Abs(currentPoint.X - startPoint.X);
        double height = Math.Abs(currentPoint.Y - startPoint.Y);

        var selectionRect = new Windows.Foundation.Rect(left, top, width, height);
        HighlightSelectionOverlay(selectionRect);
    }

    private async void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!isSelecting) return;

        isSelecting = false;
        RegionClickCanvas.ReleasePointerCapture(e.Pointer);

        double canvasLeft = Math.Min(startPoint.X, currentPoint.X);
        double canvasTop = Math.Min(startPoint.Y, currentPoint.Y);
        double canvasWidth = Math.Abs(currentPoint.X - startPoint.X);
        double canvasHeight = Math.Abs(currentPoint.Y - startPoint.Y);

        System.Diagnostics.Debug.WriteLine($"Canvas selection: {canvasWidth}x{canvasHeight} at ({canvasLeft}, {canvasTop})");

        try
        {
            // Small delay to ensure the UI thread renders the latest selection overlay before the screen is captured.
            await Task.Delay(1);

            var virtualScreen = ImageHelper.GetVirtualScreenBounds();
            double windowActualWidth = this.Bounds.Width;
            double windowActualHeight = this.Bounds.Height;

            double scaleX = virtualScreen.Width / windowActualWidth;
            double scaleY = virtualScreen.Height / windowActualHeight;

            System.Diagnostics.Debug.WriteLine($"=== OCR Coordinate Debug ===");
            System.Diagnostics.Debug.WriteLine($"Virtual screen: {virtualScreen}");
            System.Diagnostics.Debug.WriteLine($"Window actual size: {windowActualWidth}x{windowActualHeight}");
            System.Diagnostics.Debug.WriteLine($"Scale factors: X={scaleX:F3}, Y={scaleY:F3}");
            System.Diagnostics.Debug.WriteLine($"Canvas selection (raw): {canvasLeft}, {canvasTop}, {canvasWidth}, {canvasHeight}");

            int screenLeft = virtualScreen.X + (int)(canvasLeft * scaleX);
            int screenTop = virtualScreen.Y + (int)(canvasTop * scaleY);
            int screenWidth = (int)(canvasWidth * scaleX);
            int screenHeight = (int)(canvasHeight * scaleY);

            var screenRect = new System.Drawing.Rectangle(screenLeft, screenTop, screenWidth, screenHeight);
            System.Diagnostics.Debug.WriteLine($"Screen capture rect (corrected): {screenRect}");

            var regionBitmap = ImageHelper.GetRegionOfScreenAsBitmap(screenRect);
            System.Diagnostics.Debug.WriteLine($"Captured bitmap: {regionBitmap.Width}x{regionBitmap.Height}");

            CloseCaptureOverlay();

            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessOcrAsync(regionBitmap);
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

    private async Task ProcessOcrAsync(Bitmap regionBitmap)
    {
        string ocrText = await OcrHelper.GetTextFromBitmapAsync(regionBitmap, currentLanguage);

        if (!string.IsNullOrWhiteSpace(ocrText))
        {
            try
            {
                var dispatcherQueue = App.MainWindow.DispatcherQueue;
                dispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                        dataPackage.SetText(ocrText);
                        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

                        System.Diagnostics.Debug.WriteLine("✅ Text copied to clipboard successfully!");
                        System.Diagnostics.Debug.WriteLine($"Copied text: '{ocrText.Trim()}'");

                        _notificationService?.ShowSystemNotification(
                            "Notification_OCR_Success_Title",
                            "Notification_OCR_Success_Message",
                            force: false,
                            addTag: true,
                            messageArgs: new object[] { ocrText.Trim().Length }
                        );
                    }
                    catch (Exception clipboardEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Failed to copy to clipboard: {clipboardEx.Message}");
                        _notificationService?.ShowSystemNotification(
                            "Notification_OCR_Error_Title",
                            "Notification_OCR_Error_Clipboard_Message",
                            force: false,
                            addTag: true
                        );
                    }
                });
            }
            catch (Exception dispatcherEx)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to dispatch to UI thread: {dispatcherEx.Message}");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ No text found in OCR result");
            _notificationService?.ShowSystemNotification(
                "Notification_OCR_NoText_Title",
                "Notification_OCR_NoText_Message",
                force: false,
                addTag: true
            );
        }
    }
}
