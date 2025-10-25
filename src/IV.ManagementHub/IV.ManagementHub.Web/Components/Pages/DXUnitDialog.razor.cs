using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.Kernel.Enums;
using IV.DX.Kernel.Models;
using IV.ManagementHub.Common.Models;
using IV.ManagementHub.Web.ApiClients;
using IV.ManagementHub.Web.Components.Custom;
using IV.ManagementHub.Web.Models;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json.Linq;

namespace IV.ManagementHub.Web.Components.Pages
{
    public partial class DXUnitDialog : ManagementHubComponentBase
    {

        [Parameter] public DXModel Content { get; set; } = default!;
        [Parameter] public EventCallback OnClosed { get; set; }
        [Parameter] public EventCallback<DXModel> OnSaved { get; set; }

        DXUnitStructureApiClient _dxUnitStructureApiCLient;


        DXElementDefinitionStructure multiItemsDataGridDefinition { get; set; }
        DXMultiElement MultiElement { get; set; }



        [Inject]
        IApiClientResolver Resolver { get; set; } = default!;

        protected override async Task OnParametersSetAsync()
        {
            if (Content != null)
            {
                this._dxUnitStructureApiCLient = this.Resolver.Get<DXUnitStructureApiClient>(base.AppKey);

                var dxUnitType = Content.MainElement.Item.GetValue<string>("S_Type");

                var dxElementStructure = await this._dxUnitStructureApiCLient.GetAsync(dxUnitType);



                multiItemsDataGridDefinition = dxElementStructure.MultiItemsOptional.Single(x => x.Name.Equals("DXColumnDefinitionElement"));

                MultiElement = Content.DXMultiElements.First(x => x.Name.Equals("DXColumnDefinitionElement"));
            }
        }

        private async Task SaveAsync()
        {


            //var dxItemsToAdd = this.DXMultiElement.Announced.Where(x => !announcedDXItemsOriginal.Any(y => y.ID == x.ID)).ToHashSet();
            //var dxItemsToDelete = announcedDXItemsOriginal.Where(x => !this.DXMultiElement.Announced.Any(y => y.ID == x.ID)).ToHashSet();

            //this.DXMultiElement.Announced = dxItemsToAdd;
            //this.DXMultiElement.Deleted = dxItemsToDelete;

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