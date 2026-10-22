namespace AddinStore;

internal static class SetupDownloader
{
    private static readonly HttpClient Http = new(new SocketsHttpHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.All
    })
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    static SetupDownloader()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("AddinStore.Client");
    }

    public static async Task<ReleaseInfo?> FetchReleaseAsync(string releasePageUrl, string namePrefix, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(releasePageUrl, UriKind.Absolute, out var page) || page.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        var api = SetupAsset.ReleaseApi(page);
        if (api == null)
        {
            return null;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));
        using var request = new HttpRequestMessage(HttpMethod.Get, api);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await Http.SendAsync(request, timeout.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
        return GitHubRelease.TryRead(json, namePrefix, out var info) ? info : null;
    }

    public static async Task<string?> DownloadAsync(
        string releaseOrFileUrl,
        string namePrefix,
        string destinationPath,
        string? sha256,
        CancellationToken cancellationToken)
    {
        var installer = await ResolveAsync(releaseOrFileUrl, namePrefix, cancellationToken).ConfigureAwait(false);
        if (installer == null)
        {
            return null;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        using var response = await Http.GetAsync(installer, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
        await using (var target = File.Create(destinationPath))
        {
            await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
        }

        if (!HasExeHeader(destinationPath))
        {
            File.Delete(destinationPath);
            throw new InvalidOperationException("Link tải không phải file cài .exe.");
        }

        if (!string.IsNullOrWhiteSpace(sha256))
        {
            var actual = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(destinationPath, cancellationToken).ConfigureAwait(false)));
            if (!actual.Equals(sha256.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(destinationPath);
                throw new InvalidOperationException("File tải về không khớp SHA-256 trên store.");
            }
        }

        return destinationPath;
    }

    private static async Task<Uri?> ResolveAsync(string releaseOrFileUrl, string namePrefix, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(releaseOrFileUrl, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        if (SetupAsset.IsDirectExe(url))
        {
            return url;
        }

        var api = SetupAsset.ReleaseApi(url);
        if (api == null)
        {
            return null;
        }

        using var response = await Http.GetAsync(api, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var asset = SetupAsset.FindSetupUrl(json, namePrefix);
        return asset == null ? null : new Uri(asset);
    }

    private static bool HasExeHeader(string path)
    {
        var header = new byte[2];
        using var stream = File.OpenRead(path);
        return stream.Read(header, 0, 2) == 2 && header[0] == (byte)'M' && header[1] == (byte)'Z';
    }
}
