using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.ManagementHub.Web.ApiClients;
using IV.DX.ManagementHub.Common.Models.DXUnits;
using IV.DX.ManagementHub.Web.Components.Custom.Base;
using IV.DX.ManagementHub.Web.Models.Tree;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace IV.DX.ManagementHub.Web.Components.Layout
{
    public partial class NavMenu : ManagementHubComponentBase
    {
        [Inject] IApiClientResolver Resolver { get; set; } = default!;
        [Inject] IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] ILogger<NavMenu> Logger { get; set; } = default!;

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
                Logger.LogError(ex, "NavMenu failed to load navigation for instance {AppKey}.", key);
            }
        }
    }
}
