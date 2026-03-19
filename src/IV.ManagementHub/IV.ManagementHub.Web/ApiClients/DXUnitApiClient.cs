using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DX.Kernel.Models;
using IV.ManagementHub.Web.Services;
using Microsoft.JSInterop;

namespace IV.DataProvider.WebApp.Services.Web.ApiClients;

internal class DXUnitApiClient(IInstanceClientProvider clientProvider, IJSRuntime JSRuntime) : DXUnitBaseApiClient<DXUnitDefinitionUnit>(clientProvider, JSRuntime)
{

}
