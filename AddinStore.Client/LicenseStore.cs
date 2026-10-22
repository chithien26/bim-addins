using System.Text.Json;
using System.Text.Json.Serialization;

namespace AddinStore;

internal sealed class LicenseStore
{
    private readonly string _path;

    public LicenseStore(string folder)
    {
        _path = Path.Combine(folder, "license.json");
    }

    public bool TryUse(DateTimeOffset now)
    {
        var cache = Read();
        if (cache is not { Valid: true })
        {
            return false;
        }

        return cache.ExpiresAt is DateTimeOffset expires && now < expires;
    }

    public void SaveUntil(DateTimeOffset until)
    {
        Write(new LicenseFile
        {
            Valid = true,
            ExpiresAt = until,
            CheckedAt = DateTimeOffset.Now
        });
    }

    public void Clear()
    {
        StoreLock.Run(() =>
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        });
    }

    private LicenseFile? Read()
    {
        LicenseFile? cache = null;
        StoreLock.Run(() =>
        {
            if (!File.Exists(_path))
            {
                return;
            }

            try
            {
                cache = JsonSerializer.Deserialize<LicenseFile>(File.ReadAllText(_path), StoreJson.Options);
            }
            catch (JsonException)
            {
                cache = null;
            }
        });
        return cache;
    }

    private void Write(LicenseFile cache)
    {
        StoreLock.Run(() =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var temp = _path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(cache, StoreJson.Options));
            File.Move(temp, _path, overwrite: true);
        });
    }

    private sealed class LicenseFile
    {
        [JsonPropertyName("valid")]
        public bool Valid { get; set; }

        [JsonPropertyName("expiresAt")]
        public DateTimeOffset? ExpiresAt { get; set; }

        [JsonPropertyName("checkedAt")]
        public DateTimeOffset CheckedAt { get; set; }
    }
}
