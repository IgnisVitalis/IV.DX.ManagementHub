using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.ManagementHub.Web.ApiClients;
using IV.DX.ManagementHub.Web.Components.Custom.Base;
using IV.DX.ManagementHub.Web.Models;
using IV.DX.ManagementHub.Web.Models.Tree;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;

namespace IV.DX.ManagementHub.Web.Components.Layout
{
    public partial class NavMenu : ManagementHubComponentBase
    {
        [Inject] IApiClientResolver Resolver { get; set; } = default!;
        [Inject] IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] ILogger<NavMenu> Logger { get; set; } = default!;

        private static readonly Guid _navItemsQueryId = new("622f1a24-64d4-4fb2-919c-3c631bcc3189");
        private static readonly Guid _cardViewDefinitionId = new("91ef7b1b-811f-4b84-804c-baacbf4424ac");
        private static readonly Guid _dataSetViewDefinitionId = new("a8e5858b-fe0e-4de3-a750-073568417fc0");

        IReadOnlyList<BiTreeNode<DXPNavItem>> roots = [];
        private string _loadedAppKey = "\0";

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
                var queryClient = await Resolver.GetAsync<DXQueryResultApiClient>(key);
                var result = await queryClient.GetAsync(_navItemsQueryId, null);

                var items = result.Content.Select(row => new DXPNavItem
                {
                    ID = result.GetID(row),
                    Name = row["Name"]?.Value<string>() ?? string.Empty,
                    ParentID = ParseOptionalGuid(row, "ParentID"),
                    Order = row["Order"]?.Value<int>() ?? 0,
                    ComponentType = ParseOptionalGuid(row, "ComponentType"),
                    ComponentID = ParseOptionalGuid(row, "ComponentID")
                });

                roots = BiTreeBuilder.BuildForest(items, i => i.ID, i => i.ParentID, i => i.Order);
            }
            catch (Exception ex)
            {
                roots = [];
                Logger.LogError(ex, "NavMenu failed to load navigation for instance {AppKey}.", key);
            }
        }

        internal string? GetComponentHref(Guid? componentType, Guid? componentId)
        {
            if (componentType is null || componentId is null) return null;
            var route = componentType.Value == _cardViewDefinitionId ? "dxUnitCardView" : "dxUnitSetView";
            return $"/app/{AppKey}/{route}/{componentId}";
        }

        private static Guid? ParseOptionalGuid(JObject row, string key)
        {
            var token = row[key];
            if (token == null || token.Type == JTokenType.Null) return null;
            return Guid.TryParse(token.ToString(), out var g) ? g : null;
        }
    }
}
