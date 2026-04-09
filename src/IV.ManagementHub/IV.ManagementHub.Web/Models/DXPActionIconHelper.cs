using IV.DX.Presentation.Application.Contracts.Models.Enums;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.Icons.Regular;

namespace IV.ManagementHub.Web.Models
{
    public static class DXPActionIconHelper
    {
        public static Icon GetIcon(DXPActionIconEnum icon) => icon switch
        {
            DXPActionIconEnum.Edit     => new Size16.Edit(),
            DXPActionIconEnum.Delete   => new Size16.Delete(),
            DXPActionIconEnum.Export   => new Size16.ArrowExportUp(),
            DXPActionIconEnum.Navigate => new Size16.Open(),
            DXPActionIconEnum.Add      => new Size16.AddCircle(),
            DXPActionIconEnum.Refresh  => new Size16.ArrowClockwise(),
            DXPActionIconEnum.Settings => new Size16.Settings(),
            DXPActionIconEnum.View     => new Size16.Eye(),
            DXPActionIconEnum.Search   => new Size16.Search(),
            DXPActionIconEnum.Archive  => new Size16.Archive(),
            _                          => new Size16.Circle()
        };
    }
}
