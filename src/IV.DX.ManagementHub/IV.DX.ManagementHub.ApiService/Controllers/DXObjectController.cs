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
    [Route("api/{typeName}")]
    public class DXObjectController(InstanceApiClientFactory clientFactory) : DXApiControllerBase
    {
        /// <summary>Get all objects of the specified type.</summary>
        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<JObject>> GetAllAsync([FromRoute] string typeName, [FromQuery] string? filter = null)
        {
            var ct = HttpContext.RequestAborted;
            var client = await clientFactory.CreateFromContextAsync(ct);

            var url = string.IsNullOrEmpty(filter)
                ? $"api/management/{typeName}"
                : $"api/management/{typeName}?filter={Uri.EscapeDataString(filter)}";

            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var body = await response.Content.ReadAsStringAsync(ct);
            return JObject.Parse(body);
        }

        /// <summary>Search using long filter (POST, JSON).</summary>
        [HttpPost("search")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<JObject>> SearchAsync([FromRoute] string typeName, [FromBody] string filter)
        {
            var ct = HttpContext.RequestAborted;
            var client = await clientFactory.CreateFromContextAsync(ct);

            using var content = new StringContent(JsonConvert.SerializeObject(filter), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync($"api/management/{typeName}/search", content, ct);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var body = await response.Content.ReadAsStringAsync(ct);
            return JObject.Parse(body);
        }

        /// <summary>Get object of the specified type by Id.</summary>
        [HttpGet("{id:guid}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JObject>> GetByIdAsync([FromRoute] string typeName, [FromRoute] Guid id)
        {
            var ct = HttpContext.RequestAborted;
            var client = await clientFactory.CreateFromContextAsync(ct);

            using var response = await client.GetAsync($"api/management/{typeName}/{id}", ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return NotFound();
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var body = await response.Content.ReadAsStringAsync(ct);
            return JObject.Parse(body);
        }

        /// <summary>Get multiple objects of the specified type by IDs.</summary>
        [HttpPost("by-ids")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<JObject>> GetByIdsAsync([FromRoute] string typeName, [FromBody] Guid[] ids)
        {
            if (ids is null || ids.Length == 0)
                return new JObject();

            var ct = HttpContext.RequestAborted;
            var client = await clientFactory.CreateFromContextAsync(ct);

            using var content = new StringContent(JsonConvert.SerializeObject(ids), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync($"api/management/{typeName}/by-ids", content, ct);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var body = await response.Content.ReadAsStringAsync(ct);
            return JObject.Parse(body);
        }

        /// <summary>Create or update an object of the specified type.</summary>
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<JObject>> CreateOrUpdateAsync([FromRoute] string typeName, [FromBody] JObject body)
        {
            var ct = HttpContext.RequestAborted;
            var client = await clientFactory.CreateFromContextAsync(ct);

            using var content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync($"api/management/{typeName}", content, ct);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            return JObject.Parse(responseBody);
        }

        /// <summary>Update an existing object of the specified type by Id.</summary>
        [HttpPut("{id:guid}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<JObject>> UpdateAsync([FromRoute] string typeName, [FromRoute] Guid id, [FromBody] JObject body)
        {
            var ct = HttpContext.RequestAborted;
            var client = await clientFactory.CreateFromContextAsync(ct);

            using var content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync($"api/management/{typeName}", content, ct);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            return JObject.Parse(responseBody);
        }

        /// <summary>Remove an object of the specified type by Id.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteAsync([FromRoute] string typeName, [FromRoute] Guid id)
        {
            var ct = HttpContext.RequestAborted;
            var client = await clientFactory.CreateFromContextAsync(ct);

            using var response = await client.DeleteAsync($"api/management/{typeName}/{id}", ct);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            return NoContent();
        }
    }
}
