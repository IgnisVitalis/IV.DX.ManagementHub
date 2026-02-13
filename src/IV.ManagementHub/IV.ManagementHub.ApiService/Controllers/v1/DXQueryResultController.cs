using Asp.Versioning;
using IV.DX.Application.Contracts.Abstractions;
using IV.ManagementHub.ApiService.Controllers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace IV.ManagementHub.ApiService.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/DXQueryResult")]
    public class DXQueryResultController(IDXQueryResultProvider dxQueryResultProvider) : DXApiControllerBase
    {
        /// <summary>Get DXUnitStructure object by Name.</summary>
        [HttpGet("{dxQueryID:guid}/{dxFilterID:guid?}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JObject>> Get([FromRoute] Guid dxQueryID, [FromRoute] Guid? dxFilterID)
        {
            var dxQueryResult = await dxQueryResultProvider.GetAsync(dxQueryID, dxFilterID);

            if (dxQueryResult == null)
                return NotFound();

            return dxQueryResult;
        }
    }
}
