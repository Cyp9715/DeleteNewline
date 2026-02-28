using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Windows.ApplicationModel.Resources;

namespace Delete_Newline.Helpers;

public static class LocalizationHelper
{
    public static string GetLocalizedString(string resourceKey)
    {
        try
        {
            return new ResourceLoader().GetString(resourceKey);
        }
        catch (COMException ex)
        {
            Debug.WriteLine($"Missing localization resource key: {resourceKey}. {ex.Message}");
            return resourceKey;
        }
    }

    public static string GetLocalizedString(string resourceKey, params object[] args)
    {
        try
        {
            var format = new ResourceLoader().GetString(resourceKey);
            return string.Format(format, args);
        }
        catch (COMException ex)
        {
            Debug.WriteLine($"Missing localization resource key: {resourceKey}. {ex.Message}");
            return resourceKey;
        }
        catch (FormatException ex)
        {
            Debug.WriteLine($"Localization format error for key: {resourceKey}. {ex.Message}");
            return resourceKey;
        }
    }
}
