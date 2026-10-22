# BIM Add-ins

Nhiều add-in Revit trong một repo. Mỗi add-in một version, một bộ cài, một GitHub Release. License và version nằm ở `AddinStore.Client`: lệnh chỉ gọi `StoreClient.Open`.

Store: `https://addin-store.onrender.com`. Máy dev trỏ local bằng cách copy `build/settings.example.json` thành `%AppData%\AddinStore\settings.json`, hoặc đặt biến `ADDIN_STORE_URL`.

Add-in build `net10.0-windows` vì Revit 2025 trên máy này nạp runtime đó, cùng kiểu với Site Designer. API Revit lấy từ gói NuGet 2025, không copy `RevitAPI.dll` vào bộ cài.

## Lệnh

```csharp
if (!StoreClient.Open("sheet-sync", commandData))
    return Result.Cancelled;
```

`Open` làm đúng thứ tự Site Designer đang dùng.

1. `GET /api/addins/{slug}`. Add-in miễn phí bỏ qua bước license.
2. Add-in trả phí lấy username của Revit và email trong `LoginState.xml` của Autodesk, rồi `POST /api/licenses/check`. Không hỏi người dùng gõ lại. Một license còn hạn mở mọi add-in trả phí. Cache đến `expiresAt`; không có ngày hết hạn thì 12 giờ, và không ngắn hơn 5 phút. Không kiểm tra được, hoặc không có license, thì chặn lệnh.
3. Mỗi phiên Revit lấy `githubReleaseUrl` của đúng add-in, đọc `tag_name` trên GitHub, rồi so với version DLL. Bản cũ hơn thì hỏi tiếp tục, hoặc lưu file, tải `{TênAssembly}-{năm Revit}-Setup.exe`, chờ bạn tắt Revit rồi cài và mở lại dự án. Lệnh vừa bấm vẫn chạy sau khi chọn cập nhật. GitHub lỗi thì im, không chặn lệnh.

Link trên store là trang release của đúng add-in, hoặc file exe. Không gọi `releases/latest` của cả repo.

## Thêm add-in

1. Copy `src/SheetSync`, đổi slug, `<Version>`, GUID trong csproj và `addins.json`.
2. `dotnet sln add src/<Tên>/<Tên>.csproj`
3. Build Debug: file `.addin` được ghi vào `%AppData%\Autodesk\Revit\Addins\2025\`. Mở lại Revit.

Version trong csproj là version so với store. Đang bằng `latestVersion` thì không hiện hộp thoại cập nhật.

## Release riêng

Tag đúng version trong csproj, ví dụ `sheet-sync-v2.1.0` hoặc `rebar-mark-v1.4.0`. Workflow tạo `SheetSync-2025-Setup-2.1.0.exe` và một GitHub Release cho riêng tag đó. Dán `githubReleaseUrl` và `sha256` trong log vào version trên store.

Repo phải public thì máy khách tải được file release. Cài [Inno Setup 6](https://jrsoftware.org/isdl.php) nếu chạy `build/stage-release.ps1` trên máy.
