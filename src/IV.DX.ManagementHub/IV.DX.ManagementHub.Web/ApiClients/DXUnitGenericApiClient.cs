using IV.DX.Kernel.Models;
using IV.DX.ManagementHub.Web.Services;
using Microsoft.JSInterop;

namespace IV.DataProvider.WebApp.Services.Web.ApiClients
{
    public abstract class DXUnitGenericApiClient<T>(IInstanceClientProvider clientProvider, IJSRuntime JSRuntime) where T : DXUnit
    {
        protected readonly IInstanceClientProvider ClientProvider = clientProvider;
        protected readonly IJSRuntime JSRuntime = JSRuntime;
    }
}
