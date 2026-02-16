namespace IV.ManagementHub.Web.Services
{
    public sealed class AppState
    {
        public record NavItem(string Title, string Icon, string Href);
        public record NavGroup(string Key, string Title, string Icon, IReadOnlyList<NavItem> Items);
        public record AppInfo(string Key, string Title);

        private readonly ApiSourceCatalog _apiSourceCatalog;

        public AppState(ApiSourceCatalog apiSourceCatalog)
        {
            _apiSourceCatalog = apiSourceCatalog;
        }

        public IReadOnlyList<AppInfo> Apps => _apiSourceCatalog.Sources
            .Select(source => new AppInfo(source.Key, source.Title))
            .ToList();

        public string DefaultAppKey => _apiSourceCatalog.DefaultKey;

        public bool IsValidApp(string? key) =>
            !string.IsNullOrWhiteSpace(key) &&
            Apps.Any(app => string.Equals(app.Key, key, StringComparison.OrdinalIgnoreCase));

        public IEnumerable<NavItem> GetTopLevel(string appKey) => new[]
        {
            new NavItem("Dashboard", "bi-speedometer2", $"/app/{appKey}/dashboard"),
            new NavItem("Settings",  "bi-gear",        $"/app/{appKey}/settings"),
        };

        public IEnumerable<NavGroup> GetGroups(string appKey) => appKey switch
        {
            "base" => new[]
            {
                new NavGroup(
                    Key: "datastruct",
                    Title: "Data structure",
                    Icon: "bi-diagram-3",
                    Items: new[]
                    {
                        new NavItem("Entities", "bi-list-nested", $"/app/{appKey}/entities"),
                    }
                ),
            },
            "lit" => new[]
            {
                new NavGroup(
                    Key: "datastruct",
                    Title: "Data structure",
                    Icon: "bi-diagram-3",
                    Items: new[]
                    {
                        new NavItem("Entities", "bi-list-nested", $"/app/{appKey}/entities"),
                        new NavItem("Dictionaries", "bi-journal", $"/app/{appKey}/dictionaries"),
                    }
                ),
            },
            _ => new[]
            {
                new NavGroup(
                    Key: "datastruct",
                    Title: "Data structure",
                    Icon: "bi-diagram-3",
                    Items: new[]
                    {
                        new NavItem("Entities", "bi-list-nested", $"/app/{appKey}/entities"),
                    }
                ),
            }
        };

        public bool GroupIsActive(string currentPath, NavGroup group) =>
            group.Items.Any(item => currentPath.StartsWith(item.Href, StringComparison.OrdinalIgnoreCase));
    }
}
