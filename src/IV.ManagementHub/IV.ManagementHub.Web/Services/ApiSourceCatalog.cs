namespace IV.ManagementHub.Web.Services
{
    public sealed class ApiSourceCatalog
    {
        private readonly Dictionary<string, ApiSourceDefinition> _sourcesByKey;

        public ApiSourceCatalog(IEnumerable<ApiSourceDefinition> sources)
        {
            var sourceList = (sources ?? Enumerable.Empty<ApiSourceDefinition>())
                .Where(source => !string.IsNullOrWhiteSpace(source.Key) &&
                                 !string.IsNullOrWhiteSpace(source.ApiBaseUrl))
                .Select(source => source.Normalize())
                .GroupBy(source => source.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            if (sourceList.Count == 0)
            {
                sourceList.Add(new ApiSourceDefinition
                {
                    Key = "base",
                    Title = "Base",
                    ApiBaseUrl = "http://localhost:5455"
                });
            }

            Sources = sourceList;
            _sourcesByKey = sourceList.ToDictionary(source => source.Key, StringComparer.OrdinalIgnoreCase);
            DefaultKey = Sources[0].Key;
        }

        public IReadOnlyList<ApiSourceDefinition> Sources { get; }

        public string DefaultKey { get; }

        public ApiSourceDefinition Resolve(string? key)
        {
            if (!string.IsNullOrWhiteSpace(key) && _sourcesByKey.TryGetValue(key, out var source))
            {
                return source;
            }

            return Sources[0];
        }
    }

    public sealed class ApiSourceDefinition
    {
        public string Key { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string ApiBaseUrl { get; init; } = string.Empty;

        public string HttpClientName => $"ApiSource:{Key}";

        internal ApiSourceDefinition Normalize()
        {
            var normalizedKey = Key.Trim();
            var normalizedTitle = string.IsNullOrWhiteSpace(Title) ? normalizedKey : Title.Trim();
            var normalizedUrl = ApiBaseUrl.Trim();

            return new ApiSourceDefinition
            {
                Key = normalizedKey,
                Title = normalizedTitle,
                ApiBaseUrl = normalizedUrl
            };
        }
    }
}
