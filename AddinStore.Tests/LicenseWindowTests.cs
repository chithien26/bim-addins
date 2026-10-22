namespace AddinStore.Tests;

public class LicenseWindowTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NullExpiryLastsTwelveHours()
    {
        Assert.Equal(Now.AddHours(12), LicenseWindow.Until(null, Now));
    }

    [Fact]
    public void ShortExpiryIsAtLeastFiveMinutes()
    {
        Assert.Equal(Now.AddMinutes(5), LicenseWindow.Until(Now.AddMinutes(1), Now));
    }

    [Fact]
    public void FutureExpiryIsKept()
    {
        var expires = Now.AddDays(30);
        Assert.Equal(expires, LicenseWindow.Until(expires, Now));
    }
}
