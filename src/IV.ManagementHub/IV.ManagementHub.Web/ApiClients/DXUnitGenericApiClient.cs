using IV.DX.Kernel.Models;
using Microsoft.JSInterop;

namespace IV.DataProvider.WebApp.Services.Web.ApiClients
{
    internal abstract class DXUnitGenericApiClient<T>(HttpClient httpClient, IJSRuntime JSRuntime) where T : DXUnit
    {
    }
}