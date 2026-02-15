namespace IV.ManagementHub.ApiService.Bootstrap
{
    public sealed class BootstrapSettings
    {
        public string DatabaseType { get; init; } = string.Empty;

        public string ConnectionString { get; init; } = string.Empty;

        public string RootUserName { get; init; } = string.Empty;

        public string RootPasswordHash { get; init; } = string.Empty;

        public string RootPasswordSalt { get; init; } = string.Empty;

        public DateTimeOffset CreatedAtUtc { get; init; }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(DatabaseType) &&
            !string.IsNullOrWhiteSpace(ConnectionString) &&
            !string.IsNullOrWhiteSpace(RootUserName) &&
            !string.IsNullOrWhiteSpace(RootPasswordHash) &&
            !string.IsNullOrWhiteSpace(RootPasswordSalt);
    }
}
