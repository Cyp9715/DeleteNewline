using Windows.ApplicationModel.Resources;

namespace Delete_Newline.Helpers;

public static class LocalizationHelper
{
    private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();

    public static string GetLocalizedString(string resourceKey)
    {
        return _resourceLoader.GetString(resourceKey);
    }

    public static string GetLocalizedString(string resourceKey, params object[] args)
    {
        var format = _resourceLoader.GetString(resourceKey);
        return string.Format(format, args);
    }
} 