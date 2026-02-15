using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace IV.ManagementHub.Web.Services
{
    public sealed class RootAuthApiClient(IHttpClientFactory factory)
    {
        public async Task<SetupStatusResult> GetSetupStatusAsync(CancellationToken cancellationToken = default)
        {
            var client = factory.CreateClient("Base");
            using var response = await client.GetAsync("api/v1.0/setup/status", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new SetupStatusResult(
                    RequiresSetup: true,
                    RequiresRestart: false,
                    RuntimeReady: false,
                    Error: $"Unable to read setup status ({(int)response.StatusCode}).");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var payload = JsonConvert.DeserializeObject<SetupStatusResponse>(responseBody);

            if (payload is null)
            {
                return new SetupStatusResult(
                    RequiresSetup: true,
                    RequiresRestart: false,
                    RuntimeReady: false,
                    Error: "Setup status payload is empty.");
            }

            return new SetupStatusResult(
                RequiresSetup: payload.RequiresSetup,
                RequiresRestart: payload.RequiresRestart,
                RuntimeReady: payload.RuntimeReady);
        }

        public async Task<SetupCompleteResult> CompleteSetupAsync(
            SetupCompleteRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var client = factory.CreateClient("Base");
            var payload = JsonConvert.SerializeObject(new
            {
                request.DatabaseType,
                request.ConnectionString,
                request.UserName,
                request.Password
            });

            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("api/v1.0/setup/complete", content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var completeResponse = JsonConvert.DeserializeObject<SetupCompleteResponse>(responseBody);

            if (response.IsSuccessStatusCode)
            {
                return SetupCompleteResult.Success(
                    completeResponse?.RequiresRestart ?? false,
                    completeResponse?.Message ?? "Setup completed.");
            }

            var error = completeResponse?.Message ?? $"Setup failed ({(int)response.StatusCode}).";
            return SetupCompleteResult.Fail(error, completeResponse?.RequiresRestart ?? false);
        }

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

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                var message = await ReadErrorMessageAsync(response, cancellationToken);
                return LoginResult.Fail(message ?? "Service is not ready. Complete setup and restart API service.");
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

        private static async Task<string?> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
        {
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return null;
            }

            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseBody);
            return string.IsNullOrWhiteSpace(errorResponse?.Error) ? null : errorResponse.Error;
        }

        private sealed class TokenResponse
        {
            [JsonProperty("access_token")]
            public string? AccessToken { get; init; }
        }

        private sealed class SetupStatusResponse
        {
            [JsonProperty("requiresSetup")]
            public bool RequiresSetup { get; init; }

            [JsonProperty("requiresRestart")]
            public bool RequiresRestart { get; init; }

            [JsonProperty("runtimeReady")]
            public bool RuntimeReady { get; init; }
        }

        private sealed class SetupCompleteResponse
        {
            [JsonProperty("requiresRestart")]
            public bool RequiresRestart { get; init; }

            [JsonProperty("message")]
            public string? Message { get; init; }
        }

        private sealed class ErrorResponse
        {
            [JsonProperty("error")]
            public string? Error { get; init; }
        }
    }

    public sealed record LoginResult(bool IsSuccess, string? AccessToken, string? Error)
    {
        public static LoginResult Success(string accessToken) => new(true, accessToken, null);

        public static LoginResult Fail(string error) => new(false, null, error);
    }

    public sealed record SetupStatusResult(
        bool RequiresSetup,
        bool RequiresRestart,
        bool RuntimeReady,
        string? Error = null);

    public sealed record SetupCompleteRequest(
        string DatabaseType,
        string ConnectionString,
        string UserName,
        string Password);

    public sealed record SetupCompleteResult(bool IsSuccess, bool RequiresRestart, string? Message, string? Error)
    {
        public static SetupCompleteResult Success(bool requiresRestart, string message) =>
            new(true, requiresRestart, message, null);

        public static SetupCompleteResult Fail(string error, bool requiresRestart) =>
            new(false, requiresRestart, null, error);
    }
}
