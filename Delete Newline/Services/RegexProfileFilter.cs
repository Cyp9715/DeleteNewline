using Delete_Newline.Contracts.Structures;

namespace Delete_Newline.Services;

public static class RegexProfileFilter
{
    public static IEnumerable<RegexPageStructure> FilterByName(IEnumerable<RegexPageStructure> profiles, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return profiles;
        }

        string normalizedSearchText = searchText.Trim();
        return profiles.Where(profile =>
            !string.IsNullOrEmpty(profile.HotkeyName) &&
            profile.HotkeyName.Contains(normalizedSearchText, StringComparison.CurrentCultureIgnoreCase));
    }
}
