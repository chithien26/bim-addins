using System.Text.Json;

namespace AddinStore.Tests;

public class StoreJsonTests
{
    [Fact]
    public void LicenseRequestKeepsTheStoreFieldNames()
    {
        var json = JsonSerializer.Serialize(
            new LicenseCheckRequest { Username = "a", Email = "b@c" },
            StoreJson.Options);

        Assert.Contains("\"username\":\"a\"", json);
        Assert.Contains("\"email\":\"b@c\"", json);
    }

    [Fact]
    public void SettingsFileKeepsTheStoreFieldNames()
    {
        const string json = """{"apiBaseUrl":"http://localhost:8080","webBaseUrl":"http://localhost:5173"}""";
        var settings = JsonSerializer.Deserialize<StoreSettings>(json, StoreJson.Options);

        Assert.NotNull(settings);
        Assert.Equal("http://localhost:8080", settings.ApiBaseUrl);
        Assert.Equal("http://localhost:5173", settings.WebBaseUrl);
    }
}
