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

        DXNavigationItemUnitApiClient _dxNavigationItemUnitApiClient;

        IReadOnlyList<BiTreeNode<DXNavigationItemUnit>> roots;

        protected override async Task OnInitializedAsync()
        {

            this._dxNavigationItemUnitApiClient = Resolver.Get<DXNavigationItemUnitApiClient>(base.AppKey);

            var navigationItems = await this._dxNavigationItemUnitApiClient.GetItemsAsync();


            roots = BiTreeBuilder.BuildForest(
                navigationItems,
                x => x.ID,
                x => x.Parent,
                x => x.Order);
                      
            //var index = roots.BuildIndexById();
                       
            //var node = index.GetById(navigationItems.Skip(2).First().ID);
                      
            //var breadcrumbNames = node.PathFromRoot().Select(n => n.Item.Name);
                      
            //var breadcrumbItems = node.ItemPathFromRoot();

            //var leafNodes = roots.Leaves();



            //
        }
    }
}