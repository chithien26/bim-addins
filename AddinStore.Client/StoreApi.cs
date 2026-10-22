using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AddinStore;

internal sealed class StoreApi
{
    private static readonly HttpClient Http = new(new SocketsHttpHandler
    {
        AutomaticDecompression = DecompressionMethods.All,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    })
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    private readonly string _baseUrl;

    static StoreApi()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("AddinStore.Client");
    }

    public StoreApi(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public Task<StoreResult<AddinDetailDto>> GetAddinAsync(string slug, CancellationToken cancellationToken) =>
        SendAsync<AddinDetailDto>(() =>
        {
            var uri = new Uri($"{_baseUrl}/api/addins/{Uri.EscapeDataString(slug)}");
            return Http.GetAsync(uri, cancellationToken);
        });

    public Task<StoreResult<LicenseCheckDto>> CheckLicenseAsync(StoreIdentity identity, CancellationToken cancellationToken) =>
        SendAsync<LicenseCheckDto>(() =>
        {
            var uri = new Uri($"{_baseUrl}/api/licenses/check");
            var body = new LicenseCheckRequest
            {
                Username = identity.Username,
                Email = identity.Email
            };
            return Http.PostAsJsonAsync(uri, body, StoreJson.Options, cancellationToken);
        });

    private static async Task<StoreResult<T>> SendAsync<T>(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            using var response = await send().ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return StoreResult<T>.Fail(StoreStatus.NotFound, "Không thấy add-in trên store.");
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return StoreResult<T>.Fail(StoreStatus.Throttled, "Store đang giới hạn số lần kiểm tra. Thử lại sau ít phút.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return StoreResult<T>.Fail(StoreStatus.Unavailable, $"Store trả về {(int)response.StatusCode}.");
            }

            var value = await response.Content.ReadFromJsonAsync<T>(StoreJson.Options).ConfigureAwait(false);
            if (value == null)
            {
                return StoreResult<T>.Fail(StoreStatus.Unavailable, "Store trả về dữ liệu trống.");
            }

            return StoreResult<T>.Ok(value);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return StoreResult<T>.Fail(StoreStatus.Unavailable, "Không kết nối được store.");
        }
    }
}
