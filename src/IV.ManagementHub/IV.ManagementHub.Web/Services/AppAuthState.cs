namespace IV.ManagementHub.Web.Services
{
    public sealed class AppAuthState
    {
        public event Action? Changed;

        public string? AccessToken { get; private set; }

        public string? UserName { get; private set; }

        public string? AppKey { get; private set; }

        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

        public void SetAccessToken(string accessToken, string? userName = null, string? appKey = null)
        {
            AccessToken = accessToken;
            UserName = userName;
            AppKey = appKey;
            Changed?.Invoke();
        }

        public void SetAppKey(string appKey)
        {
            if (string.IsNullOrWhiteSpace(appKey))
            {
                return;
            }

            AppKey = appKey.Trim();
            Changed?.Invoke();
        }

        public void Clear()
        {
            AccessToken = null;
            UserName = null;
            AppKey = null;
            Changed?.Invoke();
        }
    }
}
