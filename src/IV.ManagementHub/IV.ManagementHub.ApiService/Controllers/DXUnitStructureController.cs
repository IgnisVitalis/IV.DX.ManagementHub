using IV.ManagementHub.ApiService.Controllers;
using IV.ManagementHub.ApiService.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net;

namespace IV.ManagementHub.ApiService.Controllers
{
    [ApiController]
    [Route("api/DXUnitStructure")]
    public class DXUnitStructureController(InstanceApiClientFactory clientFactory) : DXApiControllerBase
    {
        /// <summary>Get DX unit structure definition by type name.</summary>
        [HttpGet("{name}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JObject>> GetByName([FromRoute] string name)
        {
            var ct = HttpContext.RequestAborted;
            var client = await clientFactory.CreateFromContextAsync(ct);

            using var response = await client.GetAsync($"api/management/unit-structure/{name}", ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return NotFound();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var body = await response.Content.ReadAsStringAsync(ct);
            return JObject.Parse(body);
        }
    }
}
