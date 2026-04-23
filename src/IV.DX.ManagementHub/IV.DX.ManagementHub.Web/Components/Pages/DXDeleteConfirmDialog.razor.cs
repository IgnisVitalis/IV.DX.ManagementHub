using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.ManagementHub.Web.ApiClients;
using IV.DX.ManagementHub.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace IV.DX.ManagementHub.Web.Components.Pages
{
    public partial class DXDeleteConfirmDialog : ComponentBase, IDialogContentComponent<DXDeleteConfirmInput>
    {
        public static DialogParameters DefaultParameters => new()
        {
            Width = "400px",
            Modal = true
        };

        [CascadingParameter] public FluentDialog Dialog { get; set; } = default!;
        [Parameter] public DXDeleteConfirmInput Content { get; set; } = default!;

        [Inject] IApiClientResolver Resolver { get; set; } = default!;

        private bool _isRunning;
        private string? _errorMessage;

        private async Task ConfirmAsync()
        {
            _isRunning = true;
            _errorMessage = null;
            try
            {
                var coreApi = await Resolver.GetAsync<DXUnitCoreApiClient>(Content.AppKey);
                foreach (var id in Content.Ids)
                    await coreApi.DeleteAsync(Content.TypeName, id);
                await Dialog.CloseAsync();
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
            }
            finally
            {
                _isRunning = false;
            }
        }

        private async Task CancelAsync() => await Dialog.CancelAsync();
    }
}
