namespace AddinStore;

internal static class SetupAsset
{
    public static bool IsDirectExe(Uri url) =>
        url.IsAbsoluteUri
        && url.Scheme == Uri.UriSchemeHttps
        && url.AbsolutePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);

    public static Uri? ReleaseApi(Uri page)
    {
        if (!page.IsAbsoluteUri || page.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        if (page.Host.Equals("api.github.com", StringComparison.OrdinalIgnoreCase))
        {
            return page;
        }

        if (!page.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parts = page.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 5 && parts[2] == "releases" && parts[3] == "tag")
        {
            return new Uri($"https://api.github.com/repos/{parts[0]}/{parts[1]}/releases/tags/{Uri.EscapeDataString(parts[4])}");
        }

        if (parts.Length >= 4 && parts[2] == "releases" && parts[3] == "latest")
        {
            return new Uri($"https://api.github.com/repos/{parts[0]}/{parts[1]}/releases/latest");
        }

        return null;
    }

    public static string? FindSetupUrl(string? json, string namePrefix)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(namePrefix))
        {
            return null;
        }

        string? fallback = null;
        var from = 0;
        while (from < json.Length)
        {
            var keyAt = json.IndexOf("\"browser_download_url\"", from, StringComparison.Ordinal);
            if (keyAt < 0)
            {
                break;
            }

            var colon = json.IndexOf(':', keyAt);
            var quoteStart = colon < 0 ? -1 : json.IndexOf('"', colon + 1);
            var quoteEnd = quoteStart < 0 ? -1 : json.IndexOf('"', quoteStart + 1);
            if (quoteStart < 0 || quoteEnd < 0)
            {
                break;
            }

            var url = json[(quoteStart + 1)..quoteEnd].Replace("\\/", "/", StringComparison.Ordinal);
            from = quoteEnd + 1;
            if (url.IndexOf(namePrefix, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            if (!url.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (url.IndexOf(namePrefix + "-", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return url;
            }

            fallback ??= url;
        }

        return fallback;
    }
}
