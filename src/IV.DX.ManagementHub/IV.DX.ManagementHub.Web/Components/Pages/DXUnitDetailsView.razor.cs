using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.ManagementHub.Common.Models;
using IV.DX.ManagementHub.Web.ApiClients;
using IV.DX.ManagementHub.Web.Components.Custom.Base;
using IV.DX.ManagementHub.Web.Models;
using IV.DX.ManagementHub.Web.Services;
using Microsoft.AspNetCore.Components;

namespace IV.DX.ManagementHub.Web.Components.Pages
{
    public partial class DXUnitDetailsView : ManagementHubComponentBase
    {
        [Parameter, EditorRequired] public string Type { get; set; } = default!;
        [Parameter, EditorRequired] public Guid Id { get; set; }

        [Parameter] public bool IsEditable { get; set; }
        [Parameter] public bool IsDeletable { get; set; }
        [Parameter] public bool IsExportable { get; set; }
        [Parameter] public EventCallback OnChanged { get; set; }
        [Parameter] public RenderFragment? AdditionalActions { get; set; }

        [Inject] IApiClientResolver Resolver { get; set; } = default!;

        private DXModelDefinition _dxUnitDefinitionStructure = default!;
        private DXUnitRecordModel dxModel = default!;

        private DXUnitCoreApiClient _coreApi = default!;
        private DXUnitStructureApiClient _dxUnitStructureApiClient = default!;

        private bool _isLoaded;

        private string? _loadedType;
        private Guid _loadedId;

        protected override async Task OnParametersSetAsync()
        {
            if (string.IsNullOrWhiteSpace(Type) || Id == default)
            {
                _isLoaded = false;
                return;
            }

            if (_isLoaded && string.Equals(_loadedType, Type, StringComparison.Ordinal) && _loadedId == Id)
                return;

            _isLoaded = false;

            _coreApi = await Resolver.GetAsync<DXUnitCoreApiClient>(base.AppKey);
            _dxUnitStructureApiClient = await Resolver.GetAsync<DXUnitStructureApiClient>(base.AppKey);

            _dxUnitDefinitionStructure = await _dxUnitStructureApiClient.GetAsync(Type);
            await LoadDXUnit(Type, Id, _dxUnitDefinitionStructure);

            _loadedType = Type;
            _loadedId = Id;
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

        private bool IsDXModelContainsSingleElement(DXElementDefinition dxElementDefinition)
            => dxModel.GetSingleElement(dxElementDefinition.Name)?.Item != null;

        private bool IsDXModelContainsMultiElement(DXElementDefinition dxElementDefinition)
            => dxModel.GetMultiElement(dxElementDefinition.Name) != null;
    }
}

