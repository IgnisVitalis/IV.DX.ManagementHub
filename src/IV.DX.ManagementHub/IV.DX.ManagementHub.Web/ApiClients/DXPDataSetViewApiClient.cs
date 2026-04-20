using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DX.Presentation.Application.Contracts.Models;
using IV.DX.ManagementHub.Web.Services;
using Microsoft.JSInterop;

public class DXPDataSetViewApiClient(IInstanceClientProvider clientProvider, IJSRuntime JSRuntime) : DXUnitBaseApiClient<DXPDataSetViewUnit>(clientProvider, JSRuntime)
{

}
