using IV.DX.ManagementHub.ApiService.Controllers;
using IV.DX.ManagementHub.ApiService.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;

namespace IV.DataProvider.WebApp.Services.ApiService.Controllers.v1
{
    [ApiController]
    [Route("api/{definitionId:guid}")]
    public class DXByDefinitionController(InstanceApiClientFactory clientFactory) : DXApiControllerBase
    {
        /// <summary>Get all items by unit definition Id.</summary>
        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<JObject>> GetAllAsync([FromRoute] Guid definitionId, [FromQuery] string? filter = null)
        {
            var ct = HttpContext.RequestAborted;
            var client = await clientFactory.CreateFromContextAsync(ct);

            var url = string.IsNullOrEmpty(filter)
                ? $"api/management/{definitionId}"
                : $"api/management/{definitionId}?filter={Uri.EscapeDataString(filter)}";

            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var body = await response.Content.ReadAsStringAsync(ct);
            return JObject.Parse(body);
        }

        /// <summary>Get item by unit definition Id and item Id.</summary>
        [HttpGet("{id:guid}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JObject>> GetByIdAsync([FromRoute] Guid definitionId, [FromRoute] Guid id)
        {
            var ct = HttpContext.RequestAborted;
            var client = await clientFactory.CreateFromContextAsync(ct);

            using var response = await client.GetAsync($"api/management/{definitionId}/{id}", ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return NotFound();
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var body = await response.Content.ReadAsStringAsync(ct);
            return JObject.Parse(body);
        }

        /// <summary>Get items by unit definition Id and multiple item IDs.</summary>
        [HttpPost("by-ids")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<JArray>> GetByIdsAsync([FromRoute] Guid definitionId, [FromBody] Guid[] ids)
        {
            if (ids is null || ids.Length == 0)
                return new JArray();

            var ct = HttpContext.RequestAborted;
            var client = await clientFactory.CreateFromContextAsync(ct);

            using var content = new StringContent(JsonConvert.SerializeObject(ids), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync($"api/management/{definitionId}/by-ids", content, ct);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var body = await response.Content.ReadAsStringAsync(ct);
            return JArray.Parse(body);
        }

        /// <summary>Delete item by unit definition Id and item Id.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteAsync([FromRoute] Guid definitionId, [FromRoute] Guid id)
        {
            var ct = HttpContext.RequestAborted;
            var client = await clientFactory.CreateFromContextAsync(ct);

            using var response = await client.DeleteAsync($"api/management/{definitionId}/{id}", ct);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            return NoContent();
        }
    }
}
