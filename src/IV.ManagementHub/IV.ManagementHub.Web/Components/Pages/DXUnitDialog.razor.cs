using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.ManagementHub.Common.Models;
using IV.ManagementHub.Web.ApiClients;
using IV.ManagementHub.Web.Components.Custom.Base;
using IV.ManagementHub.Web.Models;
using IV.ManagementHub.Web.Services;
using Microsoft.AspNetCore.Components;

namespace IV.ManagementHub.Web.Components.Pages
{
    public partial class DXUnitDialog : ManagementHubComponentBase
    {
        [Parameter] public string Type { get; set; } = default!;
        [Parameter] public Guid ID { get; set; } = default!;
        [Parameter] public EventCallback OnClosed { get; set; }
        [Parameter] public EventCallback OnSaved { get; set; }

        DXModelDefinition _dxUnitDefinitionStructure = default!;

        DXUnitRecordModel dxModel = default!;

        bool isAccordion = true;

        DXUnitCoreApiClient _coreApi = default!;
        DXUnitStructureApiClient _dxUnitStructureApiCLient = default!;

        [Inject]
        IApiClientResolver Resolver { get; set; } = default!;

        bool _isLoaded = false;

        protected override async Task OnInitializedAsync()
        {
            this._coreApi = await Resolver.GetAsync<DXUnitCoreApiClient>(base.AppKey);
            this._dxUnitStructureApiCLient = await this.Resolver.GetAsync<DXUnitStructureApiClient>(base.AppKey);

            this._dxUnitDefinitionStructure = await this._dxUnitStructureApiCLient.GetAsync(Type);

            await LoadDXUnit(this.Type, this.ID, this._dxUnitDefinitionStructure);

            _isLoaded = true;
        }

        private DXRecordItem GetDXMainElement()
        {
            return dxModel.MainItem;
        }

        private DXElementDefinition GetDXMainElementStructure()
        {
            return _dxUnitDefinitionStructure.MainSingleElement;
        }

        private DXRecordSingleElement GetDXSingleElement(string dxElementName)
        {
            var singleElement = dxModel.GetSingleElement(dxElementName);

            return singleElement ?? throw new InvalidOperationException($"Single element '{dxElementName}' not found.");
        }

        private DXRecordMultiElement GetDXMultiElement(string dxElementName)
        {
            var multiElement = dxModel.GetMultiElement(dxElementName);

            return multiElement ?? throw new InvalidOperationException($"Multi element '{dxElementName}' not found.");
        }

        private DXElementDefinition GetMandatoryDXSingleElementStructure(string dxElementName)
        {
            var singleItemsDataGridDefinition = this._dxUnitDefinitionStructure.RequiredSingleElements.Single(x => x.Name.Equals(dxElementName));

            return singleItemsDataGridDefinition;
        }

        private DXElementDefinition GetOptionalDXSingleElementStructure(string dxElementName)
        {
            var singleItemsDataGridDefinition = this._dxUnitDefinitionStructure.OptionalSingleElements.Single(x => x.Name.Equals(dxElementName));

            return singleItemsDataGridDefinition;
        }

        private DXElementDefinition GetMandatoryDXMultiElementStructure(string dxElementName)
        {
            var multiItemsDataGridDefinition = this._dxUnitDefinitionStructure.RequiredMultiElements.Single(x => x.Name.Equals(dxElementName));

            return multiItemsDataGridDefinition;
        }

        private DXElementDefinition GetOptionalDXMultiElementStructure(string dxElementName)
        {
            var multiItemsDataGridDefinition = this._dxUnitDefinitionStructure.OptionalMultiElements.Single(x => x.Name.Equals(dxElementName));

            return multiItemsDataGridDefinition;
        }

        private async Task LoadDXUnit(string type, Guid id, DXModelDefinition structure)
        {
            var content = await this._coreApi.GetRecord(this.Type, this.ID);

            if (content == null)
            {
                dxModel = DXRecordModelFactory.GetDefault(structure);
                return;
            }

            dxModel = DXRecordModelFactory.FromBlock(content, structure);
        }

        private async Task SaveAsync()
        {
            var block = DXRecordModelFactory.ToBlock(dxModel, _dxUnitDefinitionStructure);

            await this._coreApi.SaveRecordAsync(block);

            if (OnSaved.HasDelegate)
                await OnSaved.InvokeAsync();
        }

        private bool IsDXModelContainsSingleElement(DXElementDefinition dxElementDefinition)
        {
            var existingSingleElement = this.dxModel.GetSingleElement(dxElementDefinition.Name);

            return existingSingleElement?.Item != null;
        }

        private bool IsDXModelContainsMultiElement(DXElementDefinition dxElementDefinition)
        {
            return this.dxModel.GetMultiElement(dxElementDefinition.Name) != null;
        }

        private async Task CancelAsync()
        {
            if (OnClosed.HasDelegate)
                await OnClosed.InvokeAsync();
        }

        private void CreateSingleElement(DXElementDefinition dxElementDefinition)
        {
            var newSingleItem = GetNewDXItem(dxElementDefinition, dxModel.MainItem.ID);

            var existing = this.dxModel.GetSingleElement(dxElementDefinition.Name);
            if (existing == null)
            {
                this.dxModel.SetSingleElement(new DXRecordSingleElement(dxElementDefinition.Name, newSingleItem));
            }
            else
            {
                existing.Item = newSingleItem;
            }
        }

        private void DeleteSingleElement(DXElementDefinition dxElementDefinition)
        {
            var existing = this.dxModel.GetSingleElement(dxElementDefinition.Name);
            if (existing != null)
            {
                existing.Item = null;
            }
        }

        private DXRecordItem GetNewDXItem(DXElementDefinition item, Guid dxUnitId)
        {
            var elementID = Guid.NewGuid();

            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var column in item.Columns)
            {
                if (!DXItemEditorBaseComponent.TryApplyDefault(dict, column))
                {
                    if (!column.AllowNull)
                        DXItemEditorBaseComponent.ApplyNonNullableFallback(dict, column);
                }
            }

            return new DXRecordItem(item.Name, elementID, dxUnitId, DateTime.UtcNow, dict);
        }
    }
}

