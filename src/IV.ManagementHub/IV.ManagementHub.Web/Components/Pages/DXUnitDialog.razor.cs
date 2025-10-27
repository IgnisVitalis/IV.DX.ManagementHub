using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.Kernel.Models;
using IV.ManagementHub.Common.Helpers;
using IV.ManagementHub.Common.Models;
using IV.ManagementHub.Web.ApiClients;
using IV.ManagementHub.Web.Components.Custom;
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

        DXModel Content;


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

            this.Content = await LoadDXUnit(this.Type, this.ID, this._dxUnitDefinitionStructure);

            _isLoaded = true;
        }

        private DXSingleElement GetDXSingleElement(string dxElementName)
        {
            var singleElement = Content.DXSingleElements.Single(x => x.Name.Equals(dxElementName));

            return singleElement;
        }

        private DXMultiElement GetDXMultiElement(string dxElementName)
        {
            var multiElement = Content.DXMultiElements.Single(x => x.Name.Equals(dxElementName));

            return multiElement;
        }

        private DXElementDefinition GetMandatoryDXSingleElementStructure(string dxElementName)
        {
            var singleItemsDataGridDefinition = this._dxUnitDefinitionStructure.SingleItemMandatory.Single(x => x.Name.Equals(dxElementName));

            return singleItemsDataGridDefinition;
        }

        private DXElementDefinition GetOptionalDXSingleElementStructure(string dxElementName)
        {
            var singleItemsDataGridDefinition = this._dxUnitDefinitionStructure.SingleItemOptional.Single(x => x.Name.Equals(dxElementName));

            return singleItemsDataGridDefinition;
        }

        private DXElementDefinition GetMandatoryDXMultiElementStructure(string dxElementName)
        {
            var multiItemsDataGridDefinition = this._dxUnitDefinitionStructure.MultiItemsMandatory.Single(x => x.Name.Equals(dxElementName));

            return multiItemsDataGridDefinition;
        }

        private DXElementDefinition GetOptionalDXMultiElementStructure(string dxElementName)
        {
            var multiItemsDataGridDefinition = this._dxUnitDefinitionStructure.MultiItemsOptional.Single(x => x.Name.Equals(dxElementName));

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
                dxModel = DXModelFactory.Normalize(DXModel.Parse(content), structure);
            }

            return dxModel;
        }

        private async Task SaveAsync()
        {


            //var dxItemsToAdd = this.DXMultiElement.Announced.Where(x => !announcedDXItemsOriginal.Any(y => y.ID == x.ID)).ToHashSet();
            //var dxItemsToDelete = announcedDXItemsOriginal.Where(x => !this.DXMultiElement.Announced.Any(y => y.ID == x.ID)).ToHashSet();

            //this.DXMultiElement.Announced = dxItemsToAdd;
            //this.DXMultiElement.Deleted = dxItemsToDelete;




            //var actualBlock = await ESQLBlockApiCLient.Get(editedBlock.MainElement.Item.ID.Value);

            //if (actualBlock == null)
            //{
            //    //selectedBlock = 
            //        await coreApi.SaveAsync(editedBlock.ConvertToJObject());
            //}
            //else
            //{
            //    var columnsToAdd = editedBlock.DXColumnDefinitionElement.Announced.Where(x => !actualBlock.DXColumnDefinitionElement.Announced.Any(y => y.ID == x.ID)).ToList();
            //    var columnsToRemove = actualBlock.DXColumnDefinitionElement.Announced.Where(x => !editedBlock.DXColumnDefinitionElement.Announced.Any(y => y.ID == x.ID)).ToList();

            //    editedBlock.DXColumnDefinitionElement.Mode = MultiElementsMode.Target;
            //    editedBlock.DXColumnDefinitionElement.Announced = Copy(columnsToAdd);
            //    editedBlock.DXColumnDefinitionElement.Deleted = Copy(columnsToRemove);

            //    selectedBlock = await ESQLBlockApiCLient.SaveAsync(editedBlock);
            //}
            await this._coreApi.SaveAsync(this.Content.ConvertToJObject());

            if (OnSaved.HasDelegate)
                await OnSaved.InvokeAsync(this.Content);
        }

        private async Task CancelAsync()
        {
            if (OnClosed.HasDelegate)
                await OnClosed.InvokeAsync();
        }
    }
}