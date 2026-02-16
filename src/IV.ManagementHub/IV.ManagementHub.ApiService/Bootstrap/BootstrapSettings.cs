namespace IV.ManagementHub.ApiService.Bootstrap
{
    public sealed class BootstrapSettings
    {
        public string RootUserName { get; init; } = string.Empty;

        public string RootPasswordHash { get; init; } = string.Empty;

        public string RootPasswordSalt { get; init; } = string.Empty;

        public DateTimeOffset CreatedAtUtc { get; init; }

        public List<BootstrapInstanceSettings> Instances { get; init; } = [];

        public string? DatabaseType { get; init; }

        public string? ConnectionString { get; init; }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(RootUserName) &&
            !string.IsNullOrWhiteSpace(RootPasswordHash) &&
            !string.IsNullOrWhiteSpace(RootPasswordSalt);

        public bool HasInstances => Instances.Count > 0;

        public BootstrapInstanceSettings? ResolveInstance(string? key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                var selected = Instances.FirstOrDefault(instance =>
                    string.Equals(instance.Key, key.Trim(), StringComparison.OrdinalIgnoreCase));

                if (selected is not null)
                {
                    return selected;
                }
            }

            return null;
        }

        public BootstrapSettings Normalize()
        {
            var normalizedInstances = Instances
                .Where(instance =>
                    !string.IsNullOrWhiteSpace(instance.Key) &&
                    !string.IsNullOrWhiteSpace(instance.DatabaseType) &&
                    !string.IsNullOrWhiteSpace(instance.ConnectionString))
                .Select(instance => instance.Normalize())
                .GroupBy(instance => instance.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            if (normalizedInstances.Count == 0 &&
                !string.IsNullOrWhiteSpace(DatabaseType) &&
                !string.IsNullOrWhiteSpace(ConnectionString))
            {
                normalizedInstances.Add(new BootstrapInstanceSettings
                {
                    Key = "default",
                    Title = "Default",
                    DatabaseType = DatabaseType.Trim(),
                    ConnectionString = ConnectionString.Trim(),
                    CreatedAtUtc = CreatedAtUtc == default ? DateTimeOffset.UtcNow : CreatedAtUtc,
                    IsInitialized = true
                });
            }

            return new BootstrapSettings
            {
                RootUserName = RootUserName?.Trim() ?? string.Empty,
                RootPasswordHash = RootPasswordHash?.Trim() ?? string.Empty,
                RootPasswordSalt = RootPasswordSalt?.Trim() ?? string.Empty,
                CreatedAtUtc = CreatedAtUtc,
                Instances = normalizedInstances
            };
        }
    }
}
