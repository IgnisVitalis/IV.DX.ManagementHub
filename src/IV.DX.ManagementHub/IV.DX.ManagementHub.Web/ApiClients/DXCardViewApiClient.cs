using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DX.Presentation.Application.Contracts.Models;
using IV.DX.ManagementHub.Web.Services;
using Microsoft.JSInterop;

namespace IV.DX.ManagementHub.Web.ApiClients
{
    public class DXCardViewApiClient(IInstanceClientProvider clientProvider, IJSRuntime JSRuntime)
        : DXUnitBaseApiClient<DXPCardViewUnit>(clientProvider, JSRuntime)
    {
    }
}
