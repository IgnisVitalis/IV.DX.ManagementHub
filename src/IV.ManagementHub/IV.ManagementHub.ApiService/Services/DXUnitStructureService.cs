using IV.DX.Application.Contracts.Abstractions;
using IV.DX.Kernel;
using IV.DX.Kernel.Enums;
using IV.DX.Kernel.Models;
using IV.ManagementHub.ApiService.Contracts.Services;
using IV.ManagementHub.Common.Models;

namespace IV.ManagementHub.ApiService.Services
{
    internal class DXUnitStructureService(
       IDXUnitDataService dxUnitDataService,
       IDXEnumDataService dxEnumDataService,
       IDXStructureService dxStructureService,
       IDXQueryResultProvider dxQueryResultProvider) : IDXUnitStructureService
    {
        public async Task<DXModelDefinition> GetAsync(string name, CancellationToken ct = default)
        {
            var result = await dxUnitDataService.GetItemsAsync<DXUnitDefinitionUnit>($"Name = '{name}'", ct: ct);

            if (result.Count() == 0)
                return null;

            if (result.Count() > 1)
                throw new InvalidOperationException($"More than one DXUnitDefinitionUnit found with name '{name}'");

            var mainDXUnitDefinition = result.Single();

            DXElementDefinition mainSingleElement = null;
            List<DXElementDefinition> singleItemMandatory = new List<DXElementDefinition>();
            List<DXElementDefinition> singleItemOptional = new List<DXElementDefinition>();
            List<DXElementDefinition> multiItemsMandatory = new List<DXElementDefinition>();
            List<DXElementDefinition> multiItemsOptional = new List<DXElementDefinition>();

            do
            {
                var columns = await this.GetColumnDefinitionsAsync(
                       mainDXUnitDefinition.Name,
                       mainDXUnitDefinition.DXColumnDefinitionElement?.Announced, ct: ct);

                var enumsAsColumns = await this.GetEnumsAsColumns(mainDXUnitDefinition.Name, ct: ct);
                var relationAsColumns = await this.GetRelationsAsColumnsAsync(mainDXUnitDefinition.Name, ct: ct);

                var combined = columns.Concat(enumsAsColumns).Concat(relationAsColumns);

                var mainDXElementDefintion = new DXElementDefinition()
                {
                    Name = mainDXUnitDefinition.Name,
                    Columns = combined
                };

                if (mainSingleElement == null)
                {
                    mainSingleElement = mainDXElementDefintion;
                }
                else
                {
                    mainSingleElement.AddColumns(mainDXElementDefintion.Columns);
                }

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

                var baseDXUnitID = mainDXUnitDefinition.BaseDXUnit;

                if (!baseDXUnitID.HasValue)
                    break;

                mainDXUnitDefinition = await dxUnitDataService.GetItemAsync<DXUnitDefinitionUnit>(baseDXUnitID.Value, ct: ct);

            } while (true);

            return new DXModelDefinition()
            {
                Name = name,
                MainSingleElement = mainSingleElement,
                RequiredMultiElements = multiItemsMandatory,
                OptionalMultiElements = multiItemsOptional,
                RequiredSingleElements = singleItemMandatory,
                OptionalSingleElements = singleItemOptional,
            };
        }

        private async Task<DXElementDefinition> GetEnumDefinitionAsync(Guid elementID, CancellationToken ct)
        {
            var block = await dxUnitDataService.GetItemAsync<DXElementDefinitionUnit>(elementID, ct: ct);

            if (block.DXColumnDefinitionElement == null)
                return new DXElementDefinition() { Name = block.Name, Columns = Enumerable.Empty<DXColumnDefinition>() };
            else
            {
                var columns = await this.GetColumnDefinitionsAsync(block.Name, block.DXColumnDefinitionElement?.Announced, ct);
                var enumsAsColumns = await this.GetEnumsAsColumns(block.Name, ct: ct);
                var relationAsColumns = await this.GetRelationsAsColumnsAsync(block.Name, ct: ct);

                var combined = columns.Concat(enumsAsColumns).Concat(relationAsColumns);

                return new DXElementDefinition()
                {
                    Name = block.Name,
                    Columns = combined
                };
            }
        }

        private async Task<IEnumerable<DXColumnDefinition>> GetColumnDefinitionsAsync(string dxElementName, IEnumerable<DXColumnDefinitionElement> columns, CancellationToken ct)
        {
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

            return regularColumns;
        }

        public async Task<IEnumerable<DXColumnDefinition>> GetEnumsAsColumns(string dxElementName, CancellationToken ct)
        {
            var list = new List<DXColumnDefinition>();

            var enums = dxStructureService.GetDXRelations(dxElementName).ToList();

            var notNullEnumRelations = enums.Where(x =>
                x.RelationType == DXRelationTypeEnum.ManyToOne
                && x.RelationColumnTypeRight == DXColumnTypeEnum.Int)
                .ToList();
            // dxUnitDataService.GetItemsAsync<DXRelationDefinitionUnit>($"DXRelationDefinitionMainElement.ObjectNameLeft = '{dxElementName}' AND DXRelationDefinitionMainElement.RelationType = 4", ct: ct);

            foreach (var enumRelation in notNullEnumRelations)
            {
                var enumDefinition = await this.GetEnumAsync(enumRelation.ObjectNameRight);

                if (enumDefinition == null)
                    continue;

                var enumValues = await dxEnumDataService.GetItemsAsync(enumRelation.ObjectNameRight, ct: ct);

                list.Add(new DXColumnDefinition()
                {
                    Name = enumRelation.RelationNameRight,
                    ColumnType = enumRelation.RelationColumnTypeRight.Value,
                    AllowNull = false,
                    EnumValues = enumValues
                });
            }

            var nullableEnumRelations = enums.Where(x =>
                x.RelationType == DXRelationTypeEnum.ManyToZeroOne
                && x.RelationColumnTypeRight == DXColumnTypeEnum.Int)
                .ToList();
            // dxUnitDataService.GetItemsAsync<DXRelationDefinitionUnit>($"DXRelationDefinitionMainElement.ObjectNameLeft = '{dxElementName}' AND DXRelationDefinitionMainElement.RelationType = 6", ct: ct);

            foreach (var enumRelation in nullableEnumRelations)
            {
                var enumDefinition = await this.GetEnumAsync(enumRelation.ObjectNameRight);

                if (enumDefinition == null)
                    continue;

                var enumValues = await dxEnumDataService.GetItemsAsync(enumRelation.ObjectNameRight, ct: ct);

                list.Add(new DXColumnDefinition()
                {
                    Name = enumRelation.RelationNameRight,
                    ColumnType = enumRelation.RelationColumnTypeRight.Value,
                    AllowNull = true,
                    EnumValues = enumValues
                });
            }

            return list;
        }

        public async Task<IEnumerable<DXColumnDefinition>> GetRelationsAsColumnsAsync(string dxElementName, CancellationToken ct)
        {
            var allRelations = dxStructureService.GetDXRelations(dxElementName);

            var dxUnitValues = dxStructureService.DXUnits.Select
                (x => new KeyValuePair<Guid, string>(x.ID, x.Name))
                .ToDictionary(x => x.Key, x => x.Value);

            var relationsToObject = allRelations
                .Where(x =>
                    x.RelationColumnNameRight == "ID"
                    && x.RelationColumnTypeRight == DXColumnTypeEnum.GUID
                    && !x.RelationNameRight.EndsWith(Constants.DXUnitIDSuffix))
                .ToList();

            var manyToOneRelations = relationsToObject.Where(x => x.RelationType == DXRelationTypeEnum.ManyToOne).Select(
                x =>
                {
                    return new DXColumnDefinition()
                    {
                        Name = x.RelationNameRight,
                        AllowNull = false,
                        ColumnType = DXColumnTypeEnum.GUID,
                        RelationValues = this.GetSelectValues(x.ObjectNameRight)
                    };
                }).ToList();

            var manyToZeroOneRelations = relationsToObject.Where(x => x.RelationType == DXRelationTypeEnum.ManyToZeroOne).Select(
                x =>
                {
                    return new DXColumnDefinition()
                    {
                        Name = x.RelationNameRight,
                        AllowNull = true,
                        ColumnType = DXColumnTypeEnum.GUID,
                        RelationValues = this.GetSelectValues(x.ObjectNameRight)
                    };
                }).ToList();

            var zeroOneToOneRelations = relationsToObject.Where(x => x.RelationType == DXRelationTypeEnum.ZeroOneToOne).Select(
                x =>
                {
                    return new DXColumnDefinition()
                    {
                        Name = x.RelationNameRight,
                        AllowNull = false,
                        ColumnType = DXColumnTypeEnum.GUID,
                        RelationValues = this.GetSelectValues(x.ObjectNameRight)
                    };
                }).ToList();

            var zeroOneToZeroOneRelations = relationsToObject.Where(x => x.RelationType == DXRelationTypeEnum.ZeroOneToZeroOne).Select(
                x =>
                {
                    return new DXColumnDefinition()
                    {
                        Name = x.RelationNameRight,
                        AllowNull = false,
                        ColumnType = DXColumnTypeEnum.GUID,
                        RelationValues = this.GetSelectValues(x.ObjectNameRight)
                    };
                }).ToList();

            var combined = manyToOneRelations.Concat(manyToZeroOneRelations).Concat(zeroOneToOneRelations).Concat(zeroOneToZeroOneRelations).ToList();

            return combined;
        }

        private IDictionary<Guid, string> GetSelectValues(string objectNameRight)
        {
            IDictionary<Guid, string> values = new Dictionary<Guid, string>();

            if (objectNameRight == "DXUnitDefinitionUnit")
            {
                values = dxStructureService.DXUnits.Select
                   (x => new KeyValuePair<Guid, string>(x.ID, x.Name))
                   .ToDictionary(x => x.Key, x => x.Value);
            }
            else if (objectNameRight == "DXElementDefinitionUnit")
            {
                values = dxStructureService.DXElements.Select
                   (x => new KeyValuePair<Guid, string>(x.ID, x.Name))
                   .ToDictionary(x => x.Key, x => x.Value);
            }
            else
            {
                var displayValues = dxQueryResultProvider.GetDisplayValuesAsync(objectNameRight).Result;

                values = displayValues.ToDictionary(x => x.ID, x => x.DisplayValue);
            }

            return values;
        }

        private async Task<DXEnumDefinitionUnit> GetEnumAsync(string name, CancellationToken ct = default)
        {
            var items = await dxUnitDataService.GetItemsAsync<DXEnumDefinitionUnit>($"Name = '{name}'", ct: ct);

            if (items.Count() > 1)
                throw new Exception($"There more than 1 entry for DXEnumDefinitionUnit by name '{name}'");

            return items.SingleOrDefault();
        }

        public Task<DXColumnDefinition> GetDXColumnDefinition(string dxObjectName, string dxColumnName, string? dxSqlFilter = null)
        {
            //var columnDefinition = this.dat
            throw new NotImplementedException();
        }

        private readonly string[] systemColumns = new[] { "ID", "DXUnitID", "TimeStamp" };
    }
}
