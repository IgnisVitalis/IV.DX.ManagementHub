using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.ManagementHub.Web.ApiClients;
using IV.ManagementHub.Common.Models.DXUnits;
using IV.ManagementHub.Web.Components.Custom.Base;
using IV.ManagementHub.Web.Models.Tree;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace IV.ManagementHub.Web.Components.Layout
{
    public partial class NavMenu : ManagementHubComponentBase
    {
        [Inject] IApiClientResolver Resolver { get; set; } = default!;
        [Inject] IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] IV.ManagementHub.Web.Services.ConsoleLogService Log { get; set; } = default!;

        IReadOnlyList<BiTreeNode<DXPNavigationItemUnit>> roots = [];
        private string _loadedAppKey = "\0"; // sentinel — never equal to a real key

        protected override async Task OnParametersSetAsync()
        {
            var key = AppKey ?? string.Empty;
            if (string.Equals(_loadedAppKey, key, StringComparison.OrdinalIgnoreCase))
                return;

            _loadedAppKey = key;

            if (string.IsNullOrEmpty(key))
            {
                roots = [];
                return;
            }

            try
            {
                var client = await Resolver.GetAsync<DXPNavigationItemUnitApiClient>(key);
                var items = await client.GetItemsAsync();
                roots = BiTreeBuilder.BuildForest(
                    items,
                    item => item.ID,
                    item => item.Parent,
                    item => item.Order);
            }
            catch (Exception ex)
            {
                roots = [];
                Log.Error($"[NavMenu:{key}] {ex.Message}");
            }
        }
    }
}
