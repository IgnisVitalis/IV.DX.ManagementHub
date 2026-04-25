using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.Application.Contracts.Actions;
using IV.DX.Kernel.Attributes;
using Microsoft.AspNetCore.Components;

namespace IV.DX.ManagementHub.Web.Actions
{
    [DXAction("IV.DX.ManagementHub", "Select")]
    public class SelectInstanceAction : DXUnitActionBase
    {
        private readonly IApiClientResolver _resolver;
        private readonly NavigationManager _navigation;

        public SelectInstanceAction(IApiClientResolver resolver, NavigationManager navigation)
        {
            _resolver = resolver;
            _navigation = navigation;
        }

        protected override async Task<DXActionResult> ExecuteAsync(
            Guid unitId, string unitType, DXActionParameters parameters, CancellationToken ct)
        {
            var instances = await _resolver.GetInstancesAsync(ct);
            var instance = instances.FirstOrDefault(i => i.Id == unitId);

            if (instance is null)
                return DXActionResult.Fail("Instance not found.");

            _navigation.NavigateTo($"/app/{instance.Key}");
            return DXActionResult.Ok($"Navigated to {instance.Title}.");
        }
    }
}
