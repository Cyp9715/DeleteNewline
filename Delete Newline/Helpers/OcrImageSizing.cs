using System.Drawing;

namespace Delete_Newline.Helpers;

public static class OcrImageSizing
{
    public static Size Fit(int width, int height, double scale, int maxDimension)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxDimension);
        if (!double.IsFinite(scale) || scale <= 0) scale = 1;
        scale = Math.Min(scale, (double)maxDimension / Math.Max(width, height));
        return new Size(Math.Clamp((int)Math.Round(width * scale), 1, maxDimension),
                        Math.Clamp((int)Math.Round(height * scale), 1, maxDimension));
    }

    public static Rectangle SelectionToPixels(RectangleF selection, SizeF view, Size bitmap)
    {
        if (view.Width <= 0 || view.Height <= 0 || bitmap.Width <= 0 || bitmap.Height <= 0 ||
            !float.IsFinite(view.Width) || !float.IsFinite(view.Height)) return Rectangle.Empty;
        double scaleX = bitmap.Width / (double)view.Width;
        double scaleY = bitmap.Height / (double)view.Height;
        int left = (int)Math.Clamp(Math.Floor(selection.Left * scaleX), 0, bitmap.Width);
        int top = (int)Math.Clamp(Math.Floor(selection.Top * scaleY), 0, bitmap.Height);
        int right = (int)Math.Clamp(Math.Ceiling(selection.Right * scaleX), left, bitmap.Width);
        int bottom = (int)Math.Clamp(Math.Ceiling(selection.Bottom * scaleY), top, bitmap.Height);
        return Rectangle.FromLTRB(left, top, right, bottom);
    }
}
