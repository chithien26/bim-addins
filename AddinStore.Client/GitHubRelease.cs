namespace AddinStore;

internal sealed class ReleaseInfo
{
    public ReleaseInfo(Version version, string? name, string? htmlUrl, string? setupUrl)
    {
        Version = version;
        Name = name;
        HtmlUrl = htmlUrl;
        SetupUrl = setupUrl;
    }

    public Version Version { get; }
    public string? Name { get; }
    public string? HtmlUrl { get; }
    public string? SetupUrl { get; }
}

internal static class GitHubRelease
{
    public static bool TryRead(string? json, string namePrefix, out ReleaseInfo info)
    {
        info = new ReleaseInfo(new Version(0, 0, 0), null, null, null);
        if (!ProductVersion.TryParse(JsonString(json, "tag_name"), out var version))
        {
            return false;
        }

        info = new ReleaseInfo(version, JsonString(json, "name"), JsonString(json, "html_url"), SetupAsset.FindSetupUrl(json, namePrefix));
        return true;
    }

    private static string? JsonString(string? json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
        {
            return null;
        }

        var needle = "\"" + key + "\"";
        var keyAt = json.IndexOf(needle, StringComparison.Ordinal);
        if (keyAt < 0)
        {
            return null;
        }

        var colon = json.IndexOf(':', keyAt + needle.Length);
        if (colon < 0)
        {
            return null;
        }

        var quoteStart = json.IndexOf('"', colon + 1);
        if (quoteStart < 0)
        {
            return null;
        }

        var cursor = quoteStart + 1;
        var value = new System.Text.StringBuilder();
        while (cursor < json.Length)
        {
            var character = json[cursor++];
            if (character == '\\' && cursor < json.Length)
            {
                value.Append(json[cursor++]);
                continue;
            }

            if (character == '"')
            {
                return value.ToString();
            }

            value.Append(character);
        }

        return null;
    }
}
