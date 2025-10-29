using IV.DX.Kernel;
using IV.DX.Kernel.Models;
using IV.ManagementHub.Common.Models;
using IV.ManagementHub.Web.Components.Custom.Base;
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
          
        }      

        private void Add()
        {
            var id = Guid.NewGuid();
            var timeStamp = DateTime.UtcNow;

            var dict = new Dictionary<string, object>();

            if (Definition?.Columns != null)
            {
                foreach (var col in Definition.Columns)
                {
                    if (!base.TryApplyDefault(dict, col))
                    {
                        if (!col.AllowNull)
                            base.ApplyNonNullableFallback(dict, col);
                    }
                }
            }

            this.DXMultiElement.Add(new DXItem(Definition.Name, id, Parent.DXMainElement.Item.ID, timeStamp, dict));
        }

        private void Remove(DXItem dxItem)
        {
            this.DXMultiElement.Remove(dxItem);
        }    
    }
}
