using Microsoft.Windows.ApplicationModel.Resources;

namespace Delete_Newline.Helpers;

public static class ResourceExtensionsHelper
{
    private static readonly ResourceLoader _resourceLoader = new();

    public static string GetLocalized(this string resourceKey) => _resourceLoader.GetString(resourceKey);
}
