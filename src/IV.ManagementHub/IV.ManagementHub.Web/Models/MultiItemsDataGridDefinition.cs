namespace IV.ManagementHub.Web.Models
{
    public class MultiItemsDataGridDefinition(string DPBlockName,  IEnumerable<ColumnDefinition> ColumnDefinitions)
    {
        public string DPBlockName { get; } = DPBlockName;
        public IEnumerable<ColumnDefinition> ColumnDefinitions { get; } = ColumnDefinitions;
    }
}
