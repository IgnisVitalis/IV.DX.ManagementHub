using Asp.Versioning;
using IV.ManagementHub.ApiService.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace IV.ManagementHub.ApiService.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/auth")]
    public sealed class AuthController(RootAuthOptions options, RootTokenService tokenService) : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("token")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<TokenResponse> IssueToken([FromBody] TokenRequest request)
        {
            if (!string.Equals(request.Username, options.Username, StringComparison.Ordinal) ||
                !string.Equals(request.Password, options.Password, StringComparison.Ordinal))
            {
                return Unauthorized();
            }

            var token = tokenService.CreateAccessToken();

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
