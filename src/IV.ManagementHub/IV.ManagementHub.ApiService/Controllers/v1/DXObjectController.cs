using Asp.Versioning;
using IV.DX.Application.Contracts.Abstractions;
using IV.DX.Kernel.Models;
using IV.ManagementHub.ApiService.Controllers.v1;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace IV.DataProvider.WebApp.Services.ApiService.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/{typeName}")]
    //[ExcludeTypeName("DXUnitDefinitionUnit")]
    public class DXObjectController : ControllerBase
    {
        private readonly IDXUnitDataService _dataService;

        public DXObjectController(IDXUnitDataService dataService, ILogger<DXObjectController> logger)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        }

        /// <summary>Get all objects of the specified type.</summary>
        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async virtual Task<ActionResult<JArray>> GetAllAsync([FromRoute] string typeName, [FromQuery] string? filter = null)
        {
            var items = string.IsNullOrEmpty(filter)
                ? await _dataService.GetItemsAsync(typeName)
                : await _dataService.GetItemsAsync(typeName, filter);
            
            var jarray = new JArray(items);

            return jarray;
        }

        /// <summary>Search using long filter (POST, JSON).</summary>
        [HttpPost("search")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async virtual Task<ActionResult<JArray>> SearchAsync([FromRoute] string typeName, [FromBody] string body)
        {
            var items = await _dataService.GetItemsAsync(typeName, body);

            var jarray = new JArray(items);

            return jarray;
        }

        /// <summary>Get object of the specified type by ID.</summary>
        [HttpGet("{id:guid}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async virtual Task<ActionResult<JObject>> GetByIdAsync([FromRoute] string typeName, [FromRoute] Guid id)
        {
            var item = await _dataService.GetItemAsync(typeName, id);

            return item is null ? NotFound() : item;
        }

        /// <summary>Create or update an object of the specified type.</summary>
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async virtual Task<ActionResult<JObject>> CreateOrUpdateAsync([FromRoute] string typeName, [FromBody] JObject body)
        {
            var actualItem = await _dataService.InsertOrUpdateAsync(body);

            return actualItem;
        }

        /// <summary>Remove an object of the specified type by ID.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async virtual Task<IActionResult> DeleteAsync([FromRoute] string typeName, [FromRoute] Guid id)
        {
            var block = new DXDataBlock<DXUnitRecord>
            {
                Meta = new DXMeta
                {
                    Kind = "DXUnit",
                    Type = typeName,
                    Op = "Delete",
                    IsMulti = true,
                    IsRequired = false
                },
                Data = new DXData<DXUnitRecord>
                {
                    Delete = new List<DXDeleteRef>
                    {
                        new DXDeleteRef { ID = id }
                    }
                }
            };

            await _dataService.DeleteAsync(JObject.FromObject(block));

            return NoContent();
        }
    }
}
