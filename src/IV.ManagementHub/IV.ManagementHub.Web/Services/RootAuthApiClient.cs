using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace IV.ManagementHub.Web.Services
{
    public sealed class RootAuthApiClient(IHttpClientFactory factory)
    {
        public async Task<LoginResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return LoginResult.Fail("Username and password are required.");
            }

            var client = factory.CreateClient("Base");
            var payload = JsonConvert.SerializeObject(new
            {
                username,
                password
            });

            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("api/v1.0/auth/token", content, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return LoginResult.Fail("Invalid username or password.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return LoginResult.Fail($"Authentication failed ({(int)response.StatusCode}).");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseBody);

            if (string.IsNullOrWhiteSpace(tokenResponse?.AccessToken))
            {
                return LoginResult.Fail("Authentication succeeded but no access token was returned.");
            }

            return LoginResult.Success(tokenResponse.AccessToken);
        }

        private sealed class TokenResponse
        {
            [JsonProperty("access_token")]
            public string? AccessToken { get; init; }
        }
    }

    public sealed record LoginResult(bool IsSuccess, string? AccessToken, string? Error)
    {
        public static LoginResult Success(string accessToken) => new(true, accessToken, null);

        public static LoginResult Fail(string error) => new(false, null, error);
    }
}
