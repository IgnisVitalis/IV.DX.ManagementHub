using Microsoft.AspNetCore.Components;

namespace IV.ManagementHub.Web.Components.Custom.Base
{
    public abstract class ManagementHubComponentBase : ComponentBase
    {
        [Parameter, EditorRequired] public string AppKey { get; set; } = default!;     
    }
}