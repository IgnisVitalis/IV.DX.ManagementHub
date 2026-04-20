using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DX.Application.Contracts.Actions;
using IV.DX.Kernel.Models;
using IV.DX.Presentation.Application.Contracts.Models;
using IV.DX.ManagementHub.Web.ApiClients;
using IV.DX.ManagementHub.Web.Components.Custom.Base;
using IV.DX.ManagementHub.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Newtonsoft.Json.Linq;

namespace IV.DX.ManagementHub.Web.Components.Custom.DXComponents
{
    public partial class DXPDataSetView : DXPComponent<DXPDataSetViewUnit, DXPDataSetViewApiClient>
    {
        [Inject]
        IDXActionExecutor ActionExecutor { get; set; } = default!;

        DXUnitCoreApiClient? coreApi;
        DXQueryResultApiClient? dxQueryApi;
        DXPButtonActionUnitApiClient? buttonActionApiClient;
        DXActionDefinitionUnitApiClient? actionDefinitionApiClient;

        private record CustomButtonDef(int Order, DXPButtonActionUnit Button, DXActionDefinitionUnit ActionDef);

        private readonly List<CustomButtonDef> _customButtonDefs = new();
        private DXQueryResult dxQueryResult = DXQueryResult.Empty();

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
        private string? _bulkActionErrorMessage;

        private string BulkConfirmTitle =>
            _pendingBulkAction switch
            {
                BulkActionKind.Delete => "Delete items",
                BulkActionKind.Export => "Export items",
                _ => "Confirm",
            };

        private bool _collapse = true;
        private bool HasCurrentType => !string.IsNullOrWhiteSpace(dxQueryResult?.TypeName);
        private bool HasRowActions =>
            (ComponentUnit is { IsEditable: true } or { IsDeletable: true } or { IsExportable: true })
            || _customButtonDefs.Count > 0;

        private IEnumerable<JObject> dxUnits = new List<JObject>();

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
                    AddSelected(id);
            }

            SyncPreviewWithSelection();
            return Task.CompletedTask;
        }

        protected override async Task LoadDataAsync()
        {
            coreApi ??= await Resolver.GetAsync<DXUnitCoreApiClient>(AppKey);
            dxQueryApi ??= await Resolver.GetAsync<DXQueryResultApiClient>(AppKey);
            buttonActionApiClient ??= await Resolver.GetAsync<DXPButtonActionUnitApiClient>(AppKey);
            actionDefinitionApiClient ??= await Resolver.GetAsync<DXActionDefinitionUnitApiClient>(AppKey);

            await LoadCustomButtonDefsAsync();

            dxQueryResult = await dxQueryApi.GetAsync(ComponentUnit!.DXQuery, ComponentUnit.DXFilter);
            if (dxQueryResult is null || string.IsNullOrWhiteSpace(dxQueryResult.TypeName))
                throw new InvalidOperationException("Query result is empty or invalid for the selected data set.");

            var result = await coreApi.GetItems(dxQueryResult.TypeName);
            dxUnits = (result["Data"]?["Items"] as JArray ?? new JArray()).OfType<JObject>();

            BuildDisplayStringIndex();
            ClearSelection();
            SyncPreviewWithSelection();
        }

        private async Task OnDeleted()
        {
            await ReloadAsync();
            OnClosed();
        }

        private async Task OpenEditDialog(Guid id)
        {
            if (!HasCurrentType)
                return;

            selectedItemID = id != default ? id : Guid.NewGuid();
            _showDialog = true;
        }

        private Task OnRowClicked(Guid id)
        {
            ToggleSelected(id);
            SyncPreviewWithSelection();
            return Task.CompletedTask;
        }

        private Task OnDataRowClicked(JObject row)
        {
            var id = dxQueryResult.GetID(row);
            return OnRowClicked(id);
        }

        private string? GetDataRowStyle(JObject row)
        {
            var id = dxQueryResult.GetID(row);
            return IsSelected(id)
                ? "background-color: var(--neutral-fill-secondary-rest);"
                : null;
        }

        private bool IsSelected(Guid id) => id != default && _selectedIdSet.Contains(id);

        private Task SetSelected(Guid id, bool? selected)
        {
            if (selected == true)
                AddSelected(id);
            else
                RemoveSelected(id);

            SyncPreviewWithSelection();
            return Task.CompletedTask;
        }

        private int SelectedCount => _selectedIds.Count;

        private IEnumerable<string> SelectedDisplayStrings =>
            _selectedIds.Select(id => GetDisplayString(id));

        private IReadOnlyList<DXActionButton> BuildRowActions(JObject item)
        {
            var id = dxQueryResult.GetID(item);
            if (id == default || !HasCurrentType || ComponentUnit is null)
                return [];

            var result = new List<DXActionButton>();

            var ordered = new List<(int Order, string Key)>();
            if (ComponentUnit.IsEditable) ordered.Add((10, DXActionButtonKeys.Edit));
            if (ComponentUnit.IsDeletable) ordered.Add((20, DXActionButtonKeys.Delete));
            if (ComponentUnit.IsExportable) ordered.Add((30, DXActionButtonKeys.Export));

            result.AddRange(DXActionButtonRegistry.Build(
                ordered.OrderBy(x => x.Order).Select(x => x.Key),
                new DXActionButtonContext
                {
                    EntityID = id,
                    TypeName = dxQueryResult.TypeName,
                    AppKey = AppKey,
                    OnEdit = EventCallback.Factory.Create(this, () => OpenEditDialog(id)),
                    OnExport = EventCallback.Factory.Create(this, () => ExportSingleAsync(id)),
                    OnDelete = EventCallback.Factory.Create(this, () => DeleteSingleAsync(id))
                }));

            foreach (var def in _customButtonDefs)
            {
                var capturedDef = def;
                var capturedId = id;
                result.Add(new DXActionButton
                {
                    Key = $"custom_{def.ActionDef.Key}",
                    Label = def.ActionDef.Name,
                    IconKey = DXActionButtonMapper.ToIconKey(def.Button.Icon),
                    Appearance = DXActionButtonMapper.ToAppearance(def.Button.Style),
                    IconColor = DXActionButtonMapper.ToIconColor(def.Button.Color, def.Button.Style),
                    OnClick = EventCallback.Factory.Create(this, () => ExecuteCustomActionAsync(capturedDef, capturedId))
                });
            }

            return result;
        }

        private async Task LoadCustomButtonDefsAsync()
        {
            _customButtonDefs.Clear();
            var elements = ComponentUnit?.DXPComponentButtonActionElement?.Announced;
            if (elements is null || elements.Count == 0)
                return;

            foreach (var element in elements)
            {
                var buttonUnit = await buttonActionApiClient!.Get(element.Action);
                if (buttonUnit is null)
                    continue;

                var actionDef = await actionDefinitionApiClient!.Get(buttonUnit.ActionDefinition);
                if (actionDef is null)
                    continue;

                _customButtonDefs.Add(new CustomButtonDef(element.Order, buttonUnit, actionDef));
            }

            _customButtonDefs.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        private async Task ExecuteCustomActionAsync(CustomButtonDef def, Guid entityId)
        {
            var parameters = new DXActionParameters().Set("InstanceId", entityId);
            await ActionExecutor.ExecuteAsync(def.ActionDef.Module, def.ActionDef.Key, parameters);
        }

        private async Task ExportSingleAsync(Guid id)
        {
            if (id == default || !HasCurrentType)
                return;

            await coreApi!.ExportAsync(dxQueryResult.TypeName, id);
        }

        private async Task DeleteSingleAsync(Guid id)
        {
            if (id == default || !HasCurrentType)
                return;

            await coreApi!.DeleteAsync(dxQueryResult.TypeName, id);
            await OnDeleted();
        }

        private void OnClosed()
        {
            CloseBulkConfirmInternal();
            ClearSelection();
            selectedPreviewItemID = null;
            _collapse = true;
        }

        bool _showDialog = false;

        private void CloseDialog() => _showDialog = false;

        private async Task OnSaved()
        {
            await ReloadAsync();
            CloseDialog();
        }

        Orientation orientation = Orientation.Horizontal;

        private void OnResizedHandler(SplitterResizedEventArgs args) { }

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
                RemoveSelected(id);
            else
                AddSelected(id);
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
            selectedPreviewItemID = SelectedCount == 1 ? _selectedIds[0] : null;
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
            if (_pendingBulkAction is null || !HasCurrentType)
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
            _bulkActionErrorMessage = null;

            try
            {
                switch (action)
                {
                    case BulkActionKind.Delete:
                        foreach (var id in ids)
                            await coreApi!.DeleteAsync(dxQueryResult.TypeName, id);

                        await ReloadAsync();
                        break;

                    case BulkActionKind.Export:
                        await coreApi!.ExportAsync(dxQueryResult.TypeName, ids);
                        break;
                }
            }
            catch (Exception ex)
            {
                _bulkActionErrorMessage = ex.Message;
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
            var candidates = new[] { "DisplayString", "DXTitleExpression", "Name" };

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
