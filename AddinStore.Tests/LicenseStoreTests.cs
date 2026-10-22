namespace AddinStore.Tests;

public class LicenseStoreTests
{
    [Fact]
    public void UsesCacheUntilDeadline()
    {
        var store = new LicenseStore(NewFolder());
        var now = new DateTimeOffset(2026, 10, 22, 0, 0, 0, TimeSpan.Zero);
        store.SaveUntil(now.AddHours(12));

        Assert.True(store.TryUse(now.AddHours(11)));
        Assert.False(store.TryUse(now.AddHours(12)));
    }

    [Fact]
    public void MissingFileIsNotUsable()
    {
        var store = new LicenseStore(NewFolder());
        Assert.False(store.TryUse(DateTimeOffset.Now));
    }

    private static string NewFolder() => Path.Combine(Path.GetTempPath(), "addin-store-tests", Guid.NewGuid().ToString("N"));
}
