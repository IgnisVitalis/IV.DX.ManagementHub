using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.ManagementHub.Web.ApiClients;
using Microsoft.AspNetCore.Components;

namespace IV.ManagementHub.Web.Components.Custom
{
    public partial class EntityBaseActionsFluentToolbar : ManagementHubComponentBase
    {
        DXUnitCoreApiClient _coreApiCLient;
        [Parameter, EditorRequired] public Guid EntityID { get; set; }
        [Parameter, EditorRequired] public string TypeName { get; set; }

        [Parameter] public EventCallback OnDeleted { get; set; }
        [Parameter] public EventCallback OnExported { get; set; }
        [Parameter] public EventCallback OnEdited { get; set; }

        [Inject]
        IApiClientResolver Resolver { get; set; } = default!;

        protected override async Task OnParametersSetAsync()
        {
            this._coreApiCLient = this.Resolver.Get<DXUnitCoreApiClient>(base.AppKey);            
        }

        private async Task DeleteAsync()
        {
            await this._coreApiCLient.DeleteAsync(this.TypeName, this.EntityID);

            if (OnDeleted.HasDelegate)
                await OnDeleted.InvokeAsync();
        }

        private async Task ExportAsync()
        {
            await this._coreApiCLient.ExportAsync(this.TypeName, this.EntityID);

            if (OnExported.HasDelegate)
                await OnExported.InvokeAsync();
        }

        private async Task EditAsync()
        {
            if (OnEdited.HasDelegate)
                await OnEdited.InvokeAsync();
        }
    }
}