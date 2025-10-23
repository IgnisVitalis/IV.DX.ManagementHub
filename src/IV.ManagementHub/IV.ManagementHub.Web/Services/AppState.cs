public sealed class AppState
{
    public record NavItem(string Title, string Icon, string Href);
    public record NavGroup(string Key, string Title, string Icon, IReadOnlyList<NavItem> Items);

    public readonly string[] Apps = ["base", "lit"];
    public bool IsValidApp(string? key) => !string.IsNullOrWhiteSpace(key) && Apps.Contains(key);

    // Верхние (негрупповые) пункты
    public IEnumerable<NavItem> GetTopLevel(string appKey) => new[]
    {
        new NavItem("Dashboard", "bi-speedometer2", $"/app/{appKey}/dashboard"),
        new NavItem("Settings",  "bi-gear",        $"/app/{appKey}/settings"),
    };

    // Группы со вложенными пунктами
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
                    new NavItem("Blocks",   "bi-grid",        $"/app/{appKey}/blocks"),
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
                    new NavItem("Entities",     "bi-list-nested", $"/app/{appKey}/entities"),
                    new NavItem("Blocks",       "bi-grid",        $"/app/{appKey}/blocks"),
                    new NavItem("Dictionaries", "bi-journal",     $"/app/{appKey}/dictionaries"),
                }
            ),
        },
        _ => Array.Empty<NavGroup>()
    };

    // Нужна ли группе автоподсветка/раскрытие по текущему URL
    public bool GroupIsActive(string currentPath, NavGroup group) =>
        group.Items.Any(i => currentPath.StartsWith(i.Href, StringComparison.OrdinalIgnoreCase));
}
