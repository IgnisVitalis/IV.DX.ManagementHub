using IV.DX.Application.Contracts.Actions;
using IV.DX.Kernel.Models;
using IV.DX.ManagementHub.Web.ApiClients;
using IV.DX.ManagementHub.Web.Components.Custom.Base;
using IV.DX.ManagementHub.Web.Components.Pages;
using IV.DX.ManagementHub.Web.Models;
using IV.DX.Presentation.Application.Contracts.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Newtonsoft.Json.Linq;

namespace IV.DX.ManagementHub.Web.Components.Custom.DXComponents
{
    public partial class DXPCardView : DXPComponent<DXPCardViewUnit, DXCardViewApiClient>
    {
        [Inject] IDXActionExecutor ActionExecutor { get; set; } = default!;
        [Inject] IDialogService DialogService { get; set; } = default!;

        private DXUnitCoreApiClient? _coreApi;
        private DXPClickActionUnitApiClient? _clickActionApiClient;
        private DXActionDefinitionUnitApiClient? _actionDefinitionApiClient;

        private string _typeName = string.Empty;
        private readonly List<JObject> _cardItems = new();
        private DXActionDefinitionUnit? _clickActionDef;

        protected override async Task LoadDataAsync()
        {
            _coreApi ??= await Resolver.GetAsync<DXUnitCoreApiClient>(AppKey);
            _clickActionApiClient ??= await Resolver.GetAsync<DXPClickActionUnitApiClient>(AppKey);
            _actionDefinitionApiClient ??= await Resolver.GetAsync<DXActionDefinitionUnitApiClient>(AppKey);

            _cardItems.Clear();
            _typeName = string.Empty;
            _clickActionDef = null;

            var block = await _coreApi.GetItemsByDefinitionAsync(ComponentUnit!.DXUnitDefinition);
            if (block is null) return;

            _typeName = block["Meta"]?.Value<string>("Type") ?? string.Empty;
            var items = (block["Data"]?["Items"] as JArray ?? new JArray()).OfType<JObject>();
            _cardItems.AddRange(items);

            if (ComponentUnit.DXPClickAction is { } clickActionId)
            {
                var clickAction = await _clickActionApiClient.Get(clickActionId);
                if (clickAction is not null)
                    _clickActionDef = await _actionDefinitionApiClient.Get(clickAction.ActionDefinition);
            }
        }

        private async Task OnCardClickedAsync(Guid itemId)
        {
            if (_clickActionDef is null) return;
            var parameters = new DXActionParameters()
                .Set("UnitId", itemId)
                .Set("UnitType", _typeName);
            await ActionExecutor.ExecuteAsync(_clickActionDef.Module, _clickActionDef.Key, parameters);
        }

        private async Task OpenCreateDialog()
        {
            var input = new DXUnitDialogInput(AppKey, _typeName, Guid.NewGuid());
            var dialog = await DialogService.ShowDialogAsync<DXUnitDialog>(input, DXUnitDialog.DefaultParameters);
            var result = await dialog.Result;
            if (!result.Cancelled)
                await ReloadAsync();
        }

        private async Task OnCardActionChanged() => await ReloadAsync();
    }
}
