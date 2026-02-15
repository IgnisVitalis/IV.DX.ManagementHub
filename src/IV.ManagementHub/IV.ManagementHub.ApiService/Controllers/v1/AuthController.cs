using Asp.Versioning;
using IV.ManagementHub.ApiService.Bootstrap;
using IV.ManagementHub.ApiService.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace IV.ManagementHub.ApiService.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/auth")]
    public sealed class AuthController(
        IBootstrapSetupService bootstrapSetupService,
        RootTokenService tokenService) : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("token")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<TokenResponse>> IssueToken([FromBody] TokenRequest request, CancellationToken ct)
        {
            var validation = await bootstrapSetupService.ValidateCredentialsAsync(request.Username, request.Password, ct);
            if (validation.Status is BootstrapAuthValidationStatus.SetupNotCompleted or BootstrapAuthValidationStatus.RuntimeNotReady)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    error = validation.Message ?? "Service setup is not completed."
                });
            }

            if (validation.Status != BootstrapAuthValidationStatus.Valid || string.IsNullOrWhiteSpace(validation.UserName))
            {
                return Unauthorized();
            }

            var token = tokenService.CreateAccessToken(validation.UserName);

            return Ok(new TokenResponse
            {
                AccessToken = token.Token,
                TokenType = "Bearer",
                ExpiresIn = token.ExpiresInSeconds
            });
        }
    }

    public sealed class TokenRequest
    {
        [JsonProperty("username")]
        public string Username { get; init; } = string.Empty;

        [JsonProperty("password")]
        public string Password { get; init; } = string.Empty;
    }

    public sealed class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonProperty("token_type")]
        public string TokenType { get; init; } = string.Empty;

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; init; }
    }
}
