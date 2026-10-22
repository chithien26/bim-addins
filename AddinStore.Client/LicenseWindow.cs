namespace AddinStore;

internal static class LicenseWindow
{
    public static DateTimeOffset Until(DateTimeOffset? expiresAt, DateTimeOffset now)
    {
        var until = expiresAt ?? now.AddHours(12);
        var minimum = now.AddMinutes(5);
        return until < minimum ? minimum : until;
    }
}
