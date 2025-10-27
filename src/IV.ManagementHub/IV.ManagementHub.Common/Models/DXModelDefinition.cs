using IV.DX.Kernel.Enums;

namespace IV.ManagementHub.Common.Models
{
    public class DXModelDefinition
    {
        public string Name { get; set; }
        public List<DXElementDefinition> SingleItemMandatory { get; set; }
        public List<DXElementDefinition> SingleItemOptional { get; set; }
        public List<DXElementDefinition> MultiItemsMandatory { get; set; }
        public List<DXElementDefinition> MultiItemsOptional { get; set; }

      
    }

    public class DXElementDefinition
    {
        public string Name { get; set; }

        public IEnumerable<DXColumnDefinition> Columns { get; set; }
    }

    public class DXColumnDefinition
    {
        public string Name { get; set; }
        public string Title { get { return Name; } }
        public DXColumnTypeEnum ColumnType { get; set; }
        public int? Length { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
        public bool AllowNull { get; set; }
        public string DefaultValue { get; set; }
        public IDictionary<int, string> EnumValues { get; set; }
    }
}
