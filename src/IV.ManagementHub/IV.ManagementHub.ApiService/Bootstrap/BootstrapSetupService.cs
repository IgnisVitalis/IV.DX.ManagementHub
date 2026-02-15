namespace IV.ManagementHub.ApiService.Bootstrap
{
    public sealed class BootstrapSetupService(
        IBootstrapSettingsStore store,
        BootstrapRuntimeState runtimeState,
        IConfiguration configuration,
        IBootstrapRuntimeActivator runtimeActivator) : IBootstrapSetupService
    {
        private readonly SemaphoreSlim _sync = new(1, 1);

        public async Task<BootstrapSetupStatus> GetStatusAsync(CancellationToken ct = default)
        {
            var settings = await store.LoadAsync(ct);
            var isConfigured = settings?.IsConfigured == true;

            return new BootstrapSetupStatus(
                RequiresSetup: !isConfigured,
                RequiresRestart: isConfigured && !runtimeState.IsDxRuntimeEnabled,
                RuntimeReady: runtimeState.IsDxRuntimeEnabled);
        }

        public async Task<BootstrapCompleteResult> CompleteSetupAsync(BootstrapCompleteRequest request, CancellationToken ct = default)
        {
            if (request is null)
            {
                return new BootstrapCompleteResult(
                    BootstrapCompleteStatus.ValidationError,
                    RequiresRestart: false,
                    Message: "Request payload is required.");
            }

            if (string.IsNullOrWhiteSpace(request.DatabaseType) ||
                string.IsNullOrWhiteSpace(request.ConnectionString) ||
                string.IsNullOrWhiteSpace(request.UserName) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return new BootstrapCompleteResult(
                    BootstrapCompleteStatus.ValidationError,
                    RequiresRestart: false,
                    Message: "Database type, connection string, username and password are required.");
            }

            await _sync.WaitAsync(ct);
            try
            {
                var current = await store.LoadAsync(ct);
                var isAlreadyConfigured = current?.IsConfigured == true;
                BootstrapSettings settings;

                if (isAlreadyConfigured)
                {
                    settings = current!;
                }
                else
                {
                    var salt = BootstrapCrypto.CreateSalt();
                    var hash = BootstrapCrypto.HashPassword(request.Password, salt);

                    settings = new BootstrapSettings
                    {
                        DatabaseType = request.DatabaseType.Trim(),
                        ConnectionString = request.ConnectionString.Trim(),
                        RootUserName = request.UserName.Trim(),
                        RootPasswordSalt = salt,
                        RootPasswordHash = hash,
                        CreatedAtUtc = DateTimeOffset.UtcNow
                    };

                    await store.SaveAsync(settings, ct);
                }

                configuration["Database:Type"] = settings.DatabaseType;
                configuration["Database:ConnectionString"] = settings.ConnectionString;

                var activation = await runtimeActivator.ActivateAsync(ct);
                if (!activation.IsSuccess)
                {
                    return new BootstrapCompleteResult(
                        BootstrapCompleteStatus.ActivationFailed,
                        RequiresRestart: true,
                        Message: isAlreadyConfigured
                            ? $"DX runtime activation failed. {activation.Message}"
                            : $"Setup saved but DX runtime activation failed. {activation.Message}");
                }

                return new BootstrapCompleteResult(
                    isAlreadyConfigured
                        ? BootstrapCompleteStatus.AlreadyConfigured
                        : BootstrapCompleteStatus.Completed,
                    RequiresRestart: false,
                    Message: isAlreadyConfigured
                        ? "Setup is already completed. DX runtime is active."
                        : "Setup completed. DX runtime is active.");
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task<BootstrapAuthValidationResult> ValidateCredentialsAsync(
            string userName,
            string password,
            CancellationToken ct = default)
        {
            var settings = await store.LoadAsync(ct);
            if (settings?.IsConfigured != true)
            {
                return new BootstrapAuthValidationResult(
                    BootstrapAuthValidationStatus.SetupNotCompleted,
                    Message: "Setup is not completed.");
            }

            if (!runtimeState.IsDxRuntimeEnabled)
            {
                return new BootstrapAuthValidationResult(
                    BootstrapAuthValidationStatus.RuntimeNotReady,
                    Message: "DX runtime is not active.");
            }

            var namesMatch = string.Equals(settings.RootUserName, userName, StringComparison.Ordinal);
            var passwordMatch = BootstrapCrypto.VerifyPassword(password, settings.RootPasswordSalt, settings.RootPasswordHash);

            return namesMatch && passwordMatch
                ? new BootstrapAuthValidationResult(BootstrapAuthValidationStatus.Valid, settings.RootUserName)
                : new BootstrapAuthValidationResult(BootstrapAuthValidationStatus.InvalidCredentials);
        }
    }
}
