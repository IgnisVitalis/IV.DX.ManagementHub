using IV.DX.ManagementHub.ApiService.Controllers;
using IV.DX.ManagementHub.ApiService.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net;

namespace IV.DX.ManagementHub.ApiService.Controllers
{
    [ApiController]
    [Route("api/DXQueryResult")]
    public class DXQueryResultController(InstanceApiClientFactory clientFactory) : DXApiControllerBase
    {
        /// <summary>Get DX query result by query ID.</summary>
        [HttpGet("{dxQueryID:guid}/{dxFilterID:guid?}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JObject>> Get([FromRoute] Guid dxQueryID, [FromRoute] Guid? dxFilterID)
        {
            var ct = HttpContext.RequestAborted;
            var client = await clientFactory.CreateFromContextAsync(ct);

            var url = dxFilterID.HasValue
                ? $"api/management/query-result/{dxQueryID}/{dxFilterID.Value}"
                : $"api/management/query-result/{dxQueryID}";
            using var response = await client.GetAsync(url, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return NotFound();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var body = await response.Content.ReadAsStringAsync(ct);
            return JObject.Parse(body);
        }
    }
}
