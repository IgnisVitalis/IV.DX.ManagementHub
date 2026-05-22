using IV.DX.ManagementHub.ApiService.Services;
using System.Net.Http.Headers;

namespace IV.DX.ManagementHub.Web.Services
{
    public interface IInstanceClientProvider
    {
        Task<HttpClient> GetClientAsync(CancellationToken ct = default);
        string GetCollectionUri(string typeName, string? filter = null);
        string GetItemUri(string typeName, Guid id);
        string GetCreateUri(string typeName);
        string GetUpdateUri(string typeName, Guid id);
        string GetDeleteUri(string typeName, Guid id);
        string GetByIdsUri(string typeName);
        string GetSearchUri(string typeName);
        string GetUnitStructureUri(string typeName);
        string GetQueryResultUri(Guid dxQueryId, Guid? dxFilterId = null);
        string GetByDefinitionUri(Guid definitionId);
        string GetByDefinitionItemUri(Guid definitionId, Guid id);
        string GetByDefinitionByIdsUri(Guid definitionId);
    }

    internal sealed class DirectInstanceClientProvider(
        InstanceApiClientFactory factory,
        string apiUrl,
        string serviceKey) : IInstanceClientProvider
    {
        public Task<HttpClient> GetClientAsync(CancellationToken ct = default) =>
            factory.CreateAsync(apiUrl, serviceKey, ct);

        public string GetCollectionUri(string typeName, string? filter = null) =>
            BuildCollectionUri($"api/management/{typeName}", filter);

        public string GetItemUri(string typeName, Guid id) =>
            $"api/management/{typeName}/{id}";

        public string GetCreateUri(string typeName) =>
            $"api/management/{typeName}";

        public string GetUpdateUri(string typeName, Guid id) =>
            $"api/management/{typeName}/{id}";

        public string GetDeleteUri(string typeName, Guid id) =>
            $"api/management/{typeName}/{id}";

        public string GetByIdsUri(string typeName) =>
            $"api/management/{typeName}/by-ids";

        public string GetSearchUri(string typeName) =>
            $"api/management/{typeName}/search";

        public string GetUnitStructureUri(string typeName) =>
            $"api/management/unit-structure/{typeName}";

        public string GetQueryResultUri(Guid dxQueryId, Guid? dxFilterId = null) =>
            dxFilterId.HasValue
                ? $"api/management/query-result/{dxQueryId}/{dxFilterId.Value}"
                : $"api/management/query-result/{dxQueryId}";

        public string GetByDefinitionUri(Guid definitionId) =>
            $"api/management/{definitionId}";

        public string GetByDefinitionItemUri(Guid definitionId, Guid id) =>
            $"api/management/{definitionId}/{id}";

        public string GetByDefinitionByIdsUri(Guid definitionId) =>
            $"api/management/{definitionId}/by-ids";

        private static string BuildCollectionUri(string baseUri, string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return baseUri;

            return $"{baseUri}?filter={Uri.EscapeDataString(filter)}";
        }
    }

    internal sealed class ProxyInstanceClientProvider(
        IHttpClientFactory httpClientFactory,
        AppAuthState authState,
        string apiUrl,
        string instanceKey) : IInstanceClientProvider
    {
        public Task<HttpClient> GetClientAsync(CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(authState.AccessToken))
                throw new InvalidOperationException("Cannot proxy instance requests without an authenticated access token.");

            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", authState.AccessToken);
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-MH-Instance", instanceKey);

            return Task.FromResult(client);
        }

        public string GetCollectionUri(string typeName, string? filter = null) =>
            BuildCollectionUri($"api/{typeName}", filter);

        public string GetItemUri(string typeName, Guid id) =>
            $"api/{typeName}/{id}";

        public string GetCreateUri(string typeName) =>
            $"api/{typeName}";

        public string GetUpdateUri(string typeName, Guid id) =>
            $"api/{typeName}/{id}";

        public string GetDeleteUri(string typeName, Guid id) =>
            $"api/{typeName}/{id}";

        public string GetByIdsUri(string typeName) =>
            $"api/{typeName}/by-ids";

        public string GetSearchUri(string typeName) =>
            $"api/{typeName}/search";

        public string GetUnitStructureUri(string typeName) =>
            $"api/DXUnitStructure/{typeName}";

        public string GetQueryResultUri(Guid dxQueryId, Guid? dxFilterId = null) =>
            dxFilterId.HasValue
                ? $"api/DXQueryResult/{dxQueryId}/{dxFilterId.Value}"
                : $"api/DXQueryResult/{dxQueryId}";

        public string GetByDefinitionUri(Guid definitionId) =>
            $"api/{definitionId}";

        public string GetByDefinitionItemUri(Guid definitionId, Guid id) =>
            $"api/{definitionId}/{id}";

        public string GetByDefinitionByIdsUri(Guid definitionId) =>
            $"api/{definitionId}/by-ids";

        private static string BuildCollectionUri(string baseUri, string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return baseUri;

            return $"{baseUri}?filter={Uri.EscapeDataString(filter)}";
        }
    }
}
