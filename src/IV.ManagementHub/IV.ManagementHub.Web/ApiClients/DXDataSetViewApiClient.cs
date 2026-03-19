using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.ManagementHub.Common.Models.DXUnits;
using IV.ManagementHub.Web.Services;
using Microsoft.JSInterop;

internal class DXDataSetViewApiClient(IInstanceClientProvider clientProvider, IJSRuntime JSRuntime) : DXUnitBaseApiClient<DXDataSetViewUnit>(clientProvider, JSRuntime)
{

}
