namespace AddinStore;

/// <summary>
/// Cùng quy tắc với <c>com.addinstore.support.Semver</c>: chỉ so ba số, bỏ hậu tố sau dấu trừ.
/// </summary>
internal static class Semver
{
    public static readonly IComparer<string> Comparer = Comparer<string>.Create(Compare);

    public static int Compare(string? left, string? right)
    {
        var a = Parts(left);
        var b = Parts(right);
        for (var i = 0; i < 3; i++)
        {
            var diff = a[i].CompareTo(b[i]);
            if (diff != 0)
            {
                return diff;
            }
        }

        return 0;
    }

    private static int[] Parts(string? version)
    {
        var core = string.IsNullOrWhiteSpace(version) ? "" : version.Split('-', 2)[0];
        var plus = core.IndexOf('+', StringComparison.Ordinal);
        if (plus >= 0)
        {
            core = core[..plus];
        }

        var raw = core.Split('.');
        var parts = new int[3];
        for (var i = 0; i < 3; i++)
        {
            parts[i] = i < raw.Length ? Parse(raw[i]) : 0;
        }

        return parts;
    }

    private static int Parse(string token)
    {
        var length = 0;
        while (length < token.Length && char.IsDigit(token[length]))
        {
            length++;
        }

        if (length == 0 || !int.TryParse(token[..length], out var value))
        {
            return 0;
        }

        return value;
    }
}
