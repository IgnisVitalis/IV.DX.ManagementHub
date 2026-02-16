namespace IV.ManagementHub.ApiService.Bootstrap
{
    public sealed class BootstrapInstanceSettings
    {
        public string Key { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string DatabaseType { get; init; } = "PostgreSQL";

        public string ConnectionString { get; init; } = string.Empty;

        public DateTimeOffset CreatedAtUtc { get; init; }

        public bool? IsInitialized { get; init; }

        public BootstrapInstanceSettings Normalize()
        {
            var normalizedKey = Key?.Trim() ?? string.Empty;
            var normalizedTitle = string.IsNullOrWhiteSpace(Title) ? normalizedKey : Title.Trim();

            return new BootstrapInstanceSettings
            {
                Key = normalizedKey,
                Title = normalizedTitle,
                DatabaseType = (DatabaseType ?? string.Empty).Trim(),
                ConnectionString = (ConnectionString ?? string.Empty).Trim(),
                CreatedAtUtc = CreatedAtUtc,
                IsInitialized = IsInitialized ?? true
            };
        }
    }
}
