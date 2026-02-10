using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.Kernel.Models;
using IV.ManagementHub.Web.Models;
using IV.ManagementHub.Common.Models;
using IV.ManagementHub.Web.Components.Custom.Base;
using Microsoft.AspNetCore.Components;

namespace IV.ManagementHub.Web.Components.Custom
{
    public partial class DXMultiElementsFluentDataEditor : ManagementHubComponentBase
    {
        [Parameter, EditorRequired] public DXElementDefinition Definition { get; set; } = default!;
        [Parameter, EditorRequired] public DXRecordMultiElement DXMultiElement { get; set; } = default!;
        [Parameter, EditorRequired] public DXUnitRecordModel Parent { get; set; } = default!;

        private readonly string[] systemColumns = new[] { "ID", "DXUnitID", "TimeStamp" };
        private const int RowEditorThresholdColumns = 6;
        private bool UseRowEditor => CountVisibleColumns() > RowEditorThresholdColumns;
        DXEnumApiClient _dxEnumApiClient = default!;
        DXElementApiClient _dxElementApiClient = default!;
        DXUnitApiClient _dxUnitApiClient = default!;

        [Inject]
        IApiClientResolver Resolver { get; set; } = default!;

        IEnumerable<DXEnumDefinitionUnit> enumDefinitions = new List<DXEnumDefinitionUnit>();

        private bool _isRowEditorOpen;
        private bool _isNewItem;
        private DXRecordItem? _editingOriginal;
        private DXRecordItem? _editingItem;


        protected override async Task OnInitializedAsync()
        {
            this._dxEnumApiClient = Resolver.Get<DXEnumApiClient>(base.AppKey);
            this._dxElementApiClient = Resolver.Get<DXElementApiClient>(base.AppKey);
            this._dxUnitApiClient = Resolver.Get<DXUnitApiClient>(base.AppKey);

            if (Definition.Name.Equals("DXObjectEnumElement"))
            {
                enumDefinitions = await this._dxEnumApiClient.GetItemsAsync();

                var columnDefinitionEnumType = Definition.Columns.Single(x => x.Name.Equals("EnumType"));

                columnDefinitionEnumType.RelationValues = enumDefinitions.ToDictionary(x => x.ID, x => x.Name);
            }
        }

        private void Add()
        {
            if (!UseRowEditor)
            {
                this.DXMultiElement.Add(CreateNewItem());
                return;
            }

            _editingOriginal = null;
            _editingItem = CreateNewItem();
            _isNewItem = true;
            _isRowEditorOpen = true;
        }

        private void Edit(DXRecordItem item)
        {
            if (!UseRowEditor)
                return;

            _editingOriginal = item;
            _editingItem = CloneItem(item);
            _isNewItem = false;
            _isRowEditorOpen = true;
        }

        private void ApplyEdit()
        {
            if (_editingItem == null)
                return;

            if (_isNewItem)
            {
                this.DXMultiElement.Add(_editingItem);
            }
            else if (_editingOriginal != null)
            {
                CopyItem(_editingItem, _editingOriginal);
            }

            CloseRowEditor();
        }

        private void CancelEdit()
        {
            CloseRowEditor();
        }

        private void CloseRowEditor()
        {
            _isRowEditorOpen = false;
            _isNewItem = false;
            _editingOriginal = null;
            _editingItem = null;
        }

        private int CountVisibleColumns()
        {
            if (Definition?.Columns == null)
                return 0;

            return Definition.Columns.Count(column => !systemColumns.Contains(column.Name, StringComparer.OrdinalIgnoreCase));
        }

        private string GetDisplayText(DXRecordItem item)
        {
            if (TryGetNonEmptyString(item, "DisplayString", out var displayString))
                return displayString;

            if (item.Content.TryGetValue("DisplayValue", out var displayValue) && displayValue != null)
            {
                var displayFieldName = displayValue.ToString();
                if (!string.IsNullOrWhiteSpace(displayFieldName)
                    && item.Content.TryGetValue(displayFieldName, out var displayFieldValue)
                    && displayFieldValue != null)
                {
                    return displayFieldValue.ToString() ?? string.Empty;
                }
            }

            if (TryGetNonEmptyString(item, "Name", out var name))
                return name;

            return string.Empty;
        }

        private static bool TryGetNonEmptyString(DXRecordItem item, string fieldName, out string value)
        {
            value = string.Empty;

            if (!item.Content.TryGetValue(fieldName, out var raw) || raw == null)
                return false;

            var str = raw.ToString();
            if (string.IsNullOrWhiteSpace(str))
                return false;

            value = str;
            return true;
        }

        private DXRecordItem CreateNewItem()
        {
            var id = Guid.NewGuid();
            var timeStamp = DateTime.UtcNow;

            var dict = new Dictionary<string, object?>();

            if (Definition?.Columns != null)
            {
                foreach (var col in Definition.Columns)
                {
                    if (!DXItemEditorBaseComponent.TryApplyDefault(dict, col))
                    {
                        if (!col.AllowNull)
                            DXItemEditorBaseComponent.ApplyNonNullableFallback(dict, col);
                    }
                }
            }

            return new DXRecordItem(Definition.Name, id, Parent.MainItem.ID, timeStamp, dict);
        }

        private IEnumerable<DXRecordItem> GetVisibleItems()
        {
            if (!Definition.Name.Equals("DXColumnDefinitionElement", StringComparison.OrdinalIgnoreCase))
                return this.DXMultiElement.Announced;

            return this.DXMultiElement.Announced.Where(item => !IsSystemColumnDefinition(item));
        }

        private bool IsSystemColumnDefinition(DXRecordItem item)
        {
            if (item.Content == null)
                return false;

            if (!item.Content.TryGetValue("Name", out var raw) || raw == null)
                return false;

            var name = raw.ToString();
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return systemColumns.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        private DXColumnDefinition GetColumnDefinition(DXColumnDefinition columnDefinition, DXRecordItem dxItem)
        {
            if (dxItem.Type.Equals("DXObjectEnumElement"))
            {
                if (columnDefinition.Name.Equals("EnumType"))
                {
                    var columnDefinitionEnumType = columnDefinition.DeepClone();

                    columnDefinitionEnumType.RelationValues = enumDefinitions.ToDictionary(x => x.ID, x => x.Name);

                    return columnDefinitionEnumType;
                }
                else if (columnDefinition.Name.Equals("EnumKey"))
                {
                    var customColumnDefintion = columnDefinition.DeepClone();

                    if (!dxItem.Content.TryGetValue("EnumType", out var enumValue) || enumValue == null)
                        return columnDefinition;

                    var enumTypeAsGuid = enumValue is Guid
                        ? (Guid)enumValue
                        : Guid.Parse(enumValue.ToString()!);

                    var selectedEnumTypeDefinition = enumDefinitions.SingleOrDefault(x => x.ID == enumTypeAsGuid);

                    if (selectedEnumTypeDefinition != null)
                    {
                        customColumnDefintion.RelationValues =
                            selectedEnumTypeDefinition.DXColumnDefinitionElement.Announced
                            .Where(x => x.ColumnType == IV.DX.Kernel.Enums.DXColumnTypeEnum.Int)
                            .ToDictionary(x => x.ID, x => x.Name);

                        return customColumnDefintion;
                    }
                }
            }

            return columnDefinition;
        }

        private void Remove(DXRecordItem dxItem)
        {
            this.DXMultiElement.Remove(dxItem);
        }

        private static DXRecordItem CloneItem(DXRecordItem item)
        {
            var content = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in item.Content)
                content[kvp.Key] = CloneValue(kvp.Value);

            return new DXRecordItem(item.Type, item.ID, item.DXUnitID, item.TimeStamp, content);
        }

        private static void CopyItem(DXRecordItem source, DXRecordItem target)
        {
            target.DXUnitID = source.DXUnitID;
            target.TimeStamp = source.TimeStamp;

            var keys = target.Content.Keys.ToList();
            foreach (var key in keys)
                target.Content.Remove(key);

            foreach (var kvp in source.Content)
                target.Content[kvp.Key] = CloneValue(kvp.Value);
        }

        private static object? CloneValue(object? value)
        {
            if (value == null)
                return null;

            if (value is byte[] bytes)
                return bytes.ToArray();

            if (value is ICloneable cloneable)
                return cloneable.Clone();

            if (value is Array array && value is not string)
                return array.Clone();

            return value;
        }
    }
}
