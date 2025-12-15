using Asp.Versioning;
using IV.DX.Application.Contracts.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace IV.ManagementHub.ApiService.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/DXQuery")]
    public class DXQueryController(IDXQueryResultProvider dxQueryResultProvider) : ControllerBase
    {
        /// <summary>Get DXUnitStructure object by Name.</summary>
        [HttpGet("{dxQueryID:guid}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JObject>> Get([FromRoute] Guid dxQueryID)
        {
            var dxQueryResult = await dxQueryResultProvider.GetAsync(dxQueryID);

            if (dxQueryResult == null)
                return NotFound();

            return dxQueryResult;
        }
    }
}