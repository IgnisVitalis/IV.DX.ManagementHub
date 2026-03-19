using IV.ManagementHub.ApiService.Bootstrap;
using IV.ManagementHub.ApiService.Controllers;
using IV.ManagementHub.ApiService.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace IV.ManagementHub.ApiService.Controllers.v1
{
    [ApiController]
    [Route("api/instances")]
    public sealed class InstancesController(
        IBootstrapInstanceService instanceService) : DXApiControllerBase
    {
        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<InstanceResponse>>> GetAll(CancellationToken ct)
        {
            var instances = await instanceService.GetInstancesAsync(ct);
            return Ok(instances.Select(instance => new InstanceResponse
            {
                Key = instance.Key,
                Title = instance.Title,
                ApiUrl = instance.ApiUrl,
                CreatedAtUtc = instance.CreatedAtUtc
            }).ToList());
        }

        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<CreateInstanceResponse>> Create([FromBody] CreateInstanceRequest request, CancellationToken ct)
        {
            var result = await instanceService.CreateInstanceAsync(
                new BootstrapCreateInstanceRequest(
                    request.Key,
                    request.Title,
                    request.ApiUrl,
                    request.ServiceKey),
                ct);

            var response = new CreateInstanceResponse
            {
                Message = result.Message ?? string.Empty,
                Instance = result.Instance is null
                    ? null
                    : new InstanceResponse
                    {
                        Key = result.Instance.Key,
                        Title = result.Instance.Title,
                        ApiUrl = result.Instance.ApiUrl,
                        CreatedAtUtc = result.Instance.CreatedAtUtc
                    }
            };

            return result.Status switch
            {
                BootstrapCreateInstanceStatus.Created => StatusCode(StatusCodes.Status201Created, response),
                BootstrapCreateInstanceStatus.SetupNotCompleted => BadRequest(response),
                BootstrapCreateInstanceStatus.ValidationError => BadRequest(response),
                BootstrapCreateInstanceStatus.Conflict => Conflict(response),
                _ => BadRequest(response)
            };
        }

    }

    public sealed class CreateInstanceRequest
    {
        [JsonProperty("key")]
        public string Key { get; init; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; init; } = string.Empty;

        [JsonProperty("apiUrl")]
        public string ApiUrl { get; init; } = string.Empty;

        [JsonProperty("serviceKey")]
        public string ServiceKey { get; init; } = string.Empty;
    }

    public sealed class CreateInstanceResponse
    {
        [JsonProperty("message")]
        public string Message { get; init; } = string.Empty;

        [JsonProperty("instance")]
        public InstanceResponse? Instance { get; init; }
    }

    public sealed class InstanceResponse
    {
        [JsonProperty("key")]
        public string Key { get; init; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; init; } = string.Empty;

        [JsonProperty("apiUrl")]
        public string ApiUrl { get; init; } = string.Empty;

        [JsonProperty("createdAtUtc")]
        public DateTimeOffset CreatedAtUtc { get; init; }
    }
}
