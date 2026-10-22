namespace AddinStore.Tests;

public class SetupAssetTests
{
    [Fact]
    public void DirectExeSkipsTheGithubApi()
    {
        var url = new Uri("https://github.com/chithien26/bim-addins/releases/download/sheet-sync-v2.1.0/SheetSync-2025-Setup-2.1.0.exe");
        Assert.True(SetupAsset.IsDirectExe(url));
    }

    [Fact]
    public void TagPageBecomesTheReleaseApi()
    {
        var url = new Uri("https://github.com/chithien26/revit-site-designer/releases/tag/v1.0.1");
        Assert.Equal(
            "https://api.github.com/repos/chithien26/revit-site-designer/releases/tags/v1.0.1",
            SetupAsset.ReleaseApi(url)?.AbsoluteUri);
    }

    [Fact]
    public void PicksTheSetupForThisAddinAndRevitYear()
    {
        const string json = """
            {
              "assets": [
                {"browser_download_url": "https://github.com/acme/bim-addins/releases/download/v1/RebarMark-2025-Setup-1.4.0.exe"},
                {"browser_download_url": "https://github.com/acme/bim-addins/releases/download/v1/SheetSync-2025-Setup-2.1.0.exe"}
              ]
            }
            """;

        Assert.Equal(
            "https://github.com/acme/bim-addins/releases/download/v1/SheetSync-2025-Setup-2.1.0.exe",
            SetupAsset.FindSetupUrl(json, "SheetSync-2025-Setup"));
    }
}
