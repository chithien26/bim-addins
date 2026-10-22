using System.Xml.Linq;
using Autodesk.Revit.UI;

namespace AddinStore;

internal static class AutodeskIdentity
{
    public static StoreIdentity Resolve(UIApplication application)
    {
        var username = application.Application?.Username;
        var email = ReadLoginEmail();
        if (string.IsNullOrWhiteSpace(email) && LooksLikeEmail(username))
        {
            email = username;
        }

        return new StoreIdentity
        {
            Username = Blank(username),
            Email = Blank(email)
        };
    }

    private static string? ReadLoginEmail()
    {
        string[] folders =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Autodesk", "Web Services"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Autodesk", "Web Services")
        ];
        foreach (var folder in folders)
        {
            var path = Path.Combine(folder, "LoginState.xml");
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var xml = XDocument.Load(path);
                foreach (var node in xml.Descendants())
                {
                    var value = node.Value?.Trim();
                    if (LooksLikeEmail(value) && value!.IndexOf('\n') < 0)
                    {
                        return value;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        return null;
    }

    private static bool LooksLikeEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.IndexOf('@') > 0;

    private static string? Blank(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}

internal sealed class StoreIdentity
{
    public string? Username { get; set; }
    public string? Email { get; set; }

    public bool HasAny =>
        !string.IsNullOrWhiteSpace(Username) || !string.IsNullOrWhiteSpace(Email);
}
