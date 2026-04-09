using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.Kernel.Models;
using IV.DX.ManagementHub.ApiService.Services;
using IV.DX.ManagementHub.Common.Models;
using IV.DX.ManagementHub.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace IV.DataProvider.WebApp.Services.Web.Services;

public sealed class ApiClientResolver(
    InstanceApiClientFactory instanceFactory,
    IJSRuntime jsRuntime,
    NavigationManager navigationManager,
    IConfiguration configuration,
    AppAuthState authState,
    IHttpClientFactory httpClientFactory,
    IServiceProvider sp) : IApiClientResolver
{
    private readonly Dictionary<string, (string BaseUrl, string ServiceKey)> _resolved =
        new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<MHInstanceUnit>? _instances;

    public async Task<T> GetAsync<T>(string? instanceKey) where T : class
    {
        IInstanceClientProvider provider;

        if (!string.IsNullOrWhiteSpace(instanceKey))
        {
            var normalizedKey = instanceKey.Trim();
            if (!_resolved.TryGetValue(normalizedKey, out _))
            {
                await EnsureInstancesLoadedAsync();
                if (!_resolved.TryGetValue(normalizedKey, out _))
                    throw new InvalidOperationException(
                        $"No DX instance found for key '{normalizedKey}'.");
            }

            var selfUrl = navigationManager.BaseUri.TrimEnd('/');
            provider = new ProxyInstanceClientProvider(httpClientFactory, authState, selfUrl, normalizedKey);
        }
        else
        {
            var selfUrl = navigationManager.BaseUri.TrimEnd('/');
            var serviceKey = configuration["Secrets:ServiceKey"] ?? string.Empty;
            provider = new DirectInstanceClientProvider(instanceFactory, selfUrl, serviceKey);
        }

        return (T)ActivatorUtilities.CreateInstance(sp, typeof(T), provider, jsRuntime);
    }

    public async Task<IReadOnlyList<MHInstanceUnit>> GetInstancesAsync(CancellationToken ct = default)
    {
        if (_instances is null)
            await EnsureInstancesLoadedAsync(ct);
        return _instances!;
    }

    private async Task EnsureInstancesLoadedAsync(CancellationToken ct = default)
    {
        var selfUrl = navigationManager.BaseUri.TrimEnd('/');
        var serviceKey = configuration["Secrets:ServiceKey"] ?? string.Empty;
        var http = await instanceFactory.CreateAsync(selfUrl, serviceKey, ct);
        var response = await http.GetAsync("api/management/MHInstanceUnit", ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        var units = DXUnit.ParseItems<MHInstanceUnit>(json).ToList();
        foreach (var unit in units.Where(u => !string.IsNullOrWhiteSpace(u.Key)))
            _resolved[unit.Key] = (unit.BaseUrl, unit.ServiceKey);
        _instances = units;
    }
}
