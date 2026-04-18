using IV.DX.ManagementHub.Web.ApiClients;
using IV.DX.ManagementHub.Web.Components.Custom.Base;
using IV.DX.Presentation.Application.Contracts.Models;
using Newtonsoft.Json.Linq;

namespace IV.DX.ManagementHub.Web.Components.Pages
{
    public partial class DXUnitCardView : DXPComponent<DXPCardViewUnit, DXCardViewApiClient>
    {
        private DXUnitCoreApiClient? _coreApi;
        private string _typeName = string.Empty;
        private readonly List<JObject> _cardItems = new();
        private bool _showDialog;
        private Guid _dialogItemId;

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

        private void OpenDialog(Guid id)
        {
            _dialogItemId = id;
            _showDialog = true;
        }

        private void CloseDialog() => _showDialog = false;

        private async Task OnSaved()
        {
            _showDialog = false;
            await ReloadAsync();
        }
    }
}
