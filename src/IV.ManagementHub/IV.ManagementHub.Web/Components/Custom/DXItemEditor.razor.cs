using IV.DX.Kernel.Models;
using IV.ManagementHub.Common.Models;
using IV.ManagementHub.Web.Components.Custom.Base;
using Microsoft.AspNetCore.Components;

namespace IV.ManagementHub.Web.Components.Custom
{
    public partial class DXItemEditor : DXItemEditorBaseComponent
    {
        [Parameter, EditorRequired] public DXColumnDefinition ColumnDefinition { get; set; } = default!;
        [Parameter, EditorRequired] public DXItem DXItem { get; set; } = default!;
    }
}