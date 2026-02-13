using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.ManagementHub.Web.Services;
using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace IV.DataProvider.WebApp.Services.Web.Services
{
    public sealed class ApiClientResolver(
        IHttpClientFactory factory,
        IJSRuntime jsRuntime,
        AppAuthState authState,
        IServiceProvider sp) : IApiClientResolver
    {
        public T Get<T>(string? sourceKey) where T : class
        {
            var name = (sourceKey ?? "base").ToLowerInvariant() switch
            {
                "base" => "Base",
                "lit" => "Lit",
                _ => "Base"
            };

            var http = factory.CreateClient(name);
            http.DefaultRequestHeaders.Authorization = authState.IsAuthenticated
                ? new AuthenticationHeaderValue("Bearer", authState.AccessToken)
                : null;

            return (T)ActivatorUtilities.CreateInstance(sp, typeof(T), http, jsRuntime);
        }
    }
}
