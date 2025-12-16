using IV.DX.Kernel.Attributes;
using IV.DX.Kernel.Models;

namespace IV.ManagementHub.Common.Models.DXUnits
{
    [DXUnit("DXDataSetViewUnit")]
    public class DXDataSetViewUnit : DXUnit
    {
        [DXColumn("Name")]
        public string Name { get; set; }      
        [DXColumn("DXQuery")]
        public Guid DXQuery { get; set; }
        [DXColumn("DXFilter")]
        public Guid? DXFilter { get; set; }
    }
}