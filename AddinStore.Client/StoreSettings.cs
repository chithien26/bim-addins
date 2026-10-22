using System.Text.Json;

namespace AddinStore;

internal sealed class StoreSettings
{
    public const string DefaultApi = "https://addin-store.onrender.com";

    public string ApiBaseUrl { get; set; } = DefaultApi;
    public string? WebBaseUrl { get; set; }

    public string Web => string.IsNullOrWhiteSpace(WebBaseUrl) ? ApiBaseUrl : WebBaseUrl.TrimEnd('/');

    public static StoreSettings Load()
    {
        var settings = new StoreSettings();
        var path = Path.Combine(StorePaths.Root, "settings.json");
        if (File.Exists(path))
        {
            try
            {
                var loaded = JsonSerializer.Deserialize<StoreSettings>(File.ReadAllText(path), StoreJson.Options);
                if (loaded != null && !string.IsNullOrWhiteSpace(loaded.ApiBaseUrl))
                {
                    settings = loaded;
                }
            }
            catch (JsonException)
            {
            }
        }

        var env = Environment.GetEnvironmentVariable("ADDIN_STORE_URL");
        if (!string.IsNullOrWhiteSpace(env))
        {
            settings.ApiBaseUrl = env.Trim();
        }

        settings.ApiBaseUrl = settings.ApiBaseUrl.TrimEnd('/');
        return settings;
    }
}
