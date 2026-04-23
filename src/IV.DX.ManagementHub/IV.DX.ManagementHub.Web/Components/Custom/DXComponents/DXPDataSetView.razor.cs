using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DX.Application.Contracts.Actions;
using IV.DX.Kernel.Models;
using IV.DX.Presentation.Application.Contracts.Models;
using IV.DX.ManagementHub.Web.ApiClients;
using IV.DX.ManagementHub.Web.Components.Custom.Base;
using IV.DX.ManagementHub.Web.Components.Pages;
using IV.DX.ManagementHub.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Newtonsoft.Json.Linq;

namespace IV.DX.ManagementHub.Web.Components.Custom.DXComponents
{
    public partial class DXPDataSetView : DXPComponent<DXPDataSetViewUnit, DXPDataSetViewApiClient>
    {
        [Inject] IDXActionExecutor ActionExecutor { get; set; } = default!;
        [Inject] IDialogService DialogService { get; set; } = default!;

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

        private bool _collapse = true;
        private bool HasCurrentType => !string.IsNullOrWhiteSpace(dxQueryResult?.TypeName);
        private bool HasRowActions =>
            (ComponentUnit is { IsEditable: true } or { IsDeletable: true } or { IsExportable: true })
            || _customButtonDefs.Count > 0;

        private IEnumerable<JObject> dxUnits = new List<JObject>();

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

        private async Task OnActionChanged()
        {
            await ReloadAsync();
            ClearSelection();
            SyncPreviewWithSelection();
        }

        private async Task OpenCreateDialogAsync()
        {
            if (!HasCurrentType) return;
            var input = new DXUnitDialogInput(AppKey, dxQueryResult.TypeName, Guid.NewGuid());
            var dialog = await DialogService.ShowDialogAsync<DXUnitDialog>(input, DXUnitDialog.DefaultParameters);
            var result = await dialog.Result;
            if (!result.Cancelled)
                await OnActionChanged();
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

        private void OnClosed()
        {
            ClearSelection();
            selectedPreviewItemID = null;
            _collapse = true;
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
