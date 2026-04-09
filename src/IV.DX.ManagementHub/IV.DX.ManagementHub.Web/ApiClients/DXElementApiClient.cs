using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DX.Kernel.Models;
using IV.DX.ManagementHub.Web.Services;
using Microsoft.JSInterop;

namespace IV.DataProvider.WebApp.Services.Web.ApiClients;

internal class DXElementApiClient(IInstanceClientProvider clientProvider, IJSRuntime JSRuntime) : DXUnitBaseApiClient<DXElementDefinitionUnit>(clientProvider, JSRuntime)
{

}
