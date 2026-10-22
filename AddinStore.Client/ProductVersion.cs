namespace AddinStore;

internal static class ProductVersion
{
    public static bool TryParse(string? text, out Version version)
    {
        version = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var original = text.Trim();
        text = original;
        if (text.StartsWith('v') || text.StartsWith('V'))
        {
            text = text[1..];
        }

        var plus = text.IndexOf('+', StringComparison.Ordinal);
        if (plus >= 0)
        {
            text = text[..plus];
        }

        var dash = text.IndexOf('-', StringComparison.Ordinal);
        if (dash >= 0)
        {
            text = text[..dash];
        }

        if (Version.TryParse(text, out Version? parsed) && parsed != null)
        {
            version = parsed;
            return true;
        }

        return TryParseTrailing(original, out version);
    }

    private static bool TryParseTrailing(string text, out Version version)
    {
        version = new Version(0, 0, 0);
        var end = text.Length;
        while (end > 0 && (char.IsDigit(text[end - 1]) || text[end - 1] == '.'))
        {
            end--;
        }

        if (end == text.Length)
        {
            return false;
        }

        if (!Version.TryParse(text[end..], out Version? parsed) || parsed == null)
        {
            return false;
        }

        version = parsed;
        return true;
    }
}
