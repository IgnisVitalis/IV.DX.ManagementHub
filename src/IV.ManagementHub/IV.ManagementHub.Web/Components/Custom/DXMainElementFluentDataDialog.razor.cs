using IV.DX.Kernel.Models;
using IV.ManagementHub.Common.Models;
using IV.ManagementHub.Web.Components.Custom.Base;
using Microsoft.AspNetCore.Components;

namespace IV.ManagementHub.Web.Components.Custom
{
    public partial class DXMainElementFluentDataDialog : DXItemFluentDataDialog
    {
        [Parameter, EditorRequired] public DXElementDefinition Definition { get; set; } = default!;
        [Parameter, EditorRequired] public DXMainElement DXMainElement { get; set; } = default!;
        [Parameter, EditorRequired] public DXModel Parent { get; set; } = default!;

        private readonly string[] systemColumns = new[] { "ID", "DXUnitID", "TimeStamp" };

        private DXItem DXItem
        {
            get
            {
                return DXMainElement.Item;
            }
        }
    }
}
