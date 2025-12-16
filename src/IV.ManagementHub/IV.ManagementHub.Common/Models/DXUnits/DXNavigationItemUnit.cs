using IV.DX.Kernel.Attributes;
using IV.DX.Kernel.Models;

namespace IV.ManagementHub.Common.Models.DXUnits
{
    [DXUnit("DXNavigationItemUnit")]
    public class DXNavigationItemUnit : DXUnit
    {
        [DXColumn("Name")]
        public string Name { get; set; }
        [DXColumn("Order")]
        public int Order { get; set; }
        [DXColumn("Parent")]
        public Guid? Parent { get; set; }
        [DXColumn("DataSetView")]
        public Guid? DataSetView { get; set; }
    }
}