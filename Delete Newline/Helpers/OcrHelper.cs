using Microsoft.UI.Xaml.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using WinUIEx;

namespace Delete_Newline.Helpers;

public static class OcrHelper
{
    public static async Task<string> GetRegionsTextAsync(WindowEx window, Rectangle selectedRegion, Language language)
    {
        var windowRect = GetWindowRect(window);
        
        int correctedLeft = windowRect.Left + selectedRegion.Left;
        int correctedTop = windowRect.Top + selectedRegion.Top;

        Rectangle correctedRegion = new(correctedLeft, correctedTop, selectedRegion.Width, selectedRegion.Height);
        Bitmap bmp = ImageHelper.GetRegionOfScreenAsBitmap(correctedRegion);

        return await GetTextFromBitmapAsync(bmp, language);
    }

    public static async Task<string> GetTextFromBitmapAsync(Bitmap bitmap, Language language)
    {
        double scale = await GetIdealScaleFactorForOcrAsync(bitmap, language);
        using Bitmap scaledBitmap = ImageHelper.ScaleBitmapUniform(bitmap, scale);
        
        OcrResult ocrResult = await GetOcrResultFromBitmapAsync(scaledBitmap, language);
        return GetTextFromOcrResult(ocrResult, language);
    }

    public static async Task<OcrResult> GetOcrResultFromBitmapAsync(Bitmap bitmap, Language language)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Bmp);
        stream.Position = 0;

        var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
        using var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

        OcrEngine ocrEngine = OcrEngine.TryCreateFromLanguage(language);
        if (ocrEngine == null)
        {
            // Fallback to English if the requested language is not available
            ocrEngine = OcrEngine.TryCreateFromLanguage(new Language("en"));
        }

        return await ocrEngine.RecognizeAsync(softwareBitmap);
    }

    private static string GetTextFromOcrResult(OcrResult ocrResult, Language language)
    {
        StringBuilder text = new();
        bool isSpaceJoiningLanguage = IsSpaceJoiningLanguage(language);

        foreach (OcrLine ocrLine in ocrResult.Lines)
        {
            if (isSpaceJoiningLanguage)
            {
                text.AppendLine(ocrLine.Text);
            }
            else
            {
                bool isFirstWord = true;
                foreach (OcrWord ocrWord in ocrLine.Words)
                {
                    if (!isFirstWord)
                        text.Append(' ');
                    text.Append(ocrWord.Text);
                    isFirstWord = false;
                }
                text.AppendLine();
            }
        }

        return text.ToString();
    }

    private static bool IsSpaceJoiningLanguage(Language language)
    {
        // Most languages use spaces between words
        // Languages like Chinese, Japanese don't typically use spaces
        var nonSpaceJoiningLanguages = new[] { "zh", "ja", "ko" };
        return !nonSpaceJoiningLanguages.Any(lang => language.LanguageTag.StartsWith(lang));
    }

    public static async Task<double> GetIdealScaleFactorForOcrAsync(Bitmap bitmap, Language language)
    {
        OcrResult ocrResult = await GetOcrResultFromBitmapAsync(bitmap, language);
        return GetIdealScaleFactorForOcrResult(ocrResult, bitmap.Height, bitmap.Width);
    }

    private static double GetIdealScaleFactorForOcrResult(OcrResult ocrResult, int height, int width)
    {
        var heightsList = new List<double>();
        double scaleFactor = 1.5;

        foreach (OcrLine ocrLine in ocrResult.Lines)
            foreach (OcrWord ocrWord in ocrLine.Words)
                heightsList.Add(ocrWord.BoundingRect.Height);

        double lineHeight = 10;

        if (heightsList.Count > 0)
            lineHeight = heightsList.Average();

        // Ideal Line Height is 40px
        const double idealLineHeight = 40.0;
        scaleFactor = idealLineHeight / lineHeight;

        if (width * scaleFactor > OcrEngine.MaxImageDimension || height * scaleFactor > OcrEngine.MaxImageDimension)
        {
            int largerDim = Math.Max(width, height);
            scaleFactor = OcrEngine.MaxImageDimension / largerDim;
        }

        return Math.Max(0.1, Math.Min(scaleFactor, 10.0)); // Clamp between 0.1 and 10
    }

    private static Rectangle GetWindowRect(WindowEx window)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var rect = new RECT();
        GetWindowRect(hwnd, ref rect);
        return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, ref RECT rectangle);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static async Task<string> GetClickedWordAsync(WindowEx window, Windows.Foundation.Point clickedPoint, Language language)
    {
        Bitmap bmp = ImageHelper.GetWindowsBoundsBitmap(window);
        OcrResult ocrResult = await GetOcrResultFromBitmapAsync(bmp, language);
        
        foreach (OcrLine ocrLine in ocrResult.Lines)
            foreach (OcrWord ocrWord in ocrLine.Words)
                if (ocrWord.BoundingRect.Contains(clickedPoint))
                    return ocrWord.Text;

        return string.Empty;
    }
}


public static class ImageHelper
{
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, ref RECT rectangle);

    // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getsystemmetrics
    private const int SM_CXSCREEN = 0;       // Width of the primary display monitor
    private const int SM_CYSCREEN = 1;       // Height of the primary display monitor
    private const int SM_CXVIRTUALSCREEN = 78; // Width of the virtual screen
    private const int SM_CYVIRTUALSCREEN = 79; // Height of the virtual screen
    private const int SM_XVIRTUALSCREEN = 76;  // Left coordinate of the virtual screen
    private const int SM_YVIRTUALSCREEN = 77;  // Top coordinate of the virtual screen

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static Bitmap GetRegionOfScreenAsBitmap(Rectangle region)
    {
        Bitmap bmp = new(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using Graphics g = Graphics.FromImage(bmp);

        g.CopyFromScreen(region.Left, region.Top, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
        return PadImage(bmp);
    }

    public static Bitmap GetFullDesktopScreenshot()
    {
        // Get virtual screen bounds (covers all monitors)
        int left = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int top = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        Rectangle bounds = new(left, top, width, height);

        Bitmap screenshot = new(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using Graphics g = Graphics.FromImage(screenshot);
        g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);

        return screenshot;
    }

    public static BitmapImage GetFullDesktopScreenshotAsImageSource()
    {
        using var bitmap = GetFullDesktopScreenshot();
        return BitmapToImageSource(bitmap);
    }

    public static Rectangle GetVirtualScreenBounds()
    {
        int left = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int top = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        return new Rectangle(left, top, width, height);
    }

    public static Rectangle GetPrimaryScreenBounds()
    {
        int width = GetSystemMetrics(SM_CXSCREEN);
        int height = GetSystemMetrics(SM_CYSCREEN);

        return new Rectangle(0, 0, width, height);
    }

    public static Bitmap PadImage(Bitmap image, int minW = 64, int minH = 64)
    {
        if (image.Height >= minH && image.Width >= minW)
            return image;

        int width = Math.Max(image.Width + 16, minW + 16);
        int height = Math.Max(image.Height + 16, minH + 16);

        // Create a compatible bitmap
        Bitmap destination = new(width, height, image.PixelFormat);
        using Graphics gd = Graphics.FromImage(destination);

        gd.Clear(image.GetPixel(0, 0));
        gd.DrawImageUnscaled(image, 8, 8);

        return destination;
    }

    public static Bitmap GetWindowsBoundsBitmap(WindowEx window)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var windowRect = GetWindowRect(hwnd);

        int windowWidth = windowRect.Width;
        int windowHeight = windowRect.Height;

        Bitmap bmp = new(windowWidth, windowHeight, PixelFormat.Format32bppArgb);
        using Graphics g = Graphics.FromImage(bmp);

        g.CopyFromScreen(windowRect.Left, windowRect.Top, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
        return bmp;
    }

    private static Rectangle GetWindowRect(IntPtr hwnd)
    {
        var rect = new RECT();
        GetWindowRect(hwnd, ref rect);
        return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    public static BitmapImage BitmapToImageSource(Bitmap bitmap)
    {
        using MemoryStream memory = new();
        bitmap.Save(memory, ImageFormat.Bmp);
        memory.Position = 0;

        var bitmapImage = new BitmapImage();
        bitmapImage.SetSource(memory.AsRandomAccessStream());

        return bitmapImage;
    }

    public static Bitmap ScaleBitmapUniform(Bitmap passedBitmap, double scale)
    {
        int newWidth = (int)(passedBitmap.Width * scale);
        int newHeight = (int)(passedBitmap.Height * scale);

        Bitmap scaledBitmap = new(newWidth, newHeight, passedBitmap.PixelFormat);
        using Graphics g = Graphics.FromImage(scaledBitmap);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(passedBitmap, 0, 0, newWidth, newHeight);

        return scaledBitmap;
    }

    public static async Task<SoftwareBitmap> BitmapToSoftwareBitmapAsync(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Bmp);
        stream.Position = 0;

        var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
        return await decoder.GetSoftwareBitmapAsync();
    }

    public static BitmapImage GetWindowBoundsImage(WindowEx window)
    {
        using var bitmap = GetWindowsBoundsBitmap(window);
        return BitmapToImageSource(bitmap);
    }
}