using IV.DataProvider.WebApp.Services.Web.Contracts;
using Microsoft.JSInterop;

namespace IV.DataProvider.WebApp.Services.Web.Services
{
    public sealed class ApiClientResolver(
        IHttpClientFactory factory,
        IJSRuntime jsRuntime,
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

            return (T)ActivatorUtilities.CreateInstance(sp, typeof(T), http, jsRuntime);
        }
    }
}