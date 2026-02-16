using Microsoft.JSInterop;
using Newtonsoft.Json;
using System.Text;

namespace IV.ManagementHub.Web.ApiClients
{
    internal sealed class DXInstancesApiClient(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        public async Task<IReadOnlyList<DXInstanceDto>> GetItemsAsync(CancellationToken cancellationToken = default)
        {
            using var response = await httpClient.GetAsync("api/v1.0/instances", cancellationToken);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonConvert.DeserializeObject<List<DXInstanceDto>>(body) ?? [];
        }

        public async Task<CreateInstanceResult> CreateAsync(CreateInstanceRequest request, CancellationToken cancellationToken = default)
        {
            var payload = JsonConvert.SerializeObject(request);
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await httpClient.PostAsync("api/v1.0/instances", content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            var payloadResponse = JsonConvert.DeserializeObject<CreateInstanceResponse>(responseBody);
            if (response.IsSuccessStatusCode && payloadResponse?.Instance is not null)
            {
                return CreateInstanceResult.Success(payloadResponse.Instance, payloadResponse.Message ?? "Instance created.");
            }

            return CreateInstanceResult.Fail(payloadResponse?.Message ?? $"Failed to create instance ({(int)response.StatusCode}).");
        }
    }

    internal sealed record DXInstanceDto
    {
        [JsonProperty("key")]
        public string Key { get; init; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; init; } = string.Empty;

        [JsonProperty("databaseType")]
        public string DatabaseType { get; init; } = string.Empty;

        [JsonProperty("createdAtUtc")]
        public DateTimeOffset CreatedAtUtc { get; init; }
    }

    internal sealed record CreateInstanceRequest(
        [property: JsonProperty("key")] string Key,
        [property: JsonProperty("title")] string Title,
        [property: JsonProperty("databaseType")] string DatabaseType,
        [property: JsonProperty("connectionString")] string ConnectionString);

    internal sealed record CreateInstanceResult(bool IsSuccess, DXInstanceDto? Instance, string? Message, string? Error)
    {
        public static CreateInstanceResult Success(DXInstanceDto instance, string message) =>
            new(true, instance, message, null);

        public static CreateInstanceResult Fail(string error) =>
            new(false, null, null, error);
    }

    internal sealed class CreateInstanceResponse
    {
        [JsonProperty("message")]
        public string? Message { get; init; }

        [JsonProperty("instance")]
        public DXInstanceDto? Instance { get; init; }
    }
}
