namespace IV.ManagementHub.ApiService.Bootstrap
{
    public sealed class BootstrapInstanceSettings
    {
        public string Key { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string ApiUrl { get; init; } = string.Empty;

        public string ServiceKey { get; init; } = string.Empty;

        public DateTimeOffset CreatedAtUtc { get; init; }

        public BootstrapInstanceSettings Normalize()
        {
            var normalizedKey = Key?.Trim() ?? string.Empty;
            var normalizedTitle = string.IsNullOrWhiteSpace(Title) ? normalizedKey : Title.Trim();

            return new BootstrapInstanceSettings
            {
                Key = normalizedKey,
                Title = normalizedTitle,
                ApiUrl = (ApiUrl ?? string.Empty).Trim().TrimEnd('/'),
                ServiceKey = (ServiceKey ?? string.Empty).Trim(),
                CreatedAtUtc = CreatedAtUtc
            };
        }
    }
}
