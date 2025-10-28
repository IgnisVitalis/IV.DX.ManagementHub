using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.Kernel.Models;
using IV.ManagementHub.Web.ApiClients;
using IV.ManagementHub.Web.Components.Custom.Base;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace IV.ManagementHub.Web.Components.Pages
{
    public partial class Blocks : ManagementHubComponentBase
    {
        [Inject]
        IApiClientResolver Resolver { get; set; } = default!;

        DXElementApiClient ESQLBlockApiCLient = default!;
        DXUnitCoreApiClient coreApi = default!;

        protected override async Task OnParametersSetAsync()
        {
            ESQLBlockApiCLient = Resolver.Get<DXElementApiClient>(base.AppKey);
            coreApi = Resolver.Get<DXUnitCoreApiClient>(base.AppKey);

            await LoadDataAsync(true);
        }

        private bool _isInitialLoading;
        private bool _isRefreshing;
        private bool _isSaving;
        private bool _collapse = true;

        private List<DXElementDefinitionUnit> blocks = new();
        private DXElementDefinitionUnit? selectedBlock;

        
        private Guid selectedItemID{ get; set; }

        private string selectedItemType
        {
            get
            {
                return "DXElementDefinitionUnit";
            }
        }

        private bool isEditing = false;
        private bool showDetails = false;
        private string editBlockName = string.Empty;

        private async Task LoadDataAsync(bool initial)
        {
            if (initial) _isInitialLoading = true; else _isRefreshing = true;
            try
            {
                var blks = await ESQLBlockApiCLient.GetAllAsync();
                blocks = blks?.ToList() ?? new();
            }
            finally
            {
                _isInitialLoading = false;
                _isRefreshing = false;
                StateHasChanged();
            }
        }

        private async Task OnDeleted()
        {
            await this.LoadDataAsync(false);

            this.OnClosed();
        }

        private async Task OpenEditDialog(DXElementDefinitionUnit? selectedItem = null)
        {
            if (selectedItem != null)
            {
                selectedItemID = selectedItem.ID;
            }
            else
            {
                selectedItemID = Guid.NewGuid();             
            }

            _showDialog = true;
        }

        private async Task OpenPanelRightAsync(Guid selectedBlockID)
        {
            selectedBlock = await LoadDetailsAsync(selectedBlockID);

            _collapse = false;
        }

        private async Task<DXElementDefinitionUnit> LoadDetailsAsync(Guid id)
        {
            var item = await ESQLBlockApiCLient.Get(id);
            return item;
        }

        private void OnClosed()
        {
            selectedBlock = null;
            _collapse = true;
        }

        bool _showDialog = false;

        private void CloseDialog()
        {
            _showDialog = false;
        }

        private async Task OnSaved()
        {
            await this.LoadDataAsync(false);

            this.CloseDialog();
        }

        Orientation orientation = Orientation.Horizontal;

        private void OnResizedHandler(SplitterResizedEventArgs args)
        {

        }
    }
}