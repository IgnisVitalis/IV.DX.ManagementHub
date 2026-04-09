using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.ManagementHub.Web.ApiClients;
using IV.DX.ManagementHub.Web.Components.Custom.Base;
using IV.DX.ManagementHub.Web.Models;
using Microsoft.AspNetCore.Components;

namespace IV.DX.ManagementHub.Web.Components.Custom
{
    public partial class EntityBaseActionsFluentToolbar : ManagementHubComponentBase
    {
        private DXUnitCoreApiClient? _coreApiClient;
        private IReadOnlyList<DXActionButton> _resolvedActions = [];

        [Parameter] public Guid EntityID { get; set; }
        [Parameter] public string TypeName { get; set; } = string.Empty;
        [Parameter] public IReadOnlyList<DXActionButton>? Actions { get; set; }

        [Parameter] public EventCallback OnDeleted { get; set; }
        [Parameter] public EventCallback OnExported { get; set; }
        [Parameter] public EventCallback OnEdited { get; set; }

        [Inject]
        IApiClientResolver Resolver { get; set; } = default!;

        protected override async Task OnParametersSetAsync()
        {
            if (Actions is not null)
            {
                _resolvedActions = Actions;
                return;
            }

            if (string.IsNullOrWhiteSpace(TypeName))
                throw new InvalidOperationException($"{nameof(TypeName)} is required when custom {nameof(Actions)} are not provided.");

            _coreApiClient = await Resolver.GetAsync<DXUnitCoreApiClient>(AppKey);
            _resolvedActions = DXActionButtonRegistry.Build(
                DXActionButtonRegistry.DefaultActionKeys,
                new DXActionButtonContext
                {
                    EntityID = EntityID,
                    TypeName = TypeName,
                    AppKey = AppKey,
                    OnEdit = EventCallback.Factory.Create(this, EditAsync),
                    OnExport = EventCallback.Factory.Create(this, ExportAsync),
                    OnDelete = EventCallback.Factory.Create(this, DeleteAsync)
                });
        }

        private IEnumerable<DXActionButton> VisibleActions =>
            _resolvedActions.Where(action => action.Visible);

        private async Task InvokeActionAsync(DXActionButton action)
        {
            if (action.Disabled || !action.OnClick.HasDelegate)
                return;

            await action.OnClick.InvokeAsync();
        }

        private async Task DeleteAsync()
        {
            if (_coreApiClient is null)
                return;

            await _coreApiClient.DeleteAsync(TypeName, EntityID);

            if (OnDeleted.HasDelegate)
                await OnDeleted.InvokeAsync();
        }

        private async Task ExportAsync()
        {
            if (_coreApiClient is null)
                return;

            await _coreApiClient.ExportAsync(TypeName, EntityID);

            if (OnExported.HasDelegate)
                await OnExported.InvokeAsync();
        }

        private async Task EditAsync()
        {
            if (OnEdited.HasDelegate)
                await OnEdited.InvokeAsync();
        }
    }
}
