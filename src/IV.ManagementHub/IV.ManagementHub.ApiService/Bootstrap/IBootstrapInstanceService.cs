namespace IV.ManagementHub.ApiService.Bootstrap
{
    public interface IBootstrapInstanceService
    {
        Task<IReadOnlyList<BootstrapInstanceDescriptor>> GetInstancesAsync(CancellationToken ct = default);

        Task<BootstrapCreateInstanceResult> CreateInstanceAsync(BootstrapCreateInstanceRequest request, CancellationToken ct = default);
    }

    public sealed record BootstrapInstanceDescriptor(
        string Key,
        string Title,
        string DatabaseType,
        DateTimeOffset CreatedAtUtc);

    public sealed record BootstrapCreateInstanceRequest(
        string Key,
        string Title,
        string DatabaseType,
        string ConnectionString);

    public sealed record BootstrapCreateInstanceResult(
        BootstrapCreateInstanceStatus Status,
        BootstrapInstanceDescriptor? Instance = null,
        string? Message = null);

    public enum BootstrapCreateInstanceStatus
    {
        Created,
        ValidationError,
        SetupNotCompleted,
        Conflict,
        ActivationFailed
    }
}
