using IV.DX.Kernel.Enums;

namespace IV.ManagementHub.Common.Models
{
    public class DXModelDefinition
    {
        public string Name { get; set; }
        public DXElementDefinition MainSingleElement { get; set; }
        public List<DXElementDefinition> RequiredSingleElements { get; set; }
        public List<DXElementDefinition> OptionalSingleElements { get; set; }
        public List<DXElementDefinition> RequiredMultiElements { get; set; }
        public List<DXElementDefinition> OptionalMultiElements { get; set; }


        public bool IsRequired(string name)
        {
            if (MainSingleElement.Name == name)
                return true;

            if (RequiredSingleElements != null && RequiredSingleElements.Select(x => x.Name).Contains(name))
                return true;

            if (RequiredMultiElements != null && RequiredMultiElements.Select(x => x.Name).Contains(name))
                return true;

            return false;
        }
    }

    public class DXElementDefinition
    {
        public string Name { get; set; }

        public IEnumerable<DXColumnDefinition> Columns { get; set; }

        public void AddColumns(IEnumerable<DXColumnDefinition> columns)
        {
            this.Columns = this.Columns.Concat(columns);
        }
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
        public IDictionary<Guid, string> RelationValues { get; set; }

        public DXColumnDefinition DeepClone()
        {
            return new DXColumnDefinition()
            {
                Name = this.Name,
                ColumnType = this.ColumnType,
                Length = this.Length,
                Precision = this.Precision,
                Scale = this.Scale,
                AllowNull = this.AllowNull,
                DefaultValue = this.DefaultValue,
                EnumValues = this.EnumValues,
                RelationValues = this.RelationValues,
            };
        }
    }
}