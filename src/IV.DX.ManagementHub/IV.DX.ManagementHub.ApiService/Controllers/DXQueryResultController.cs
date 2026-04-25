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
        /// <summary>Get DX query result by query Id.</summary>
        [HttpGet("{dxQueryId:guid}/{dxFilterId:guid?}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JObject>> Get([FromRoute] Guid dxQueryId, [FromRoute] Guid? dxFilterId)
        {
            var ct = HttpContext.RequestAborted;
            var client = await clientFactory.CreateFromContextAsync(ct);

            var url = dxFilterId.HasValue
                ? $"api/management/query-result/{dxQueryId}/{dxFilterId.Value}"
                : $"api/management/query-result/{dxQueryId}";
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
