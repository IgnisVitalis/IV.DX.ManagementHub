using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.ManagementHub.ApiService.Bootstrap;
using IV.ManagementHub.ApiService.Services;
using IV.ManagementHub.Web.Services;
using Microsoft.JSInterop;

namespace IV.DataProvider.WebApp.Services.Web.Services
{
    public sealed class ApiClientResolver(
        BootstrapSettingsSnapshot settingsSnapshot,
        InstanceApiClientFactory instanceFactory,
        IJSRuntime jsRuntime,
        IServiceProvider sp) : IApiClientResolver
    {
        public T Get<T>(string? instanceKey) where T : class
        {
            var settings = settingsSnapshot.Current;
            var instance = settings?.ResolveInstance(instanceKey)
                ?? settings?.Instances.FirstOrDefault();

            if (instance is null)
                throw new InvalidOperationException(
                    $"No DX instance found for key '{instanceKey}'. Ensure at least one instance is registered.");

            var provider = new InstanceClientProvider(instanceFactory, instance.ApiUrl, instance.ServiceKey);
            return (T)ActivatorUtilities.CreateInstance(sp, typeof(T), provider, jsRuntime);
        }
    }
}
