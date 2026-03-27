using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DX.Presentation.Application.Contracts.Models;
using IV.ManagementHub.Web.Services;
using Microsoft.JSInterop;

internal class DXDataSetViewApiClient(IInstanceClientProvider clientProvider, IJSRuntime JSRuntime) : DXUnitBaseApiClient<DXPDataSetViewUnit>(clientProvider, JSRuntime)
{

}
