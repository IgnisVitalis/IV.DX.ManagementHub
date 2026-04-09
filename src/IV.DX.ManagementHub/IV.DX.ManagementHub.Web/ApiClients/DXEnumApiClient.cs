using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DX.Kernel.Models;
using IV.DX.ManagementHub.Web.Services;
using Microsoft.JSInterop;

namespace IV.DataProvider.WebApp.Services.Web.ApiClients;

internal class DXEnumApiClient(IInstanceClientProvider clientProvider, IJSRuntime JSRuntime) : DXUnitBaseApiClient<DXEnumDefinitionUnit>(clientProvider, JSRuntime)
{
}
