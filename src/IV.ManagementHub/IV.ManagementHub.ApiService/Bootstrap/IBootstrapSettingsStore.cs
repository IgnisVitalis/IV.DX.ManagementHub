namespace IV.ManagementHub.ApiService.Bootstrap
{
    public interface IBootstrapSettingsStore
    {
        Task<BootstrapSettings?> LoadAsync(CancellationToken ct = default);

        Task SaveAsync(BootstrapSettings settings, CancellationToken ct = default);
    }
}
