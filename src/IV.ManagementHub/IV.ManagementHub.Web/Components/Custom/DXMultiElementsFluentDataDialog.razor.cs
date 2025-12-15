using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.Kernel.Models;
using IV.ManagementHub.Common.Models;
using IV.ManagementHub.Web.Components.Custom.Base;
using Microsoft.AspNetCore.Components;

namespace IV.ManagementHub.Web.Components.Custom
{
    public partial class DXMultiElementsFluentDataDialog : ManagementHubComponentBase
    {
        [Parameter, EditorRequired] public DXElementDefinition Definition { get; set; } = default!;
        [Parameter, EditorRequired] public DXMultiElement DXMultiElement { get; set; } = default!;
        [Parameter, EditorRequired] public DXModel Parent { get; set; } = default!;

        private readonly string[] systemColumns = new[] { "ID", "DXUnitID", "TimeStamp" };
        DXEnumApiClient _dxEnumApiClient = default!;
        DXElementApiClient _dxElementApiClient = default!;
        DXUnitApiClient _dxUnitApiClient = default!;

        [Inject]
        IApiClientResolver Resolver { get; set; } = default!;

        IEnumerable<DXEnumDefinitionUnit> enumDefinitions = new List<DXEnumDefinitionUnit>();


        protected override async Task OnInitializedAsync()
        {
            this._dxEnumApiClient = Resolver.Get<DXEnumApiClient>(base.AppKey);
            this._dxElementApiClient = Resolver.Get<DXElementApiClient>(base.AppKey);
            this._dxUnitApiClient = Resolver.Get<DXUnitApiClient>(base.AppKey);

            if (Definition.Name.Equals("DXObjectEnumElement"))
            {
                enumDefinitions = await this._dxEnumApiClient.GetItemsAsync();

                var columnDefinitionEnumType = Definition.Columns.Single(x => x.Name.Equals("EnumType"));

                columnDefinitionEnumType.RelationValues = enumDefinitions.ToDictionary(x => x.ID, x => x.Name);
            }
        }

        private void Add()
        {
            var id = Guid.NewGuid();
            var timeStamp = DateTime.UtcNow;

            var dict = new Dictionary<string, object>();

            if (Definition?.Columns != null)
            {
                foreach (var col in Definition.Columns)
                {
                    if (!DXItemEditorBaseComponent.TryApplyDefault(dict, col))
                    {
                        if (!col.AllowNull)
                            DXItemEditorBaseComponent.ApplyNonNullableFallback(dict, col);
                    }
                }
            }

            this.DXMultiElement.Add(new DXItem(Definition.Name, id, Parent.DXMainElement.Item.ID, timeStamp, dict));
        }

        private DXColumnDefinition GetColumnDefinition(DXColumnDefinition columnDefinition, DXItem dxItem)
        {
            if (dxItem.Type.Equals("DXObjectEnumElement"))
            {
                if (columnDefinition.Name.Equals("EnumType"))
                {
                    var columnDefinitionEnumType = columnDefinition.DeepClone();

                    columnDefinitionEnumType.RelationValues = enumDefinitions.ToDictionary(x => x.ID, x => x.Name);

                    return columnDefinitionEnumType;
                }
                else if (columnDefinition.Name.Equals("EnumKey"))
                {
                    var customColumnDefintion = columnDefinition.DeepClone();

                    var enumTypeAsGuid = dxItem.Content["EnumType"] is Guid
                        ? (Guid)dxItem.Content["EnumType"] : Guid.Parse(dxItem.Content["EnumType"].ToString());

                    var selectedEnumTypeDefinition = enumDefinitions.SingleOrDefault(x => x.ID == enumTypeAsGuid);

                    if (selectedEnumTypeDefinition != null)
                    {
                        customColumnDefintion.RelationValues =
                            selectedEnumTypeDefinition.DXColumnDefinitionElement.Announced
                            .Where(x => x.ColumnType == DX.Kernel.Enums.DXColumnTypeEnum.Int)
                            .ToDictionary(x => x.ID, x => x.Name);

                        return customColumnDefintion;
                    }
                }
            }

            return columnDefinition;
        }

        private void Remove(DXItem dxItem)
        {
            this.DXMultiElement.Remove(dxItem);
        }
    }
}