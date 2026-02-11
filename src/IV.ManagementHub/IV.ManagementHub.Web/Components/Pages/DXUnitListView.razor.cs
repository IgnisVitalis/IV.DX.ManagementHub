using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.ManagementHub.Web.ApiClients;
using IV.ManagementHub.Web.Components.Custom.Base;
using IV.ManagementHub.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
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

        private readonly List<Guid> _selectedIds = new();
        private readonly HashSet<Guid> _selectedIdSet = new();
        private readonly Dictionary<Guid, string> _displayStringById = new();

        private enum BulkActionKind
        {
            Export,
            Delete,
        }

        private BulkActionKind? _pendingBulkAction;
        private bool _showBulkConfirm;
        private bool _isBulkActionRunning;

        private string BulkConfirmTitle =>
            _pendingBulkAction switch
            {
                BulkActionKind.Delete => "Delete items",
                BulkActionKind.Export => "Export items",
                _ => "Confirm",
            };

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

        private IReadOnlyList<Guid> AllRowIds
        {
            get
            {
                if (dxQueryResult?.Content is null || dxQueryResult.Content.Count == 0)
                    return Array.Empty<Guid>();

                return dxQueryResult.Content
                    .Select(dxQueryResult.GetID)
                    .Where(id => id != default)
                    .Distinct()
                    .ToArray();
            }
        }

        private bool IsAllRowsSelected
        {
            get
            {
                var allIds = AllRowIds;
                if (allIds.Count == 0)
                    return false;

                foreach (var id in allIds)
                {
                    if (!_selectedIdSet.Contains(id))
                        return false;
                }

                return true;
            }
        }

        private Task SetSelectAllRows(bool selected)
        {
            var allIds = AllRowIds;
            if (allIds.Count == 0)
                return Task.CompletedTask;

            if (!selected)
            {
                ClearSelection();
            }
            else
            {
                foreach (var id in allIds)
                {
                    AddSelected(id);
                }
            }

            SyncPreviewWithSelection();
            return Task.CompletedTask;
        }

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

                BuildDisplayStringIndex();
                ClearSelection();
                SyncPreviewWithSelection();
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

        private Task OnRowClicked(Guid id)
        {
            ToggleSelected(id);
            SyncPreviewWithSelection();
            return Task.CompletedTask;
        }

        private bool IsSelected(Guid id) => id != default && _selectedIdSet.Contains(id);

        private Task SetSelected(Guid id, bool? selected)
        {
            if (selected == true)
            {
                AddSelected(id);
            }
            else
            {
                RemoveSelected(id);
            }

            SyncPreviewWithSelection();
            return Task.CompletedTask;
        }

        private int SelectedCount => _selectedIds.Count;

        private IEnumerable<string> SelectedDisplayStrings =>
            _selectedIds.Select(id => GetDisplayString(id));

        private async Task ExportSingleAsync(Guid id)
        {
            if (id == default)
                return;

            await coreApi.ExportAsync(dxQueryResult.TypeName, id);
        }

        private async Task DeleteSingleAsync(Guid id)
        {
            if (id == default)
                return;

            await coreApi.DeleteAsync(dxQueryResult.TypeName, id);
            await OnDeleted();
        }

        //private async Task<JObject> LoadDetailsAsync(Guid id)
        //{
        //    var item = await coreApi.Get(DXUnitTypeName, id);
        //    return item;
        //}

        private void OnClosed()
        {
            CloseBulkConfirmInternal();
            ClearSelection();
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

        private void AddSelected(Guid id)
        {
            if (id == default || _selectedIdSet.Contains(id))
                return;

            _selectedIdSet.Add(id);
            _selectedIds.Add(id);
        }

        private void RemoveSelected(Guid id)
        {
            if (id == default || !_selectedIdSet.Remove(id))
                return;

            _selectedIds.Remove(id);
        }

        private void ToggleSelected(Guid id)
        {
            if (IsSelected(id))
            {
                RemoveSelected(id);
            }
            else
            {
                AddSelected(id);
            }
        }

        private void ClearSelection()
        {
            _selectedIds.Clear();
            _selectedIdSet.Clear();
        }

        private void SyncPreviewWithSelection()
        {
            if (SelectedCount <= 0)
            {
                selectedPreviewItemID = null;
                _collapse = true;
                return;
            }

            _collapse = false;

            if (SelectedCount == 1)
            {
                selectedPreviewItemID = _selectedIds[0];
            }
            else
            {
                selectedPreviewItemID = null;
            }
        }

        private string BulkConfirmMessage
        {
            get
            {
                var count = SelectedCount;
                var itemWord = count == 1 ? "item" : "items";

                return _pendingBulkAction switch
                {
                    BulkActionKind.Delete => $"Delete {count} selected {itemWord}?",
                    BulkActionKind.Export => $"Export {count} selected {itemWord}?",
                    _ => $"Confirm action for {count} selected {itemWord}?",
                };
            }
        }

        private void RequestBulkAction(BulkActionKind kind)
        {
            if (SelectedCount <= 0)
                return;

            _pendingBulkAction = kind;
            _showBulkConfirm = true;
        }

        private void CloseBulkConfirm()
        {
            if (_isBulkActionRunning)
                return;

            CloseBulkConfirmInternal();
        }

        private void CloseBulkConfirmInternal()
        {
            _showBulkConfirm = false;
            _pendingBulkAction = null;
        }

        private async Task ConfirmBulkActionAsync()
        {
            if (_pendingBulkAction is null)
            {
                CloseBulkConfirm();
                return;
            }

            var ids = _selectedIds.ToArray();
            if (ids.Length == 0)
            {
                CloseBulkConfirm();
                return;
            }

            var action = _pendingBulkAction.Value;
            _isBulkActionRunning = true;

            try
            {
                switch (action)
                {
                    case BulkActionKind.Delete:
                        foreach (var id in ids)
                        {
                            await coreApi.DeleteAsync(dxQueryResult.TypeName, id);
                        }

                        await LoadDataAsync(false);
                        break;

                    case BulkActionKind.Export:
                        await coreApi.ExportAsync(dxQueryResult.TypeName, ids);
                        break;
                }
            }
            finally
            {
                _isBulkActionRunning = false;
                CloseBulkConfirmInternal();

                if (action == BulkActionKind.Delete)
                {
                    ClearSelection();
                    SyncPreviewWithSelection();
                }
            }
        }

        private string GetDisplayString(Guid id)
        {
            if (id == default)
                return string.Empty;

            if (_displayStringById.TryGetValue(id, out var display) && !string.IsNullOrWhiteSpace(display))
                return display;

            return id.ToString();
        }

        private void BuildDisplayStringIndex()
        {
            _displayStringById.Clear();

            if (dxQueryResult?.Content != null)
            {
                foreach (var row in dxQueryResult.Content)
                {
                    if (!TryGetId(row, out var id))
                        continue;

                    var display = GetDisplayStringFromObject(row);
                    if (!string.IsNullOrWhiteSpace(display))
                        _displayStringById[id] = display;
                }
            }

            foreach (var unit in dxUnits)
            {
                if (!TryGetId(unit, out var id))
                    continue;

                var display = GetDisplayStringFromObject(unit);
                if (!string.IsNullOrWhiteSpace(display))
                    _displayStringById[id] = display;
            }
        }

        private static bool TryGetId(JObject obj, out Guid id)
        {
            id = default;

            if (obj is null)
                return false;

            var token = obj.GetValue("ID", StringComparison.OrdinalIgnoreCase);
            if (token is null || token.Type == JTokenType.Null)
                return false;

            if (token.Type == JTokenType.Guid)
            {
                id = token.Value<Guid>();
                return id != default;
            }

            if (Guid.TryParse(token.ToString(), out id))
                return id != default;

            return false;
        }

        private static string GetDisplayStringFromObject(JObject obj)
        {
            var candidates = new[] { "DisplayString", "DisplayValue", "Name" };

            foreach (var name in candidates)
            {
                var token = obj.GetValue(name, StringComparison.OrdinalIgnoreCase);
                if (token is null || token.Type == JTokenType.Null)
                    continue;

                var value = token.Type == JTokenType.String ? token.Value<string>() : token.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return string.Empty;
        }
    }
}
