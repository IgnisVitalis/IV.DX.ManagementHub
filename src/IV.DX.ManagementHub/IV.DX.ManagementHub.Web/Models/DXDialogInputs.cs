namespace IV.DX.ManagementHub.Web.Models
{
    public record DXUnitDialogInput(string AppKey, string Type, Guid ID);

    public class DXDeleteConfirmInput
    {
        public string AppKey { get; init; } = string.Empty;
        public string TypeName { get; init; } = string.Empty;
        public IReadOnlyList<Guid> Ids { get; init; } = Array.Empty<Guid>();
    }
}
