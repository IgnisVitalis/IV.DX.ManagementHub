using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.ManagementHub.Common.Models;
using IV.DX.ManagementHub.Web.ApiClients;
using IV.DX.ManagementHub.Web.Components.Custom.Base;
using IV.DX.ManagementHub.Web.Models;
using IV.DX.ManagementHub.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace IV.DX.ManagementHub.Web.Components.Pages
{
    public partial class DXUnitDialog : ManagementHubComponentBase, IDialogContentComponent<DXUnitDialogInput>
    {
        public static DialogParameters DefaultParameters => new()
        {
            Width = "min(90vw, 900px)",
            Modal = true
        };

        [CascadingParameter] public FluentDialog Dialog { get; set; } = default!;
        [Parameter] public DXUnitDialogInput Content { get; set; } = default!;

        [Inject] IApiClientResolver Resolver { get; set; } = default!;

        private DXModelDefinition _dxUnitDefinitionStructure = default!;
        private DXUnitRecordModel dxModel = default!;
        private bool isAccordion = true;
        private DXUnitCoreApiClient _coreApi = default!;
        private DXUnitStructureApiClient _dxUnitStructureApiCLient = default!;
        private bool _isLoaded;

        protected override async Task OnInitializedAsync()
        {
            AppKey = Content.AppKey;

            _coreApi = await Resolver.GetAsync<DXUnitCoreApiClient>(Content.AppKey);
            _dxUnitStructureApiCLient = await Resolver.GetAsync<DXUnitStructureApiClient>(Content.AppKey);

            _dxUnitDefinitionStructure = await _dxUnitStructureApiCLient.GetAsync(Content.Type);
            await LoadDXUnit(Content.Type, Content.ID, _dxUnitDefinitionStructure);

            _isLoaded = true;
        }

        private DXRecordItem GetDXMainElement() => dxModel.MainItem;

        private DXElementDefinition GetDXMainElementStructure() => _dxUnitDefinitionStructure.MainSingleElement;

        private DXRecordSingleElement GetDXSingleElement(string dxElementName)
        {
            var singleElement = dxModel.GetSingleElement(dxElementName);
            return singleElement ?? throw new InvalidOperationException($"Single element '{dxElementName}' not found.");
        }

        private DXRecordMultiElement GetDXMultiElement(string dxElementName)
        {
            var multiElement = dxModel.GetMultiElement(dxElementName);
            return multiElement ?? throw new InvalidOperationException($"Multi element '{dxElementName}' not found.");
        }

        private DXElementDefinition GetMandatoryDXSingleElementStructure(string dxElementName)
            => _dxUnitDefinitionStructure.RequiredSingleElements.Single(x => x.Name.Equals(dxElementName));

        private DXElementDefinition GetOptionalDXSingleElementStructure(string dxElementName)
            => _dxUnitDefinitionStructure.OptionalSingleElements.Single(x => x.Name.Equals(dxElementName));

        private DXElementDefinition GetMandatoryDXMultiElementStructure(string dxElementName)
            => _dxUnitDefinitionStructure.RequiredMultiElements.Single(x => x.Name.Equals(dxElementName));

        private DXElementDefinition GetOptionalDXMultiElementStructure(string dxElementName)
            => _dxUnitDefinitionStructure.OptionalMultiElements.Single(x => x.Name.Equals(dxElementName));

        private async Task LoadDXUnit(string type, Guid id, DXModelDefinition structure)
        {
            var content = await _coreApi.GetRecord(type, id);
            if (content == null)
            {
                dxModel = DXRecordModelFactory.GetDefault(structure);
                return;
            }
            dxModel = DXRecordModelFactory.FromBlock(content, structure);
        }

        private async Task SaveAsync()
        {
            var block = DXRecordModelFactory.ToBlock(dxModel, _dxUnitDefinitionStructure);
            await _coreApi.SaveRecordAsync(block);
            await Dialog.CloseAsync();
        }

        private async Task CancelAsync() => await Dialog.CancelAsync();

        private bool IsDXModelContainsSingleElement(DXElementDefinition dxElementDefinition)
            => dxModel.GetSingleElement(dxElementDefinition.Name)?.Item != null;

        private bool IsDXModelContainsMultiElement(DXElementDefinition dxElementDefinition)
            => dxModel.GetMultiElement(dxElementDefinition.Name) != null;

        private void CreateSingleElement(DXElementDefinition dxElementDefinition)
        {
            var newSingleItem = GetNewDXItem(dxElementDefinition, dxModel.MainItem.ID);
            var existing = dxModel.GetSingleElement(dxElementDefinition.Name);
            if (existing == null)
                dxModel.SetSingleElement(new DXRecordSingleElement(dxElementDefinition.Name, newSingleItem));
            else
                existing.Item = newSingleItem;
        }

        private void DeleteSingleElement(DXElementDefinition dxElementDefinition)
        {
            var existing = dxModel.GetSingleElement(dxElementDefinition.Name);
            if (existing != null)
                existing.Item = null;
        }

        private DXRecordItem GetNewDXItem(DXElementDefinition item, Guid dxUnitId)
        {
            var elementID = Guid.NewGuid();
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var column in item.Columns)
            {
                if (!DXItemEditorBaseComponent.TryApplyDefault(dict, column))
                {
                    if (!column.AllowNull)
                        DXItemEditorBaseComponent.ApplyNonNullableFallback(dict, column);
                }
            }

            return new DXRecordItem(item.Name, elementID, dxUnitId, DateTime.UtcNow, dict);
        }
    }
}
