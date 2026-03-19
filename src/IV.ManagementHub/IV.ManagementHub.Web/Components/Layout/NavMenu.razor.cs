using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.ManagementHub.Common.Models.DXUnits;
using IV.ManagementHub.Web.ApiClients;
using IV.ManagementHub.Web.Components.Custom.Base;
using IV.ManagementHub.Web.Models.Tree;
using Microsoft.AspNetCore.Components;

namespace IV.ManagementHub.Web.Components.Layout
{
    public partial class NavMenu : ManagementHubComponentBase
    {
        [Inject]
        IApiClientResolver Resolver { get; set; } = default!;

        DXNavigationItemUnitApiClient? _dxNavigationItemUnitApiClient;
        string? _lastAppKey;

        IReadOnlyList<BiTreeNode<DXNavigationItemUnit>> roots = [];

        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrWhiteSpace(base.AppKey))
            {
                _dxNavigationItemUnitApiClient = Resolver.Get<DXNavigationItemUnitApiClient>(base.AppKey);
            }
            _lastAppKey = base.AppKey;

            await LoadNavigationAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (string.Equals(_lastAppKey, base.AppKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _dxNavigationItemUnitApiClient = string.IsNullOrWhiteSpace(base.AppKey)
                ? null
                : Resolver.Get<DXNavigationItemUnitApiClient>(base.AppKey);
            _lastAppKey = base.AppKey;
            await LoadNavigationAsync();
        }

        private async Task LoadNavigationAsync()
        {
            if (_dxNavigationItemUnitApiClient is null)
            {
                roots = [];
                return;
            }

            try
            {
                var navigationItems = await _dxNavigationItemUnitApiClient.GetItemsAsync();
                roots = BiTreeBuilder.BuildForest(
                    navigationItems,
                    item => item.ID,
                    item => item.Parent,
                    item => item.Order);
            }
            catch
            {
                roots = [];
            }
        }
    }
}
