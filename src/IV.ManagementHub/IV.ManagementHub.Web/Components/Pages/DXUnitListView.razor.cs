using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.ManagementHub.Web.ApiClients;
using IV.ManagementHub.Web.Components.Custom.Base;
using IV.ManagementHub.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Newtonsoft.Json.Linq;
using System.Data;

namespace IV.ManagementHub.Web.Components.Pages
{
    public partial class DXUnitListView : ManagementHubComponentBase
    {
        [Inject]
        IApiClientResolver Resolver { get; set; } = default!;

        DXUnitCoreApiClient coreApi = default!;
        DXQueryResultApiClient dxQueryApi = default!;
        DXDataSetViewApiClient dxDataSetViewApiClient = default!;

        private Guid _dxDataSetViewID;

        [Parameter, EditorRequired]
        public string dxDataSetViewID
        {
            get
            {
                return this._dxDataSetViewID.ToString();
            }
            set
            {
                this._dxDataSetViewID = Guid.Parse(value);
            }
        }

        private DXQueryResult dxQueryResult;
        private IDictionary<string, object> rows;

        protected override async Task OnParametersSetAsync()
        {
            coreApi = Resolver.Get<DXUnitCoreApiClient>(base.AppKey);
            dxQueryApi = Resolver.Get<DXQueryResultApiClient>(base.AppKey);
            dxDataSetViewApiClient = Resolver.Get<DXDataSetViewApiClient>(base.AppKey);

            await LoadDataAsync(true);
        }

        private bool _isInitialLoading;
        private bool _isRefreshing;
        private bool _isSaving;
        private bool _collapse = true;

        private IEnumerable<JObject> dxUnits = new List<JObject>();
        private DataTable values;

        private Guid selectedItemID { get; set; }
        private Guid? selectedPreviewItemID { get; set; }


        private bool isEditing = false;
        private bool showDetails = false;
        private string editBlockName = string.Empty;

        private async Task LoadDataAsync(bool initial)
        {
            if (initial) _isInitialLoading = true; else _isRefreshing = true;

            try
            {
                var dxDataSetView = await dxDataSetViewApiClient.Get(_dxDataSetViewID);

                dxQueryResult = await dxQueryApi.GetAsync(dxDataSetView.DXQuery, dxDataSetView.DXFilter);
                dxUnits = await coreApi.GetItems(dxQueryResult.TypeName);

                values = dxQueryResult.AsDataTable();
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

        private async Task OpenEditDialog(Guid id)
        {
            if (id != default(Guid))
            {
                selectedItemID = id;
            }
            else
            {
                selectedItemID = Guid.NewGuid();
            }

            _showDialog = true;
        }

        private async Task OpenPanelRightAsync(Guid selectedBlockID)
        {
            selectedPreviewItemID = selectedBlockID;
            _collapse = false;
        }

        //private async Task<JObject> LoadDetailsAsync(Guid id)
        //{
        //    var item = await coreApi.Get(DXUnitTypeName, id);
        //    return item;
        //}

        private void OnClosed()
        {
            selectedPreviewItemID = null;
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

        public Guid GetGuid(DataRow row, string columnName)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            if (!row.Table.Columns.Contains(columnName))
                throw new ArgumentException($"Column '{columnName}' not found.", nameof(columnName));

            var value = row[columnName];

            if (value == DBNull.Value || value == null)
                return default(Guid);

            if (value is Guid g)
                return g;

            if (value is string s && Guid.TryParse(s, out g))
                return g;

            if (value is byte[] bytes && bytes.Length == 16)
                return new Guid(bytes);

            throw new InvalidCastException($"Cannot convert value in '{columnName}' to Guid.");
        }

        public IQueryable<DataRow> AsQueryableRows(DataTable table)
        {
            return table?.AsEnumerable()?.AsQueryable() ?? Enumerable.Empty<DataRow>().AsQueryable();
        }
    }
}
