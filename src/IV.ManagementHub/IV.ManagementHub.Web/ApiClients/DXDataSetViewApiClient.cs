using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.ManagementHub.Common.Models.DXUnits;
using Microsoft.JSInterop;

internal class DXDataSetViewApiClient(HttpClient httpClient, IJSRuntime JSRuntime) : DXUnitBaseApiClient<DXDataSetViewUnit>(httpClient, JSRuntime)
{

}