namespace AddinStore;

internal static class StorePaths
{
    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AddinStore");
}
