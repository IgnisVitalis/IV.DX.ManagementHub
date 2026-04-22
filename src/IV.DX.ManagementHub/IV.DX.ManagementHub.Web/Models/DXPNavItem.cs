namespace IV.DX.ManagementHub.Web.Models
{
    public sealed class DXPNavItem
    {
        public Guid ID { get; init; }
        public string Name { get; init; } = string.Empty;
        public Guid? ParentID { get; init; }
        public int Order { get; init; }
        public Guid? ComponentType { get; init; }
        public Guid? ComponentID { get; init; }
    }
}
