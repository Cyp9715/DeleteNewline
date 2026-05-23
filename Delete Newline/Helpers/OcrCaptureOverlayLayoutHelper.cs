using System.Drawing;

namespace Delete_Newline.Helpers;

public static class OcrCaptureOverlayLayoutHelper
{
    public static (double Left, double Top) CalculateTopCenterToolbarPosition(
        Rectangle virtualScreenBounds,
        Rectangle primaryScreenBounds,
        double windowWidth,
        double windowHeight,
        double toolbarWidth,
        double topMargin)
    {
        double scaleX = GetPhysicalPixelsPerViewPixel(virtualScreenBounds.Width, windowWidth);
        double scaleY = GetPhysicalPixelsPerViewPixel(virtualScreenBounds.Height, windowHeight);

        double primaryLeft = (primaryScreenBounds.Left - virtualScreenBounds.Left) / scaleX;
        double primaryTop = (primaryScreenBounds.Top - virtualScreenBounds.Top) / scaleY;
        double primaryWidth = Math.Max(0, primaryScreenBounds.Width / scaleX);

        double left = primaryLeft + Math.Max(0, (primaryWidth - toolbarWidth) / 2);
        double top = primaryTop + topMargin;

        return (left, top);
    }

    private static double GetPhysicalPixelsPerViewPixel(int physicalSize, double viewSize)
    {
        if (physicalSize <= 0 || viewSize <= 0 || double.IsNaN(viewSize) || double.IsInfinity(viewSize))
        {
            return 1;
        }

        return physicalSize / viewSize;
    }
}
