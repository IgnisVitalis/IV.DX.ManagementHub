using IV.ManagementHub.ApiService.Services;

namespace IV.ManagementHub.Web.Services
{
    public interface IInstanceClientProvider
    {
        Task<HttpClient> GetClientAsync(CancellationToken ct = default);
    }

    internal sealed class InstanceClientProvider(
        InstanceApiClientFactory factory,
        string apiUrl,
        string serviceKey) : IInstanceClientProvider
    {
        public Task<HttpClient> GetClientAsync(CancellationToken ct = default) =>
            factory.CreateAsync(apiUrl, serviceKey, ct);
    }
}
