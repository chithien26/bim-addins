namespace AddinStore;

internal static class CatalogVersion
{
    public static VersionDto? Latest(AddinDetailDto detail)
    {
        var published = (detail.Versions ?? [])
            .Where(version => !string.IsNullOrWhiteSpace(version.Version))
            .Where(version => string.IsNullOrWhiteSpace(version.Status)
                || string.Equals(version.Status, "PUBLISHED", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (published.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(detail.LatestVersion))
        {
            var exact = published.FirstOrDefault(version =>
                string.Equals(version.Version, detail.LatestVersion, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                return exact;
            }
        }

        return published
            .OrderByDescending(version => version.Version ?? "", Semver.Comparer)
            .First();
    }
}
