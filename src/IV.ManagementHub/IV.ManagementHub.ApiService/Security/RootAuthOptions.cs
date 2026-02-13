namespace IV.ManagementHub.ApiService.Security
{
    public sealed class RootAuthOptions
    {
        public const string SectionName = "Authentication:Root";

        public string Issuer { get; init; } = "IV.ManagementHub.ApiService";

        public string Audience { get; init; } = "IV.ManagementHub.Clients";

        public string SigningKey { get; init; } = "temporary-hardcoded-signing-key-change-before-prod";

        public string Username { get; init; } = "root";

        public string Password { get; init; } = "root";

        public string UserId { get; init; } = "root";

        public int AccessTokenMinutes { get; init; } = 15;
    }
}
