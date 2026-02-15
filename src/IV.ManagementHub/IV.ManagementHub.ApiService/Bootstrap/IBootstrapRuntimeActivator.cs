namespace IV.ManagementHub.ApiService.Bootstrap
{
    public interface IBootstrapRuntimeActivator
    {
        Task<BootstrapActivationResult> ActivateAsync(CancellationToken ct = default);
    }

    public sealed record BootstrapActivationResult(bool IsSuccess, string? Message = null);
}
