namespace IV.ManagementHub.ApiService.Bootstrap
{
    public sealed class BootstrapSetupService(
        IBootstrapSettingsStore store,
        BootstrapSettingsSnapshot settingsSnapshot) : IBootstrapSetupService
    {
        private readonly SemaphoreSlim _sync = new(1, 1);

        public async Task<BootstrapSetupStatus> GetStatusAsync(CancellationToken ct = default)
        {
            var settings = await LoadSettingsAsync(ct);
            var isConfigured = settings?.IsConfigured == true;
            var hasInstances = settings?.HasInstances == true;

            return new BootstrapSetupStatus(
                RequiresSetup: !isConfigured,
                RequiresRestart: false,
                RuntimeReady: isConfigured,
                HasInstances: hasInstances);
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

            if (string.IsNullOrWhiteSpace(request.UserName) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return new BootstrapCompleteResult(
                    BootstrapCompleteStatus.ValidationError,
                    RequiresRestart: false,
                    Message: "Username and password are required.");
            }

            await _sync.WaitAsync(ct);
            try
            {
                var current = await LoadSettingsAsync(ct);
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
                        RootUserName = request.UserName.Trim(),
                        RootPasswordSalt = salt,
                        RootPasswordHash = hash,
                        CreatedAtUtc = DateTimeOffset.UtcNow,
                        Instances = current?.Instances?.Select(instance => instance.Normalize()).ToList() ?? []
                    };

                    await store.SaveAsync(settings, ct);
                    settingsSnapshot.Set(settings);
                }

                return new BootstrapCompleteResult(
                    isAlreadyConfigured
                        ? BootstrapCompleteStatus.AlreadyConfigured
                        : BootstrapCompleteStatus.Completed,
                    RequiresRestart: false,
                    Message: isAlreadyConfigured
                        ? "Setup is already completed."
                        : "Setup completed.");
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
            var settings = await LoadSettingsAsync(ct);
            if (settings?.IsConfigured != true)
            {
                return new BootstrapAuthValidationResult(
                    BootstrapAuthValidationStatus.SetupNotCompleted,
                    Message: "Setup is not completed.");
            }

            var namesMatch = string.Equals(settings.RootUserName, userName, StringComparison.Ordinal);
            var passwordMatch = BootstrapCrypto.VerifyPassword(password, settings.RootPasswordSalt, settings.RootPasswordHash);

            return namesMatch && passwordMatch
                ? new BootstrapAuthValidationResult(BootstrapAuthValidationStatus.Valid, settings.RootUserName)
                : new BootstrapAuthValidationResult(BootstrapAuthValidationStatus.InvalidCredentials);
        }

        private async Task<BootstrapSettings?> LoadSettingsAsync(CancellationToken ct)
        {
            var settings = await store.LoadAsync(ct) ?? settingsSnapshot.Current;
            if (settings is null)
            {
                return null;
            }

            var normalized = settings.Normalize();
            if (!ReferenceEquals(normalized, settings))
            {
                settingsSnapshot.Set(normalized);
            }

            return normalized;
        }
    }
}
