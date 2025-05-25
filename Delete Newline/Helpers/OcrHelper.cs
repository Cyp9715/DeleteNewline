using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
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