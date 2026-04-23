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
        [Inject] IDialogService DialogService { get; set; } = default!;

        private DXUnitCoreApiClient? _coreApi;
        private string _typeName = string.Empty;
        private readonly List<JObject> _cardItems = new();

        protected override async Task LoadDataAsync()
        {
            _coreApi ??= await Resolver.GetAsync<DXUnitCoreApiClient>(AppKey);

            _cardItems.Clear();
            _typeName = string.Empty;

            var block = await _coreApi.GetItemsByDefinitionAsync(ComponentUnit!.DXUnitDefinition);
            if (block is null) return;

            _typeName = block["Meta"]?.Value<string>("Type") ?? string.Empty;
            var items = (block["Data"]?["Items"] as JArray ?? new JArray()).OfType<JObject>();
            _cardItems.AddRange(items);
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
