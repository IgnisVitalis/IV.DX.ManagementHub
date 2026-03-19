using IV.ManagementHub.ApiService.Bootstrap;
using IV.ManagementHub.ApiService.Security;

namespace IV.ManagementHub.Web.Services
{
    public sealed class RootAuthApiClient(
        IBootstrapSetupService bootstrapSetupService,
        RootTokenService tokenService)
    {
        public async Task<SetupStatusResult> GetSetupStatusAsync(string? sourceKey = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var status = await bootstrapSetupService.GetStatusAsync(cancellationToken);
                return new SetupStatusResult(
                    RequiresSetup: status.RequiresSetup,
                    RequiresRestart: status.RequiresRestart,
                    RuntimeReady: status.RuntimeReady,
                    HasInstances: status.HasInstances);
            }
            catch (Exception ex)
            {
                return new SetupStatusResult(
                    RequiresSetup: true,
                    RequiresRestart: false,
                    RuntimeReady: false,
                    HasInstances: false,
                    Error: ex.Message);
            }
        }

        public async Task<SetupCompleteResult> CompleteSetupAsync(
            SetupCompleteRequest request,
            string? sourceKey = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = await bootstrapSetupService.CompleteSetupAsync(
                new BootstrapCompleteRequest(request.UserName, request.Password),
                cancellationToken);

            if (result.Status == BootstrapCompleteStatus.ValidationError)
            {
                return SetupCompleteResult.Fail(result.Message ?? "Validation failed.", result.RequiresRestart);
            }

            return SetupCompleteResult.Success(result.RequiresRestart, result.Message ?? "Setup completed.");
        }

        public async Task<LoginResult> LoginAsync(string username, string password, string? sourceKey = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return LoginResult.Fail("Username and password are required.");
            }

            var validation = await bootstrapSetupService.ValidateCredentialsAsync(username, password, cancellationToken);

            if (validation.Status is BootstrapAuthValidationStatus.SetupNotCompleted or BootstrapAuthValidationStatus.RuntimeNotReady)
            {
                return LoginResult.Fail(validation.Message ?? "Service is not ready. Complete setup and configure at least one instance.");
            }

            if (validation.Status != BootstrapAuthValidationStatus.Valid || string.IsNullOrWhiteSpace(validation.UserName))
            {
                return LoginResult.Fail("Invalid username or password.");
            }

            var token = tokenService.CreateAccessToken(validation.UserName);
            return LoginResult.Success(token.Token);
        }
    }

    public sealed record LoginResult(bool IsSuccess, string? AccessToken, string? Error)
    {
        public static LoginResult Success(string accessToken) => new(true, accessToken, null);

        public static LoginResult Fail(string error) => new(false, null, error);
    }

    public sealed record SetupStatusResult(
        bool RequiresSetup,
        bool RequiresRestart,
        bool RuntimeReady,
        bool HasInstances,
        string? Error = null);

    public sealed record SetupCompleteRequest(
        string UserName,
        string Password);

    public sealed record SetupCompleteResult(bool IsSuccess, bool RequiresRestart, string? Message, string? Error)
    {
        public static SetupCompleteResult Success(bool requiresRestart, string message) =>
            new(true, requiresRestart, message, null);

        public static SetupCompleteResult Fail(string error, bool requiresRestart) =>
            new(false, requiresRestart, null, error);
    }
}
