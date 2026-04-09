using IV.DataProvider.WebApp.Services.Web.Contracts;

namespace IV.DX.ManagementHub.Web.Services
{
    public sealed class AppState(IApiClientResolver resolver)
    {
        public record AppInfo(string Key, string Title);

        public async Task<IReadOnlyList<AppInfo>> GetAppsAsync(CancellationToken ct = default)
        {
            var units = await resolver.GetInstancesAsync(ct);
            return units
                .Where(u => !string.IsNullOrWhiteSpace(u.Key))
                .Select(u => new AppInfo(u.Key, u.Title))
                .ToList();
        }

    }
}
