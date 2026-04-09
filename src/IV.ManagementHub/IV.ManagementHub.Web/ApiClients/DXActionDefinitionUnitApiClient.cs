using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DX.Kernel.Models;
using IV.ManagementHub.Web.Services;
using Microsoft.JSInterop;

internal class DXActionDefinitionUnitApiClient(IInstanceClientProvider clientProvider, IJSRuntime JSRuntime)
    : DXUnitBaseApiClient<DXActionDefinitionUnit>(clientProvider, JSRuntime) { }
