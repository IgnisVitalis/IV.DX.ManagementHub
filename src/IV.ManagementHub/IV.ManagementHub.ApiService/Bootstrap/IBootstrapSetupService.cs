namespace IV.ManagementHub.ApiService.Bootstrap
{
    public interface IBootstrapSetupService
    {
        Task<BootstrapSetupStatus> GetStatusAsync(CancellationToken ct = default);

        Task<BootstrapCompleteResult> CompleteSetupAsync(BootstrapCompleteRequest request, CancellationToken ct = default);

        Task<BootstrapAuthValidationResult> ValidateCredentialsAsync(string userName, string password, CancellationToken ct = default);
    }

    public sealed record BootstrapSetupStatus(bool RequiresSetup, bool RequiresRestart, bool RuntimeReady, bool HasInstances);

    public sealed record BootstrapCompleteRequest(
        string UserName,
        string Password);

    public sealed record BootstrapCompleteResult(BootstrapCompleteStatus Status, bool RequiresRestart, string? Message = null);

    public enum BootstrapCompleteStatus
    {
        Completed,
        AlreadyConfigured,
        ActivationFailed,
        ValidationError
    }

    public sealed record BootstrapAuthValidationResult(
        BootstrapAuthValidationStatus Status,
        string? UserName = null,
        string? Message = null);

    public enum BootstrapAuthValidationStatus
    {
        Valid,
        SetupNotCompleted,
        RuntimeNotReady,
        InvalidCredentials
    }
}
