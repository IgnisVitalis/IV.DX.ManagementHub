using IV.DX.Kernel.Attributes;
using IV.DX.Kernel.Models;

namespace IV.ManagementHub.Common.Models;

[DXUnit("MHInstanceUnit")]
public class MHInstanceUnit : DXUnit
{
    [DXColumn("Key")]        public string Key        { get; set; } = string.Empty;
    [DXColumn("Title")]      public string Title      { get; set; } = string.Empty;
    [DXColumn("BaseUrl")]    public string BaseUrl    { get; set; } = string.Empty;
    [DXColumn("ServiceKey")] public string ServiceKey { get; set; } = string.Empty;
}
