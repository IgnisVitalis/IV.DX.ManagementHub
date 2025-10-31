using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.Kernel.Attributes;
using IV.DX.Kernel.Models;
using IV.ManagementHub.Common.Helpers;
using IV.ManagementHub.Common.Models;
using IV.ManagementHub.Web.ApiClients;
using IV.ManagementHub.Web.Components.Custom.Base;
using Microsoft.AspNetCore.Components;

namespace IV.ManagementHub.Web.Components.Pages
{
    public partial class DXUnitDialog : ManagementHubComponentBase
    {

        [Parameter] public string Type { get; set; } = default!;
        [Parameter] public Guid ID { get; set; } = default!;
        [Parameter] public EventCallback OnClosed { get; set; }
        [Parameter] public EventCallback<DXModel> OnSaved { get; set; }


        DXModelDefinition _dxUnitDefinitionStructure;

        DXModel dxModel;

        bool isAccordion = true;

        DXUnitCoreApiClient _coreApi = default!;
        DXUnitStructureApiClient _dxUnitStructureApiCLient = default!;

        [Inject]
        IApiClientResolver Resolver { get; set; } = default!;


        bool _isLoaded = false;



        protected override async Task OnInitializedAsync()
        {
            this._coreApi = Resolver.Get<DXUnitCoreApiClient>(base.AppKey);
            this._dxUnitStructureApiCLient = this.Resolver.Get<DXUnitStructureApiClient>(base.AppKey);

            this._dxUnitDefinitionStructure = await this._dxUnitStructureApiCLient.GetAsync(Type);

            this.dxModel = await LoadDXUnit(this.Type, this.ID, this._dxUnitDefinitionStructure);

            _isLoaded = true;
        }

        private DXMainElement GetDXMainElement()
        {
            return dxModel.DXMainElement;
        }

        private DXElementDefinition GetDXMainElementStructure()
        {
            return _dxUnitDefinitionStructure.MainSingleElement;
        }

        private DXSingleElement GetDXSingleElement(string dxElementName)
        {
            var singleElement = dxModel.DXSingleElements.Single(x => x.Name.Equals(dxElementName));

            return singleElement;
        }

        private DXMultiElement GetDXMultiElement(string dxElementName)
        {
            var multiElement = dxModel.DXMultiElements.Single(x => x.Name.Equals(dxElementName));

            return multiElement;
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

        private async Task<DXModel> LoadDXUnit(string type, Guid id, DXModelDefinition structure)
        {
            var content = await this._coreApi.Get(this.Type, this.ID);
            DXModel dxModel;

            if (content == null)
            {
                dxModel = DXModelFactory.GetDefault(structure);
            }
            else
            {
                dxModel = DXModel.From(content);
                DXModelFactory.Normalize(DXModel.From(content), structure);
            }

            return dxModel;
        }

        private async Task SaveAsync()
        {
            await this._coreApi.SaveAsync(this.dxModel.ToJObject());

            if (OnSaved.HasDelegate)
                await OnSaved.InvokeAsync(this.dxModel);
        }

        private bool IsDXModelContainsSingleElement(DXElementDefinition dxElementDefinition)
        {
            var existingSingleElement = this.dxModel.DXSingleElements.SingleOrDefault(x => x.Name == dxElementDefinition.Name);

            return existingSingleElement != null;
        }

        private bool IsDXModelContainsMultiElement(DXElementDefinition dxElementDefinition)
        {
            return this.dxModel.DXMultiElements.SingleOrDefault(x => x.Name == dxElementDefinition.Name) == null;
        }

        private async Task CancelAsync()
        {
            if (OnClosed.HasDelegate)
                await OnClosed.InvokeAsync();
        }

        private void CreateSingleElement(DXElementDefinition dxElementDefinition)
        {
            var newSingleElement = GetNewDXSingleElement(dxElementDefinition);

            this.dxModel.AddSingleElement(newSingleElement);
        }

        private void DeleteSingleElement(DXElementDefinition dxElementDefinition)
        {
            var existingSingleElement = GetDXSingleElement(dxElementDefinition.Name);
      
            this.dxModel.RemoveSingleElement(existingSingleElement);
        }

        private DXSingleElement GetNewDXSingleElement(DXElementDefinition item)
        {
            var dxItem = GetNewDXItem(item);

            return new DXSingleElement(item.Name, new DXElementAttribute(item.Name), dxItem, false);
        }

        private DXItem GetNewDXItem(DXElementDefinition item)
        {
            var elementID = Guid.NewGuid();

            var dict = new Dictionary<string, object>();

            foreach (var column in item.Columns)
            {
                dict.Add(column.Name, null);
            }

            return new DXItem(item.Name, elementID, this.dxModel.DXMainElement.Item.ID, DateTime.UtcNow, dict);
        }
    }
}