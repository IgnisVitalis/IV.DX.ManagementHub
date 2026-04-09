using Microsoft.AspNetCore.Components;

namespace IV.DX.ManagementHub.Web.Components.Custom.Base
{
    public abstract class ManagementHubComponentBase : ComponentBase
    {
        [Parameter] public string AppKey { get; set; } = string.Empty;
    }
}