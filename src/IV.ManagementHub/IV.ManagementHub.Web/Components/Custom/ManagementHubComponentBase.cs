using Microsoft.AspNetCore.Components;

namespace IV.ManagementHub.Web.Components.Custom
{
    public abstract class ManagementHubComponentBase : ComponentBase
    {
        [Parameter, EditorRequired] public string AppKey { get; set; } = default!;     
    }
}