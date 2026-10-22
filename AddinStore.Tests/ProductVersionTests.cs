namespace AddinStore.Tests;

public class ProductVersionTests
{
    [Theory]
    [InlineData("v1.0.1", "1.0.1")]
    [InlineData("1.0.1+commit", "1.0.1")]
    [InlineData("1.0.1-beta", "1.0.1")]
    [InlineData("sheet-sync-v2.1.0", "2.1.0")]
    public void StripsPrefixAndSuffix(string text, string expected)
    {
        Assert.True(ProductVersion.TryParse(text, out var version));
        Assert.Equal(Version.Parse(expected), version);
    }

    [Fact]
    public void OlderLocalVersionIsLessThanRemote()
    {
        Assert.True(ProductVersion.TryParse("1.0.0", out var local));
        Assert.True(ProductVersion.TryParse("1.0.1", out var remote));
        Assert.True(remote > local);
        Assert.False(remote <= local);
    }
}
