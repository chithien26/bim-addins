using System.Diagnostics;
using System.Reflection;
using Autodesk.Revit.UI;

namespace AddinStore;

public static class StoreClient
{
    private static readonly StoreSettings Settings = StoreSettings.Load();
    private static readonly StoreApi Api = new(Settings.ApiBaseUrl);
    private static readonly LicenseStore Licenses = new(StorePaths.Root);
    private static readonly Dictionary<string, AddinDetailDto> Catalog = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> VersionChecked = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// License trước nếu add-in yêu cầu, rồi kiểm tra version một lần mỗi phiên Revit.
    /// Trả về false khi lệnh phải dừng.
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static bool Open(string slug, ExternalCommandData command)
    {
        try
        {
            return OpenCore(slug, command);
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Addin Store", ex.Message);
            return false;
        }
    }

    private static bool OpenCore(string slug, ExternalCommandData command)
    {
        var assembly = CallingAddin();
        var catalog = LoadCatalog(slug);
        if (catalog.Status == StoreStatus.NotFound)
        {
            TaskDialog.Show("Addin Store", $"Không thấy add-in \"{slug}\" trên store.");
            return false;
        }

        if (catalog.Status != StoreStatus.Ok || catalog.Value == null)
        {
            return true;
        }

        var detail = catalog.Value;
        var name = string.IsNullOrWhiteSpace(detail.Name) ? slug : detail.Name!;
        if (detail.LicenseRequired && !EnsureLicense(name, command))
        {
            return false;
        }

        try
        {
            CheckVersion(slug, name, detail, command, assembly);
        }
        catch (Exception)
        {
        }

        return true;
    }

    private static bool EnsureLicense(string name, ExternalCommandData command)
    {
        if (Licenses.TryUse(DateTimeOffset.Now))
        {
            return true;
        }

        var identity = AutodeskIdentity.Resolve(command.Application);
        if (!identity.HasAny)
        {
            Deny(name, "Không lấy được tài khoản đang đăng nhập trong Revit. Liên hệ admin để được cấp.");
            return false;
        }

        var check = Call(token => Api.CheckLicenseAsync(identity, token));
        if (check.Status != StoreStatus.Ok || check.Value == null)
        {
            Deny(name, "Không kiểm tra được license. Kiểm tra mạng hoặc liên hệ admin để được cấp.");
            return false;
        }

        if (check.Value.Valid)
        {
            Licenses.SaveUntil(LicenseWindow.Until(check.Value.ExpiresAt, DateTimeOffset.Now));
            return true;
        }

        var account = !string.IsNullOrWhiteSpace(identity.Email) ? identity.Email : identity.Username;
        Deny(name, "Chưa có license. Liên hệ admin để được cấp.\n\nTài khoản: " + account);
        return false;
    }

    private static void Deny(string name, string message)
    {
        var dialog = new TaskDialog(name)
        {
            TitleAutoPrefix = false,
            MainIcon = TaskDialogIcon.TaskDialogIconWarning,
            MainInstruction = "Chưa có license",
            MainContent = message,
            CommonButtons = TaskDialogCommonButtons.Close
        };
        dialog.Show();
    }

    private static void CheckVersion(
        string slug,
        string name,
        AddinDetailDto detail,
        ExternalCommandData command,
        Assembly assembly)
    {
        if (VersionChecked.Contains(slug))
        {
            return;
        }

        if (!ProductVersion.TryParse(ReadVersion(assembly), out var local))
        {
            return;
        }

        VersionChecked.Add(slug);
        var latest = CatalogVersion.Latest(detail);
        var releaseUrl = latest?.GithubReleaseUrl;
        if (string.IsNullOrWhiteSpace(releaseUrl))
        {
            return;
        }

        var year = command.Application.Application?.VersionNumber;
        if (string.IsNullOrWhiteSpace(year))
        {
            year = "2025";
        }

        var prefix = assembly.GetName().Name + "-" + year + "-Setup";
        var release = ResolveRelease(releaseUrl, prefix, latest, detail);
        if (release == null || release.Version <= local)
        {
            return;
        }

        var releaseName = string.IsNullOrEmpty(release.Name) ? "" : " (" + release.Name + ")";
        var dialog = new TaskDialog(name)
        {
            TitleAutoPrefix = false,
            MainIcon = TaskDialogIcon.TaskDialogIconInformation,
            MainInstruction = "Đã có phiên bản mới " + release.Version,
            MainContent = "Bản đang dùng: " + local + "\nBản trên GitHub: " + release.Version
                + releaseName
                + "\n\nCài đặt cần đóng Revit vì file add-in đang bị khóa."
        };
        dialog.AddCommandLink(
            TaskDialogCommandLinkId.CommandLink1,
            "Lưu dự án, tắt Revit và cập nhật",
            "Lưu file đang mở, tải bộ cài. Sau khi bạn đóng Revit, trình cài chạy rồi mở Revit lại.");
        dialog.AddCommandLink(
            TaskDialogCommandLinkId.CommandLink2,
            "Tiếp tục sử dụng, cập nhật sau",
            "Giữ phiên làm việc hiện tại. Có thể kiểm tra lại ở About.");
        dialog.CommonButtons = TaskDialogCommonButtons.None;
        dialog.AllowCancellation = true;
        if (dialog.Show() == TaskDialogResult.CommandLink1)
        {
            BeginUpdate(name, prefix, release, command, assembly);
        }
    }

    private static ReleaseInfo? ResolveRelease(string releaseUrl, string namePrefix, VersionDto? latest, AddinDetailDto detail)
    {
        if (Uri.TryCreate(releaseUrl, UriKind.Absolute, out var page) && SetupAsset.IsDirectExe(page))
        {
            var number = latest?.Version ?? detail.LatestVersion;
            return ProductVersion.TryParse(number, out var remote)
                ? new ReleaseInfo(remote, null, releaseUrl, releaseUrl)
                : null;
        }

        try
        {
            return Call(token => SetupDownloader.FetchReleaseAsync(releaseUrl, namePrefix, token));
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void BeginUpdate(
        string name,
        string prefix,
        ReleaseInfo release,
        ExternalCommandData command,
        Assembly assembly)
    {
        var plan = DocumentPlan.Capture(command.Application);
        var unsaved = plan.Unsaved.Count;
        foreach (var document in plan.ToSave)
        {
            try
            {
                document.Save();
            }
            catch (Exception)
            {
                unsaved++;
            }
        }

        if (unsaved > 0)
        {
            Notify(
                name,
                "Có " + unsaved + " tài liệu chưa lưu được (file mới chưa đặt tên, workshared, hoặc read-only). Hãy Save As trước khi đóng Revit.",
                warning: true);
        }

        var page = string.IsNullOrWhiteSpace(release.HtmlUrl) ? release.SetupUrl : release.HtmlUrl;
        if (string.IsNullOrWhiteSpace(release.SetupUrl))
        {
            OpenUrl(page);
            Notify(name, "Không tìm thấy file cài " + prefix + ".exe trong release. Đã mở trang GitHub.", warning: true);
            return;
        }

        var setupPath = Path.Combine(Path.GetTempPath(), prefix + ".exe");
        string? downloaded;
        try
        {
            downloaded = Call(token => SetupDownloader.DownloadAsync(release.SetupUrl, prefix, setupPath, null, token));
        }
        catch (Exception ex)
        {
            OpenUrl(page);
            Notify(name, "Không tải được bộ cài. Đã mở trang GitHub để tải tay.\n\n" + ex.Message, warning: true);
            return;
        }

        if (downloaded == null)
        {
            OpenUrl(page);
            Notify(name, "Không tìm thấy file cài " + prefix + ".exe trong release. Đã mở trang GitHub.", warning: true);
            return;
        }

        string? revitPath = null;
        try
        {
            revitPath = Process.GetCurrentProcess().MainModule?.FileName;
        }
        catch (Exception)
        {
        }

        UpdateLauncher.Start(assembly.GetName().Name ?? prefix, Process.GetCurrentProcess().Id, downloaded, revitPath, plan.Reopen);
        Notify(
            name,
            "Đã tải bản " + release.Version + ".\n\nHãy đóng Revit. Trình cài sẽ chạy khi Revit tắt, sau đó mở lại dự án.",
            warning: false);
    }

    private static void Notify(string title, string message, bool warning)
    {
        var dialog = new TaskDialog(title)
        {
            TitleAutoPrefix = false,
            MainIcon = warning ? TaskDialogIcon.TaskDialogIconWarning : TaskDialogIcon.TaskDialogIconNone,
            MainInstruction = message,
            AllowCancellation = true,
            CommonButtons = TaskDialogCommonButtons.Ok
        };
        dialog.Show();
    }

    private static void OpenUrl(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception)
        {
        }
    }

    private static StoreResult<AddinDetailDto> LoadCatalog(string slug)
    {
        if (Catalog.TryGetValue(slug, out var cached))
        {
            return StoreResult<AddinDetailDto>.Ok(cached);
        }

        var result = Call(token => Api.GetAddinAsync(slug, token));
        if (result.Status == StoreStatus.Ok && result.Value != null)
        {
            Catalog[slug] = result.Value;
        }

        return result;
    }

    private static Assembly CallingAddin()
    {
        var own = typeof(StoreClient).Assembly;
        foreach (var frame in new StackTrace().GetFrames() ?? [])
        {
            var assembly = frame.GetMethod()?.DeclaringType?.Assembly;
            if (assembly == null || assembly == own)
            {
                continue;
            }

            var assemblyName = assembly.GetName().Name ?? "";
            if (assemblyName.StartsWith("System", StringComparison.Ordinal)
                || assemblyName.StartsWith("Microsoft", StringComparison.Ordinal)
                || assemblyName.StartsWith("Autodesk", StringComparison.Ordinal))
            {
                continue;
            }

            return assembly;
        }

        return own;
    }

    internal static string ReadVersion(Assembly assembly)
    {
        var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            var plus = info.IndexOf('+', StringComparison.Ordinal);
            return plus >= 0 ? info[..plus].Trim() : info.Trim();
        }

        var file = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        if (!string.IsNullOrWhiteSpace(file))
        {
            return file;
        }

        return assembly.GetName().Version?.ToString() ?? "0.0.0";
    }

    private static T Call<T>(Func<CancellationToken, Task<T>> work) =>
        Task.Run(() => work(CancellationToken.None)).GetAwaiter().GetResult();
}
