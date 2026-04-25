namespace IV.DX.ManagementHub.Web.Models
{
    public sealed class DXPNavItem
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public Guid? ParentId { get; init; }
        public int Order { get; init; }
        public Guid? ComponentType { get; init; }
        public Guid? ComponentId { get; init; }
    }
}
