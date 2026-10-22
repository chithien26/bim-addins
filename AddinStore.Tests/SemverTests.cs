namespace AddinStore.Tests;

public class SemverTests
{
    [Theory]
    [InlineData("1.0.0", "1.0.1", -1)]
    [InlineData("1.2.0", "1.2", 0)]
    [InlineData("1.2.0", "1.2.0-beta", 0)]
    [InlineData("2.0.0", "1.9.9", 1)]
    [InlineData("1.10.0", "1.9.0", 1)]
    [InlineData("1.2.0+commit", "1.2.0", 0)]
    public void CompareMatchesStore(string left, string right, int expected)
    {
        var actual = Math.Sign(Semver.Compare(left, right));
        Assert.Equal(expected, actual);
    }
}
