namespace AddinStore;

internal sealed class AddinDetailDto
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public bool LicenseRequired { get; set; }
    public string? LatestVersion { get; set; }
    public List<VersionDto>? Versions { get; set; }
}

internal sealed class VersionDto
{
    public string? Version { get; set; }
    public string? Status { get; set; }
    public string? GithubReleaseUrl { get; set; }
    public string? Sha256 { get; set; }
}

internal sealed class LicenseCheckDto
{
    public bool Valid { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
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
