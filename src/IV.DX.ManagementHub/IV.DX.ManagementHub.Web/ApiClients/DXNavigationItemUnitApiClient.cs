using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DX.ManagementHub.Common.Models.DXUnits;
using IV.DX.ManagementHub.Web.Services;
using Microsoft.JSInterop;

namespace IV.DX.ManagementHub.Web.ApiClients
{
    internal class DXPNavigationItemUnitApiClient(IInstanceClientProvider clientProvider, IJSRuntime JSRuntime) : DXUnitBaseApiClient<DXPNavigationItemUnit>(clientProvider, JSRuntime)
    {
        public override async Task<IEnumerable<DXPNavigationItemUnit>> GetItemsAsync(string? dxFilter = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return await base.GetItemsAsync(dxFilter, cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return [];
            }
        }
    }
}
