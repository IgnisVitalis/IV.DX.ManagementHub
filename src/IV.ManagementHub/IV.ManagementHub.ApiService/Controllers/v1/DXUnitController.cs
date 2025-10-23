using Asp.Versioning;
using IV.DX.Application.Contracts.Abstractions;
using IV.DX.Kernel.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace IV.DataProvider.WebApp.Services.ApiService.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/DXUnitDefinitionUnit")]
    public class DXUnitController : ControllerBase
    {
        private readonly IDXUnitDataService _dataService;

        public DXUnitController(IDXUnitDataService dataService, ILogger<DXObjectController> logger)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        }

        /// <summary>Get all DXUnitDefinitionUnit objects.</summary>
        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<JArray>> GetAllAsync([FromQuery] string? filter = null)
        {
            var items = string.IsNullOrEmpty(filter)
                ? await _dataService.GetItemsAsync<DXUnitDefinitionUnit>()
                : await _dataService.GetItemsAsync<DXUnitDefinitionUnit>(filter);

            var jarray = new JArray(items.Select(x => x.ToJObject()));

            return jarray;
        }

        /// <summary>Search using long filter (POST, JSON).</summary>
        [HttpPost("search")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<JArray>> SearchAsync([FromBody] JToken body)
        {
            var items = await _dataService.GetItemsAsync<DXUnitDefinitionUnit>(body.ToString());

            var jarray = new JArray(items.Select(x => x.ToJObject()));

            return jarray;
        }

        /// <summary>Get DXUnitDefinitionUnit object by ID.</summary>
        [HttpGet("{id:guid}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JObject>> GetByIdAsync([FromRoute] Guid id)
        {
            var result = await _dataService.GetItemAsync<DXUnitDefinitionUnit>(id);

            var jObject = result?.ToJObject();

            return jObject is null ? NotFound() : jObject;
        }

        /// <summary>Create or update an object of the specified type.</summary>
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<JObject>> CreateOrUpdateAsync([FromBody] JObject body)
        {
            var item = await _dataService.InsertOrUpdateAsync(body);

            //Guid id = item.Value<Guid>("ID");

            //var typeName = "DXUnitDefinitionUnit";

            //var version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

            //return CreatedAtAction(
            //    nameof(GetByIdAsync),
            //    new { version, typeName, id },
            //    item);

            return item;
        }
    }
}
