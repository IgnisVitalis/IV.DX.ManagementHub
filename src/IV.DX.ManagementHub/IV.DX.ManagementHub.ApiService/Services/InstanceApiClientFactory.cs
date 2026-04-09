using IV.DX.ManagementHub.ApiService.Bootstrap;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IV.DX.ManagementHub.ApiService.Services
{
    public sealed class InstanceApiClientFactory(IHttpClientFactory httpClientFactory)
    {
        private readonly ConcurrentDictionary<string, InstanceTokenCache> _tokenCaches =
            new(StringComparer.OrdinalIgnoreCase);

        public Task<HttpClient> CreateFromContextAsync(CancellationToken ct)
        {
            var apiUrl = InstanceApiContext.ApiUrl;
            var serviceKey = InstanceApiContext.ServiceKey;

            if (string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(serviceKey))
                throw new InvalidOperationException("Instance API context is not set for the current request.");

            return CreateAsync(apiUrl, serviceKey, ct);
        }

        public async Task<HttpClient> CreateAsync(string apiUrl, string serviceKey, CancellationToken ct)
        {
            var tokenCache = _tokenCaches.GetOrAdd(apiUrl, _ => new InstanceTokenCache());
            var token = await tokenCache.GetOrRefreshAsync(apiUrl, serviceKey, httpClientFactory, ct);

            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
    }

    internal sealed class InstanceTokenCache
    {
        private readonly SemaphoreSlim _sync = new(1, 1);
        private string? _accessToken;
        private DateTimeOffset _expiresAt;

        private bool IsValid => _accessToken != null && DateTimeOffset.UtcNow < _expiresAt;

        public async Task<string> GetOrRefreshAsync(
            string apiUrl,
            string serviceKey,
            IHttpClientFactory factory,
            CancellationToken ct)
        {
            if (IsValid)
                return _accessToken!;

            await _sync.WaitAsync(ct);
            try
            {
                if (IsValid)
                    return _accessToken!;

                var client = factory.CreateClient();
                var requestBody = JsonSerializer.Serialize(new { ServiceKey = serviceKey });
                using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                var tokenUrl = $"{apiUrl.TrimEnd('/')}/api/service-auth/token";
                using var response = await client.PostAsync(tokenUrl, content, ct);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                // Try both camelCase and PascalCase property names
                var accessToken = TryGetString(root, "accessToken") ?? TryGetString(root, "AccessToken")
                    ?? throw new InvalidOperationException("Missing accessToken in service token response.");

                var expiresAt = TryGetDateTimeOffset(root, "expiresAt") ?? TryGetDateTimeOffset(root, "ExpiresAt")
                    ?? throw new InvalidOperationException("Missing expiresAt in service token response.");

                _accessToken = accessToken;
                _expiresAt = expiresAt - TimeSpan.FromSeconds(30);

                return _accessToken;
            }
            finally
            {
                _sync.Release();
            }
        }

        private static string? TryGetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
                ? prop.GetString()
                : null;
        }

        private static DateTimeOffset? TryGetDateTimeOffset(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return null;

            if (prop.TryGetDateTimeOffset(out var value))
                return value;

            return null;
        }
    }
}
