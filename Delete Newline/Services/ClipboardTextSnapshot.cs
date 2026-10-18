namespace Delete_Newline.Services;

internal sealed record ClipboardTextSnapshot(string Text, uint Version)
{
    public static async Task<ClipboardTextSnapshot?> ReadAsync(Func<uint> getVersion, Func<Task<string?>> readText)
    {
        uint version = getVersion();
        string? text = await readText();
        if (text == null) return null;

        uint renderedVersion = getVersion();
        if (version == renderedVersion) return new(text, renderedVersion);

        // Delayed rendering can advance the sequence while GetTextAsync reads it.
        // Accept that version only if a second, stable read confirms the exact text.
        // A genuinely newer copy is left untouched.
        string? confirmedText = await readText();
        return renderedVersion == getVersion() && text == confirmedText
            ? new(text, renderedVersion) : null;
    }
}
