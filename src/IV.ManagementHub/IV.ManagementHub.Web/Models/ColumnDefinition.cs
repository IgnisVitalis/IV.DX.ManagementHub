using IV.DX.Kernel.Enums;

namespace IV.ManagementHub.Web.Models
{
    public class ColumnDefinition(string Title, string Value, DXColumnTypeEnum ColumnType)
    {
        public string Title { get; } = Title;
        public string Value { get; } = Value;
        public DXColumnTypeEnum ColumnType { get; } = ColumnType;
    }
}