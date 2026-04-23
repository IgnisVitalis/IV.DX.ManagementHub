using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.ManagementHub.Web.ApiClients;
using IV.DX.ManagementHub.Web.Components.Pages;
using IV.DX.ManagementHub.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace IV.DX.ManagementHub.Web.Components.Custom.DXComponents
{
    public partial class DXPActionButtons : ComponentBase
    {
        [Inject] IApiClientResolver Resolver { get; set; } = default!;
        [Inject] IDialogService DialogService { get; set; } = default!;

        [Parameter, EditorRequired] public string AppKey { get; set; } = string.Empty;
        [Parameter, EditorRequired] public string TypeName { get; set; } = string.Empty;
        [Parameter] public IReadOnlyList<Guid> SelectedIds { get; set; } = Array.Empty<Guid>();
        [Parameter] public bool IsEditable { get; set; }
        [Parameter] public bool IsDeletable { get; set; }
        [Parameter] public bool IsExportable { get; set; }
        [Parameter] public bool ShowLabels { get; set; }
        [Parameter] public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;
        [Parameter] public EventCallback OnChanged { get; set; }
        [Parameter] public RenderFragment? ChildContent { get; set; }

        private DXUnitCoreApiClient? _coreApi;
        private bool _isRunning;

        private async Task OpenEditDialog()
        {
            var input = new DXUnitDialogInput(AppKey, TypeName, SelectedIds[0]);
            var dialog = await DialogService.ShowDialogAsync<DXUnitDialog>(input, DXUnitDialog.DefaultParameters);
            var result = await dialog.Result;
            if (!result.Cancelled)
                await OnChanged.InvokeAsync();
        }

        private async Task RequestDelete()
        {
            var input = new DXDeleteConfirmInput
            {
                AppKey = AppKey,
                TypeName = TypeName,
                Ids = SelectedIds
            };
            var dialog = await DialogService.ShowDialogAsync<DXDeleteConfirmDialog>(input, DXDeleteConfirmDialog.DefaultParameters);
            var result = await dialog.Result;
            if (!result.Cancelled)
                await OnChanged.InvokeAsync();
        }

        private async Task ExportAsync()
        {
            if (string.IsNullOrWhiteSpace(TypeName)) return;
            _coreApi ??= await Resolver.GetAsync<DXUnitCoreApiClient>(AppKey);
            _isRunning = true;
            try
            {
                if (SelectedIds.Count == 1)
                    await _coreApi.ExportAsync(TypeName, SelectedIds[0]);
                else
                    await _coreApi.ExportAsync(TypeName, SelectedIds.ToArray());
                await OnChanged.InvokeAsync();
            }
            finally
            {
                _isRunning = false;
            }
        }
    }
}
