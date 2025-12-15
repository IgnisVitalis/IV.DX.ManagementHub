using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.Kernel.Enums;
using IV.ManagementHub.Web.Components.Custom.Base;
using Microsoft.AspNetCore.Components;
using System.Linq;

namespace IV.ManagementHub.Web.Components.Layout
{
    public partial class NavMenu : ManagementHubComponentBase
    {
        [Inject]
        IApiClientResolver Resolver { get; set; } = default!;

        DXUnitApiClient _dxUnitApiClient;

        protected override async Task OnInitializedAsync()
        {
            this._dxUnitApiClient = Resolver.Get<DXUnitApiClient>(base.AppKey);

            var dxUnits = await this._dxUnitApiClient.GetItemsAsync();
            var dxEnums = await this._dxUnitApiClient.GetItemsAsync();
            
            
            var dxUnitsCore = dxUnits
                .Where(x => x.Kind == DXObjectKindEnum.Core)
                .Select(x=> new NavItem(x.Name, $"/app/{AppKey}/dxUnitSetView/{x.Name}/1"));

            var dxUnitsCustom = dxUnits
                .Where(x => x.Kind == DXObjectKindEnum.Custom)
                .Select(x => new NavItem(x.Name, $"/app/{AppKey}/dxUnitSetView/{x.Name}/2"));

            var dxEnumsCore = dxEnums
                .Where(x => x.Kind == DXObjectKindEnum.Core);

            var dxEnumsCustom = dxEnums
                .Where(x => x.Kind == DXObjectKindEnum.Custom);




            //
        }
    }

    public record NavItem(string Title, string Uri)
    {
    }
}