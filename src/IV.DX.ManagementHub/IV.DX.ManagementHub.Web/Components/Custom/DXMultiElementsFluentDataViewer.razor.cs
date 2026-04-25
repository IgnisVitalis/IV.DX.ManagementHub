using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.Kernel.Models;
using IV.DX.ManagementHub.Common.Models;
using IV.DX.ManagementHub.Web.Components.Custom.Base;
using IV.DX.ManagementHub.Web.Models;
using Microsoft.AspNetCore.Components;

namespace IV.DX.ManagementHub.Web.Components.Custom
{
    public partial class DXMultiElementsFluentDataViewer : ManagementHubComponentBase
    {
        [Parameter, EditorRequired] public DXElementDefinition Definition { get; set; } = default!;
        [Parameter, EditorRequired] public DXRecordMultiElement DXMultiElement { get; set; } = default!;
        [Parameter, EditorRequired] public DXUnitRecordModel Parent { get; set; } = default!;

        private readonly string[] systemColumns = new[] { "Id", "DXUnitId", "TimeStamp" };

        private DXEnumApiClient _dxEnumApiClient = default!;

        [Inject] IApiClientResolver Resolver { get; set; } = default!;

        private IEnumerable<DXEnumDefinitionUnit> enumDefinitions = new List<DXEnumDefinitionUnit>();

        protected override async Task OnInitializedAsync()
        {
            _dxEnumApiClient = await Resolver.GetAsync<DXEnumApiClient>(base.AppKey);

            if (Definition.Name.Equals("DXObjectEnumElement", StringComparison.OrdinalIgnoreCase))
            {
                enumDefinitions = await _dxEnumApiClient.GetItemsAsync();

                var columnDefinitionEnumType = Definition.Columns.Single(x => x.Name.Equals("EnumType"));
                columnDefinitionEnumType.RelationValues = enumDefinitions.ToDictionary(x => x.Id, x => x.Name);
            }
        }

        private IEnumerable<DXRecordItem> GetVisibleItems()
        {
            if (!Definition.Name.Equals("DXColumnDefinitionElement", StringComparison.OrdinalIgnoreCase))
                return DXMultiElement.Announced;

            return DXMultiElement.Announced.Where(item => !IsSystemColumnDefinition(item));
        }

        private bool IsSystemColumnDefinition(DXRecordItem item)
        {
            if (item.Content == null)
                return false;

            if (!item.Content.TryGetValue("Name", out var raw) || raw == null)
                return false;

            var name = raw.ToString();
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return systemColumns.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        private DXColumnDefinition GetColumnDefinition(DXColumnDefinition columnDefinition, DXRecordItem dxItem)
        {
            if (dxItem.Type.Equals("DXObjectEnumElement"))
            {
                if (columnDefinition.Name.Equals("EnumType"))
                {
                    var columnDefinitionEnumType = columnDefinition.DeepClone();
                    columnDefinitionEnumType.RelationValues = enumDefinitions.ToDictionary(x => x.Id, x => x.Name);
                    return columnDefinitionEnumType;
                }
                else if (columnDefinition.Name.Equals("EnumKey"))
                {
                    var customColumnDefintion = columnDefinition.DeepClone();

                    if (!dxItem.Content.TryGetValue("EnumType", out var enumValue) || enumValue == null)
                        return columnDefinition;

                    var enumTypeAsGuid = enumValue is Guid
                        ? (Guid)enumValue
                        : Guid.Parse(enumValue.ToString()!);

                    var selectedEnumTypeDefinition = enumDefinitions.SingleOrDefault(x => x.Id == enumTypeAsGuid);

                    if (selectedEnumTypeDefinition != null)
                    {
                        customColumnDefintion.RelationValues =
                            selectedEnumTypeDefinition.DXColumnDefinitionElement.Announced
                            .Where(x => x.ColumnType == IV.DX.Kernel.Enums.DXColumnTypeEnum.Int)
                            .ToDictionary(x => x.Id, x => x.Name);

                        return customColumnDefintion;
                    }
                }
            }

            return columnDefinition;
        }
    }
}

