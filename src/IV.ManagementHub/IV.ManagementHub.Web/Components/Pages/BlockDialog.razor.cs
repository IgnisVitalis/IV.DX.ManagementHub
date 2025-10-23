using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.Kernel.Enums;
using IV.DX.Kernel.Models;
using IV.ManagementHub.Common.Models;
using IV.ManagementHub.Web.ApiClients;
using IV.ManagementHub.Web.Components.Custom;
using IV.ManagementHub.Web.Models;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json.Linq;

namespace IV.ManagementHub.Web.Components.Pages
{
    public partial class BlockDialog : ManagementHubComponentBase
    {
        
        [Parameter] public DXElementDefinitionUnit Content { get; set; } = default!;
        [Parameter] public EventCallback OnClosed { get; set; }
        [Parameter] public EventCallback<DXElementDefinitionUnit> OnSaved { get; set; }

        DXUnitStructureApiClient _dxUnitStructureApiCLient;


        DXElementDefinitionStructure multiItemsDataGridDefinition { get; set; }
        JArray items { get; set; } = new JArray();

        [Inject]
        IApiClientResolver Resolver { get; set; } = default!;

        protected override async Task OnParametersSetAsync()
        {
            this._dxUnitStructureApiCLient = this.Resolver.Get<DXUnitStructureApiClient>(base.AppKey);

            var dxElementStructure = await this._dxUnitStructureApiCLient.GetAsync("DXElementDefinitionUnit");


            var columnDefinitions = new List<ColumnDefinition>()
            {
                new ColumnDefinition("Name", "Name", DXColumnTypeEnum.String),
                new ColumnDefinition("Length", "Length", DXColumnTypeEnum.Int),
                new ColumnDefinition("Allow Null", "AllowNull", DXColumnTypeEnum.Bool),
                new ColumnDefinition("Default Value", "DefaultValue", DXColumnTypeEnum.String),
                new ColumnDefinition("Precision", "Precision", DXColumnTypeEnum.Int),
                new ColumnDefinition("Scale", "Scale", DXColumnTypeEnum.Int),
            };

            multiItemsDataGridDefinition = dxElementStructure.MultiItemsOptional.Single(x => x.Name.Equals("DXColumnDefinitionElement"));


            items = new JArray(Content.DXColumnDefinitionElement.Announced.Select(x => x.ToJObject()));


        }

        private readonly List<KeyValuePair<DXColumnTypeEnum, string>> ColumnTypes = new()
        {
            new(DXColumnTypeEnum.GUID, "GUID"),
            new(DXColumnTypeEnum.String, "String"),
            new(DXColumnTypeEnum.Text, "Text"),
            new(DXColumnTypeEnum.DateTime, "DateTime"),
            new(DXColumnTypeEnum.Bool, "Bool"),
            new(DXColumnTypeEnum.Short, "Short"),
            new(DXColumnTypeEnum.Int, "Int"),
            new(DXColumnTypeEnum.Long, "Long"),
            new(DXColumnTypeEnum.Decimal, "Decimal"),
            new(DXColumnTypeEnum.Float, "Float"),
            new(DXColumnTypeEnum.Currency, "Currency"),
            new(DXColumnTypeEnum.Blob, "Blob")
        };

        private readonly string[] systemColumns = new[] { "ID", "DXUnitID", "TimeStamp" };

        private void Add()
        {
            Content.DXColumnDefinitionElement.AddToAnnounced(new DXColumnDefinitionElement()
            {
                ID = Guid.NewGuid(),
                ColumnType = DXColumnTypeEnum.String
            });
        }

        private void Remove(DXColumnDefinitionElement column)
        {
            Content.DXColumnDefinitionElement.RemoveFromAnnounced(column);
        }

        private async Task SaveAsync()
        {
            if (OnSaved.HasDelegate)
                await OnSaved.InvokeAsync(this.Content);
        }

        private async Task CancelAsync()
        {
            if (OnClosed.HasDelegate)
                await OnClosed.InvokeAsync();
        }
    }
}