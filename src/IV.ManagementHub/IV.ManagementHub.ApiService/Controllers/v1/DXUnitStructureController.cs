using Asp.Versioning;
using IV.ManagementHub.ApiService.Contracts.Services;
using IV.ManagementHub.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace IV.ManagementHub.ApiService.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/DXUnitStructure")]
    public class DXUnitStructureController(IDXUnitStructureService dxUnitStructureService) : ControllerBase
    {
        /// <summary>Get DXUnitStructure object by Name.</summary>
        [HttpGet("{name}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DXModelDefinition>> GetByName([FromRoute] string name)
        {
            return await dxUnitStructureService.GetAsync(name) is not { } result
                ? NotFound()
                : Ok(result);
        }
    }
}