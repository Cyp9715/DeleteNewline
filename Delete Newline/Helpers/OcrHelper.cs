using Microsoft.UI.Xaml.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using WinUIEx;
using WinRT;

namespace Delete_Newline.Helpers;

public static class OcrHelper
{
    public static async Task<string> GetRegionsTextAsync(WindowEx window, Rectangle selectedRegion, Language language)
    {
        var windowRect = GetWindowRect(window);
        
        int correctedLeft = windowRect.Left + selectedRegion.Left;
        int correctedTop = windowRect.Top + selectedRegion.Top;

        Rectangle correctedRegion = new(correctedLeft, correctedTop, selectedRegion.Width, selectedRegion.Height);
        using Bitmap bmp = ImageHelper.GetRegionOfScreenAsBitmap(correctedRegion);

        return await GetTextFromBitmapAsync(bmp, language);
    }

    public static async Task<string> GetTextFromBitmapAsync(Bitmap bitmap, Language language)
    {
        OcrEngine engine = CreateEngine(language);
        Size initialSize = OcrImageSizing.Fit(bitmap.Width, bitmap.Height, 1, (int)OcrEngine.MaxImageDimension);
        using Bitmap? fitted = initialSize == bitmap.Size ? null : ImageHelper.ScaleBitmap(bitmap, initialSize);
        Bitmap initial = fitted ?? bitmap;
        OcrResult result = await RecognizeAsync(initial, engine);
        double scale = GetIdealScaleFactorForOcrResult(result, initial.Height, initial.Width);
        Size scaledSize = OcrImageSizing.Fit(initial.Width, initial.Height, scale, (int)OcrEngine.MaxImageDimension);
        // A second recognition pass is only useful when the text size changes materially.
        if (scaledSize != initial.Size && Math.Abs((double)scaledSize.Width / initial.Width - 1) > 0.15)
        {
            using Bitmap scaled = ImageHelper.ScaleBitmap(initial, scaledSize);
            result = await RecognizeAsync(scaled, engine);
        }
        return GetTextFromOcrResult(result, language);
    }

    public static async Task<OcrResult> GetOcrResultFromBitmapAsync(Bitmap bitmap, Language language)
    {
        OcrEngine engine = CreateEngine(language);
        Size size = OcrImageSizing.Fit(bitmap.Width, bitmap.Height, 1, (int)OcrEngine.MaxImageDimension);
        using Bitmap? fitted = size == bitmap.Size ? null : ImageHelper.ScaleBitmap(bitmap, size);
        return await RecognizeAsync(fitted ?? bitmap, engine);
    }

    private static OcrEngine CreateEngine(Language language) =>
        OcrEngine.TryCreateFromLanguage(language)
        ?? throw new InvalidOperationException($"The Windows OCR language '{language.LanguageTag}' is not installed.");

    private static async Task<OcrResult> RecognizeAsync(Bitmap bitmap, OcrEngine engine)
    {
        using var softwareBitmap = ImageHelper.BitmapToSoftwareBitmap(bitmap);
        return await engine.RecognizeAsync(softwareBitmap);
    }

    private static string GetTextFromOcrResult(OcrResult ocrResult, Language language)
    {
        StringBuilder text = new();
        foreach (OcrLine ocrLine in ocrResult.Lines)
        {
            // Windows provides language-aware spacing, including Japanese and Chinese.
            text.AppendLine(ocrLine.Text);
        }

        return text.ToString();
    }

    public static async Task<double> GetIdealScaleFactorForOcrAsync(Bitmap bitmap, Language language)
    {
        OcrResult ocrResult = await GetOcrResultFromBitmapAsync(bitmap, language);
        return GetIdealScaleFactorForOcrResult(ocrResult, bitmap.Height, bitmap.Width);
    }

    private static double GetIdealScaleFactorForOcrResult(OcrResult ocrResult, int height, int width)
    {
        double totalHeight = 0;
        int wordCount = 0;

        foreach (OcrLine ocrLine in ocrResult.Lines)
            foreach (OcrWord ocrWord in ocrLine.Words)
            {
                totalHeight += ocrWord.BoundingRect.Height;
                wordCount++;
            }

        double lineHeight = wordCount > 0 ? totalHeight / wordCount : 10;

        // Ideal Line Height is 40px
        const double idealLineHeight = 40.0;
        double scaleFactor = idealLineHeight / lineHeight;

        if (width * scaleFactor > OcrEngine.MaxImageDimension || height * scaleFactor > OcrEngine.MaxImageDimension)
        {
            int largerDim = Math.Max(width, height);
            scaleFactor = (double)OcrEngine.MaxImageDimension / largerDim;
        }

        return Math.Min(scaleFactor, 10.0);
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
        using Bitmap bmp = ImageHelper.GetWindowsBoundsBitmap(window);
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
        using Bitmap bmp = new(region.Width, region.Height, PixelFormat.Format32bppArgb);
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
        try
        {
            using Graphics g = Graphics.FromImage(screenshot);
            g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            return screenshot;
        }
        catch
        {
            screenshot.Dispose();
            throw;
        }
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
            return new Bitmap(image);

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

    public static async Task<SoftwareBitmapSource> BitmapToImageSourceAsync(Bitmap bitmap)
    {
        using var pixels = BitmapToSoftwareBitmap(bitmap);
        var source = new SoftwareBitmapSource();
        try
        {
            await source.SetBitmapAsync(pixels);
            return source;
        }
        catch
        {
            source.Dispose();
            throw;
        }
    }

    public static Bitmap ScaleBitmapUniform(Bitmap passedBitmap, double scale)
    {
        Size size = OcrImageSizing.Fit(passedBitmap.Width, passedBitmap.Height, scale, (int)OcrEngine.MaxImageDimension);
        return ScaleBitmap(passedBitmap, size);
    }

    public static Bitmap ScaleBitmap(Bitmap passedBitmap, Size size)
    {
        Bitmap scaledBitmap = new(size.Width, size.Height, PixelFormat.Format32bppArgb);
        using Graphics g = Graphics.FromImage(scaledBitmap);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(passedBitmap, 0, 0, size.Width, size.Height);

        return scaledBitmap;
    }

    public static unsafe SoftwareBitmap BitmapToSoftwareBitmap(Bitmap bitmap)
    {
        // Screenshots and OCR use opaque BGRA pixels. Copy rows directly instead of
        // retaining a full-screen encoded BMP stream in the XAML image decoder.
        var pixels = new SoftwareBitmap(BitmapPixelFormat.Bgra8, bitmap.Width, bitmap.Height, BitmapAlphaMode.Ignore);
        try
        {
            using var buffer = pixels.LockBuffer(BitmapBufferAccessMode.Write);
            using var reference = buffer.CreateReference();
            reference.As<IMemoryBufferByteAccess>().GetBuffer(out byte* destination, out uint capacity);
            BitmapPlaneDescription plane = buffer.GetPlaneDescription(0);
            int rowBytes = checked(bitmap.Width * 4);
            long end = (long)plane.StartIndex + (long)(bitmap.Height - 1) * plane.Stride + rowBytes;
            if (plane.StartIndex < 0 || plane.Stride < rowBytes || end > capacity)
                throw new InvalidOperationException("The bitmap pixel buffer is smaller than the image.");

            BitmapData source = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    Buffer.MemoryCopy((byte*)source.Scan0 + (long)y * source.Stride,
                        destination + plane.StartIndex + (long)y * plane.Stride, rowBytes, rowBytes);
                }
            }
            finally
            {
                bitmap.UnlockBits(source);
            }
            return pixels;
        }
        catch
        {
            pixels.Dispose();
            throw;
        }
    }

    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}
