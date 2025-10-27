using IV.DX.Kernel;
using IV.DX.Kernel.Models;
using IV.ManagementHub.Common.Models;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json.Linq;

namespace IV.ManagementHub.Web.Components.Custom
{
    public partial class DXMultiElementsFluentDataDialog : DXItemFluentDataDialog
    {
        [Parameter, EditorRequired] public DXElementDefinition Definition { get; set; } = default!;
        [Parameter, EditorRequired] public DXMultiElement DXMultiElement { get; set; } = default!;
        [Parameter, EditorRequired] public DXModel Parent { get; set; } = default!;

        private readonly string[] systemColumns = new[] { "ID", "DXUnitID", "TimeStamp" };

        protected override async Task OnInitializedAsync()
        {
            DXMultiElement.Announced = DXMultiElement.Announced.Where(x => !systemColumns.Contains(x.GetValue<string>("Name"))).ToHashSet();
            DXMultiElement.Deleted = new HashSet<DXItem>();
        }      

        private void Add()
        {
            var id = Guid.NewGuid();

            var jObject = new JObject();

            jObject[Constants.ID] = id;
            jObject[Constants.DXUnitID] = Parent.MainElement.Item.ID;
            jObject[Constants.TimeStamp] = DateTime.UtcNow;
            jObject[Constants.SystemPropertyTypeName] = Definition.Name;

            if (Definition?.Columns != null)
            {
                foreach (var col in Definition.Columns)
                {
                    if (!base.TryApplyDefault(jObject, col))
                    {
                        if (!col.AllowNull)
                            base.ApplyNonNullableFallback(jObject, col);
                    }
                }
            }

            this.DXMultiElement.AddToAnnounced(new DXItem()
            {
                ID = id,
                DXUnitID = Parent.MainElement.Item.ID,
                Content = jObject
            });
        }

        private void Remove(DXItem dxItem)
        {
            this.DXMultiElement.RemoveFromAnnounced(dxItem);
            this.DXMultiElement.AddToDeleted(dxItem);
        }    
    }
}
