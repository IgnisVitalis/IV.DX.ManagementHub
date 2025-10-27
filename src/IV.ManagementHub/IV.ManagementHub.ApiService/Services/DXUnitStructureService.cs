using IV.DX.Application.Contracts.Abstractions;
using IV.DX.Kernel.Enums;
using IV.DX.Kernel.Models;
using IV.ManagementHub.ApiService.Contracts.Services;
using IV.ManagementHub.Common.Models;

namespace IV.ManagementHub.ApiService.Services
{
    internal class DXUnitStructureService(
       IDXUnitDataService dxUnitDataService,
       IDXEnumDataService dxEnumDataService) : IDXUnitStructureService
    {
        public async Task<DXModelDefinition> GetAsync(string name, CancellationToken ct = default)
        {
            var result = await dxUnitDataService.GetItemsAsync<DXUnitDefinitionUnit>($"DXObjectDefinitionMainElement.Name = '{name}'", ct: ct);

            if (result.Count() == 0)
                return null;

            if (result.Count() > 1)
                throw new InvalidOperationException($"More than one DXUnitDefinitionUnit found with name '{name}'");

            var mainDXUnitDefinition = result.Single();

            List<DXElementDefinition> singleItemMandatory = new List<DXElementDefinition>();
            List<DXElementDefinition> singleItemOptional = new List<DXElementDefinition>();
            List<DXElementDefinition> multiItemsMandatory = new List<DXElementDefinition>();
            List<DXElementDefinition> multiItemsOptional = new List<DXElementDefinition>();

            do
            {
                var mainDXElementDefintion = new DXElementDefinition()
                {
                    Name = mainDXUnitDefinition.DXObjectDefinitionMainElement.Name,
                    Columns = await this.GetColumnDefinitionsAsync(
                       mainDXUnitDefinition.DXObjectDefinitionMainElement.Name,
                       mainDXUnitDefinition.DXColumnDefinitionElement?.Announced, ct: ct)
                };

                singleItemMandatory.Add(mainDXElementDefintion);

                var blockInEntityDefinitions = mainDXUnitDefinition.DXElementInUnitDefinitionElement?.Announced;

                if (blockInEntityDefinitions != null)
                {
                    foreach (var blockInEntityDefinition in blockInEntityDefinitions)
                    {
                        var blockDefinition = await GetEnumDefinitionAsync(blockInEntityDefinition.DXElementDefinitionUnit, ct);

                        switch (blockInEntityDefinition.RelationType)
                        {
                            case DXElementInUnitTypeEnum.SingleOptional:
                                singleItemOptional.Add(blockDefinition);
                                break;
                            case DXElementInUnitTypeEnum.SingleMandatory:
                                singleItemMandatory.Add(blockDefinition);
                                break;
                            case DXElementInUnitTypeEnum.MultiOptional:
                                multiItemsOptional.Add(blockDefinition);
                                break;
                            case DXElementInUnitTypeEnum.MultiMandatory:
                                multiItemsMandatory.Add(blockDefinition);
                                break;
                        }
                    }
                }

                var baseDXUnitID = mainDXUnitDefinition.DXUnitInheritanceElement?.BaseDXUnit;

                if (!baseDXUnitID.HasValue)
                    break;

                mainDXUnitDefinition = await dxUnitDataService.GetItemAsync<DXUnitDefinitionUnit>(baseDXUnitID.Value, ct: ct);

            } while (true);

            return new DXModelDefinition()
            {
                Name = name,
                MultiItemsMandatory = multiItemsMandatory.ToList(),
                MultiItemsOptional = multiItemsOptional.ToList(),
                SingleItemMandatory = singleItemMandatory.ToList(),
                SingleItemOptional = singleItemOptional.ToList(),
            };
        }

        private async Task<DXElementDefinition> GetEnumDefinitionAsync(Guid elementID, CancellationToken ct)
        {
            var block = await dxUnitDataService.GetItemAsync<DXElementDefinitionUnit>(elementID, ct: ct);

            if (block.DXColumnDefinitionElement == null)
                return new DXElementDefinition() { Name = block.DXObjectDefinitionMainElement.Name, Columns = Enumerable.Empty<DXColumnDefinition>() };

            else
                return new DXElementDefinition()
                {
                    Name = block.DXObjectDefinitionMainElement.Name,
                    Columns = await this.GetColumnDefinitionsAsync(block.DXObjectDefinitionMainElement.Name, block.DXColumnDefinitionElement?.Announced, ct)
                };
        }

        private async Task<IEnumerable<DXColumnDefinition>> GetColumnDefinitionsAsync(string dxElementName, IEnumerable<DXColumnDefinitionElement> columns, CancellationToken ct)
        {
            var list = new List<DXColumnDefinition>();

            var regularColumns = columns?
                .Where(c => !systemColumns.Contains(c.Name, StringComparer.OrdinalIgnoreCase))
                .Select(c =>
                {
                    return new DXColumnDefinition()
                    {
                        Name = c.Name,
                        ColumnType = c.ColumnType,
                        Length = c.Length,
                        Precision = c.Precision,
                        Scale = c.Scale,
                        AllowNull = c.AllowNull,
                        DefaultValue = c.DefaultValue,
                    };
                }) ?? Enumerable.Empty<DXColumnDefinition>();

            list.AddRange(regularColumns);

            var notNullEnumRelations = await dxUnitDataService.GetItemsAsync<DXRelationDefinitionUnit>($"DXRelationDefinitionMainElement.ObjectNameLeft = '{dxElementName}' AND DXRelationDefinitionMainElement.RelationType = 4", ct: ct);

            foreach (var enumRelation in notNullEnumRelations)
            {
                var enumDefinition = await this.GetEnumAsync(enumRelation.DXRelationDefinitionMainElement.ObjectNameRight);

                if (enumDefinition == null)
                    continue;

                var enumValues = await dxEnumDataService.GetItemsAsync(enumRelation.DXRelationDefinitionMainElement.ObjectNameRight, ct: ct);

                list.Add(new DXColumnDefinition()
                {
                    Name = enumRelation.DXRelationDefinitionMainElement.RelationNameRight,
                    ColumnType = enumRelation.DXRelationDefinitionMainElement.RelationColumnTypeRight.Value,
                    AllowNull = false,
                    EnumValues = enumValues
                });
            }

            var nullableEnumRelations = await dxUnitDataService.GetItemsAsync<DXRelationDefinitionUnit>($"DXRelationDefinitionMainElement.ObjectNameLeft = '{dxElementName}' AND DXRelationDefinitionMainElement.RelationType = 6", ct: ct);

            foreach (var enumRelation in nullableEnumRelations)
            {
                var enumDefinition = await this.GetEnumAsync(enumRelation.DXRelationDefinitionMainElement.ObjectNameRight);

                if (enumDefinition == null)
                    continue;

                var enumValues = await dxEnumDataService.GetItemsAsync(enumRelation.DXRelationDefinitionMainElement.ObjectNameRight, ct: ct);

                list.Add(new DXColumnDefinition()
                {
                    Name = enumRelation.DXRelationDefinitionMainElement.RelationNameRight,
                    ColumnType = enumRelation.DXRelationDefinitionMainElement.RelationColumnTypeRight.Value,
                    AllowNull = true,
                    EnumValues = enumValues
                });
            }

            return list;
        }

        private async Task<DXEnumDefinitionUnit> GetEnumAsync(string name, CancellationToken ct = default)
        {
            var items = await dxUnitDataService.GetItemsAsync<DXEnumDefinitionUnit>($"DXObjectDefinitionMainElement.Name = '{name}'", ct: ct);

            if (items.Count() > 1)
                throw new Exception($"There more than 1 entry for DXEnumDefinitionUnit by name '{name}'");

            return items.SingleOrDefault();
        }

        private readonly string[] systemColumns = new[] { "ID", "DXUnitID", "TimeStamp" };
    }
}
