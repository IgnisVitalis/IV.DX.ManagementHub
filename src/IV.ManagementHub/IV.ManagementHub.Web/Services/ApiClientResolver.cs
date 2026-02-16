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
        ApiSourceCatalog apiSourceCatalog,
        IServiceProvider sp) : IApiClientResolver
    {
        public T Get<T>(string? sourceKey) where T : class
        {
            var requestedInstanceKey = !string.IsNullOrWhiteSpace(sourceKey)
                ? sourceKey
                : authState.AppKey;

            var source = apiSourceCatalog.Resolve(authState.AppKey);

            var http = factory.CreateClient(source.HttpClientName);
            http.DefaultRequestHeaders.Authorization = authState.IsAuthenticated
                ? new AuthenticationHeaderValue("Bearer", authState.AccessToken)
                : null;
            http.DefaultRequestHeaders.Remove("X-MH-Instance");
            if (!string.IsNullOrWhiteSpace(requestedInstanceKey))
            {
                http.DefaultRequestHeaders.Add("X-MH-Instance", requestedInstanceKey);
            }

            return (T)ActivatorUtilities.CreateInstance(sp, typeof(T), http, jsRuntime);
        }
    }
}
