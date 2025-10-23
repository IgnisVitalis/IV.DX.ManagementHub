using IV.DX.Kernel.Models;
using IV.ManagementHub.Web.Components.Custom;
using Microsoft.AspNetCore.Components;

namespace IV.ManagementHub.Web.Components.Pages
{
    public partial class BlockPanel : ManagementHubComponentBase
    {
        [Parameter] public DXElementDefinitionUnit Content { get; set; } = default!;
    
        [Parameter] public EventCallback OnEdited { get; set; }
        [Parameter] public EventCallback OnDeleted { get; set; }    

        private readonly string[] systemColumns = new[] { "ID", "DXUnitID", "TimeStamp" };
    }
}