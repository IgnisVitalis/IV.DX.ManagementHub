using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.ManagementHub.Common.Models.DXUnits;
using IV.ManagementHub.Web.Services;
using Microsoft.JSInterop;

namespace IV.ManagementHub.Web.ApiClients
{
    internal class DXPNavigationItemUnitApiClient(IInstanceClientProvider clientProvider, IJSRuntime JSRuntime) : DXUnitBaseApiClient<DXPNavigationItemUnit>(clientProvider, JSRuntime)
    {

    }
}
