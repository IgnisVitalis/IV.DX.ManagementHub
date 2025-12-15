using IV.DX.Kernel.Models;
using Microsoft.JSInterop;

namespace IV.DataProvider.WebApp.Services.Web.ApiClients;

internal class DXEnumApiClient(HttpClient httpClient, IJSRuntime JSRuntime) : DXUnitBaseApiClient<DXEnumDefinitionUnit>(httpClient, JSRuntime)
{
}