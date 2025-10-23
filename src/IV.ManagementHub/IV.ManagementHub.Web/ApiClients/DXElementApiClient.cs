using IV.DX.Kernel.Models;
using Microsoft.JSInterop;

namespace IV.DataProvider.WebApp.Services.Web.ApiClients;

internal class DXElementApiClient(HttpClient httpClient, IJSRuntime JSRuntime) : DXUnitBaseApiClient<DXElementDefinitionUnit>(httpClient, JSRuntime)
{

}