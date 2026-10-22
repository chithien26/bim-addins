using System.Text.Json.Serialization;

namespace AddinStore;

internal sealed class AddinDetailDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("licenseRequired")]
    public bool LicenseRequired { get; set; }

    [JsonPropertyName("latestVersion")]
    public string? LatestVersion { get; set; }

    [JsonPropertyName("versions")]
    public List<VersionDto>? Versions { get; set; }
}

internal sealed class VersionDto
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("githubReleaseUrl")]
    public string? GithubReleaseUrl { get; set; }

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }
}

internal sealed class LicenseCheckDto
{
    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; }
}

internal sealed class LicenseCheckRequest
{
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

internal enum StoreStatus
{
    Ok,
    NotFound,
    Throttled,
    Unavailable
}

internal sealed class StoreResult<T>
{
    public StoreStatus Status { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }

    public static StoreResult<T> Ok(T value) => new() { Status = StoreStatus.Ok, Value = value };
    public static StoreResult<T> Fail(StoreStatus status, string error) => new() { Status = status, Error = error };
}
