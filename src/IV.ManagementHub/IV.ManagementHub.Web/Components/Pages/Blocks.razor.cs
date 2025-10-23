using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.Kernel.Models;
using IV.ManagementHub.Web.Components.Custom;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace IV.ManagementHub.Web.Components.Pages
{
    public partial class Blocks : ManagementHubComponentBase
    {
        [Inject]
        IApiClientResolver Resolver { get; set; } = default!;

        DXElementApiClient ESQLBlockApiCLient = default!;

        protected override async Task OnParametersSetAsync()
        {
            ESQLBlockApiCLient = Resolver.Get<DXElementApiClient>(base.AppKey);
            await LoadDataAsync(true);
        }

        private bool _isInitialLoading;
        private bool _isRefreshing;
        private bool _isSaving;
        private bool _collapse = true;

        private List<DXElementDefinitionUnit> blocks = new();
        private DXElementDefinitionUnit? selectedBlock;

        private readonly string[] systemColumns = new[] { "ID", "DXUnitID", "TimeStamp" };

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
                selectedBlock = await LoadDetailsAsync(selectedItem.ID);
            }
            else
            {
                var newID = Guid.NewGuid();

                selectedBlock = new DXElementDefinitionUnit
                {
                    ID = newID,
                    
                    DXObjectDefinitionMainElement = new DXObjectDefinitionMainElement
                    {
                        ID = Guid.NewGuid(),
                        DXUnitID = newID,
                        Name = string.Empty,
                        DisplayValue = null
                    },
                    DXColumnDefinitionElement = new DXMultiElementsContainer<DXColumnDefinitionElement>
                    {
                        Mode = MultiElementsMode.Target,
                        Announced = new HashSet<DXColumnDefinitionElement>(),
                        Deleted = new HashSet<DXColumnDefinitionElement>()
                    }
                };
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

        private async Task SaveDialog(DXElementDefinitionUnit editedBlock)
        {
            var actualBlock = await ESQLBlockApiCLient.Get(editedBlock.ID);

            if (actualBlock == null)
            {
                selectedBlock = await ESQLBlockApiCLient.SaveAsync(editedBlock);
            }
            else
            {
                var columnsToAdd = editedBlock.DXColumnDefinitionElement.Announced.Where(x => !actualBlock.DXColumnDefinitionElement.Announced.Any(y => y.ID == x.ID)).ToList();
                var columnsToRemove = actualBlock.DXColumnDefinitionElement.Announced.Where(x => !editedBlock.DXColumnDefinitionElement.Announced.Any(y => y.ID == x.ID)).ToList();

                editedBlock.DXColumnDefinitionElement.Mode = MultiElementsMode.Target;
                editedBlock.DXColumnDefinitionElement.Announced = Copy(columnsToAdd);
                editedBlock.DXColumnDefinitionElement.Deleted = Copy(columnsToRemove);

                selectedBlock = await ESQLBlockApiCLient.SaveAsync(editedBlock);
            }

            await this.LoadDataAsync(false);

            this.CloseDialog();
        }

        private HashSet<DXColumnDefinitionElement> Copy(IEnumerable<DXColumnDefinitionElement> columns) =>
            columns
                .Where(x => !systemColumns.Contains(x.Name))
                .Select(x => new DXColumnDefinitionElement
                {
                    ID = x.ID,
                    DXUnitID = selectedBlock!.ID,
                    Name = x.Name,
                    ColumnType = x.ColumnType,
                    AllowNull = x.AllowNull,
                    Length = x.Length,
                    Precision = x.Precision,
                    Scale = x.Scale,
                    DefaultValue = x.DefaultValue
                })
                .ToHashSet();


        Orientation orientation = Orientation.Horizontal;

        private void OnResizedHandler(SplitterResizedEventArgs args)
        {

        }
    }
}