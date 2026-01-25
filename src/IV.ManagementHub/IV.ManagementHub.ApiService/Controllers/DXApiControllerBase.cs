using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IV.ManagementHub.ApiService.Controllers
{

    [ApiController]
    [Authorize]
    public abstract class DXApiControllerBase : ControllerBase
    {
        protected string UserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Missing user id claim.");

        protected string? Email =>
            User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
    }
}
