namespace AddinStore.Tests;

public class GitHubReleaseTests
{
    [Fact]
    public void ReadsTheTagAndTheSetupForThisRevitYear()
    {
        const string json = """
            {
              "tag_name": "v1.0.1",
              "name": "Site Designer 1.0.1",
              "html_url": "https://github.com/chithien26/revit-site-designer/releases/tag/v1.0.1",
              "assets": [
                {"browser_download_url": "https://github.com/chithien26/revit-site-designer/releases/download/v1.0.1/SiteDesigner-2024-Setup-1.0.1.exe"},
                {"browser_download_url": "https://github.com/chithien26/revit-site-designer/releases/download/v1.0.1/SiteDesigner-2025-Setup-1.0.1.exe"}
              ]
            }
            """;

        Assert.True(GitHubRelease.TryRead(json, "SiteDesigner-2025-Setup", out var release));
        Assert.Equal(new Version(1, 0, 1), release.Version);
        Assert.Equal("Site Designer 1.0.1", release.Name);
        Assert.Equal(
            "https://github.com/chithien26/revit-site-designer/releases/download/v1.0.1/SiteDesigner-2025-Setup-1.0.1.exe",
            release.SetupUrl);
    }

    [Fact]
    public void ReadsAPerAddinTag()
    {
        const string json = """
            { "tag_name": "sheet-sync-v2.2.0", "name": "Sheet Sync 2.2.0", "html_url": "https://github.com/acme/bim-addins/releases/tag/sheet-sync-v2.2.0" }
            """;

        Assert.True(GitHubRelease.TryRead(json, "SheetSync-2025-Setup", out var release));
        Assert.Equal(new Version(2, 2, 0), release.Version);
        Assert.Null(release.SetupUrl);
    }
}
