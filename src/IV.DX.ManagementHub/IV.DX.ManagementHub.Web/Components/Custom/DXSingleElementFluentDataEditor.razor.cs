using IV.DX.ManagementHub.Common.Models;
using IV.DX.ManagementHub.Web.Models;
using Microsoft.AspNetCore.Components;

namespace IV.DX.ManagementHub.Web.Components.Custom
{
    public partial class DXSingleElementFluentDataEditor : ComponentBase
    {
        [Parameter, EditorRequired] public DXElementDefinition Definition { get; set; } = default!;
        [Parameter, EditorRequired] public DXRecordSingleElement DXSingleElement { get; set; } = default!;
        [Parameter, EditorRequired] public DXUnitRecordModel Parent { get; set; } = default!;

        private readonly string[] systemColumns = new[] { "Id", "DXUnitId", "TimeStamp" };

        private DXRecordItem DXItem
        {
            get
            {
                return DXSingleElement.Item ?? throw new InvalidOperationException("DXSingleElement item is null.");
            }
        }

    }
}


