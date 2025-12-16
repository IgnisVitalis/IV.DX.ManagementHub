using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.ManagementHub.Common.Models.DXUnits;
using Microsoft.JSInterop;

namespace IV.ManagementHub.Web.ApiClients
{
    internal class DXNavigationItemUnitApiClient(HttpClient httpClient, IJSRuntime JSRuntime) : DXUnitBaseApiClient<DXNavigationItemUnit>(httpClient, JSRuntime)
    {

    }
}
