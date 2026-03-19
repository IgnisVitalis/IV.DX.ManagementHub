using IV.ManagementHub.ApiService.Bootstrap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace IV.ManagementHub.ApiService.Controllers
{
    [ApiController]
    [Route("api/setup")]
    public sealed class SetupController(IBootstrapSetupService bootstrapSetupService) : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet("status")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<SetupStatusResponse>> GetStatus(CancellationToken ct)
        {
            var status = await bootstrapSetupService.GetStatusAsync(ct);
            return Ok(new SetupStatusResponse
            {
                RequiresSetup = status.RequiresSetup,
                RequiresRestart = status.RequiresRestart,
                RuntimeReady = status.RuntimeReady,
                HasInstances = status.HasInstances
            });
        }

        [AllowAnonymous]
        [HttpPost("complete")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SetupCompleteResponse>> Complete([FromBody] SetupCompleteApiRequest request, CancellationToken ct)
        {
            var result = await bootstrapSetupService.CompleteSetupAsync(
                new BootstrapCompleteRequest(
                    request.UserName,
                    request.Password),
                ct);

            return result.Status switch
            {
                BootstrapCompleteStatus.ValidationError => BadRequest(new SetupCompleteResponse
                {
                    RequiresRestart = result.RequiresRestart,
                    Message = result.Message ?? "Validation failed."
                }),
                BootstrapCompleteStatus.AlreadyConfigured => Ok(new SetupCompleteResponse
                {
                    RequiresRestart = result.RequiresRestart,
                    Message = result.Message ?? "Setup already completed."
                }),
                _ => Ok(new SetupCompleteResponse
                {
                    RequiresRestart = result.RequiresRestart,
                    Message = result.Message ?? "Setup completed."
                })
            };
        }
    }

    public sealed class SetupStatusResponse
    {
        [JsonProperty("requiresSetup")]
        public bool RequiresSetup { get; init; }

        [JsonProperty("requiresRestart")]
        public bool RequiresRestart { get; init; }

        [JsonProperty("runtimeReady")]
        public bool RuntimeReady { get; init; }

        [JsonProperty("hasInstances")]
        public bool HasInstances { get; init; }
    }

    public sealed class SetupCompleteApiRequest
    {
        [JsonProperty("userName")]
        public string UserName { get; init; } = string.Empty;

        [JsonProperty("password")]
        public string Password { get; init; } = string.Empty;
    }

    public sealed class SetupCompleteResponse
    {
        [JsonProperty("requiresRestart")]
        public bool RequiresRestart { get; init; }

        [JsonProperty("message")]
        public string Message { get; init; } = string.Empty;
    }
}
