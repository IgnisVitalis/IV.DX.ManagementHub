using IV.DX.Kernel.Enums;

namespace IV.ManagementHub.Common.Models
{
    public class DXUnitDefinitionStructure
    {
        public string Name { get; set; }
        public List<DXElementDefinitionStructure> SingleItemMandatory { get; set; }
        public List<DXElementDefinitionStructure> SingleItemOptional { get; set; }
        public List<DXElementDefinitionStructure> MultiItemsMandatory { get; set; }
        public List<DXElementDefinitionStructure> MultiItemsOptional { get; set; }
    }

    public class DXElementDefinitionStructure
    {
        public string Name { get; set; }

        public IEnumerable<DXColumnDefinitionStructure> Columns { get; set; }
    }

    public class DXColumnDefinitionStructure
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
