namespace IV.ManagementHub.Web.Services
{
    public sealed class AppAuthState
    {
        public event Action? Changed;

        public string? AccessToken { get; private set; }

        public string? UserName { get; private set; }

        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

        public void SetAccessToken(string accessToken, string? userName = null)
        {
            AccessToken = accessToken;
            UserName = userName;
            Changed?.Invoke();
        }

        public void Clear()
        {
            AccessToken = null;
            UserName = null;
            Changed?.Invoke();
        }
    }
}
