using IV.DX.Kernel.Models;
using Microsoft.JSInterop;

namespace IV.DataProvider.WebApp.Services.Web.ApiClients;

internal class DXUnitApiClient(HttpClient httpClient, IJSRuntime JSRuntime) : DXUnitBaseApiClient<DXUnitDefinitionUnit>(httpClient, JSRuntime)
{   
   
}