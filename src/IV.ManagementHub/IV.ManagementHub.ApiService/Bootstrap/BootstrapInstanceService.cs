namespace IV.ManagementHub.ApiService.Bootstrap
{
    public sealed class BootstrapInstanceService(
        IBootstrapSettingsStore store,
        BootstrapSettingsSnapshot settingsSnapshot) : IBootstrapInstanceService
    {
        private readonly SemaphoreSlim _sync = new(1, 1);

        public async Task<IReadOnlyList<BootstrapInstanceDescriptor>> GetInstancesAsync(CancellationToken ct = default)
        {
            var settings = await LoadSettingsAsync(ct);
            return settings?.Instances
                .Select(ToDescriptor)
                .OrderBy(instance => instance.Title, StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];
        }

        public async Task<BootstrapCreateInstanceResult> CreateInstanceAsync(BootstrapCreateInstanceRequest request, CancellationToken ct = default)
        {
            if (request is null)
            {
                return new BootstrapCreateInstanceResult(
                    BootstrapCreateInstanceStatus.ValidationError,
                    Message: "Request payload is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Title) ||
                string.IsNullOrWhiteSpace(request.ApiUrl) ||
                string.IsNullOrWhiteSpace(request.ServiceKey))
            {
                return new BootstrapCreateInstanceResult(
                    BootstrapCreateInstanceStatus.ValidationError,
                    Message: "Title, API URL and service key are required.");
            }

            await _sync.WaitAsync(ct);
            try
            {
                var settings = await LoadSettingsAsync(ct);
                if (settings?.IsConfigured != true)
                {
                    return new BootstrapCreateInstanceResult(
                        BootstrapCreateInstanceStatus.SetupNotCompleted,
                        Message: "Setup is not completed.");
                }

                var normalizedKey = NormalizeKey(request.Key, request.Title);
                var normalizedTitle = request.Title.Trim();
                var normalizedApiUrl = request.ApiUrl.Trim().TrimEnd('/');
                var normalizedServiceKey = request.ServiceKey.Trim();

                if (string.IsNullOrWhiteSpace(normalizedKey))
                {
                    return new BootstrapCreateInstanceResult(
                        BootstrapCreateInstanceStatus.ValidationError,
                        Message: "Instance key is invalid.");
                }

                var existing = settings.ResolveInstance(normalizedKey);
                if (existing is not null)
                {
                    return new BootstrapCreateInstanceResult(
                        BootstrapCreateInstanceStatus.Conflict,
                        ToDescriptor(existing),
                        "Instance with the same key already exists.");
                }

                var instance = new BootstrapInstanceSettings
                {
                    Key = normalizedKey,
                    Title = normalizedTitle,
                    ApiUrl = normalizedApiUrl,
                    ServiceKey = normalizedServiceKey,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };

                var updatedSettings = new BootstrapSettings
                {
                    RootUserName = settings.RootUserName,
                    RootPasswordHash = settings.RootPasswordHash,
                    RootPasswordSalt = settings.RootPasswordSalt,
                    CreatedAtUtc = settings.CreatedAtUtc,
                    Instances = settings.Instances
                        .Select(existingInstance => existingInstance.Normalize())
                        .Concat([instance.Normalize()])
                        .ToList()
                };

                await store.SaveAsync(updatedSettings, ct);
                settingsSnapshot.Set(updatedSettings);

                return new BootstrapCreateInstanceResult(
                    BootstrapCreateInstanceStatus.Created,
                    ToDescriptor(instance),
                    "Instance created.");
            }
            finally
            {
                _sync.Release();
            }
        }

        private async Task<BootstrapSettings?> LoadSettingsAsync(CancellationToken ct)
        {
            var settings = await store.LoadAsync(ct) ?? settingsSnapshot.Current;
            if (settings is null)
            {
                return null;
            }

            var normalized = settings.Normalize();
            settingsSnapshot.Set(normalized);
            return normalized;
        }

        private static BootstrapInstanceDescriptor ToDescriptor(BootstrapInstanceSettings settings)
        {
            var normalized = settings.Normalize();
            return new BootstrapInstanceDescriptor(
                normalized.Key,
                normalized.Title,
                normalized.ApiUrl,
                normalized.CreatedAtUtc);
        }

        private static string NormalizeKey(string key, string title)
        {
            var raw = string.IsNullOrWhiteSpace(key) ? title : key;
            var chars = raw.Trim()
                .Select(character =>
                    char.IsLetterOrDigit(character)
                        ? char.ToLowerInvariant(character)
                        : '-')
                .ToArray();

            var compact = new string(chars);
            while (compact.Contains("--", StringComparison.Ordinal))
            {
                compact = compact.Replace("--", "-", StringComparison.Ordinal);
            }

            return compact.Trim('-');
        }
    }
}
