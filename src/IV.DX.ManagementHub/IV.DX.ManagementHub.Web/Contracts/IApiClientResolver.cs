using IV.DX.ManagementHub.Common.Models;

namespace IV.DataProvider.WebApp.Services.Web.Contracts
{
    public interface IApiClientResolver
    {
        Task<T> GetAsync<T>(string? instanceKey) where T : class;
        Task<IReadOnlyList<MHInstanceUnit>> GetInstancesAsync(CancellationToken ct = default);
    }
}
