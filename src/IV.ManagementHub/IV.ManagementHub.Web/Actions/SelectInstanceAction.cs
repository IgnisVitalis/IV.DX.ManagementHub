using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.Application.Contracts.Actions;
using IV.DX.Kernel.Attributes;
using IV.DX.Kernel.Models;
using Microsoft.AspNetCore.Components;

namespace IV.ManagementHub.Web.Actions
{
    [DXAction("IV.ManagementHub", "Select")]
    public class SelectInstanceAction : DXActionBase
    {
        private readonly IApiClientResolver _resolver;
        private readonly NavigationManager _navigation;

        public SelectInstanceAction(IApiClientResolver resolver, NavigationManager navigation)
        {
            _resolver = resolver;
            _navigation = navigation;
        }

        [DXActionParameter("InstanceId", DXActionParameterDirectionEnum.In)]
        public Guid InstanceId { get; set; }

        public override async Task<DXActionResult> ExecuteAsync(CancellationToken ct)
        {
            var instances = await _resolver.GetInstancesAsync(ct);
            var instance = instances.FirstOrDefault(i => i.ID == InstanceId);

            if (instance is null)
                return DXActionResult.Fail("Instance not found.");

            _navigation.NavigateTo($"/app/{instance.Key}");
            return DXActionResult.Ok($"Navigated to {instance.Title}.");
        }
    }
}
