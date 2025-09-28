using System.Drawing;
using Delete_Newline.Helpers;
using Delete_Newline.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Globalization;
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
        internal const int WS_EX_LAYERED = 0x80000;
    }

    private readonly NotificationService? _notificationService;
    private bool isFadeInStarted = false;
    private Microsoft.UI.Xaml.Media.Imaging.BitmapImage? backgroundImage;
    private Windows.Foundation.Point startPoint = new();
    private Windows.Foundation.Point currentPoint = new();
    private bool isSelecting = false;
    private Language currentLanguage = new Language("en"); // Default language setting

    public OcrCaptureWindow()
    {
        InitializeComponent();
        _notificationService = App.GetService<NotificationService>();
        this.Activated += OnWindowActivated_FirstTime;
    }

    private void OnWindowActivated_FirstTime(object sender, WindowActivatedEventArgs args)
    {
        if (isFadeInStarted) return;
        isFadeInStarted = true;
        var hwnd = WindowNative.GetWindowHandle(this);

        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE) | NativeMethods.WS_EX_LAYERED);
        NativeMethods.SetLayeredWindowAttributes(hwnd, 0, 0, NativeMethods.LWA_ALPHA);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(3) };
        byte alpha = 0;
        const byte MAX_ALPHA = 255;   // <‑‑ 여기서 최종 투명도를 정의 (255보다 낮게)

        timer.Tick += (s, e) =>
        {
            alpha = (byte)Math.Min(alpha + 10, MAX_ALPHA);
            NativeMethods.SetLayeredWindowAttributes(hwnd, 0, alpha, NativeMethods.LWA_ALPHA);
            if (alpha >= MAX_ALPHA)
            {
                timer.Stop();
            }
        };
        timer.Start();

        this.Activated -= OnWindowActivated_FirstTime;
    }

    public void SetupFullscreen(Microsoft.UI.Xaml.Media.Imaging.BitmapImage preloadedBackground, Language? selectedLanguage = null, bool applyRegexEnabled = false)
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        currentLanguage = selectedLanguage ?? new Language("en");
        backgroundImage = preloadedBackground;
        RemoveWindowFrames(hwnd);

        var virtualScreen = ImageHelper.GetVirtualScreenBounds();

        NativeMethods.SetWindowPos(hwnd,
            new IntPtr(-1), // HWND_TOPMOST
            virtualScreen.X,
            virtualScreen.Y,
            virtualScreen.Width,
            virtualScreen.Height,
            NativeMethods.SWP_NOZORDER | NativeMethods.SWP_SHOWWINDOW);

        System.Diagnostics.Debug.WriteLine($"Window positioned: {virtualScreen}");
        System.Diagnostics.Debug.WriteLine($"Background image size: {backgroundImage?.PixelWidth}x{backgroundImage?.PixelHeight}");
        System.Diagnostics.Debug.WriteLine("Initializing OCR capture window");

        BackgroundImage.Source = backgroundImage;
        SetupOverlayRectangles();
        SetupCanvasEvents();
        SetupKeyHandling();
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
        MainGrid.KeyDown += OcrCaptureWindow_KeyDown;
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

    private void OcrCaptureWindow_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        this.Close();
        e.Handled = true;
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
        SelectionBorder.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionBorder, startPoint.X);
        Canvas.SetTop(SelectionBorder, startPoint.Y);
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

        Canvas.SetLeft(SelectionBorder, left);
        Canvas.SetTop(SelectionBorder, top);
        SelectionBorder.Width = width;
        SelectionBorder.Height = height;

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
            SelectionBorder.Visibility = Visibility.Collapsed;

            // Small delay to ensure the UI thread renders the change (hiding the border) before the screen is captured.
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

            this.Close();

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
        var ocrEngine = Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(currentLanguage);

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