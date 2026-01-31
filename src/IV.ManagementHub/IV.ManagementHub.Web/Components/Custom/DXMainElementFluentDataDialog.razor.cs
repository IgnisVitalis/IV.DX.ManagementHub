using IV.ManagementHub.Common.Models;
using IV.ManagementHub.Web.Models;
using Microsoft.AspNetCore.Components;

namespace IV.ManagementHub.Web.Components.Custom
{
    public partial class DXMainElementFluentDataDialog : ComponentBase
    {
        [Parameter, EditorRequired] public DXElementDefinition Definition { get; set; } = default!;
        [Parameter, EditorRequired] public DXRecordItem MainItem { get; set; } = default!;
        [Parameter, EditorRequired] public DXUnitRecordModel Parent { get; set; } = default!;

        private readonly string[] systemColumns = new[] { "ID", "DXUnitID", "TimeStamp" };

        private DXRecordItem DXItem
        {
            get
            {
                return MainItem;
            }
        }
    }
}

